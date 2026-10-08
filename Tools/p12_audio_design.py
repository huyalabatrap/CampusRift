"""Deterministic layered game roars from credited recordings, not synthesized animal claims."""
import io,json,zipfile,subprocess,shutil
from pathlib import Path
import numpy as np
from scipy import signal
from scipy.io import wavfile
import imageio_ffmpeg
root=Path(__file__).resolve().parents[1];src=root/'Assets/SkyBeast/Audio/Source';out=root/'Assets/SkyBeast/Audio/Designed';out.mkdir(parents=True,exist_ok=True)
ff=imageio_ffmpeg.get_ffmpeg_exe();sr=44100
def read(path):
    raw=subprocess.check_output([ff,'-v','error','-i',str(path),'-f','f32le','-ac','1','-ar',str(sr),'pipe:1'])
    return np.frombuffer(raw,dtype=np.float32).astype(np.float64)
def pitch(x,factor):return signal.resample_poly(x,100,int(100*factor))
def fit(x,n):return np.pad(x,(0,max(0,n-len(x))))[:n]
def save(name,x):
    x=np.nan_to_num(x);x=np.tanh(x*1.6);x/=max(.001,np.max(np.abs(x)));x*=.84
    wavfile.write(out/name,sr,(x*32767).astype(np.int16));return dict(file=name,seconds=round(len(x)/sr,3),peak=round(float(np.max(np.abs(x))),4),rms=round(float(np.sqrt(np.mean(x*x))),4))
z=zipfile.ZipFile(src/'monster-monster_sfx_pack.zip');monsters=[]
for i in range(1,11):
    p=src/f'monster-{i}.wav';p.write_bytes(z.read(f'monster_sfx_pack/monster-{i}.wav'));monsters.append(read(p))
cats=[read(src/'cat1-1881.mp3'),read(src/'cat2-1882.mp3')]
report=[]
for dragon,pitch_base in [('020',.68),('023',.48),('026',.37)]:
    for i in range(4):
        cat=pitch(cats[i%2],pitch_base+i*.023);monster=pitch(monsters[(i+int(dragon))%10],pitch_base*.85)
        n=min(int(sr*7.5),max(len(cat),len(monster))+int(sr*1.4));x=fit(cat,n)*.6+fit(monster,n)*.8
        if dragon=='020':x=signal.sosfilt(signal.butter(2,180,'high',fs=sr,output='sos'),x)
        elif dragon=='023':x=signal.sosfilt(signal.butter(2,2000,'low',fs=sr,output='sos'),x)+fit(cat,n)*.25
        else:x=np.tanh(x*2)+fit(pitch(monsters[(i+6)%10],.4),n)*.3
        dry=x.copy()
        for delay,gain in [(.13,.25),(.29,.18),(.53,.12),(.89,.08)]:
            samples=int(delay*sr);x[samples:]+=dry[:-samples]*gain
        fade=int(sr*.12);x[:fade]*=np.linspace(0,1,fade);x[-int(sr*.9):]*=np.linspace(1,0,int(sr*.9))
        report.append(save(f'{dragon}-roar-{i+1}.wav',x))
rng=np.random.default_rng(1202);n=sr*6;t=np.arange(n)/sr;env=np.sin(np.pi*np.arange(n)/n)**1.3
rumble=signal.sosfilt(signal.butter(3,110,'low',fs=sr,output='sos'),rng.normal(0,1,n))+.16*np.sin(2*np.pi*48*t)
report.append(save('dragon-rumble.wav',rumble*env))
wind=signal.sosfilt(signal.butter(3,[90,1700],'bandpass',fs=sr,output='sos'),rng.normal(0,1,n))*env
report.append(save('dragon-wind.wav',wind))
(out/'design.json').write_text(json.dumps(dict(sampleRate=sr,sources=['Joseph SARDIN actual cat recordings','Ogrebane monster pack'],processing='pitch by resampling, layered recordings, EQ, soft saturation, four delay taps, peak -1.5 dBFS',clips=report),indent=2),encoding='utf-8')
resources=root/'Assets/Enemies/Resources/P12';resources.mkdir(parents=True,exist_ok=True)
shutil.copyfile(src/'boss-final_stand_phase_1.3_0.ogg',resources/'BossPhase1.ogg');shutil.copyfile(src/'boss-final_stand_phase_2.2.heavy_drums_0.ogg',resources/'BossPhase2.ogg')
print(json.dumps(report,indent=2))
