using System;
using UnityEngine;
namespace CampusRift.AR
{
    [Flags] public enum ARModeFeature { None=0, Elites=1, Events=2, Dodge=4, Sequences=8, SecondaryRifts=16, Dragon=32, Quiz=64, HandPhysics=128, Rhythm=256 }
    public enum ARWinRule { ClearWaves, CloseRifts, DefeatBoss, CompleteRhythm }
    public enum ARThirdStar { Reactions, Ultimates, Score }
    public enum ARBattleEventKind { ShrineRescue, PackLeader, MeteorRain }
    public enum AREliteKind { None, MetalArmor, Invisible, Split, Haste }
    [Serializable] public sealed class ARWaveDefinition
    {
        public int count=3; public float spawnInterval=.85f, breakSeconds=3; public int eliteEvery;
        public float minimumSeconds;
        public ARBattleEventKind[] events=Array.Empty<ARBattleEventKind>();
    }
    [CreateAssetMenu(menuName="Campus Rift/AR/Game Mode")]
    public sealed class ARGameMode : ScriptableObject
    {
        public string id, nameVN, nameEN, descriptionVN, descriptionEN, durationVN, durationEN;
        public int order; public bool playable; public ARWinRule winRule; public bool loseOnShrine=true;
        public ARModeFeature features; public ARWaveDefinition[] waves=Array.Empty<ARWaveDefinition>();
        public float timeLimit, shrineHealth=300, maximumHealthLoss=.3f;
        public ARThirdStar thirdStar; public int thirdStarTarget=2, pointsPerKill=100, pointsPerReaction=150, pointsPerUltimate=300;
        public int victoryPoints=1000, healthPoints=500, rewardPerStar=5;
        public bool Has(ARModeFeature feature)=>(features&feature)==feature;
        public string Title(bool vn)=>vn?nameVN:nameEN;
        public string StarHint(bool vn)
        {if(Has(ARModeFeature.Rhythm))return (vn?"Sao: hoàn thành / đúng ≥70% / ":"Stars: complete / ≥70% matched / ")+thirdStarTarget+(vn?" điểm":" points");string metric=thirdStar==ARThirdStar.Reactions?(vn?"combo":"reactions"):thirdStar==ARThirdStar.Ultimates?(vn?"tuyệt kỹ":"ultimates"):(vn?"điểm":"points");return (vn?"Sao: thắng / mất ≤":"Stars: win / lose ≤")+Mathf.RoundToInt(maximumHealthLoss*100)+"% HP / "+thirdStarTarget+" "+metric;}
        public int Stars(ARBattleSummary s)
        {
            if(!s.won)return 0;
            int n=1;if(s.healthLostFraction<=maximumHealthLoss)n++;
            int value=thirdStar==ARThirdStar.Reactions?s.reactions:thirdStar==ARThirdStar.Ultimates?s.ultimates:s.score;
            if(value>=thirdStarTarget)n++;return n;
        }
        public int Score(int kills,int reactions,int ultimates,float hp,bool won)=>Mathf.Max(0,kills*pointsPerKill+reactions*pointsPerReaction+ultimates*pointsPerUltimate+(won?victoryPoints:0)+Mathf.RoundToInt(Mathf.Clamp01(hp)*healthPoints));
    }
    public sealed class ARBattleSummary
    {
        public bool won; public int score, stars, kills, reactions, ultimates, seed, stage, reward;
        public float seconds, healthLostFraction; public string modeId; public bool saved;
    }
}
