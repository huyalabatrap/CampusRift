using CampusRift.UI;
using CampusRift.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static partial class UIFoundationBuilder
{
    [MenuItem("Campus Rift/UI/Phase 7 - Build Game Over")]
    public static void BuildGameOver()
    {
        Prepare();EditorSceneManager.OpenScene(GamePath);var ui=Object.FindAnyObjectByType<UIManager>();
        if(ui.GameOver!=null)throw new System.InvalidOperationException("Game Over already exists.");
        ui.GameOver=Panel(ui.transform,"GameOver");var gameover=ui.GameOver.gameObject.AddComponent<GameOverUI>();
        var card=Card(ui.GameOver.transform,"Defeat Card",850,520);
        Text(card,"Eyebrow","THE RIFT CLAIMED ANOTHER",60,42,730,40,18,Theme.DangerColor).alignment=TMPro.TextAlignmentOptions.Center;
        Text(card,"Title","DEFEATED",40,126,770,110,86,Theme.TextPrimary).alignment=TMPro.TextAlignmentOptions.Center;
        Text(card,"Description","Stand up. Try again.",40,270,770,50,27,Theme.TextSecondary).alignment=TMPro.TextAlignmentOptions.Center;
        ui.GameOver.FirstSelection=Button(card,"RETRY","RETRY",60,380,345,64,gameover.Retry);
        Button(card,"MAIN MENU","MAIN MENU",445,380,345,64,gameover.MainMenu);
        var health=Object.FindAnyObjectByType<PlayerMonsterHealth>();health.respawnOnDefeat=false;
        if(health.GetComponent<PlayerDeathUIAdapter>()==null)health.gameObject.AddComponent<PlayerDeathUIAdapter>();
        EditorUtility.SetDirty(health);EditorUtility.SetDirty(ui);EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("CAMPUS RIFT: Defeated event opens Game Over. Retry owns respawn.");
    }
}
