from ar_session import *
print(code(Path('task/ar/fix1-visual.cs').read_text()),flush=True)
for i in range(150):
 if Path('task/ar/fix1/VISUAL-DONE.txt').exists():break
 time.sleep(1)
else:raise RuntimeError('visual capture timed out')
print(Path('task/ar/fix1/visual.json').read_text(),flush=True)
console('task/ar/fix1/visual-console.json')
