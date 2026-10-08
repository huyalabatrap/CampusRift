from pathlib import Path
p=Path('Tools/ar_nohand.py');s=p.read_text(encoding='utf-8-sig');a=s.index('def call(');b=s.index('def code(',a)
s=s[:a]+'''def call(name,args):
 for attempt in range(25):
  r=u.call(name,args)['result'];v=r.get('structuredContent')
  if v is None:v=json.loads(r['content'][0]['text'])
  if isinstance(v.get('result'),dict):v=v['result']
  if v.get('success') is not False:return v
  if v.get('data') is not None or v.get('message') and not any(t in str(v).lower() for t in ['no_unity_session','not ready','retry','compiling','domain reload','busy']):raise RuntimeError(v)
  time.sleep(2)
 raise RuntimeError(v)
'''+s[b:];p.write_text(s,encoding='utf-8')
