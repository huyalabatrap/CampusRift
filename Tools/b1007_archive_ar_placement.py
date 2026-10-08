from pathlib import Path
root=Path('task/batch-1007/regression');names=['ARRiftPlayTest','ARGestureCheckMock','ARMode-training','ARMode-defense','ARMode-rift-hunt','ARMode-dragon-duel','ARMode-seal-practice']
for name in names:
    folder=root/'runs'/name
    if not folder.exists() or not (folder/'invoked.json').exists():continue
    assert not (folder/'runtime-start.json').exists(),name
    attempt=1;destination=root/'setup-errors'/('preplacement-'+name)
    while destination.exists():attempt+=1;destination=root/'setup-errors'/('preplacement-'+str(attempt)+'-'+name)
    folder.rename(destination)
with (root.parent/'PROGRESS.md').open('a',encoding='utf-8') as f:f.write('\n- Mốc XR fixture: góc camera cố định không tạo được plane. Khôi phục sweep6yaw của harness GóiA; diagnostic đã xác nhận polygon/anchor thật trên bàn XRSimulation. Attempt dừngtrước AddComponent/emit/PreviewPhase nằmở regression/setup-errors/preplacement-*; chưa chạy mục regression. Thêm runtime-start.json phân biệt setup với invocationthật.\n')
