"""Author combat variants without touching the P12 assets; preserve CC0 audio provenance."""
from pathlib import Path
import re,uuid,json,hashlib,urllib.request,urllib.parse,zipfile,io
dest=Path('Assets/SkyBeast/Resources/P14');dest.mkdir(parents=True,exist_ok=True)
source=Path('task/p14/source');source.mkdir(parents=True,exist_ok=True)
def guid(path):return re.search(r'guid: (\w+)',Path(str(path)+'.meta').read_text()).group(1)
def meta(path,kind='asset'):
 p=Path(str(path)+'.meta')
 if not p.exists():p.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+('NativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' if kind=='asset' else 'ShaderImporter:\n  externalObjects: {}\n  defaultTextures: []\n  nonModifiableTextures: []\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n'),encoding='utf-8')
for id,dragon,vi,en,segments,levels,attack,path in [
 ('xich-hoa-giao','023','XÍCH HỎA GIAO','CRIMSON FIRE SERPENT',1,[(8,1),(10,1)],0,0),
 ('chu-tuoc','026','TÀ HÓA CHU TƯỚC','CORRUPTED VERMILION BIRD',2,[(9,1),(9,2),(10,1),(10,2)],1,1),
 ('hoa-long-vuong','020','CỬU U HỎA LONG VƯƠNG','NINE ABYSS FIRE DRAGON',1,[(10,3)],2,0)]:
 p=dest/(id+'.asset');text=Path('Assets/SkyBeast/Resources/P12/Dragon'+dragon+'.asset').read_text(encoding='utf-8-sig')
 text=re.sub(r'  m_Name: .*','  m_Name: '+id,text)
 text+='  nameVi: '+vi+'\n  nameEn: '+en+'\n  segments: '+str(segments)+'\n  level10Segments: 1\n  pathType: '+str(path)+'\n  phases:\n'
 for level,phase in levels:
  profile=Path(f'Assets/SkyBeast/Resources/P13/Fire{level}Phase{phase}.asset')
  text+='  - fire: {fileID: 11400000, guid: '+guid(profile)+', type: 2}\n    attack: '+str(attack)+'\n    furyMultiplier: '+('1.2' if phase==2 else '1')+'\n'
 p.write_text(text,encoding='utf-8');meta(p)
shader=Path('Assets/SkyBeast/SkyStrike.shader');meta(shader,'shader')
for name,feather in [('meteor',0),('feather',1)]:
 p=dest/(name+'.mat');p.write_text('%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!21 &2100000\nMaterial:\n  serializedVersion: 8\n  m_ObjectHideFlags: 0\n  m_Name: '+name+'\n  m_Shader: {fileID: 4800000, guid: '+guid(shader)+', type: 3}\n  m_ValidKeywords: []\n  m_InvalidKeywords: []\n  m_LightmapFlags: 4\n  m_EnableInstancingVariants: 0\n  m_DoubleSidedGI: 0\n  m_CustomRenderQueue: -1\n  m_SavedProperties:\n    serializedVersion: 3\n    m_TexEnvs: []\n    m_Ints: []\n    m_Floats:\n    - _Feather: '+str(feather)+'\n    m_Colors: []\n',encoding='utf-8');meta(p)
manifest=[]
def fetch(url):return urllib.request.urlopen(url,timeout=30).read()
page='https://opengameart.org/content/large-wings-flap';html=fetch(page).decode();(source/'wing-page.html').write_text(html,encoding='utf-8')
links=[urllib.parse.urljoin(page,x) for x in re.findall(r'href=["\']([^"\']+\.zip)["\']',html) if '/sites/default/files/' in x]
url=links[0];payload=fetch(url);(source/'wings-flap.zip').write_bytes(payload)
with zipfile.ZipFile(io.BytesIO(payload)) as z:
 member=next(n for n in z.namelist() if n.endswith('.ogg'));payload=z.read(member);p=dest/'wing-flap.ogg';p.write_bytes(payload)
manifest.append(dict(page=page,url=url,file=str(p),archiveMember=member,author='AntumDeluge (based on dave.des)',license='CC0-1.0',sha256=hashlib.sha256(payload).hexdigest()))
page='https://kenney.nl/assets/impact-sounds';html=fetch(page).decode();(source/'impact-page.html').write_text(html,encoding='utf-8')
url=urllib.parse.urljoin(page,re.findall(r'href=["\']([^"\']+\.zip)["\']',html)[0]);data=fetch(url);(source/'impact-sounds.zip').write_bytes(data)
with zipfile.ZipFile(io.BytesIO(data)) as z:
 members=[n for n in z.namelist() if n.endswith('.ogg')]
 for term,name in [('impactMining_000','meteor-impact'),('impactSoft_medium_000','feather-fall')]:
  member=next((n for n in members if term in n),next(n for n in members if 'Mining' in n) if name=='meteor-impact' else next(n for n in members if 'Soft' in n));payload=z.read(member);p=dest/(name+'.ogg');p.write_bytes(payload)
  manifest.append(dict(page=page,url=url,archiveMember=member,file=str(p),author='Kenney',license='CC0-1.0',sha256=hashlib.sha256(payload).hexdigest()))
(source/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps(manifest,indent=2))
