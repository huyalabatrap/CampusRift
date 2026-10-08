#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.SkyBeast;
using CampusRift.UI;
namespace CampusRift.Validation
{
 public sealed class P12AudioEndingPlayTest:P12PlayTest
 {
  public bool CompletionOnly;
  GameObject ambience,combat;
  protected override IEnumerator Run(){Begin();world.Mode(false);
   if(CompletionOnly){
    CampusRift.Progression.ProfileService.Instance.Data.longVuongRevealSeen=true;
    float beganFocused=Time.realtimeSinceStartup;yield return SkyBeastEnding.Play();
    Check(!SkyBeastEnding.Playing&&!SkyBeastEnding.Skipped&&Time.realtimeSinceStartup-beganFocused>=3.4f&&Time.realtimeSinceStartup-beganFocused<3.8f,"unskipped repeated P21 reveal runs3.5 real seconds and restores HUD");yield break;
   }
   var mixer=SettingsManager.Instance.Mixer;float music=0,sfx=0;mixer.GetFloat("MusicVolume",out music);bool hasSfx=mixer.GetFloat("SFXVolume",out sfx);
   ambience=new GameObject("ambient P12 QA");var a=ambience.AddComponent<AudioSource>();a.volume=.8f;
   combat=new GameObject("combat P12 QA");var b=combat.AddComponent<AudioSource>();b.volume=.7f;
   SkyBeastAudioDuck.Ensure().Duck(.4f);yield return new WaitForSecondsRealtime(.25f);
   mixer.GetFloat("MusicVolume",out var low);mixer.GetFloat("SFXVolume",out var liveSfx);
   Check(low<music-8&&a.volume<.4f,"dragon ducks real Music mixer and ambient source");Check(Mathf.Approximately(b.volume,.7f)&&(!hasSfx||Mathf.Approximately(sfx,liveSfx)),"combat SFX remain unchanged during roar duck");
   yield return new WaitForSecondsRealtime(1.1f);mixer.GetFloat("MusicVolume",out var restored);Check(Mathf.Abs(restored-music)<.1f&&Mathf.Approximately(a.volume,.8f),"music/ambient restored after roar");
   world.Look(90,14);yield return null;bool explorer=world.player.enabled;var play=StartCoroutine(SkyBeastEnding.Play());yield return new WaitForSecondsRealtime(.8f);yield return new WaitForEndOfFrame();
   Check(SkyBeastEnding.Playing&&FindObjectsByType<Canvas>().Where(c=>c.enabled).All(c=>c.name=="Ending skip"),"cinematic hides even self-updating HUD canvases");
   var skip=FindObjectsByType<Button>().FirstOrDefault(x=>x.transform.parent!=null&&x.transform.parent.name=="Ending skip");Check(skip!=null,"ending has a real clickable skip button");float began=Time.realtimeSinceStartup;skip?.onClick.Invoke();yield return null;yield return null;
   Check(SkyBeastEnding.Skipped&&!SkyBeastEnding.Playing&&Time.realtimeSinceStartup-began<.3f&&world.player.enabled==explorer,"touch/button skip immediately restores player and camera flow");
   began=Time.realtimeSinceStartup;yield return SkyBeastEnding.Play();Measure("Repeated reveal elapsed real seconds="+(Time.realtimeSinceStartup-began).ToString("F4"));Check(!SkyBeastEnding.Playing&&!SkyBeastEnding.Skipped&&Time.realtimeSinceStartup-began>=3.4f&&Time.realtimeSinceStartup-began<3.8f,"unskipped repeated P21 reveal runs3.5 real seconds and restores HUD");
  }
  protected override void Cleanup(){SkyBeastEnding.Cancel();if(ambience!=null)Destroy(ambience);if(combat!=null)Destroy(combat);base.Cleanup();}
 }
}
#endif
