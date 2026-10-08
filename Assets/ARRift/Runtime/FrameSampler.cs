using UnityEngine;
using Unity.Collections;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace CampusRift.AR
{
    // 512 is about twice palm-detector input size: leave detail for the landmark crop.
    public sealed class FrameSampler : MonoBehaviour
    {
        public ARCameraManager cameraManager;public GestureRecognizerBridge bridge;
        public int TargetHz {get;private set;}=20;public int LongEdge {get;private set;}=512;public int Submitted {get;private set;}
        public Vector2Int InputSize {get;private set;}
        public readonly System.Collections.Generic.Dictionary<string,int> Drops=new System.Collections.Generic.Dictionary<string,int>();
        void Drop(string reason){if(!Drops.ContainsKey(reason))Drops[reason]=0;Drops[reason]++;}
        public bool PreviewRequested;public Texture2D Preview {get;private set;}public GestureFrame PreviewFrame {get;private set;}
        sealed class Buffer {public NativeArray<byte> pixels;public Vector2Int size;public int rotation;public long timestamp;public GestureFrame metadata;public Matrix4x4 display;public bool hasDisplay;}
        readonly Buffer[] buffers={new Buffer(),new Buffer()};int writing,pending=-1;
        XRCpuImage.AsyncConversion conversion;bool converting;long nextId,lastTimestamp;readonly ARFrameDeadline deadline=new ARFrameDeadline();bool sampling=true;Matrix4x4 display;bool hasDisplay;int sensorOrientation=90;ARAdaptiveQuality adaptive;
        void OnEnable(){if(cameraManager!=null)cameraManager.frameReceived+=Frame;}
        void Frame(ARCameraFrameEventArgs e){if(e.displayMatrix.HasValue){display=e.displayMatrix.Value;hasDisplay=true;}}
        public static int RotationFor(ScreenOrientation o,int sensor=90){int d=o==ScreenOrientation.LandscapeLeft?90:o==ScreenOrientation.PortraitUpsideDown?180:o==ScreenOrientation.LandscapeRight?270:0;return (sensor-d+360)%360;}
        void Start()
        {
            adaptive=GetComponent<ARAdaptiveQuality>();
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var a=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var cameras=a.Call<AndroidJavaObject>("getSystemService","camera"))using(var keys=new AndroidJavaClass("android.hardware.camera2.CameraCharacteristics")){foreach(var id in cameras.Call<string[]>("getCameraIdList"))using(var c=cameras.Call<AndroidJavaObject>("getCameraCharacteristics",id))using(var facing=c.Call<AndroidJavaObject>("get",keys.GetStatic<AndroidJavaObject>("LENS_FACING"))){if(facing.Call<int>("intValue")!=1)continue;using(var orientation=c.Call<AndroidJavaObject>("get",keys.GetStatic<AndroidJavaObject>("SENSOR_ORIENTATION")))sensorOrientation=orientation.Call<int>("intValue");break;}}}catch{sensorOrientation=90;}
#endif
        }
        void PreviewPixels(Buffer b){if(!PreviewRequested)return;if(Preview==null||Preview.width!=b.size.x||Preview.height!=b.size.y){if(Preview!=null)Destroy(Preview);Preview=new Texture2D(b.size.x,b.size.y,TextureFormat.RGBA32,false);}Preview.LoadRawTextureData(b.pixels);Preview.Apply(false,false);PreviewFrame=new GestureFrame{width=b.size.x,height=b.size.y,rotation=b.rotation,displayMatrix=b.display,hasDisplayMatrix=b.hasDisplay};}
        void Update()
        {
            bool low=adaptive!=null&&adaptive.Reduced;TargetHz=low?12:20;LongEdge=low?384:512;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(converting&&conversion.status!=XRCpuImage.AsyncConversionStatus.Processing)
            {
                try{if(conversion.status!=XRCpuImage.AsyncConversionStatus.Ready)Drop("convert");if(conversion.status==XRCpuImage.AsyncConversionStatus.Ready){var data=conversion.GetData<byte>();var b=buffers[writing];if(!b.pixels.IsCreated||b.pixels.Length!=data.Length){if(b.pixels.IsCreated)b.pixels.Dispose();b.pixels=new NativeArray<byte>(data.Length,Allocator.Persistent);}data.CopyTo(b.pixels);b.metadata.convertReadyMs=GestureRecognizerBridge.Now;if(sampling&&b.metadata.epoch==bridge.Epoch){pending=writing;PreviewPixels(b);}else Drop("pause-discard");}}
                finally{conversion.Dispose();converting=false;}
            }
            if(!sampling){pending=-1;return;}
            if(pending>=0&&bridge!=null&&!bridge.Busy){var b=buffers[pending];if(bridge.Submit(b.pixels,b.metadata)){Submitted++;pending=-1;}}
            // Kotlin copies to its own direct buffer synchronously. Only one pending frame;
            // overwrite that pending slot with a newer conversion while inference continues.
            if(converting||bridge==null||cameraManager==null||!deadline.Due(Time.realtimeSinceStartupAsDouble,TargetHz))return;
            if(!cameraManager.TryAcquireLatestCpuImage(out var image)){Drop("acquire");return;}
            try{if(!deadline.Unique(image.timestamp)){Drop("duplicate");return;}if(pending>=0)Drop("pending-replaced");writing=pending>=0?pending:1-writing;pending=-1;var b=buffers[writing];float scale=Mathf.Min(1,(float)LongEdge/Mathf.Max(image.width,image.height));b.size=new Vector2Int(Mathf.Max(1,Mathf.RoundToInt(image.width*scale)),Mathf.Max(1,Mathf.RoundToInt(image.height*scale)));InputSize=b.size;b.rotation=RotationFor(Screen.orientation,sensorOrientation);b.timestamp=System.Math.Max(lastTimestamp+1,(long)(image.timestamp*1000));lastTimestamp=b.timestamp;b.display=display;b.hasDisplay=hasDisplay;b.metadata=new GestureFrame{epoch=bridge.Epoch,frameId=++nextId,timestampMs=b.timestamp,sensorTimestamp=image.timestamp,acquireMs=GestureRecognizerBridge.Now,width=b.size.x,height=b.size.y,rotation=b.rotation,displayMatrix=b.display,hasDisplayMatrix=b.hasDisplay,hasCameraPose=true,cameraPosition=cameraManager.transform.position,cameraRotation=cameraManager.transform.rotation};var parameters=new XRCpuImage.ConversionParams(image,TextureFormat.RGBA32,XRCpuImage.Transformation.None){outputDimensions=b.size};conversion=image.ConvertAsync(parameters);converting=true;}
            finally{image.Dispose();}
#endif
        }
        public void SetSamplingActive(bool value){if(sampling==value)return;sampling=value;pending=-1;deadline.Reset(Time.realtimeSinceStartupAsDouble);bridge?.SetSamplingActive(value);}
        void OnDisable(){if(cameraManager!=null)cameraManager.frameReceived-=Frame;if(converting){conversion.Dispose();converting=false;}pending=-1;}
        void OnDestroy(){if(Preview!=null)Destroy(Preview);foreach(var b in buffers)if(b.pixels.IsCreated)b.pixels.Dispose();}
    }
}
