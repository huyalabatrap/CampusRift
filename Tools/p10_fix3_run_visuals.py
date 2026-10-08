import subprocess,sys
for id in ['van-kiem-quyet','than-kiem-ngu-loi','phat-no-hoa-lien','tich-lich-nhat-thiem']:
    subprocess.run([sys.executable,'Tools/p10_fix3_capture.py',id],check=True)
subprocess.run([sys.executable,'Tools/p10_sheets.py','task/p10/screens/fix3'],check=True)
