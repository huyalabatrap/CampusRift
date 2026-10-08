from ar_session import *
out=Path('task/ar/fix1')
state=code(Path('task/ar/fix1-final-state.cs').read_text(encoding='utf-8'))
save(out/'final-state.json',state)
errors=console(out/'final-console.json')
assert not any(state[k] for k in ['playing','compiling','updating','building','dirty','androidStartup','editorStartup','androidAutomatic','editorAutomatic','runeShaderError','shadowShaderError','planeShaderError']),state
assert state['target']=='Android' and state['scene']=='Assets/Scenes/SampleScene.unity' and state['loaderInactive'],state
assert errors['data']==[],errors
before=json.loads((out/'settings-before.json').read_text(encoding='utf-8-sig'))
assert all(state[k]==before[k] for k in ['enterPlay','enterEnabled','dev','inputBackground','inputEditor','floor','safety']),state
print(json.dumps(state,ensure_ascii=False,indent=2),flush=True)
