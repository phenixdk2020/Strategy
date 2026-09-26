"""
PROJECT 1864 — painted campaign map of the Danish monarchy, 1851.

Builds from Natural Earth 10m (public domain):
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Color.png    painted colour map (sRGB)
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Height.png   16-bit height (0 = sea floor visual, land > sea level)
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Regions.png  region ids (R channel), see REGION_*
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Map.json     projection + cities + labels for Unity

Usage: python build_map.py <natural-earth-dir> [--preview]
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage
from scipy.spatial import cKDTree

from shp import read

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.abspath(os.path.join(HERE, "..", ".."))
OUT_DIR = os.path.join(PROJECT, "Assets", "Campaign1851", "Resources", "Map1851")

# Map extent (WGS84) and projection: equirectangular, x scaled by cos(LAT0).
LON_MIN, LON_MAX = 7.6, 13.6
LAT_MIN, LAT_MAX = 53.25, 57.85
LAT0 = (LAT_MIN + LAT_MAX) / 2
KX = math.cos(math.radians(LAT0))
KM_PER_DEG_LAT = 110.57
# Override with --height N (e.g. 2048 when memory is tight). Full quality is 4096.
HEIGHT_PX = int(next((a.split('=')[1] for a in __import__('sys').argv if a.startswith('--height=')), 4096))
# Multiple of 4 so the GPU block compression can use it without padding.
WIDTH_PX = int(round(HEIGHT_PX * (LON_MAX - LON_MIN) * KX / (LAT_MAX - LAT_MIN) / 4)) * 4

# Size of one repeat of the close-zoom field detail texture.
DETAIL_TILE_KM = 2.5

# Bornholm is drawn as an inset in Unity; it gets its own small texture.
BORNHOLM = (14.60, 15.25, 54.95, 55.35)

REGION_SEA, REGION_KINGDOM, REGION_SCHLESWIG, REGION_HOLSTEIN, REGION_FOREIGN = 0, 1, 2, 3, 4

# Historical boundaries (approximate, WGS84 lon/lat polylines).
# Kongeå: Kingdom / Duchy of Schleswig on the Jutland mainland.
KONGEAA = [(8.55, 55.40), (8.80, 55.44), (9.00, 55.47), (9.20, 55.47), (9.35, 55.48), (9.48, 55.49), (9.80, 55.49)]
# Eider / Levensau: Schleswig / Holstein. Fehmarn (north of the eastern extension) belonged to Schleswig.
EIDER = [(8.40, 54.30), (8.90, 54.29), (9.10, 54.32), (9.40, 54.31), (9.67, 54.30), (9.90, 54.35),
         (10.05, 54.37), (10.18, 54.40), (10.60, 54.42), (11.60, 54.42)]


# 20th-century causeways present in Natural Earth that did not exist in 1851 (removed by opening).
ANACHRONISMS = [
    (8.58, 8.75, 55.09, 55.16),  # Rømødæmningen (1948)
    (8.38, 8.68, 54.86, 54.93),  # Hindenburgdamm, Sylt (1927)
]

# Major historic forests, painted on top of the procedural woodland: (lon, lat, radius km).
FORESTS = [
    (9.83, 56.80, 7.0),   # Rold Skov
    (9.55, 56.13, 9.0),   # Silkeborgskovene
    (12.30, 56.00, 6.0),  # Gribskov
    (11.95, 55.30, 4.0),  # Sydsjællands skove
    (10.55, 55.25, 4.0),  # Sydøstfyn
    (9.95, 54.45, 6.0),   # Dänischer Wohld
    (10.60, 53.65, 8.0),  # Lauenburgische wälder / Sachsenwald
    (14.93, 55.14, 5.0),  # Almindingen
]


def boundary_lat(line, lon):
    xs = np.array([p[0] for p in line])
    ys = np.array([p[1] for p in line])
    return np.interp(lon, xs, ys)


class Raster:
    def __init__(self, lon_min, lon_max, lat_min, lat_max, width, height):
        self.lon_min, self.lon_max, self.lat_min, self.lat_max = lon_min, lon_max, lat_min, lat_max
        self.w, self.h = width, height

    def to_px(self, lon, lat):
        x = (lon - self.lon_min) / (self.lon_max - self.lon_min) * self.w
        y = (self.lat_max - lat) / (self.lat_max - self.lat_min) * self.h
        return x, y

    def grids(self):
        lon = self.lon_min + (np.arange(self.w) + 0.5) / self.w * (self.lon_max - self.lon_min)
        lat = self.lat_max - (np.arange(self.h) + 0.5) / self.h * (self.lat_max - self.lat_min)
        lon_g, lat_g = np.meshgrid(lon.astype(np.float32), lat.astype(np.float32))
        return lon_g, lat_g

    def fill(self, shapes):
        """Rasterises polygon shapes (outer rings clockwise, holes counter-clockwise)."""
        img = Image.new("L", (self.w, self.h), 0)
        draw = ImageDraw.Draw(img)
        bounds = (self.lon_min - 1, self.lon_max + 1, self.lat_min - 1, self.lat_max + 1)
        for parts in shapes:
            for ring in parts:
                if len(ring) < 3:
                    continue
                lons = [p[0] for p in ring]
                lats = [p[1] for p in ring]
                if max(lons) < bounds[0] or min(lons) > bounds[1] or max(lats) < bounds[2] or min(lats) > bounds[3]:
                    continue
                area = sum(ring[i][0] * ring[i - 1][1] - ring[i - 1][0] * ring[i][1] for i in range(len(ring)))
                hole = area < 0  # counter-clockwise in lon/lat = hole in shapefile convention
                draw.polygon([self.to_px(x, y) for x, y in ring], fill=0 if hole else 255)
        return np.asarray(img) > 127


def fbm(shape, scale, octaves, seed, persistence=0.5):
    """Smooth fractal noise in [0, 1] built from upsampled random grids."""
    rng = np.random.default_rng(seed)
    h, w = shape
    total = np.zeros(shape, np.float32)
    amp, norm = 1.0, 0.0
    for o in range(octaves):
        cells = max(2, int(scale * (2 ** o)))
        gh, gw = cells + 2, max(2, int(cells * w / h)) + 2
        grid = rng.random((gh, gw)).astype(np.float32)
        layer = np.asarray(Image.fromarray(grid).resize((w, h), Image.BICUBIC))
        total += layer * amp
        norm += amp
        amp *= persistence
    total /= norm
    return (total - total.min()) / (total.max() - total.min() + 1e-6)


def worley_cells(shape, spacing_px, seed):
    """Per-pixel random value of the nearest jittered cell centre (field patchwork)."""
    rng = np.random.default_rng(seed)
    h, w = shape
    n = int(h * w / (spacing_px ** 2))
    pts = np.column_stack([rng.random(n) * w, rng.random(n) * h])
    vals = rng.random(n).astype(np.float32)
    tree = cKDTree(pts)
    step = 1
    yy, xx = np.mgrid[0:h:step, 0:w:step]
    _, idx = tree.query(np.column_stack([xx.ravel(), yy.ravel()]), workers=-1)
    small = vals[idx].reshape(yy.shape)
    return np.asarray(Image.fromarray(small).resize((w, h), Image.NEAREST))


def detail_texture(size, cells, seed):
    """Tileable field patchwork for the shader's detail slot (0.5 grey = neutral under MulX2).
    Cells are fields, thin dark borders are hedgerows (levende hegn), plus fine grain."""
    rng = np.random.default_rng(seed)
    pts = rng.random((cells, 2))
    vals = rng.random(cells)
    tiled = np.concatenate([pts + [dx, dy] for dx in (-1, 0, 1) for dy in (-1, 0, 1)])
    tiled_vals = np.tile(vals, 9)
    tree = cKDTree(tiled)
    yy, xx = np.mgrid[0:size, 0:size] / size
    dist, idx = tree.query(np.column_stack([xx.ravel(), yy.ravel()]), k=2, workers=-1)
    field = tiled_vals[idx[:, 0]].reshape(size, size)
    border = (dist[:, 1] - dist[:, 0]).reshape(size, size) * size
    hedge = np.clip(1.0 - border / 2.2, 0, 1)
    grain = rng.random((size, size)) * 0.05
    v = 0.44 + 0.12 * field + grain - 0.16 * hedge
    # Slight warm tint on the lighter (grain) fields, cooler green on pasture.
    r = v * (1.0 + 0.05 * (field - 0.5))
    g = v
    b = v * (1.0 - 0.08 * (field - 0.5))
    return (np.clip(np.stack([r, g, b], axis=2), 0, 1) * 255).astype(np.uint8)


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a + (b - a) * t


def rgb(*c):
    return np.array(c, np.float32) / 255.0


def paint(r, land, regions, lon, lat, seed):
    shape = land.shape
    px_km = (r.lat_max - r.lat_min) * KM_PER_DEG_LAT / r.h

    # --- Distances (km) to the coast, on both sides.
    dist_sea = (ndimage.distance_transform_edt(~land) * px_km).astype(np.float32)   # sea pixels: distance to land
    dist_land = (ndimage.distance_transform_edt(land) * px_km).astype(np.float32)   # land pixels: distance to sea

    # --- Height: low coasts, rolling moraine in the east, flat outwash plains in the west (Jutland).
    hills = fbm(shape, 6, 6, seed + 1)
    ridges = 1.0 - np.abs(fbm(shape, 10, 4, seed + 2) * 2 - 1)
    eastness = np.clip((lon - 8.4) / 1.6, 0.0, 1.0)             # West Jutland is flat
    terrain = (0.55 * hills + 0.45 * ridges) * (0.35 + 0.65 * eastness)
    height = terrain * np.clip(dist_land / 6.0, 0.0, 1.0) ** 0.6  # coasts come down to sea level
    height = np.where(land, height, 0.0)

    # --- Hillshade (light from north-west). Shade the terrain only, not the coastal ramp,
    # otherwise every south-east facing shore gets a dark band.
    gy, gx = np.gradient(ndimage.gaussian_filter(terrain, 1.5))
    relief = 90.0
    nx, ny, nz = -gx * relief, gy * relief, np.ones_like(gx)
    nlen = np.sqrt(nx * nx + ny * ny + nz * nz)
    lx, ly, lz = -0.55, 0.55, 0.63
    shade = np.clip((nx * lx + ny * ly + nz * lz) / nlen, 0, 1)
    shade = 0.78 + 0.45 * (shade - 0.63)

    # --- Land colours.
    lush, olive, field, forest = rgb(84, 112, 48), rgb(120, 126, 62), rgb(160, 152, 86), rgb(38, 62, 32)
    heath, dune = rgb(112, 92, 72), rgb(206, 192, 150)

    broad = fbm(shape, 3, 4, seed + 3)
    colour = lerp(lush, olive, broad)

    # Field patchwork: each cell is grass, grain, fallow or ploughed; kept low-contrast.
    cells = worley_cells(shape, 5, seed + 4)
    grain, fallow, ploughed = rgb(168, 158, 84), rgb(112, 132, 60), rgb(122, 102, 70)
    patch = np.zeros(shape + (3,), np.float32) + colour
    patch = np.where((cells < 0.22)[..., None], grain, patch)
    patch = np.where(((cells >= 0.22) & (cells < 0.34))[..., None], ploughed, patch)
    patch = np.where(((cells >= 0.34) & (cells < 0.55))[..., None], fallow, patch)
    colour = lerp(colour, patch, np.full(shape, 0.32, np.float32))

    # Woodland: many small woods, more in the east, plus the major historic forests.
    km_x = (lon - LON_MIN) * KX * 111.32
    km_y = (lat - LAT_MIN) * KM_PER_DEG_LAT
    forest_noise = fbm(shape, 45, 3, seed + 5)
    forest_mask = np.clip((forest_noise - (0.70 - 0.06 * eastness)) * 10, 0, 1)
    edge_noise = fbm(shape, 90, 3, seed + 9)
    for f_lon, f_lat, radius in FORESTS:
        fx = (f_lon - LON_MIN) * KX * 111.32
        fy = (f_lat - LAT_MIN) * KM_PER_DEG_LAT
        d = np.sqrt((km_x - fx) ** 2 + (km_y - fy) ** 2) / radius
        # Ragged edges and clearings: the noise term dominates near the rim.
        forest_mask = np.maximum(forest_mask, np.clip((1.0 - d) * 2.2 + (edge_noise - 0.55) * 4.0, 0, 1))
    forest_mask *= land
    colour = lerp(colour, forest, forest_mask * 0.88)

    # West Jutland heath (1851: large heather moors between the coast dunes and the ridge).
    jutland_heath = np.clip((9.25 - lon) / 0.7, 0, 1) * np.clip((lat - 55.3) / 0.4, 0, 1) * np.clip((57.3 - lat) / 0.4, 0, 1)
    heath_noise = fbm(shape, 12, 4, seed + 6)
    heath_mask = np.clip(jutland_heath * 1.2 * (0.3 + heath_noise) - 0.25, 0, 1)
    colour = lerp(colour, heath, np.clip(heath_mask, 0, 0.7))

    # Sandy shore and dunes: wider on the exposed west coast.
    shore_km = 0.35 + 1.4 * np.clip((8.7 - lon) / 0.5, 0, 1)
    shore = np.clip(1 - dist_land / shore_km, 0, 1)
    colour = lerp(colour, dune, shore * 0.85)

    colour *= shade[..., None]

    # Foreign land: desaturated and darker so the monarchy reads first.
    foreign = regions == REGION_FOREIGN
    grey = colour.mean(axis=2, keepdims=True)
    muted = lerp(colour, np.repeat(grey, 3, axis=2), np.full(shape, 0.7, np.float32)) * 0.72 + rgb(18, 16, 10) * 0.5
    colour = np.where(foreign[..., None], muted, colour)

    # --- Sea: shallow teal along the coasts, deep navy offshore; darker shadow line at the shore.
    deep, mid, shallow = rgb(18, 38, 62), rgb(28, 58, 82), rgb(52, 92, 104)
    depth = np.clip(dist_sea / 10.0, 0, 1) ** 0.7
    sea = lerp(shallow, mid, np.clip(depth * 2, 0, 1))
    sea = lerp(sea, deep, np.clip(depth * 1.6 - 0.6, 0, 1))
    sea_noise = fbm(shape, 5, 4, seed + 7)
    sea *= (0.94 + 0.12 * sea_noise)[..., None]
    shore_shadow = np.exp(-dist_sea / 0.45)
    sea *= (1 - 0.28 * shore_shadow)[..., None]
    foam = np.exp(-((dist_sea - 0.25) / 0.18) ** 2) * 0.35
    sea = lerp(sea, rgb(190, 200, 190), foam)

    out = np.where(land[..., None], colour, sea)

    # --- Borders: monarchy/foreign (dark red), internal duchy borders (gold, dashed).
    def edge(mask_a, mask_b, width):
        grown = ndimage.binary_dilation(mask_a, iterations=width)
        return grown & mask_b

    monarchy = land & (regions != REGION_FOREIGN)
    outer = edge(foreign, monarchy, 2) | edge(monarchy, foreign, 1)
    dash = ((np.indices(shape).sum(axis=0) // 7) % 2) == 0
    inner = (edge(regions == REGION_KINGDOM, regions == REGION_SCHLESWIG, 1)
             | edge(regions == REGION_SCHLESWIG, regions == REGION_HOLSTEIN, 1)) & dash & land
    out = np.where(outer[..., None], lerp(out, rgb(120, 30, 24), np.full(shape, 0.85, np.float32)), out)
    out = np.where(inner[..., None], lerp(out, rgb(214, 180, 96), np.full(shape, 0.9, np.float32)), out)

    # Soft edge: the sheet fades into the deep-sea background colour used by the Unity camera,
    # so the map has no hard rectangular border when zoomed out.
    h, w = shape
    ex = np.minimum(np.arange(w), np.arange(w)[::-1]) / (w * 0.12)
    ey = np.minimum(np.arange(h), np.arange(h)[::-1]) / (h * 0.09)
    fade = np.clip(np.minimum(ex[None, :], ey[:, None]), 0, 1) ** 1.5
    edge_noise = fbm(shape, 20, 3, seed + 10)
    fade = np.clip(fade * (0.8 + 0.4 * edge_noise), 0, 1)
    # Target is darker than the camera background (18,33,51) because Unity's sun + ambient roughly
    # lifts the albedo ~20%; lit, this lands on the background colour.
    out = lerp(rgb(15, 28, 43), out, fade.astype(np.float32))

    # Painterly softening: tiny blur plus fine grain.
    img = Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8))
    img = img.filter(ImageFilter.GaussianBlur(0.6))
    grain = (fbm(shape, 180, 2, seed + 8) - 0.5) * 10
    arr = np.clip(np.asarray(img).astype(np.float32) + grain[..., None], 0, 255).astype(np.uint8)
    return arr, height


def classify(r, land, dk, sh, lon, lat):
    regions = np.where(land, REGION_FOREIGN, REGION_SEA).astype(np.uint8)
    south_of_kongeaa = lat < boundary_lat(KONGEAA, lon)
    south_of_eider = lat < boundary_lat(EIDER, lon)

    # Jutland mainland = the land component containing Viborg.
    labels, _ = ndimage.label(land)
    vx, vy = r.to_px(9.40, 56.45)
    if 0 <= vx < r.w and 0 <= vy < r.h:
        jutland = labels == labels[int(vy), int(vx)]
    else:
        jutland = np.zeros_like(land)

    def box(x0, x1, y0, y1):
        return (lon >= x0) & (lon <= x1) & (lat >= y0) & (lat <= y1)

    # Islands are classified whole, by the centre of their bounding box, so a box edge never cuts
    # an island (or a peninsula rasterised as its own piece) in two.
    island_boxes = [(9.55, 10.10, 54.84, 55.08),   # Als, Sundeved pieces
                    (10.17, 10.53, 54.80, 54.98),  # Ærø
                    (8.30, 8.72, 54.95, 55.23)]    # Rømø
    schleswig_islands = np.zeros_like(land)
    for index, slices in enumerate(ndimage.find_objects(labels), start=1):
        if slices is None:
            continue
        cy = (slices[0].start + slices[0].stop) / 2
        cx = (slices[1].start + slices[1].stop) / 2
        c_lon = r.lon_min + cx / r.w * (r.lon_max - r.lon_min)
        c_lat = r.lat_max - cy / r.h * (r.lat_max - r.lat_min)
        if any(x0 <= c_lon <= x1 and y0 <= c_lat <= y1 for x0, x1, y0, y1 in island_boxes):
            schleswig_islands[slices] |= labels[slices] == index
    dk_schleswig = dk & ((jutland & south_of_kongeaa) | (~jutland & schleswig_islands))

    kingdom = land & dk & ~dk_schleswig
    schleswig = land & (dk_schleswig | (sh & ~south_of_eider))
    holstein = land & sh & south_of_eider & ~schleswig

    regions[kingdom] = REGION_KINGDOM
    regions[schleswig] = REGION_SCHLESWIG
    regions[holstein] = REGION_HOLSTEIN
    return regions


def load(ne_dir):
    land = [s for s, _ in read(os.path.join(ne_dir, "ne_10m_land", "ne_10m_land"))]
    countries = read(os.path.join(ne_dir, "ne_10m_admin_0_countries", "ne_10m_admin_0_countries"))
    admin1 = read(os.path.join(ne_dir, "ne_10m_admin_1_states_provinces", "ne_10m_admin_1_states_provinces"))
    dk = [s for s, rec in countries if rec.get("ADM0_A3") == "DNK"]
    sh = [s for s, rec in admin1 if rec.get("iso_3166_2") == "DE-SH"]
    return land, dk, sh


def build(ne_dir, raster, seed):
    land_shapes, dk_shapes, sh_shapes = load(ne_dir)
    lon, lat = raster.grids()
    land = raster.fill(land_shapes)
    for x0, x1, y0, y1 in ANACHRONISMS:
        zone = (lon >= x0) & (lon <= x1) & (lat >= y0) & (lat <= y1)
        opened = ndimage.binary_opening(land, iterations=2)
        land = np.where(zone, opened, land)
    dk = raster.fill(dk_shapes)
    sh = raster.fill(sh_shapes)
    regions = classify(raster, land, dk, sh, lon, lat)
    colour, height = paint(raster, land, regions, lon, lat, seed)
    return colour, height, regions, land


def save_png16(path, arr01):
    Image.fromarray((np.clip(arr01, 0, 1) * 65535).astype(np.uint16)).save(path)


def main():
    ne_dir = sys.argv[1]
    preview = "--preview" in sys.argv
    os.makedirs(OUT_DIR, exist_ok=True)

    main_r = Raster(LON_MIN, LON_MAX, LAT_MIN, LAT_MAX, WIDTH_PX, HEIGHT_PX)
    colour, height, regions, land = build(ne_dir, main_r, 1864)

    bh_w = int(round(1024 * (BORNHOLM[1] - BORNHOLM[0]) * KX / (BORNHOLM[3] - BORNHOLM[2])))
    bh_r = Raster(BORNHOLM[0], BORNHOLM[1], BORNHOLM[2], BORNHOLM[3], bh_w, 1024)
    bh_colour, _, _, _ = build(ne_dir, bh_r, 1865)

    Image.fromarray(colour).save(os.path.join(OUT_DIR, "Denmark1851_Color.png"))
    save_png16(os.path.join(OUT_DIR, "Denmark1851_Height.png"), height / max(height.max(), 1e-6))
    Image.fromarray(regions * 60).save(os.path.join(OUT_DIR, "Denmark1851_Regions.png"))
    Image.fromarray(bh_colour).save(os.path.join(OUT_DIR, "Bornholm1851_Color.png"))

    # Close-zoom detail: tileable fields (one tile = DETAIL_TILE_KM) masked to land.
    Image.fromarray(detail_texture(1024, 160, 1866)).save(os.path.join(OUT_DIR, "Fields1851_Detail.png"))
    mask = np.zeros(land.shape + (4,), np.uint8)
    mask[..., 3] = (land & (regions != REGION_FOREIGN)) * 255
    mask[..., 3] = np.asarray(Image.fromarray(mask[..., 3]).filter(ImageFilter.GaussianBlur(1.5)))
    Image.fromarray(mask).resize((WIDTH_PX // 2, HEIGHT_PX // 2), Image.BILINEAR).save(os.path.join(OUT_DIR, "Denmark1851_DetailMask.png"))

    with open(os.path.join(HERE, "cities1850.json"), encoding="utf-8") as f:
        cities = json.load(f)

    width_km = (LON_MAX - LON_MIN) * KX * 111.32
    height_km = (LAT_MAX - LAT_MIN) * KM_PER_DEG_LAT
    meta = {
        "version": "v00.00.14",
        "extent": {"lonMin": LON_MIN, "lonMax": LON_MAX, "latMin": LAT_MIN, "latMax": LAT_MAX},
        "sizeKm": {"width": round(width_km, 2), "height": round(height_km, 2)},
        "textureSize": {"width": WIDTH_PX, "height": HEIGHT_PX},
        "detailTileKm": DETAIL_TILE_KM,
        "bornholm": {"lonMin": BORNHOLM[0], "lonMax": BORNHOLM[1], "latMin": BORNHOLM[2], "latMax": BORNHOLM[3]},
        "regions": {"0": "Hav", "1": "Kongeriget", "2": "Hertugdømmet Slesvig", "3": "Holsten og Lauenborg", "4": "Udland"},
        "cities": cities["cities"],
        "foreignCities": cities["foreign"],
        "labels": [
            {"text": "Skagerrak", "lat": 57.55, "lon": 8.35, "kind": "sea"},
            {"text": "Kattegat", "lat": 56.85, "lon": 11.45, "kind": "sea"},
            {"text": "Nordsøen", "lat": 55.75, "lon": 7.85, "kind": "sea"},
            {"text": "Østersøen", "lat": 54.55, "lon": 12.85, "kind": "sea"},
            {"text": "Lillebælt", "lat": 55.35, "lon": 9.78, "kind": "strait"},
            {"text": "Storebælt", "lat": 55.45, "lon": 10.98, "kind": "strait"},
            {"text": "Øresund", "lat": 55.95, "lon": 12.72, "kind": "strait"},
            {"text": "Limfjorden", "lat": 56.95, "lon": 9.25, "kind": "strait"},
            {"text": "Jylland", "lat": 56.20, "lon": 8.95, "kind": "land"},
            {"text": "Fyn", "lat": 55.28, "lon": 10.35, "kind": "land"},
            {"text": "Sjælland", "lat": 55.50, "lon": 11.75, "kind": "land"},
            {"text": "Lolland", "lat": 54.78, "lon": 11.35, "kind": "land"},
            {"text": "Falster", "lat": 54.80, "lon": 11.98, "kind": "land"},
            {"text": "Slesvig", "lat": 54.85, "lon": 9.05, "kind": "duchy"},
            {"text": "Holsten", "lat": 54.05, "lon": 10.15, "kind": "duchy"},
            {"text": "Lauenborg", "lat": 53.55, "lon": 10.62, "kind": "duchy"},
            {"text": "Skåne (Sverige)", "lat": 55.85, "lon": 13.35, "kind": "foreign"},
            {"text": "Hannover", "lat": 53.35, "lon": 9.2, "kind": "foreign"},
            {"text": "Mecklenburg", "lat": 53.75, "lon": 11.6, "kind": "foreign"}
        ]
    }
    with open(os.path.join(OUT_DIR, "Denmark1851_Map.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)

    if preview:
        Image.fromarray(colour).resize((WIDTH_PX // 4, HEIGHT_PX // 4), Image.LANCZOS).save(os.path.join(HERE, "preview.png"))
    print(f"map {WIDTH_PX}x{HEIGHT_PX} px, {width_km:.0f}x{height_km:.0f} km, {height_km / HEIGHT_PX * 1000:.0f} m/px")


if __name__ == "__main__":
    main()
