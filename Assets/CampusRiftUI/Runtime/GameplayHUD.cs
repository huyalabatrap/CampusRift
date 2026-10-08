using UnityEngine;
namespace CampusRift.UI
{
    public sealed class GameplayHUD : MonoBehaviour
    {
        public PlayerHealthUI Health;
        public BreakthroughProgressUI Breakthrough;
        public SkillBarUI Skills;
        public ObjectiveUI Objective;
        public MonsterWarningUI Warning;
        public PlayerEnergyUI Energy { get; private set; }
        void Start()
        {
            Energy=GetComponent<PlayerEnergyUI>() ?? gameObject.AddComponent<PlayerEnergyUI>();
            Energy.Bind(Health!=null && Health.Source!=null?Health.Source.GetComponent<CampusExplorer>():FindAnyObjectByType<CampusExplorer>(),
                transform.Find("Vitals"),Health!=null && Health.Fill!=null?Health.Fill.sprite:null);
        }
    }
}
