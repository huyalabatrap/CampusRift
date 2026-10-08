import requests,time,concurrent.futures
urls=['https://opengameart.org/sites/default/files/demon.rar','https://lpc.opengameart.org/sites/default/files/demon.rar']
def f(u):
    t=time.time()
    try:
        r=requests.get(u,headers={'Range':'bytes=0-65535'},timeout=20)
        print(u,r.status_code,len(r.content),round(time.time()-t,2),flush=True)
    except Exception as e:print(u,type(e).__name__,flush=True)
with concurrent.futures.ThreadPoolExecutor() as pool:list(pool.map(f,urls))
