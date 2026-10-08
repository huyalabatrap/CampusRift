#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using CampusRift.Progression;
using CampusRift.Localization;
using CampusRift.Controls;
namespace CampusRift.UI
{
    public sealed class P20ShopPlayTest:MonoBehaviour
    {
        [Serializable] public sealed class Report{public List<string> passed=new List<string>(),failed=new List<string>();}
        readonly Report report=new Report();ProfileService profile;GameSettings original;
        const string Output="task/p20/";
        void Check(bool ok,string label){(ok?report.passed:report.failed).Add(label);File.WriteAllText(Output+"shop-smoke.json",JsonUtility.ToJson(report,true));}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);profile=ProfileService.Instance;original=SettingsManager.Instance.Current.Copy();profile.UseTransient(new ProfileData{tutorial=new TutorialProgress{skipHub=true,skipCombat=true,skipFire=true}});profile.Cultivation.SetState(Realm.HoaThan,1,0);profile.Wallet.Earn(50000,"QA");
            var run=Run();while(true){bool next=false;object current=null;try{next=run.MoveNext();if(next)current=run.Current;}catch(Exception e){Check(false,e.ToString());break;}if(!next)break;yield return current;}
            profile.EndTransient();SettingsManager.Instance.Apply(original,false);File.WriteAllText(Output+"SHOP-DONE.txt",report.passed.Count+" passed; "+report.failed.Count+" failed");Destroy(gameObject);
        }
        static void Set(HubUI hub,string field,object value)=>typeof(HubUI).GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(hub,value);
        IEnumerator Run()
        {
            GameSceneManager.Instance.LoadMainMenu();float end=Time.realtimeSinceStartup+15;while((GameSceneManager.Instance.IsLoading||HubUI.Instance==null)&&Time.realtimeSinceStartup<end)yield return null;
            UIStateManager.Instance.OpenHub();yield return null;var hub=HubUI.Instance;
            var ids=new[]{"thanh-tam-dan","cuu-chuyen-hoan-hon-dan","tu-khi-dan","bao-kich-dan","ngu-hanh-phu","tam-yeu-phu","linh-luc-ho-lo","ngoc-boi-ngu-hanh"};
            for(int lang=0;lang<2;lang++)for(int mobile=0;mobile<2;mobile++)
            {
                var s=original.Copy();s.ControlMode=mobile==1?ControlMode.Mobile:ControlMode.PC;s.Language=lang==0?GameLanguage.Vietnamese:GameLanguage.English;SettingsManager.Instance.Apply(s,false);UIValidation.SetResolution(1920,1080);
                foreach(var id in ids)
                {
                    var item=ItemCatalog.Instance.Item(id);if(item!=null){Set(hub,"shopCategory",item.kind==ItemKind.Heal||item.kind==ItemKind.Revive?0:item.kind==ItemKind.Buff?1:3);Set(hub,"shopItem",item);}else {Set(hub,"shopCategory",2);Set(hub,"shopArtifact",ItemCatalog.Instance.Artifact(id));}
                    hub.Select(HubUI.Tab.Shop);yield return new WaitForSecondsRealtime(.1f);yield return new WaitForEndOfFrame();
                    string name="shop-"+id+(lang==0?"-vi":"-en")+(mobile==0?"-pc":"-mobile");var a=ComicTextAudit.Scan(name);ComicTextAudit.Save(a,Output+"screens/"+name+"-audit.json");Check(a.issues.Count==0,name+" ComicTextAudit0issues");
                    Check(hub.ContentRect.GetComponentsInChildren<Button>().Where(b=>b.isActiveAndEnabled).All(b=>b.GetComponent<RectTransform>().rect.height>=68),name+" targets>=68");
                    if(lang==0&&mobile==0&&(id=="ngu-hanh-phu"||id=="linh-luc-ho-lo")){var frame=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Output+"screens/"+name+".png",frame.EncodeToPNG());Destroy(frame);}
                    if(id=="ngu-hanh-phu")
                    {
                        hub.ContentRect.GetComponentsInChildren<Button>().First(b=>b.name=="Button Hoa").onClick.Invoke();yield return null;int cash=profile.Wallet.Balance,owned=profile.Inventory.Count("ngu-hanh-phu-hoa");
                        hub.ContentRect.GetComponentsInChildren<Button>().First(b=>b.name=="Button "+(lang==0?"TRAO ĐỔI":"EXCHANGE")).onClick.Invoke();yield return null;Check(profile.Wallet.Balance==cash-100&&profile.Inventory.Count("ngu-hanh-phu-hoa")==owned+1,name+" real element selection and purchase");
                    }
                }
            }
        }
    }
}
#endif
