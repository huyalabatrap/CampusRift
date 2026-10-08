using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CampusRift.UI;

namespace CampusRift.Levels
{
    // Starts the selected level once the gameplay scene has loaded. When no level was selected (scene opened
    // directly in the editor, QA harnesses) nothing happens and the scene behaves exactly as before.
    public static class LevelBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
            Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (LevelSession.Current == null) return;
            var ui = Object.FindAnyObjectByType<UIManager>();
            if (ui == null || !ui.IsGameplay) return;
            var director = LevelDirector.Ensure();
            director.StartCoroutine(Begin(director, LevelSession.Current));
        }

        // Two frames so that the scene's own Start methods (UIManager.EnterScene, skill setup) have run first.
        static IEnumerator Begin(LevelDirector director, LevelDefinition level)
        {
            yield return null; yield return null;
            if (director != null) director.Begin(level);
        }
    }
}
