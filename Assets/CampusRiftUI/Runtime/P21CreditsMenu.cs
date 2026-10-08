using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CampusRift.UI
{
    // Use the same attribution catalog in the menu and after the finale.
    public sealed class P21CreditsMenu:MonoBehaviour
    {
        string[] pages;int page;TMP_Text body,counter;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Attach();}
        static void Loaded(Scene scene,LoadSceneMode mode)=>Attach();
        static void Attach(){foreach(var m in Object.FindObjectsByType<UIManager>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(m.Credits!=null&&m.Credits.GetComponent<P21CreditsMenu>()==null)m.Credits.gameObject.AddComponent<P21CreditsMenu>();}
        void Awake()
        {
            var text=Resources.Load<TextAsset>("P21/Credits");if(text==null)return;pages=text.text.Replace("\r\n","\n").Split(new[]{"\n---PAGE---\n"},System.StringSplitOptions.None);
            foreach(Transform child in transform)child.gameObject.SetActive(false);
            var card=ComboUIFactory.Rect("P21 credits catalog",transform,new Vector2(1580,900),Vector2.zero);ComicTheme.Frame(card.gameObject);
            var title=ComboUIFactory.Text("Credits title",card,new Vector2(1400,65),new Vector2(0,382),38);title.color=ComicTheme.Gold;title.text=LevelHUD.Vietnamese?"DANH SÁCH THỰC HIỆN":"CREDITS";
            body=ComboUIFactory.Text("Credits content",card,new Vector2(1470,650),new Vector2(0,0),22);body.enableAutoSizing=false;body.fontStyle=FontStyles.Normal;body.fontSharedMaterial=ComicTheme.Font.material;body.textWrappingMode=TextWrappingModes.Normal;body.alignment=TextAlignmentOptions.TopLeft;
            counter=ComboUIFactory.Text("Page",card,new Vector2(400,45),new Vector2(0,-335),24);
            Button(card,"Previous",-620,()=>Show(page-1),LevelHUD.Vietnamese?"TRƯỚC":"PREVIOUS");Button(card,"Next",620,()=>Show(page+1),LevelHUD.Vietnamese?"TIẾP":"NEXT");
            Button(card,"Back",0,()=>UIStateManager.Instance?.Back(),LevelHUD.Vietnamese?"QUAY LẠI":"BACK",-405);GetComponent<PanelTransition>().FirstSelection=card.Find("Back").GetComponent<Button>();Show(0);
        }
        void Button(Transform parent,string name,float x,UnityEngine.Events.UnityAction action,string label,float y=-365){var r=ComboUIFactory.Rect(name,parent,new Vector2(250,68),new Vector2(x,y));ComicTheme.Frame(r.gameObject,"button-red",true);r.gameObject.AddComponent<Button>().onClick.AddListener(action);ComboUIFactory.Text(name+" label",r,new Vector2(230,58),Vector2.zero,24).text=label;}
        public void Show(int value){if(pages==null)return;page=(value%pages.Length+pages.Length)%pages.Length;body.text=pages[page];counter.text=(page+1)+" / "+pages.Length;}
    }
}
