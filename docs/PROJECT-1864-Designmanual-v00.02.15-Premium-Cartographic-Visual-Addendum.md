# PROJECT 1864 Designmanual — v00.02.15 Addendum

## Premium Cartographic Visual Rule

Campaign presentation skal bruge virkelig kartografi som primær visuel reference, draperet på 3D-terrain, i stedet for håndbyggede grønne flader og prototype-linjer.

### Fast regel

- DEM/geospatial geometry må ændre præsentation, men ikke strategisk distance/ETA.
- Virkeligt kortmateriale må bruges som renderlag; historisk autenticitet skal vurderes separat.
- Moderne live cartography er DEV-reference og ikke 1864-faktakilde.
- Prototype road/ferry/city overlays må ikke konkurrere visuelt med den aktive cartography.
- Close zoom skal hente/højopløse kun det aktuelle viewport, ikke hele landet.
- 3D-landmarks skal bruge semantic LOD og må ikke være kilometervis store på strategisk zoom.
- Aalborg, Aarhus og andre havne-/kystbyer skal terrain-/land-anchor korrekt; bygninger må ikke flyde i vand.
- Bro/færge/vej skal senere være eksplicit route-type i authoritative historical data; en visuel landvej må aldrig tegnes direkte over åbent vand.

### v13l visual target

`Historic / Premium Cartographic 3D Map`:
- rigtig kartografi,
- lavt troværdigt dansk relief,
- bløde skygger,
- diskret atmosfære,
- skarpt close zoom,
- små periodiske 3D-landmarks,
- ingen debug-look.
