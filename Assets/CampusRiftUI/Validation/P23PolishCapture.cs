#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Combat;
using CampusRift.Learning;
using CampusRift.Levels;
using CampusRift.Progression;
namespace CampusRift.UI
{
    // One close photo of the authored stele on the real indoor NavMesh; HUD hidden for this photo only.
    public sealed class P23PolishCapture:MonoBehaviour
    {
        IEnumerator Start()
        {
            TutorialDirector.Suppress=true;Application.runInBackground=true;UIStateManager.Instance.EnterScene(true);
            var profile=ProfileService.Instance;profile.UseTransient(new ProfileData());
            var engine=LearningService.Instance.Engine;var temporary=new LearningEngine(engine.Catalog,new ProfileLearningStore(profile),23,profile.Cultivation);
            temporary.Progress.Lesson(engine.Catalog.courses[0].lessons[0].id).completed=true;LearningService.Instance.EditorUseEngine(temporary);
            var director=LevelDirector.Ensure();director.Begin(LevelCatalog.Instance.Get(8));director.enabled=false;yield return null;
            var shrine=FindObjectsByType<LearningShrine>().OrderBy(s=>Mathf.Abs(s.transform.position.x)).First();var player=FindAnyObjectByType<CampusExplorer>();player.enabled=false;var camera=player.followCamera;player.followCamera=null;
            Vector3 approach=Vector3.zero;bool found=false;
            for(int i=0;i<16;i++)
            {
                var from=shrine.transform.position+Quaternion.Euler(0,i*22.5f,0)*Vector3.back*1.8f;
                if(!NavMesh.SamplePosition(from,out var hit,.5f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-from.y)>1||!CombatLine.Clear(hit.position+Vector3.up,shrine.transform.position+Vector3.up,player.transform))continue;
                approach=hit.position;found=true;break;
            }
            if(!found)throw new Exception("No clear shrine approach");
            var controller=player.GetComponent<CharacterController>();controller.enabled=false;player.transform.position=approach;controller.enabled=true;Physics.SyncTransforms();
            var target=shrine.transform.TransformPoint(new Vector3(0,1.15f,-.23f));bool framed=false;
            for(int i=0;i<16;i++)
            {
                float angle=(180+(i%2==0?1:-1)*((i+1)/2)*22.5f)*Mathf.Deg2Rad;
                var eye=shrine.transform.TransformPoint(new Vector3(Mathf.Sin(angle)*2.35f,1.5f,Mathf.Cos(angle)*2.35f));
                if(Physics.CheckSphere(eye,.18f,CombatLine.SolidMask,QueryTriggerInteraction.Ignore)||!CombatLine.Clear(eye,target,player.transform))continue;
                camera.transform.position=eye;camera.transform.LookAt(target);camera.fieldOfView=58;framed=true;break;
            }
            if(!framed)throw new Exception("No non-intersecting camera for shrine");
            foreach(var r in player.GetComponentsInChildren<Renderer>())r.enabled=false;
            var passiveSwords=FindObjectsByType<FlyingSword>();foreach(var sword in passiveSwords)sword.gameObject.SetActive(false);
            var canvases=FindObjectsByType<Canvas>().Where(c=>c.isActiveAndEnabled).ToArray();foreach(var canvas in canvases)canvas.gameObject.SetActive(false);
            yield return new WaitForSecondsRealtime(.35f);yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes("task/p23/screens/linh-bia-after.png",image.EncodeToPNG());Destroy(image);
            File.WriteAllText("task/p23/polish-photo.json",JsonUtility.ToJson(new Photo{shrine=shrine.transform.position,camera=camera.transform.position,approach=approach,ready=shrine.GetComponentInChildren<Light>().enabled},true));
            foreach(var canvas in canvases)canvas.gameObject.SetActive(true);foreach(var sword in passiveSwords)sword.gameObject.SetActive(true);player.followCamera=camera;director.End();LearningService.Instance.EditorUseEngine(engine);profile.EndTransient();
            File.WriteAllText("task/p23/polish-photo-DONE.txt","Captured stele on existing indoor NavMesh; HUD/avatar/passive swords hidden only during photo.");Destroy(gameObject);
        }
        [Serializable]sealed class Photo{public Vector3 shrine,camera,approach;public bool ready;}
    }
}
#endif
