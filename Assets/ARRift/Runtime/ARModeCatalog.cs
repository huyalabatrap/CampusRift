using System;
using System.Linq;
using UnityEngine;
namespace CampusRift.AR
{
    [Serializable] public sealed class ARStageDefinition
    {
        public int number; public string modeId="training"; public string nameVN,nameEN; public ARWaveDefinition[] waves;
        public string Title(bool vn)=>vn?nameVN:nameEN;
    }
    public static class ARModeCatalog
    {
        static ARGameMode[] all;
        public static ARGameMode[] All=>all??(all=Resources.LoadAll<ARGameMode>("ARModes").OrderBy(m=>m.order).ToArray());
        public static ARGameMode Training=>All.FirstOrDefault(m=>m.id=="training");
        public static ARGameMode StageMode(int stage)=>All.FirstOrDefault(m=>m.id==Stages[Mathf.Clamp(stage-1,0,9)].modeId)??Training;
        public static ARGameMode DailyMode(DateTime utc)=>All.FirstOrDefault(m=>m.id==new[]{"defense","rift-hunt","dragon-duel"}[DailySeed(utc)%3])??Training;
        public static readonly ARStageDefinition[] Stages=Enumerable.Range(1,10).Select(CreateStage).ToArray();
        static ARStageDefinition CreateStage(int i)
        {
            string[] vi={"Làm quen","Ấn đầu tiên","Tinh anh thức tỉnh","Thủ Trận","Truy Rift","Đấu Long","Thủ Trận tinh anh","Truy Rift tốc hành","Đấu Long giáp cứng","Thủ Trận đại thành"};
            string[] en={"Training Grounds","First Seal","Elite Awakening","Shrine Defense","Rift Hunt","Dragon Duel","Elite Defense","Swift Rift Hunt","Armored Dragon Duel","Master Defense"};
            string modeId=i<=3?"training":i==5||i==8?"rift-hunt":i==6||i==9?"dragon-duel":"defense";
            ARWaveDefinition[] waves;
            if(modeId=="rift-hunt")waves=new[]{new ARWaveDefinition{count=0}};
            else if(modeId=="dragon-duel")waves=new[]{new ARWaveDefinition{count=i>=9?8:6,spawnInterval=4,eliteEvery=i>=9?4:0}};
            else
            {
                int count=modeId=="defense"?5:3;waves=new ARWaveDefinition[count];
                for(int w=0;w<count;w++)waves[w]=new ARWaveDefinition{count=(w==0?3:w==1?4:6)+(i-1)/3,spawnInterval=modeId=="defense"?3:.85f,minimumSeconds=modeId=="defense"&&w<count-1?45:0,breakSeconds=modeId=="defense"?10:3,eliteEvery=i>=3?Math.Max(2,6-i/2):0,events=modeId=="defense"&&w==1?new[]{ARBattleEventKind.ShrineRescue}:modeId=="defense"&&w==2?new[]{ARBattleEventKind.PackLeader}:Array.Empty<ARBattleEventKind>()};
                if(i>=10)waves[3].events=new[]{ARBattleEventKind.MeteorRain};
            }
            return new ARStageDefinition{number=i,modeId=modeId,nameVN=vi[i-1],nameEN=en[i-1],waves=waves};
        }
        // A stable UTF8-independent integer seed, shared by all devices on this UTC day.
        public static int DailySeed(DateTime utc)
        {unchecked{int hash=1007;foreach(char c in utc.ToUniversalTime().ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture))hash=hash*31+c;return hash&int.MaxValue;}}
    }
}
