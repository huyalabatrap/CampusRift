#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P11_BENCH)
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CampusRift.Combat
{
    // Benchmark-only offscreen rendering: hidden Windows players skip the ordinary display loop.
    // Render the complete URP camera into a real GPU target and wait for its GPU fence each frame.
    [DefaultExecutionOrder(10000)]
    public sealed class ReactionGpuBenchRender : MonoBehaviour
    {
        public static ReactionGpuBenchRender Instance {get;private set;}
        public int RenderedFrames {get;private set;}
        public string Error {get;private set;}="";
        Camera camera;RenderTexture target;
        readonly HashSet<Canvas> converted=new HashSet<Canvas>();
        readonly RenderPipeline.StandardRequest request=new RenderPipeline.StandardRequest();
        public void Initialize(Camera value)
        {
            Instance=this;camera=value;
            target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf){name="P11 PC HDR GPU benchmark",antiAliasing=1};target.Create();request.destination=target;
        }
        void LateUpdate()
        {
            if(camera==null||Error.Length>0)return;
            try
            {
                if(RenderedFrames<120&&RenderedFrames%10==0)
                    foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                        if(canvas.renderMode==RenderMode.ScreenSpaceOverlay&&converted.Add(canvas))
                        {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=camera.nearClipPlane+.05f;canvas.overrideSorting=true;}
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,request);
                if(!SystemInfo.supportsGraphicsFence||!SystemInfo.supportsAsyncCompute)throw new InvalidOperationException("GPU fence polling unsupported; use D3D12 or refuse CPU-only timings");
                var fence=Graphics.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation,SynchronisationStageFlags.AllGPUOperations);
                float deadline=Time.realtimeSinceStartup+2;
                while(!fence.passed){if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("GPU render fence did not complete");System.Threading.Thread.SpinWait(64);}
                RenderedFrames++;
            }
            catch(Exception ex){Error=ex.ToString();}
        }
        public void Snapshot(string path)
        {
            var old=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,pixels.EncodeToPNG());Destroy(pixels);RenderTexture.active=old;
        }
        void OnDestroy(){if(Instance==this)Instance=null;if(target!=null){target.Release();Destroy(target);}}
    }
}
#endif
