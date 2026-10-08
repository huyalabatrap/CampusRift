using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampusRift.Progression
{
    // One session policy. The durable profile is always below the disposable copy.
    public static class DevMode
    {
        public const string ActiveKey = "CampusRift.DevMode", InvincibleKey = "CampusRift.DevMode.Invincible", CooldownKey = "CampusRift.DevMode.NoCooldown";
        public static bool Active { get; private set; }
        public static bool Invincible => Active && invincible;
        public static bool NoCooldown => Active && noCooldown;
        public static bool Changing { get; private set; }
        public static event Action Changed;
        static bool invincible, noCooldown;
        public static bool InRun => SceneManager.GetActiveScene().name != "MainMenu";
        public static string Quantity(int value) => Active ? "∞" : value.ToString("N0");
        public static bool DeveloperVisible(bool editor, bool development, int taps) => editor || development || taps >= 7;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { Active = invincible = noCooldown = Changing = false; Changed = null; }

        internal static void RestorePreferences(ProfileService profile)
        {
            invincible = PlayerPrefs.GetInt(InvincibleKey, 0) != 0;
            noCooldown = PlayerPrefs.GetInt(CooldownKey, 0) != 0;
            if (PlayerPrefs.GetInt(ActiveKey, 0) != 0) Enable(profile);
        }
        static void Enable(ProfileService profile)
        {
            // Finish pending real changes before exposing dev queries. Deep-copy every nested list.
            var copy = JsonUtility.FromJson<ProfileData>(JsonUtility.ToJson(profile.Data));
            profile.PushTransient(copy);
            Active = true;
            // No Bind while effective realm is dev: clamp the durable copy with its own realm.
            Refresh();
        }
        public static void SetActive(bool value)
        {
            if (value == Active || Changing) return;
            if (value) { Enable(ProfileService.Ensure()); SavePreferences(); return; }
            if (InRun && Application.isPlaying)
            {
                Changing = true;
                var host = new GameObject("Leave dev session").AddComponent<DevModeExit>();
                UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
                host.StartCoroutine(host.Leave());
                return;
            }
            FinishDisable();
        }
        internal static void FinishDisable()
        {
            Active = false;
            ProfileService.Instance?.PopTransient();
            Changing = false;
            SavePreferences(); Refresh();
        }
        public static void SetInvincible(bool value) { invincible = value; SavePreferences(); Changed?.Invoke(); }
        public static void SetNoCooldown(bool value) { noCooldown = value; SavePreferences(); Changed?.Invoke(); }
        static void SavePreferences()
        {
            PlayerPrefs.SetInt(ActiveKey, Active ? 1 : 0);
            PlayerPrefs.SetInt(InvincibleKey, invincible ? 1 : 0);
            PlayerPrefs.SetInt(CooldownKey, noCooldown ? 1 : 0); PlayerPrefs.Save();
        }
        static void Refresh()
        {
            ProfileService.Instance?.NotifyChanged();
            foreach (var bridge in UnityEngine.Object.FindObjectsByType<CultivationPlayerBridge>(FindObjectsSortMode.None)) bridge.Apply();
            foreach (var explorer in UnityEngine.Object.FindObjectsByType<CampusExplorer>(FindObjectsSortMode.None)) explorer.RefillEnergy();
            foreach (var stats in UnityEngine.Object.FindObjectsByType<Combat.PlayerStats>(FindObjectsSortMode.None))
                ProfileService.Instance?.Artifacts.ApplyTo(stats);
            foreach (var items in UnityEngine.Object.FindObjectsByType<PlayerItems>(FindObjectsSortMode.None)) items.BeginLevel();
            Changed?.Invoke();
        }
    }

    public sealed class DevModeExit : MonoBehaviour
    {
        internal IEnumerator Leave()
        {
            if (SceneManager.GetActiveScene().name.StartsWith("AR")) AR.ARSceneNavigation.Exit();
            else UI.GameSceneManager.Instance.LoadMainMenu();
            // Keep the overlay until every old-scene OnDisable/OnDestroy has finished.
            while (SceneManager.GetActiveScene().name != "MainMenu" || UI.HubUI.Instance == null) yield return null;
            yield return null;
            Levels.LevelSession.Clear(); Levels.LevelSession.Loadout = null;
            DevMode.FinishDisable(); UI.UIStateManager.Instance.OpenHub(); Destroy(gameObject);
        }
    }
}
