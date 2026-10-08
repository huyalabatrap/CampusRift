using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
namespace CampusRift.AR
{
    // Native cubemap only in RAM, no camera readback, file or telemetry payload.
    public sealed class ARRoomProbe : MonoBehaviour
    {
        ARBattlefield field;ARAdaptiveQuality quality;AREnvironmentProbeManager manager;bool owns;
        float nextWindow,windowUntil,nextCopy;ReflectionProbe reflection;Cubemap snapshot;MaterialPropertyBlock block;
        ARSpaceModes space;CampusRift.SkyBeast.HeavenSwordHeroVisual boundSword;Renderer[] swordRenderers;
        public bool Active {get;private set;}
        void Start()
        {
            field=GetComponent<ARBattlefield>();space=GetComponent<ARSpaceModes>();quality=FindAnyObjectByType<ARAdaptiveQuality>();
            manager=field.placement.planes.GetComponent<AREnvironmentProbeManager>();
            if(manager==null){manager=field.placement.planes.gameObject.AddComponent<AREnvironmentProbeManager>();owns=true;}
            manager.enabled=false;block=new MaterialPropertyBlock();
        }
        void Update()
        {
            if(manager==null)return;
            bool want=quality!=null&&quality.RoomProbeAllowed&&(GetComponent<ARTechSettings>()?.RoomProbe??true)&&field.Root!=null&&!field.Paused;
            if(!want){manager.enabled=false;Active=false;if(reflection!=null)reflection.enabled=false;Bind(null);return;}
            if(Time.unscaledTime>=nextWindow){windowUntil=Time.unscaledTime+.25f;nextWindow=Time.unscaledTime+2;}
            // Poll the native subsystem briefly every two seconds; the owned GPU snapshot
            // remains available between polls, even while AR Foundation's probes are inactive.
            manager.enabled=Time.unscaledTime<windowUntil;Active=snapshot!=null;
            if(reflection!=null)reflection.enabled=Active;Bind(snapshot);
            if(!manager.enabled||manager.subsystem==null||!manager.subsystem.running)return;
            manager.automaticPlacementRequested=true;manager.environmentTextureHDRRequested=true;
            if(Time.unscaledTime<nextCopy)return;
            foreach(var probe in manager.trackables)
            {
                var source=probe.GetComponent<ReflectionProbe>();var cube=source!=null?source.customBakedTexture as Cubemap:null;
                if(cube==null)continue;
                // Copy GPU faces at a sparse cadence rather than recomputing room reflections per frame.
                if(SystemInfo.copyTextureSupport==CopyTextureSupport.None)return;
                if(snapshot==null||snapshot.width!=cube.width||snapshot.format!=cube.format)
                {if(snapshot!=null)Destroy(snapshot);snapshot=new Cubemap(cube.width,cube.format,cube.mipmapCount>1){name="AR room reflection RAM"};}
                Graphics.CopyTexture(cube,snapshot);
                if(reflection==null){var go=new GameObject("AR room sparse reflection");reflection=go.AddComponent<ReflectionProbe>();reflection.mode=ReflectionProbeMode.Custom;reflection.size=Vector3.one*20;reflection.importance=100;}
                reflection.transform.position=field.Root.position;reflection.customBakedTexture=snapshot;reflection.enabled=true;Active=true;nextCopy=nextWindow;Bind(snapshot);break;
            }
        }
        void Bind(Texture cube)
        {
            var sword=space!=null?space.SwordVisual:null;if(sword==null)return;
            if(boundSword!=sword){boundSword=sword;swordRenderers=sword.GetComponentsInChildren<Renderer>();}
            foreach(var r in swordRenderers)
            {if(r.sharedMaterial==null||!r.sharedMaterial.HasProperty("_ARRoomAmount"))continue;r.GetPropertyBlock(block);block.SetFloat("_ARRoomAmount",cube!=null?.35f:0);if(cube!=null)block.SetTexture("_ARRoomCube",cube);r.SetPropertyBlock(block);}
        }
        void OnDestroy(){Bind(null);if(owns&&manager!=null)Destroy(manager);if(reflection!=null)Destroy(reflection.gameObject);if(snapshot!=null)Destroy(snapshot);}
    }
}
