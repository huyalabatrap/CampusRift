using UnityEngine;
using CampusRift.Progression;
namespace CampusRift.AR
{
    [DisallowMultipleComponent]
    public sealed class ARModeSession : MonoBehaviour
    {
        public ARGameMode Mode {get;private set;}
        public int Stage {get;private set;}=1; public bool Daily {get;private set;}
        public bool SelectionConfirmed {get;private set;}
        public int Seed {get;private set;} public ARWaveDefinition[] Waves=>Stage>0?ARModeCatalog.Stages[Stage-1].waves:Mode.waves;
        public ARModeFeature Features=>Mode.features|(Stage>=3||Daily?ARModeFeature.Elites:0)|(Stage>=4||Daily?ARModeFeature.Events:0);
        public bool Has(ARModeFeature feature)=>(Features&feature)==feature;
        // Job3 sets the switch explicitly; actual attacks still require Floor + Dodge capability.
        public bool ActiveCombat; public bool PlayerAttacksEnabled=>ActiveCombat&&GetComponent<ARBattlefield>().placement.settings.Floor&&Has(ARModeFeature.Dodge);
        public ARBattleSummary Result {get;private set;}
        public int Ultimates {get;private set;}
        public bool KnowledgeEnabled=true;
        public bool PersistenceAllowed {get{ObservePolicy();return !runTransient&&!runDeveloper&&!GetComponent<ARBattlefield>().CheckLoad;}}
        bool runTransient,runDeveloper; int runStage,runSeed; ARGameMode runMode;
        void Awake(){Mode=ARModeCatalog.Training;}
        public void Choose(ARGameMode mode,int stage=0,bool daily=false)
        {
            if(mode==null||!mode.playable||GetComponent<ARBattlefield>().Root!=null)return;
            if(stage>0&&!ARProgression.StageOpen(ProfileService.Ensure().Data,stage))return;
            Stage=Mathf.Clamp(stage,0,10);Daily=daily;Mode=daily?ARModeCatalog.DailyMode(System.DateTime.UtcNow):Stage>0?ARModeCatalog.StageMode(Stage):mode;SelectionConfirmed=true;
            Seed=daily?ARModeCatalog.DailySeed(System.DateTime.UtcNow):Random.Range(0,int.MaxValue);
        }
        public void Reopen(){SelectionConfirmed=false;}
        public void BeginRun()
        {Result=null;Ultimates=0;runMode=Mode;runStage=Stage;runSeed=Seed;var p=ProfileService.Ensure();runTransient=p.Transient;runDeveloper=DevMode.Active;}
        public void ObservePolicy(){runTransient|=ProfileService.Instance!=null&&ProfileService.Instance.Transient;runDeveloper|=DevMode.Active;}
        public void RegisterUltimate(){if(!GetComponent<ARBattlefield>().Paused)Ultimates++;}
        public void Complete(bool won,int kills,int reactions,float elapsed,float loss)
        {
            if(Result!=null)return;ObservePolicy();
            Result=new ARBattleSummary{won=won,kills=kills,reactions=reactions,ultimates=Ultimates,seconds=elapsed,healthLostFraction=loss,seed=runSeed,stage=runStage,modeId=runMode.id};
            var rhythm=GetComponent<ARSealPractice>();
            if(runMode.Has(ARModeFeature.Rhythm)&&rhythm!=null){Result.score=rhythm.Points;Result.healthLostFraction=1-rhythm.Accuracy;}
            else Result.score=runMode.Score(kills,reactions,Ultimates,1-loss,won)+(GetComponent<ARSpaceModes>()?.Points??0);
            Result.stars=runMode.Stars(Result);
            if(!runTransient&&!runDeveloper&&!GetComponent<ARBattlefield>().CheckLoad)ARProgression.Record(ProfileService.Ensure(),Result,Result.stars*runMode.rewardPerStar,System.DateTime.UtcNow);
        }
    }
}
