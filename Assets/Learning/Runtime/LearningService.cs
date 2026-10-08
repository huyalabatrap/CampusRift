using System.IO;
using UnityEngine;
using CampusRift.UI;
namespace CampusRift.Learning
{
    [DefaultExecutionOrder(-500)]
    public sealed class LearningService : MonoBehaviour
    {
        public static LearningService Instance {get;private set;}
        public LearningEngine Engine {get;private set;}
        // The pre-V2 save; kept only so it can be archived (ProfileMigration) and by old harnesses.
        public static string SavePath => Path.Combine(Application.persistentDataPath,"learning-v1.json");
#if UNITY_EDITOR
        // Acceptance harnesses swap stores only between scenes, keeping the user's save untouched.
        public void EditorUseEngine(LearningEngine engine){Engine=engine;}
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics(){Instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if(Instance!=null)return;
            var catalog=Resources.Load<LearningCatalog>("LearningCatalog");
            var profile=Progression.ProfileService.Ensure();
            if(catalog==null || profile==null)return;
            var service=new GameObject("Learning Progression").AddComponent<LearningService>();
            service.Engine=new LearningEngine(catalog,new Progression.ProfileLearningStore(profile),null,profile.Cultivation);
            service.Engine.Economy=new StudyEconomy(profile.Wallet);
        }
        void Awake(){Instance=this;DontDestroyOnLoad(gameObject);}
        void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
