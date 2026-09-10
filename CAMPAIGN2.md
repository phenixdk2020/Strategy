# campaign2 — PROJECT 1864

Ekstra campaign-spor til et nyt 3D-hovedkort af Danmark.

Dette spor rører **ikke** `channel-campaign` og **ikke** `channel-test`.

| Spor | Branch | Rolle |
|---|---|---|
| Kampagnekort (aktivt) | `channel-campaign` | v13m OSM-drape, map-only. Bevares urørt. |
| Kamp-test | `channel-test` | Taktiske slag. Bevares urørt. |
| **3D-kort reset** | **`campaign2`** | Nyt Danmark-diorama uden OSM som landets hud. |

## Hvorfor dette spor findes

`channel-campaign` v13m er et moderne OpenStreetMap-atlas klistret på et næsten fladt mesh. Det er ikke det 3D-Danmark, kampagnen skal være. I stedet for at lappe v13n oven på v13j/k/l/m får resetten sit eget spor.

Arvet v13m-kode på branchen er **kun udgangspunkt**. Den er ikke målet og må udskiftes her uden at true det aktive campaign-spor.

## Mål

Et Total War / Grand Tactician-agtigt 3D-diorama:

- rigtig kyst og øer (vand vs land i 3D)
- DEM-relief med synlig vertical exaggeration
- malet landcover, ikke satellit/OSM
- 1864-teater inkl. Slesvig-Holsten
- kamera med pitch, ikke kun top-down

Designet står i `docs/PROJECT-1864-Campaign2-3D-Denmark-Map.md`.

## Isolation

- Lat/lon, rute-km og ETA forbliver authoritative. Unity-Y er kun præsentation.
- Tactical `PrototypeBattle` auto-åbnes ikke.
- MAP-ONLY indtil kortet består visuel QA.
- Ingen merge tilbage til `channel-campaign`, før Allan har godkendt Play Mode.
