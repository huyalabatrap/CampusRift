using CampusRift.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Phase 5 - Build Pause")]
    public static void BuildPause()
    {
        Prepare();EditorSceneManager.OpenScene(GamePath);var ui=Object.FindAnyObjectByType<UIManager>();
        if(ui.PauseMenu!=null)throw new System.InvalidOperationException("Pause already exists.");
        ui.PauseMenu=Panel(ui.transform,"PauseMenu");ui.PauseMenu.GetComponent<UnityEngine.UI.Image>().color=new Color(.015f,.02f,.045f,.78f);
        var card=Card(ui.PauseMenu.transform,"Pause Card",640,750);
        Text(card,"Eyebrow","CAMPUS RIFT",56,35,520,32,18,Theme.PrimaryAccent);
        Text(card,"Heading","PAUSED",56,95,520,86,64);
        Text(card,"Hint","Take a breath. The rift can wait.",56,195,530,44,23,Theme.TextSecondary);
        ui.PauseMenu.FirstSelection=Button(card,"RESUME","RESUME",56,280,528,62,ui.Resume);
        Button(card,"SETTINGS","SETTINGS",56,358,528,58,ui.OpenSettings);
        Button(card,"RESTART","RESTART",56,430,528,58,ui.Restart);
        Button(card,"MAIN MENU","MAIN MENU",56,502,528,58,ui.Main);
        Button(card,"QUIT GAME","QUIT GAME",56,574,528,58,ui.Quit,false,true);
        Text(card,"Escape Hint","ESC  /  RESUME",56,666,528,32,16,Theme.TextSecondary);
        ui.Settings=Panel(ui.transform,"Settings");var settings=Card(ui.Settings.transform,"Settings Card",1020,800);
        Text(settings,"Title","SETTINGS",48,32,600,65,48);ui.Settings.FirstSelection=Button(settings,"BACK","BACK",48,688,280,60,ui.Back);
        EditorUtility.SetDirty(ui);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());Debug.Log("CAMPUS RIFT: Pause and input gate connected.");
    }
}
