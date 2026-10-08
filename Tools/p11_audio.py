"""Download the official CC0 packs and retain selected new reaction layers + original licenses."""
import io,json,pathlib,urllib.request,zipfile
root=pathlib.Path('Assets/Combat/Audio/Reactions');root.mkdir(parents=True,exist_ok=True)
packs=[('Impact','https://kenney.nl/media/pages/assets/impact-sounds/87b4ddecda-1677589768/kenney_impact-sounds.zip',
        ['impactGlass_heavy_001.ogg','impactMetal_heavy_001.ogg','impactPlate_heavy_000.ogg','impactBell_heavy_001.ogg','impactSoft_heavy_000.ogg']),
       ('SciFi','https://kenney.nl/media/pages/assets/sci-fi-sounds/6b296f9ecf-1677589334/kenney_sci-fi-sounds.zip',
        ['laserSmall_001.ogg','laserLarge_000.ogg','explosionCrunch_001.ogg','lowFrequency_explosion_001.ogg','forceField_003.ogg','thrusterFire_001.ogg'])]
manifest=[]
for pack,url,names in packs:
    archive=zipfile.ZipFile(io.BytesIO(urllib.request.urlopen(url,timeout=90).read()))
    for name in names:
        matching=[f for f in archive.namelist() if pathlib.PurePosixPath(f).name==name]
        if not matching:raise RuntimeError(name+' missing in official archive')
        (root/name).write_bytes(archive.read(matching[0]));manifest.append(dict(file=name,archive=url,member=matching[0],license='CC0',author='Kenney Vleugels'))
    licenses=[f for f in archive.namelist() if pathlib.PurePosixPath(f).name.lower().startswith('license')]
    for file in licenses:(root/(pack+'-'+pathlib.PurePosixPath(file).name)).write_bytes(archive.read(file))
(root/'sources.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('Downloaded',len(manifest),'CC0 audio layers')
