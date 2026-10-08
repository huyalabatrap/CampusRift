#if UNITY_EDITOR
using System.Collections;
using System.IO;
using CampusRift.Skills;
using CampusRift.Controls;
using CampusRift.UI;
using UnityEngine;
namespace CampusRift.SkyBeast
{
    public sealed class P13LookCapture:MonoBehaviour
    {
        IEnumerator Start()
        {
            Directory.CreateDirectory("task/look/screens/fix");
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();world.player.enabled=false;
            UIValidation.SetResolution(1920,1080);
            yield return new WaitForSecondsRealtime(1.2f);
            world.PlacePlayer(new Vector3(10,.13f,-5));world.camera.transform.position=new Vector3(10,.13f,-5)-Quaternion.Euler(0,25,0)*Vector3.forward*3.4f+Vector3.up*1.9f;
            world.camera.transform.rotation=Quaternion.Euler(14,25,0);world.camera.fieldOfView=60;
            world.Lighting(false);SettingsManager.Instance.Sky.SetPreset(SkyPreset.Default);SettingsManager.Instance.Sky.SetBrightness(1);
            foreach(var v in world.victims)v.gameObject.SetActive(false);
            var victim=world.victims[0];victim.gameObject.SetActive(true);victim.GetComponent<Enemies.MinionMotor>().Place(new Vector3(13,.13f,0));victim.GetComponent<Enemies.MinionMotor>().Stop();
            for(int i=0;i<10;i++)yield return null;
            LookCapture.Image("task/look/screens/fix/courtyard-day.png");
            world.Mode(true);UIValidation.SetResolution(2340,1080);
            for(int i=0;i<10;i++)yield return null;
            LookCapture.Image("task/look/screens/fix/mobile.png");
            File.WriteAllText("task/p13/look-text-audit.json",JsonUtility.ToJson(ComicTextAudit.Scan("P13 LOOK mobile"),true));
            world.player.enabled=true;world.End();File.WriteAllText("task/p13/look-capture-DONE.txt","2 real camera captures");Destroy(gameObject);
        }
    }
}
#endif
