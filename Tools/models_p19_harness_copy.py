from pathlib import Path
root=Path(__file__).resolve().parents[1]
src=(root/'Assets/Enemies/Validation/P19PlayTest.cs').read_text(encoding='utf-8')
src=src.replace('class P19PlayTest:', 'class ModelsP19PlayTest:').replace('task/p19','task/models').replace('Ninja independent','Night Demon').replace('Ninja planted','Night Demon planted').replace('P19PlayTest.json','ModelsP19PlayTest.json')
(root/'Assets/Enemies/Validation/ModelsP19PlayTest.cs').write_text(src,encoding='utf-8')
print('Copied existing smoke with isolated evidence paths; assertions unchanged.')
