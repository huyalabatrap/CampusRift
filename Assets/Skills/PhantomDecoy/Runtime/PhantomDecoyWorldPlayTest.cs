#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using CampusRift.Monsters;
using CampusRift.UI;
using UnityEngine;

namespace CampusRift.Skills
{
    // Launch explicitly through MCP in Play Mode. Never saved on the gameplay scene.
    public sealed class PhantomDecoyWorldPlayTest : MonoBehaviour
    {
        [Serializable]
        sealed class Result
        {
            public bool doorCast, crossedEntrance, openedEntrance, stairCast, descendedStair, navReady, allDecoysCleared, fixtureRestored;
            public float doorFarthestZ, doorMaxOpen, stairStartY, stairLowestY;
            public int doorFootsteps, stairFootsteps;
        }
        readonly Result result = new Result();
        PhantomDecoySkill skill;
        CampusExplorer player;
        CharacterController controller;
        MonsterBrain brain;
        PlayerMonsterHealth health;
        Vector3 originalPosition;
        bool originalPlayerEnabled, originalBrainEnabled, originalBackground, restored;
        float originalTimeScale, originalHealth, originalProtection;
        static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        void Place(Vector3 point)
        {
            controller.enabled = false; player.transform.position = point;
            controller.enabled = true; Physics.SyncTransforms();
        }
        void Ready() => typeof(PhantomDecoySkill).GetField("readyAt", Private).SetValue(skill, Time.time);
        void Save()
        {
            Directory.CreateDirectory("Artifacts/PhantomDecoy");
            File.WriteAllText("Artifacts/PhantomDecoy/WorldValidation.json", JsonUtility.ToJson(result, true));
        }
        IEnumerator Start()
        {
            originalBackground=Application.runInBackground;originalTimeScale=Time.timeScale;
            Application.runInBackground = true; Time.timeScale = 1;
            UIStateManager.Instance?.EnterScene(true);
            player = FindAnyObjectByType<CampusExplorer>();
            skill = player.GetComponent<PhantomDecoySkill>();
            controller = player.GetComponent<CharacterController>();
            originalPosition=player.transform.position;originalPlayerEnabled=player.enabled;
            health = player.GetComponent<PlayerMonsterHealth>();originalHealth=health.CurrentHealth;
            originalProtection=(float)typeof(PlayerMonsterHealth).GetField("protectedUntil",Private).GetValue(health);
            typeof(PlayerMonsterHealth).GetField("currentHealth", Private).SetValue(health, 100f);
            typeof(PlayerMonsterHealth).GetField("protectedUntil", Private).SetValue(health, -1f);
            brain = FindAnyObjectByType<MonsterBrain>();originalBrainEnabled=brain.enabled;
            brain.enabled = false;
            brain.GetComponent<MonsterNavigation>().Stop();
            brain.GetComponent<MonsterCombat>().Interrupt();
            player.enabled = false;

            // Door/lift carving becomes visible asynchronously after the scene's first updates.
            // P18 rank5 owns two pooled actors; clear the whole skill before each route.
            skill.enabled=false;yield return null;skill.enabled=true;
            yield return new WaitForSecondsRealtime(.8f);
            UnityEngine.AI.NavMeshHit initialHit;
            result.navReady=UnityEngine.AI.NavMesh.SamplePosition(new Vector3(0,.1f,-10),out initialHit,1.1f,UnityEngine.AI.NavMesh.AllAreas);

            Place(new Vector3(0, .1f, -10));
            var door = Array.Find(FindObjectsByType<CampusAutomaticDoor>(), d => d.name == "Entrance E");
            Ready(); result.doorCast = skill.Cast(Vector3.back);
            result.doorFarthestZ = skill.Decoy.transform.position.z;
            float until = Time.time + 7f;
            while (skill.Decoy.Live && Time.time < until)
            {
                result.doorFarthestZ = Mathf.Min(result.doorFarthestZ, skill.Decoy.transform.position.z);
                if (door != null) result.doorMaxOpen = Mathf.Max(result.doorMaxOpen, door.OpenAmount);
                result.doorFootsteps = Mathf.Max(result.doorFootsteps, skill.Decoy.AudioEvents);
                yield return null;
            }
            result.crossedEntrance = result.doorFarthestZ < -17f;
            result.openedEntrance = result.doorMaxOpen > .75f;
            Save();

            skill.enabled=false;yield return null;skill.enabled=true;
            Place(new Vector3(28.3f, 2.02f, 26.38f));
            Ready(); result.stairCast = skill.Cast(Vector3.forward);
            result.stairStartY = skill.Decoy.transform.position.y;
            result.stairLowestY = result.stairStartY;
            until = Time.time + 7f;
            while (skill.Decoy.Live && Time.time < until)
            {
                result.stairLowestY = Mathf.Min(result.stairLowestY, skill.Decoy.transform.position.y);
                result.stairFootsteps = Mathf.Max(result.stairFootsteps, skill.Decoy.AudioEvents);
                yield return null;
            }
            result.descendedStair = result.stairStartY > 1.4f && result.stairLowestY < .35f;
            skill.enabled=false;yield return null;result.allDecoysCleared=skill.LiveDecoys==0;skill.enabled=true;
            Restore();result.fixtureRestored=player.enabled==originalPlayerEnabled && brain.enabled==originalBrainEnabled && Vector3.Distance(player.transform.position,originalPosition)<.01f;
            Save();
            Debug.Log("PHANTOM WORLD QA " + JsonUtility.ToJson(result), this);
            File.WriteAllText("Artifacts/PhantomDecoy/WorldValidation-DONE.txt",
                result.doorCast && result.crossedEntrance && result.openedEntrance &&
                result.stairCast && result.descendedStair && result.navReady && result.allDecoysCleared && result.fixtureRestored ? "PASS" : "FAIL");
        }
        void Restore()
        {
            if(restored||player==null)return;restored=true;
            bool wasEnabled=skill.enabled;skill.enabled=false;skill.enabled=wasEnabled;
            Place(originalPosition);player.enabled=originalPlayerEnabled;
            if(brain!=null)brain.enabled=originalBrainEnabled;
            typeof(PlayerMonsterHealth).GetField("currentHealth",Private).SetValue(health,originalHealth);
            typeof(PlayerMonsterHealth).GetField("protectedUntil",Private).SetValue(health,originalProtection);
            Application.runInBackground=originalBackground;Time.timeScale=originalTimeScale;
        }
        void OnDestroy(){Restore();}
    }
}
#endif
