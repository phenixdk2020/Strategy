"""
PROJECT 1864 — painted campaign map of the Danish monarchy, 1851.

Builds from Natural Earth 10m (public domain):
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Color.png    painted colour map (sRGB)
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Height.png   16-bit height (0 = sea floor visual, land > sea level)
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Regions.png  region ids (R channel), see REGION_*
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Features.png land (R) + woodland (G) for 3D scenery placement
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Amter.png    amt id + 1 (R), see amter1851.json
  Assets/Campaign1851/Resources/Map1851/Fields1851_Parcels.png  tileable field parcels + hedgerows (Unreal close zoom)
  Assets/Campaign1851/Resources/Map1851/Denmark1851_Map.json     projection + cities + labels + roads

Projection: Lambert Azimuthal Equal-Area centred on 52N 10E (as EPSG:3035, on a sphere), so the
same map can grow from Denmark to Northern Europe or Europe by choosing a larger --region.

Usage: python build_map.py <natural-earth-dir> [--region=denmark1851] [--height=4096] [--out=DIR] [--parcels] [--preview]
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


def arg(name, default):
    return next((a.split("=", 1)[1] for a in sys.argv if a.startswith(f"--{name}=")), default)


OUT_DIR = arg("out", os.path.join(PROJECT, "Assets", "Campaign1851", "Resources", "Map1851"))

# ------------------------------------------------------------------ projection
# Lambert Azimuthal Equal-Area on a sphere, centred like EPSG:3035 (ETRS89-LAEA Europe).
PROJ_LAT0, PROJ_LON0, EARTH_R_KM = 52.0, 10.0, 6371.0088


def project(lon, lat):
    """lon/lat degrees -> x (east), y (north) in km. Works on scalars and numpy arrays."""
    lam, phi = np.radians(np.asarray(lon, np.float64) - PROJ_LON0), np.radians(np.asarray(lat, np.float64))
    phi0 = math.radians(PROJ_LAT0)
    k = np.sqrt(2.0 / (1.0 + math.sin(phi0) * np.sin(phi) + math.cos(phi0) * np.cos(phi) * np.cos(lam)))
    x = EARTH_R_KM * k * np.cos(phi) * np.sin(lam)
    y = EARTH_R_KM * k * (math.cos(phi0) * np.sin(phi) - math.sin(phi0) * np.cos(phi) * np.cos(lam))
    return x, y


def unproject(x, y):
    """x/y km -> lon, lat degrees (inverse LAEA)."""
    x, y = np.asarray(x, np.float64), np.asarray(y, np.float64)
    phi0 = math.radians(PROJ_LAT0)
    rho = np.maximum(np.hypot(x, y), 1e-9)
    c = 2.0 * np.arcsin(np.clip(rho / (2.0 * EARTH_R_KM), -1.0, 1.0))
    lat = np.degrees(np.arcsin(np.cos(c) * math.sin(phi0) + y * np.sin(c) * math.cos(phi0) / rho))
    lon = PROJ_LON0 + np.degrees(np.arctan2(x * np.sin(c), rho * math.cos(phi0) * np.cos(c) - y * math.sin(phi0) * np.sin(c)))
    return lon, lat


def box_extent(lon_min, lon_max, lat_min, lat_max):
    """Projected km bounding box of a lon/lat box (its edges curve in LAEA, so sample them)."""
    t = np.linspace(0.0, 1.0, 64)
    lons = np.concatenate([lon_min + (lon_max - lon_min) * t, np.full(64, lon_max), lon_max - (lon_max - lon_min) * t, np.full(64, lon_min)])
    lats = np.concatenate([np.full(64, lat_min), lat_min + (lat_max - lat_min) * t, np.full(64, lat_max), lat_max - (lat_max - lat_min) * t])
    x, y = project(lons, lats)
    return float(x.min()), float(x.max()), float(y.min()), float(y.max())


# Named map regions (lon/lat boxes). Add larger ones (e.g. northern_europe) as the campaign grows.
REGIONS = {
    "denmark1851": (7.6, 13.6, 53.25, 57.85),
}
REGION = arg("region", "denmark1851")
X_MIN, X_MAX, Y_MIN, Y_MAX = box_extent(*REGIONS[REGION])

# Override with --height=N (e.g. 2048 when memory is tight). Full quality is 4096.
HEIGHT_PX = int(arg("height", 4096))
# Multiple of 4 so the GPU block compression can use it without padding.
WIDTH_PX = int(round(HEIGHT_PX * (X_MAX - X_MIN) / (Y_MAX - Y_MIN) / 4)) * 4

# Size of one repeat of the close-zoom field detail texture.
DETAIL_TILE_KM = 2.5
# Field parcels for the Unreal close zoom: one repeat covers this many km (4096 px).
PARCEL_TILE_KM = 8.0
PARCEL_TEXTURE_PX = 4096
# Road routing grid, and how far/which towns are linked by main roads.
ROAD_CELL_KM = 0.5
ROAD_NEIGHBOURS = 3
ROAD_MAX_KM = 70.0

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
    """A pixel grid over a projected (LAEA km) rectangle. Row 0 is north."""

    def __init__(self, x_min, x_max, y_min, y_max, width, height):
        self.x_min, self.x_max, self.y_min, self.y_max = x_min, x_max, y_min, y_max
        self.w, self.h = width, height
        self.km_per_px = (y_max - y_min) / height

    def to_px(self, lon, lat):
        x, y = project(lon, lat)
        return (x - self.x_min) / (self.x_max - self.x_min) * self.w, (self.y_max - y) / (self.y_max - self.y_min) * self.h

    def xy_grids(self):
        """Projected km of every pixel centre."""
        x = self.x_min + (np.arange(self.w) + 0.5) / self.w * (self.x_max - self.x_min)
        y = self.y_max - (np.arange(self.h) + 0.5) / self.h * (self.y_max - self.y_min)
        return np.meshgrid(x, y)

    def grids(self):
        """lon/lat of every pixel centre (float32), for rules written in geographic terms."""
        x, y = self.xy_grids()
        lon, lat = unproject(x, y)
        return lon.astype(np.float32), lat.astype(np.float32)

    def fill(self, shapes):
        """Rasterises polygon shapes (outer rings clockwise, holes counter-clockwise)."""
        img = Image.new("L", (self.w, self.h), 0)
        draw = ImageDraw.Draw(img)
        margin = 0.05 * max(self.x_max - self.x_min, self.y_max - self.y_min)
        for parts in shapes:
            for ring in parts:
                if len(ring) < 3:
                    continue
                pts = np.asarray(ring, np.float64)
                x, y = project(pts[:, 0], pts[:, 1])
                if x.max() < self.x_min - margin or x.min() > self.x_max + margin or y.max() < self.y_min - margin or y.min() > self.y_max + margin:
                    continue
                area = float(np.sum(pts[:, 0] * np.roll(pts[:, 1], 1) - np.roll(pts[:, 0], 1) * pts[:, 1]))
                hole = area < 0  # counter-clockwise in lon/lat = hole in shapefile convention
                px = (x - self.x_min) / (self.x_max - self.x_min) * self.w
                py = (self.y_max - y) / (self.y_max - self.y_min) * self.h
                draw.polygon(list(zip(px.tolist(), py.tolist())), fill=0 if hole else 255)
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


def parcel_texture(size, tile_km, seed):
    """Tileable field parcels for the close zoom, in absolute colours (sRGB).

    Farm blocks (Voronoi cells ~1.1 km) are cut into parallel strips, as after the land reforms
    (udskiftningen); each strip carries its own crop. Blocks are edged by hedgerows with a shadow
    on the south-east side, strips by thin furrow lines. Pieces are drawn ~2x real size to match
    the exaggerated 3D scenery.
    """
    rng = np.random.default_rng(seed)
    n = int((tile_km / 1.1) ** 2)
    pts = rng.random((n, 2))
    angle = rng.random(n) * np.pi
    strip_km = rng.uniform(0.14, 0.34, n)
    offset = rng.random(n)
    pasture_block = rng.random(n) < 0.12
    # pasture, young grain, ripe grain, hay, ploughed, fallow
    crops = np.array([rgb(84, 118, 48), rgb(128, 146, 58), rgb(192, 166, 82), rgb(158, 150, 84), rgb(132, 102, 68), rgb(108, 112, 58)])
    weights = np.array([0.18, 0.22, 0.18, 0.14, 0.14, 0.14])
    table = rng.choice(len(crops), size=4096, p=weights)

    shifts = [(dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]
    tiled = np.concatenate([pts + s for s in shifts])
    tree = cKDTree(tiled)
    out = np.zeros((size, size, 3), np.float32)
    # Hedgerows are rows of tree crowns: a bumpy width.
    crowns = fbm((size, size), 700, 2, seed + 2)
    rows = 512
    for y0 in range(0, size, rows):
        yy, xx = np.mgrid[y0:y0 + rows, 0:size]
        q = np.column_stack([(xx.ravel() + 0.5) / size, (yy.ravel() + 0.5) / size])
        dist, idx = tree.query(q, k=2, workers=-1)
        i = idx[:, 0] % n
        centre = tiled[idx[:, 0]]
        lx, ly = (q[:, 0] - centre[:, 0]) * tile_km, (q[:, 1] - centre[:, 1]) * tile_km
        u = lx * np.cos(angle[i]) + ly * np.sin(angle[i])
        v = -lx * np.sin(angle[i]) + ly * np.cos(angle[i])
        s = u / strip_km[i] + offset[i]
        strip = np.floor(s).astype(np.int64)
        crop = table[(i * 7919 + strip * 104729) % len(table)]
        crop = np.where(pasture_block[i], 0, crop)
        colour = crops[crop]
        # Furrows along the strips, and a thin line between strips (not in pastures).
        furrow = 1.0 + 0.045 * np.sin(v * 2 * np.pi / 0.014) * (crop != 0)
        edge = np.minimum(s - strip, strip + 1 - s) * strip_km[i]
        line = np.where((edge < 0.006) & ~pasture_block[i], 0.86, 1.0)
        colour = colour * (furrow * line)[:, None]
        # Hedgerows on block borders: true distance (km) to the bisector with the next block.
        other = tiled[idx[:, 1]]
        nrm = other - centre
        border = ((other ** 2).sum(1) - (centre ** 2).sum(1) - 2 * (nrm * q).sum(1)) / (2 * np.hypot(*nrm.T)) * tile_km
        # Texture rows run north to south, so a neighbour block to the north-west has dx + dy < 0.
        north_west = (other[:, 0] - centre[:, 0]) + (other[:, 1] - centre[:, 1]) < 0
        bump = crowns[y0:y0 + rows].ravel()
        hedge = border < 0.009 + 0.014 * bump
        shadow = (border < 0.024 + 0.016 * bump) & north_west
        colour = np.where(shadow[:, None], colour * 0.72, colour)
        colour = np.where(hedge[:, None], rgb(44, 68, 32), colour)
        out[y0:y0 + rows] = colour.reshape(rows, size, 3)
    grain = (fbm((size, size), 60, 3, seed + 1) - 0.5) * 0.08 + (rng.random((size, size)) - 0.5) * 0.04
    out *= (1.0 + grain)[..., None]
    return (np.clip(out, 0, 1) * 255).astype(np.uint8)


def route_roads(raster, monarchy, forest, height, cities, ferries):
    """Main roads between towns, routed over land on a ROAD_CELL_KM grid, with the 1851 ferries.

    Costs favour open, level ground; the sea is only crossed at the listed ferries. Every town is
    linked to its ROAD_NEIGHBOURS nearest reachable towns (by travel cost). Returns
    [{"kind": "main" | "ferry", "km": [[x, y], ...]}] in projected km; roads are smoothed.
    """
    from scipy.sparse import coo_matrix
    from scipy.sparse.csgraph import dijkstra

    f = max(1, int(round(ROAD_CELL_KM / raster.km_per_px)))
    h, w = monarchy.shape[0] // f, monarchy.shape[1] // f

    def block(a):
        return a[:h * f, :w * f].astype(np.float32).reshape(h, f, w, f).mean(axis=(1, 3))

    land = block(monarchy) > 0.5
    gy, gx = np.gradient(block(height))
    cost = 1.0 + 2.5 * block(forest) + 40.0 * np.hypot(gx, gy)
    cell_x = (raster.x_max - raster.x_min) / raster.w * f
    cell_y = (raster.y_max - raster.y_min) / raster.h * f
    idx = np.arange(h * w).reshape(h, w)
    _, (near_r, near_c) = ndimage.distance_transform_edt(~land, return_indices=True)

    def node_at(km_xy):
        x, y = km_xy
        r = min(h - 1, max(0, int((raster.y_max - y) / cell_y)))
        c = min(w - 1, max(0, int((x - raster.x_min) / cell_x)))
        return int(idx[near_r[r, c], near_c[r, c]])

    def node_km(n):
        r, c = divmod(n, w)
        return np.array([raster.x_min + (c + 0.5) * cell_x, raster.y_max - (r + 0.5) * cell_y])

    rows, cols, wts = [], [], []
    for dy, dx in ((0, 1), (1, 0), (1, 1), (1, -1)):
        src = (slice(0, h - dy), slice(max(0, -dx), w - max(0, dx)))
        dst = (slice(dy, h), slice(max(0, dx), w - max(0, -dx)))
        ok = land[src] & land[dst]
        step = math.hypot(dx * cell_x, dy * cell_y)
        rows.append(idx[src][ok])
        cols.append(idx[dst][ok])
        wts.append((step * 0.5 * (cost[src] + cost[dst]))[ok])
    # Ferries: an edge between the two landings, costlier than the same distance on land.
    ferry_nodes = {}
    for k, fy in enumerate(ferries):
        a_km, b_km = np.array(project(*fy["a"])), np.array(project(*fy["b"]))
        a, b = node_at(a_km), node_at(b_km)
        if a == b:
            continue
        ferry_nodes[(min(a, b), max(a, b))] = (k, a_km, b_km)
        rows.append(np.array([a]))
        cols.append(np.array([b]))
        wts.append(np.array([np.hypot(*(a_km - b_km)) * 3.0 + 6.0]))
    graph = coo_matrix((np.concatenate(wts), (np.concatenate(rows), np.concatenate(cols))), shape=(h * w, h * w)).tocsr()

    towns = [c for c in cities if not c.get("bornholm")]
    km = np.array([project(c["lon"], c["lat"]) for c in towns], np.float64)
    nodes = [node_at(p) for p in km]

    def chaikin(p, rounds=3):
        for _ in range(rounds):
            q = [p[0]]
            for a, b in zip(p[:-1], p[1:]):
                q += [0.75 * a + 0.25 * b, 0.25 * a + 0.75 * b]
            q.append(p[-1])
            p = np.array(q)
        return p

    town_nodes = np.array(nodes)
    pairs, preds = set(), {}
    for i in range(len(towns)):
        dist, pred = dijkstra(graph, directed=False, indices=nodes[i], return_predecessors=True, limit=ROAD_MAX_KM * 5)
        preds[i] = pred
        reach = [(dist[n], j) for j, n in enumerate(town_nodes) if j != i and np.isfinite(dist[n])]
        for _, j in sorted(reach)[:ROAD_NEIGHBOURS]:
            pairs.add((min(i, j), max(i, j)))

    # Every ferry links the nearest towns on its two shores (the crossings carried the main routes).
    for fy in ferries:
        ends = [int(np.argmin(np.hypot(*(km - np.array(project(*fy[s]))).T))) for s in ("a", "b")]
        if ends[0] != ends[1]:
            pairs.add((min(ends), max(ends)))

    roads, used_ferries = [], set()
    for i, j in sorted(pairs):
        pred, node, path = preds[i], nodes[j], []
        while node >= 0 and node != nodes[i]:
            path.append(node)
            node = pred[node]
        if node != nodes[i]:
            continue
        path.append(nodes[i])
        path.reverse()
        # Split at ferry jumps (consecutive nodes that are not grid neighbours).
        runs, run = [], [km[i]]
        for a, b in zip(path[:-1], path[1:]):
            ra, ca = divmod(a, w)
            rb, cb = divmod(b, w)
            if max(abs(ra - rb), abs(ca - cb)) > 1:
                k, a_km, b_km = ferry_nodes[(min(a, b), max(a, b))]
                used_ferries.add(k)
                start_km, end_km = (a_km, b_km) if node_at(a_km) == a else (b_km, a_km)
                runs.append(run + [start_km])
                run = [end_km]
            else:
                run.append(node_km(b))
        run[-1] = km[j]
        runs.append(run)
        for r in runs:
            pts = np.array(r)
            if len(pts) > 3:
                pts = np.vstack([pts[0], pts[1:-1:2], pts[-1]])
            if len(pts) >= 2:
                roads.append({"kind": "main", "km": np.round(chaikin(pts), 3).tolist()})
    for k in sorted(used_ferries):
        fy = ferries[k]
        roads.append({"kind": "ferry", "name": fy["name"],
                      "km": np.round(np.array([project(*fy["a"]), project(*fy["b"])]), 3).tolist()})
    return roads


def amter_raster(raster, regions, land, amter):
    """Amt index + 1 per pixel (0 = sea/foreign): nearest anchor of an amt in the same region."""
    km_x, km_y = raster.xy_grids()
    out = np.zeros(land.shape, np.uint8)
    for code, region in (("K", REGION_KINGDOM), ("S", REGION_SCHLESWIG), ("H", REGION_HOLSTEIN)):
        anchors, owner = [], []
        for n, a in enumerate(amter):
            if a["region"] == code:
                for lon, lat in a["anchors"]:
                    anchors.append(project(lon, lat))
                    owner.append(n + 1)
        mask = land & (regions == region)
        _, nearest = cKDTree(np.array(anchors)).query(np.column_stack([km_x[mask], km_y[mask]]), workers=-1)
        out[mask] = np.array(owner, np.uint8)[nearest]
    return out


def paint_amt_borders(colour, amt, regions):
    """Thin dotted borders between amter of the same duchy/kingdom, painted into the map."""
    edge = np.zeros(amt.shape, bool)
    for dy, dx in ((0, 1), (1, 0)):
        a, b = amt[:amt.shape[0] - dy, :amt.shape[1] - dx], amt[dy:, dx:]
        ra, rb = regions[:amt.shape[0] - dy, :amt.shape[1] - dx], regions[dy:, dx:]
        diff = (a != b) & (a > 0) & (b > 0) & (ra == rb)
        edge[:amt.shape[0] - dy, :amt.shape[1] - dx] |= diff
    edge = ndimage.binary_dilation(edge, iterations=2)
    dots = ((np.indices(amt.shape).sum(axis=0) // 9) % 3) != 2
    line = edge & dots
    out = colour.astype(np.float32)
    out[line] = out[line] * 0.25 + np.array([66, 44, 22], np.float32) * 0.75
    return out.astype(np.uint8)


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a + (b - a) * t


def rgb(*c):
    return np.array(c, np.float32) / 255.0


def paint(r, land, regions, lon, lat, seed):
    shape = land.shape
    px_km = r.km_per_px

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
    km_x, km_y = r.xy_grids()
    forest_noise = fbm(shape, 45, 3, seed + 5)
    forest_mask = np.clip((forest_noise - (0.70 - 0.06 * eastness)) * 10, 0, 1)
    edge_noise = fbm(shape, 90, 3, seed + 9)
    for f_lon, f_lat, radius in FORESTS:
        fx, fy = project(f_lon, f_lat)
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
    # Where fields are painted at close zoom: not on heath, dunes or in the woods.
    farmland = land * (1 - np.clip(heath_mask / 0.5, 0, 1)) * (1 - shore) * (1 - 0.85 * forest_mask)

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
    return arr, height, forest_mask, farmland.astype(np.float32)


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
        c_lon, c_lat = float(lon[int(cy), int(cx)]), float(lat[int(cy), int(cx)])
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
    colour, height, forest, farmland = paint(raster, land, regions, lon, lat, seed)
    return colour, height, regions, land, forest, farmland


def save_png16(path, arr01):
    Image.fromarray((np.clip(arr01, 0, 1) * 65535).astype(np.uint16)).save(path)


def main():
    ne_dir = sys.argv[1]
    preview = "--preview" in sys.argv
    os.makedirs(OUT_DIR, exist_ok=True)

    main_r = Raster(X_MIN, X_MAX, Y_MIN, Y_MAX, WIDTH_PX, HEIGHT_PX)
    colour, height, regions, land, forest, farmland = build(ne_dir, main_r, 1864)

    bx0, bx1, by0, by1 = box_extent(*BORNHOLM)
    bh_w = int(round(1024 * (bx1 - bx0) / (by1 - by0)))
    bh_r = Raster(bx0, bx1, by0, by1, bh_w, 1024)
    bh_colour, _, _, _, _, _ = build(ne_dir, bh_r, 1865)

    with open(os.path.join(HERE, "amter1851.json"), encoding="utf-8") as f:
        admin = json.load(f)
    amt = amter_raster(main_r, regions, land, admin["amter"])
    colour = paint_amt_borders(colour, amt, regions)
    Image.fromarray(amt).resize((WIDTH_PX // 2, HEIGHT_PX // 2), Image.NEAREST).save(os.path.join(OUT_DIR, "Denmark1851_Amter.png"))

    Image.fromarray(colour).save(os.path.join(OUT_DIR, "Denmark1851_Color.png"))
    save_png16(os.path.join(OUT_DIR, "Denmark1851_Height.png"), height / max(height.max(), 1e-6))
    Image.fromarray(regions * 60).save(os.path.join(OUT_DIR, "Denmark1851_Regions.png"))
    Image.fromarray(bh_colour).save(os.path.join(OUT_DIR, "Bornholm1851_Color.png"))

    # Close-zoom detail: tileable fields (one tile = DETAIL_TILE_KM) masked to land.
    Image.fromarray(detail_texture(1024, 160, 1866)).save(os.path.join(OUT_DIR, "Fields1851_Detail.png"))
    if "--parcels" in sys.argv:  # Unreal only (20 MB); Unity uses Fields1851_Detail
        Image.fromarray(parcel_texture(PARCEL_TEXTURE_PX, PARCEL_TILE_KM, 1867)).save(os.path.join(OUT_DIR, "Fields1851_Parcels.png"))
    mask = np.zeros(land.shape + (4,), np.uint8)
    mask[..., 3] = (farmland * (regions != REGION_FOREIGN) * 255).astype(np.uint8)
    mask[..., 3] = np.asarray(Image.fromarray(mask[..., 3]).filter(ImageFilter.GaussianBlur(1.5)))
    Image.fromarray(mask).resize((WIDTH_PX // 2, HEIGHT_PX // 2), Image.BILINEAR).save(os.path.join(OUT_DIR, "Denmark1851_DetailMask.png"))

    # Scenery placement for the 3D close zoom (read on the CPU, not a texture):
    # R = monarchy land, G = woodland density; both 0..255 at half resolution.
    monarchy = land & (regions != REGION_FOREIGN)
    features = np.zeros(land.shape + (3,), np.uint8)
    features[..., 0] = monarchy * 255
    features[..., 1] = (np.clip(forest, 0, 1) * monarchy * 255).astype(np.uint8)
    Image.fromarray(features).resize((WIDTH_PX // 2, HEIGHT_PX // 2), Image.NEAREST).save(os.path.join(OUT_DIR, "Denmark1851_Features.png"))

    with open(os.path.join(HERE, "cities1850.json"), encoding="utf-8") as f:
        cities = json.load(f)

    width_km, height_km = X_MAX - X_MIN, Y_MAX - Y_MIN
    lon_min, lon_max, lat_min, lat_max = REGIONS[REGION]
    meta = {
        "version": "v00.00.15",
        "region": REGION,
        "projection": {"type": "laea", "lat0": PROJ_LAT0, "lon0": PROJ_LON0, "radiusKm": EARTH_R_KM},
        "extentKm": {"xMin": X_MIN, "xMax": X_MAX, "yMin": Y_MIN, "yMax": Y_MAX},
        "extent": {"lonMin": lon_min, "lonMax": lon_max, "latMin": lat_min, "latMax": lat_max},
        "sizeKm": {"width": round(width_km, 2), "height": round(height_km, 2)},
        "textureSize": {"width": WIDTH_PX, "height": HEIGHT_PX},
        "detailTileKm": DETAIL_TILE_KM,
        "parcelTileKm": PARCEL_TILE_KM,
        "bornholm": {"lonMin": BORNHOLM[0], "lonMax": BORNHOLM[1], "latMin": BORNHOLM[2], "latMax": BORNHOLM[3]},
        "bornholmKm": {"xMin": bx0, "xMax": bx1, "yMin": by0, "yMax": by1},
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
    meta["roads"] = route_roads(main_r, land & (regions != REGION_FOREIGN), forest, height, cities["cities"], admin["ferries"])

    # Amter: index i + 1 in Denmark1851_Amter.png; label at the amt pixel nearest its centre of mass.
    km_x, km_y = main_r.xy_grids()
    meta["amter"] = []
    for n, a in enumerate(admin["amter"]):
        mask = amt == n + 1
        entry = {"id": n + 1, "name": a["name"], "seat": a["seat"], "region": a["region"],
                 "areaKm2": round(float(mask.sum()) * main_r.km_per_px ** 2)}
        if mask.any():
            xs, ys = km_x[mask], km_y[mask]
            k = np.argmin((xs - xs.mean()) ** 2 + (ys - ys.mean()) ** 2)
            lon, lat = unproject(xs[k], ys[k])
            entry["lat"], entry["lon"] = round(float(lat), 4), round(float(lon), 4)
            if a.get("label", True):
                meta["labels"].append({"text": a["name"], "lat": entry["lat"], "lon": entry["lon"], "kind": "amt"})
        meta["amter"].append(entry)

    with open(os.path.join(OUT_DIR, "Denmark1851_Map.json"), "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=1)

    if preview:
        Image.fromarray(colour).resize((WIDTH_PX // 4, HEIGHT_PX // 4), Image.LANCZOS).save(os.path.join(HERE, "preview.png"))
    print(f"roads: {sum(r['kind'] == 'main' for r in meta['roads'])} road runs, {sum(r['kind'] == 'ferry' for r in meta['roads'])} ferries; amter: {len(meta['amter'])}")
    print("  " + ", ".join(f"{a['name']} {a['areaKm2']}" for a in meta["amter"]))
    print(f"map {REGION}: {WIDTH_PX}x{HEIGHT_PX} px, {width_km:.0f}x{height_km:.0f} km (LAEA), {height_km / HEIGHT_PX * 1000:.0f} m/px")


if __name__ == "__main__":
    main()
