using CampusRift;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Phase 4 - Build Gameplay HUD")]
    public static void BuildGameplay()
    {
        Prepare();EditorSceneManager.OpenScene(GamePath);
        var existing=Object.FindAnyObjectByType<UIManager>();
        if(existing!=null)throw new System.InvalidOperationException("Gameplay UI already exists. Edit the existing hierarchy.");
        var canvas=Canvas("Gameplay UI Canvas");EventSystem();var ui=canvas.AddComponent<UIManager>();ui.IsGameplay=true;
        var hud=Stretch(canvas.transform,"GameplayHUD");ui.HUD=hud.gameObject;
        var group=hud.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;group.interactable=false;
        var component=hud.gameObject.AddComponent<GameplayHUD>();
        var player=Object.FindAnyObjectByType<CampusExplorer>();player.showControls=false;
        var vitals=Rect(hud,"Vitals",48,42,380,165);
        Text(vitals,"Callsign","EXPLORER  /  VITALS",0,0,360,30,18,Theme.TextSecondary);
        component.Health=BuildHealth(vitals);
        component.Health.Source=player.GetComponent<PlayerMonsterHealth>();component.Health.Source.showLegacyHUD=false;
        Text(vitals,"EnergyLabel","ENERGY",0,104,180,30,16,Theme.BlueAccent);
        Text(vitals,"EnergyPlaceholder","--",300,104,60,30,18,Theme.TextSecondary);
        Image(vitals,"EnergyTrack",0,140,360,5,Theme.SecondaryBackground);
        Image(vitals,"EnergyPlaceholderFill",0,140,90,5,new Color(.3f,.65f,1,.4f));
        component.Breakthrough=BuildBreakthrough(hud);
        var obj=Rect(hud,"Objective",-422,42,374,120,new Vector2(1,1));
        var objective=obj.gameObject.AddComponent<ObjectiveUI>();component.Objective=objective;objective.Theme=Theme;
        objective.Status=Text(obj,"Heading","OBJECTIVE",0,0,374,30,17,Theme.TextSecondary);
        objective.Body=Text(obj,"Body","Escape from the monster.",0,36,374,80,25);Image(obj,"Accent",0,30,64,2,Theme.PrimaryAccent);
        var skills=Rect(hud,"SkillBar",-388,-154,340,112,new Vector2(1,0));
        component.Skills=skills.gameObject.AddComponent<SkillBarUI>();component.Skills.Slots=new SkillSlotUI[3];
        for(int i=0;i<3;i++)component.Skills.Slots[i]=BuildSkillSlot(skills,i*110,0,i==2);
        Text(skills,"Label","ABILITIES  /  LOCKED",0,-34,330,28,15,Theme.TextSecondary);
        var help=Rect(hud,"Control Hint",48,-72,860,36,new Vector2(0,0));Text(help,"Hint","WASD  Move   /   SHIFT  Sprint   /   SPACE  Jump   /   E  Elevator   /   ESC  Pause",0,0,860,36,16,Theme.TextSecondary);
        var warning=Rect(hud,"MonsterWarning",-225,48,450,48,new Vector2(.5f,1));component.Warning=warning.gameObject.AddComponent<MonsterWarningUI>();
        component.Warning.Indicator=warning.gameObject.AddComponent<CanvasGroup>();Text(warning,"Label","!   MONSTER NEARBY",0,0,450,48,22,Theme.DangerColor);warning.gameObject.SetActive(false);
        BuildLearningStructure(canvas.transform);
        EditorUtility.SetDirty(player);EditorUtility.SetDirty(component.Health.Source);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        var root=Rect(null,"Prefabs",0,0,600,400);
        SaveComponent(BuildHealth(root).gameObject,"HealthBar");SaveComponent(BuildSkillSlot(root,0,0,false).gameObject,"SkillSlot");
        var progress=Image(root,"ProgressBar",0,0,360,10,Theme.PrimaryAccent);progress.sprite=WhiteSprite();progress.type=UnityEngine.UI.Image.Type.Filled;progress.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;SaveComponent(progress.gameObject,"ProgressBar");
        Object.DestroyImmediate(root.gameObject);AssetDatabase.SaveAssets();Debug.Log("CAMPUS RIFT: Gameplay HUD built.");
    }
    static PlayerHealthUI BuildHealth(Transform parent)
    {
        var r=Rect(parent,"HealthBar",0,38,360,60);var ui=r.gameObject.AddComponent<PlayerHealthUI>();
        Text(r,"Label","HP",0,0,80,30,18,Theme.DangerColor);
        ui.Value=Text(r,"Value","100 / 100",100,0,260,30,22);ui.Value.alignment=TMPro.TextAlignmentOptions.Right;
        Shape(r,"Frame",-4,32,368,24,Theme.PanelBackground,RiftGraphic.Shape.Panel);
        Image(r,"Track",0,36,360,16,Theme.SecondaryBackground);
        ui.DelayedFill=Image(r,"Damage Trail",0,36,360,16,Theme.SecondaryAccent);
        ui.Fill=Image(r,"Health",0,36,360,16,Theme.DangerColor);
        foreach(var image in new[]{ui.Fill,ui.DelayedFill}){image.sprite=WhiteSprite();image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;}
        return ui;
    }
    static BreakthroughProgressUI BuildBreakthrough(Transform parent)
    {
        var r=Rect(parent,"BreakthroughProgress",48,-163,320,78,new Vector2(0,0));var ui=r.gameObject.AddComponent<BreakthroughProgressUI>();ui.Theme=Theme;
        Text(r,"Label","BREAKTHROUGH",0,0,240,24,15,Theme.SecondaryAccent);ui.Count=Text(r,"Count","0 / 5",240,0,80,24,17,Theme.TextSecondary);
        var markers=Rect(r,"Markers",0,38,180,22);ui.MarkerRoot=markers;
        var layout=markers.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=14;layout.childControlHeight=false;layout.childControlWidth=false;layout.childForceExpandWidth=false;
        for(int i=0;i<5;i++)
        {
            var marker=Shape(markers,"Rune "+(i+1),0,0,18,22,Theme.DisabledColor,RiftGraphic.Shape.Diamond);
            Shape(marker.transform,"Hollow",4,5,10,12,Theme.PrimaryBackground,RiftGraphic.Shape.Diamond);
            if(i==0)ui.MarkerTemplate=marker;
        }
        ui.Available=Text(r,"Available","BREAKTHROUGH AVAILABLE",0,-36,400,30,18,Theme.SecondaryAccent);ui.Available.gameObject.SetActive(false);return ui;
    }
    static SkillSlotUI BuildSkillSlot(Transform parent,float x,float y,bool gold)
    {
        var r=Rect(parent,gold?"Ultimate Slot":"Skill Slot",x,y,96,104);var ui=r.gameObject.AddComponent<SkillSlotUI>();
        Shape(r,"Border",0,0,96,96,gold?new Color(1,.77f,.39f,.45f):new Color(.57f,.39f,1,.45f),RiftGraphic.Shape.Panel);
        Shape(r,"Background",2,2,92,92,Theme.PanelBackground,RiftGraphic.Shape.Panel);
        ui.Icon=Image(r,"Icon",18,14,60,60,Theme.SecondaryAccent);ui.Icon.enabled=false;
        Shape(r,"Placeholder Sigil",27,18,42,52,gold?Theme.SecondaryAccent:Theme.PrimaryAccent,RiftGraphic.Shape.Diamond);
        ui.CooldownOverlay=Image(r,"Cooldown",4,4,88,88,new Color(0,0,0,.85f));ui.CooldownOverlay.sprite=WhiteSprite();ui.CooldownOverlay.type=UnityEngine.UI.Image.Type.Filled;ui.CooldownOverlay.fillMethod=UnityEngine.UI.Image.FillMethod.Radial360;ui.CooldownOverlay.fillAmount=0;
        ui.CooldownText=Text(r,"Seconds","",0,26,96,44,24);ui.CooldownText.alignment=TMPro.TextAlignmentOptions.Center;
        var locked=Image(r,"LockedOverlay",3,3,90,90,new Color(.01f,.02f,.05f,.85f));ui.LockedOverlay=locked.gameObject;
        Shape(locked.transform,"Lock",32,22,26,34,Theme.TextSecondary,RiftGraphic.Shape.Lock);
        ui.KeyLabel=Text(r,"Key","--",0,70,96,28,18,Theme.TextSecondary);ui.KeyLabel.alignment=TMPro.TextAlignmentOptions.Center;
        ui.UpgradeLabel=Text(r,"Upgrade","",48,-24,48,24,15,Theme.SecondaryAccent);
        ui.Tooltip=Text(r,"Tooltip",gold?"Thien Thu Tran Ap\nBreak through 5 times to unlock.":"Break through 5 times to unlock.",-180,-112,350,90,21,Theme.TextPrimary);ui.Tooltip.gameObject.SetActive(false);
        return ui;
    }
}
