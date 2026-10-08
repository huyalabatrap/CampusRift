#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using CampusRift.UI;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.Enemies;
using UnityEngine;

namespace CampusRift.Controls
{
    // Temporary review fixture, never saved to a scene. Both phases use identical gameplay framing.
    public sealed class LookCapture : MonoBehaviour
    {
        public string phase="before";
        SkillSet1TestWorld world;
        public static void Image(string path)
        {
            var camera=Camera.main;var previous=camera.targetTexture;
            var overlays=Object.FindObjectsByType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
            var cameras=overlays.Select(c=>c.worldCamera).ToArray();var distances=overlays.Select(c=>c.planeDistance).ToArray();
            var rt=RenderTexture.GetTemporary(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;
            try
            {
                foreach(var canvas in overlays){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=.6f;}
                Canvas.ForceUpdateCanvases();camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                var texture=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Destroy(texture);
            }
            finally
            {
                camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);
                for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=cameras[i];overlays[i].planeDistance=distances[i];}
                Canvas.ForceUpdateCanvases();
            }
        }
        IEnumerator Frames(int n=5){for(int i=0;i<n;i++)yield return null;}
        IEnumerator Shot(string name,Vector3 point,float yaw,bool night)
        {
            world.PlacePlayer(point);
            var forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
            world.player.transform.rotation=Quaternion.LookRotation(forward);
            world.camera.transform.position=point-forward*3.4f+Vector3.up*1.9f;
            world.camera.transform.rotation=Quaternion.Euler(14,yaw,0);world.camera.fieldOfView=60;
            foreach(var v in world.victims)v.gameObject.SetActive(false);
            var victim=world.victims[0];victim.gameObject.SetActive(true);
            victim.GetComponent<MinionMotor>().Place(point+forward*4.5f+Vector3.Cross(Vector3.up,forward)*1.7f);
            victim.GetComponent<MinionMotor>().Stop();
            Levels.LevelSession.Select(night?3:1);
            SettingsManager.Instance.Sky.SetPreset(Levels.LevelCatalog.Instance.Get(night?3:1).sky);
            SettingsManager.Instance.Sky.SetBrightness(1);
            ComicRendering.PreviewEnabled=phase=="before";
            if(phase=="before")
            {
                var sky=Resources.Load<Material>("Comic/sky-"+(night?"night":"dusk"));
                if(sky!=null){RenderSettings.skybox.CopyPropertiesFromMaterial(sky);RenderSettings.skybox.SetFloat("_Exposure",night?1.3f:1.15f);}
            }
            yield return Frames(8);
            Image("task/look/screens/"+phase+"/"+name+".png");yield return Frames();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory("task/look/screens/"+phase);
            world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();
            UIValidation.SetResolution(1920,1080);world.player.enabled=false;
            yield return new WaitForSecondsRealtime(1.3f);Time.timeScale=0;
            yield return Shot("courtyard-day",new Vector3(10,.13f,-5),25,false);
            yield return Shot("facade-a-day",new Vector3(35,.13f,48),180,false);
            yield return Shot("facade-b-day",new Vector3(24,.13f,-14),180,false);
            yield return Shot("facade-c-day",new Vector3(-17,.13f,14),270,false);
            yield return Shot("corridor-night",new Vector3(31,.13f,33),90,true);
            yield return Shot("stairs-night",new Vector3(47,.13f,24),0,true);
            Time.timeScale=1;world.player.enabled=true;
            if(phase=="after")
            {
                world.Mode(true);world.Arrange();world.Lighting(false);
                var loadout=world.player.GetComponent<SkillLoadout>();
                string[] ids={"phat-no-hoa-lien","than-kiem-ngu-loi","han-bang-phong-an","kim-chung-trao"};
                for(int i=0;i<4;i++)loadout.Equip(i,ids[i]);
                world.player.GetComponent<Combat.SpiritPower>().Refill();
                UIValidation.SetResolution(1920,1080);yield return Frames(8);
                Image("task/look/screens/after/mobile-1920.png");
                var lotus=(FireLotusRuntime)loadout.Get(0);
                bool cast=lotus.CastAt(world.origin+Vector3.right*8);
                float until=Time.realtimeSinceStartup+30;
                while(lotus.GetState()!=SkillState.Cooldown&&Time.realtimeSinceStartup<until)yield return null;
                UIValidation.SetResolution(2340,1080);yield return Frames(8);
                Image("task/look/screens/after/mobile-2340.png");
                File.WriteAllText("task/look/cooldown-capture-DONE.txt","Cast="+cast+"; State="+lotus.GetState()+"; Seconds="+lotus.CooldownRemaining);
                File.WriteAllText("task/look/cooldown-capture-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("mobile-real-cooldown-2340"),true));
            }
            world.End();
            ComicRendering.PreviewEnabled=null;
            File.WriteAllText("task/look/"+phase+"-capture-DONE.txt","6 fixed camera captures; review fixture, not gameplay balance");
            Destroy(gameObject);
        }
    }
}
#endif
