#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
namespace CampusRift.SkyBeast
{
    public static class P21TimelineSetup
    {
        [MenuItem("Campus Rift/DEV/P21/Build authored timelines")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory("Assets/SkyBeast/Resources/P21/Timelines");
            foreach(int level in new[]{8,9,10})
            {
                string path="Assets/SkyBeast/Resources/P21/Timelines/HeavenSword"+level+".playable";
                var existing=AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
                if(existing!=null){foreach(var outputTrack in existing.GetOutputTracks())foreach(var clip in outputTrack.GetClips()){
                    var shot=clip.asset as HeavenSwordShot;if(shot==null)continue;
                    if(clip.displayName=="Rise from campus"){shot.to=new Vector3(level==9?-100:100,46,level==10?-320:-300);shot.beastWeight=.92f;shot.fov=70;}
                    if(clip.displayName=="Ten thousand swords"){shot.from=shot.to=new Vector3(level==9?-100:100,46,level==10?-320:-300);shot.beastWeight=.92f;shot.fov=70;}
                    if(clip.displayName=="Descent"){shot.from=new Vector3(level==9?-100:100,46,level==10?-320:-300);shot.fov=70;}
                    EditorUtility.SetDirty(shot);
                }continue;}
                var timeline=ScriptableObject.CreateInstance<TimelineAsset>();timeline.name="Heaven Sword · Level "+level;
                AssetDatabase.CreateAsset(timeline,path);var track=timeline.CreateTrack<HeavenSwordShotTrack>(null,"Campus / sword / beast");
                float side=level==9?-1:1;float shift=level==10?20:0;
                var points=new[]{new Vector3(side*85,42,-195-shift),new Vector3(side*100,46,-300-shift),new Vector3(side*100,46,-300-shift),new Vector3(side*70,64,-140-shift),new Vector3(side*70,64,-140-shift),new Vector3(side*70,48,-140-shift)};
                double[] edges={0,.22,.60,.735,.80,1};string[] names={"Rise from campus","Ten thousand swords","Descent","Golden impact","Fall / dawn"};
                for(int i=0;i<5;i++){var clip=track.CreateClip<HeavenSwordShot>();clip.displayName=names[i];clip.start=edges[i]*6;clip.duration=(edges[i+1]-edges[i])*6;
                    var shot=(HeavenSwordShot)clip.asset;shot.from=points[i];shot.to=points[i+1];shot.beastWeight=i<2?.92f:i>=3?.55f:.62f;shot.fov=i<3?70:level==10?65:62;shot.followFall=i==4;EditorUtility.SetDirty(shot);}
                EditorUtility.SetDirty(timeline);
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
    }
}
#endif
