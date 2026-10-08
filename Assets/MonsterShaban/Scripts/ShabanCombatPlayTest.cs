#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using CampusRift;
using CampusRift.Monsters;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// Explicitly launched in Play Mode; never attached to production prefabs or scenes.
public sealed class ShabanCombatPlayTest : MonoBehaviour
{
    [Serializable] sealed class Report { public List<string> passed = new List<string>(); public List<string> failed = new List<string>(); }
    readonly Report report = new Report();
    CampusExplorer player;
    MonsterBrain brain;
    MonsterCombat combat;
    MonsterPerception sight;
    PlayerMonsterHealth health;
    PlayerBloodFeedback blood;
    Animator animator;
    Vector3 origin;
    Camera camera;
    void Check(bool ok, string label)
    {
        (ok ? report.passed : report.failed).Add(label);
        File.WriteAllText("Artifacts/Combat/Validation.json", JsonUtility.ToJson(report, true));
        Debug.Log("Combat QA " + (ok ? "PASS " : "FAIL ") + label);
    }
    void PlacePlayer(float distance)
    {
        var cc = player.GetComponent<CharacterController>(); cc.enabled = false;
        player.transform.SetPositionAndRotation(origin + Vector3.forward * distance, Quaternion.Euler(0,180,0));
        cc.enabled = true; Physics.SyncTransforms();
    }
    void Strike() { sight.Scan(); combat.TryAttack(); }
    void Capture(string name)
    {
        var rt = RenderTexture.GetTemporary(1280, 900, 24);
        var previous = RenderTexture.active; var target = camera.targetTexture;
        var image = new Texture2D(1280,900,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0,0,1280,900),0,0); image.Apply();
            File.WriteAllBytes("Artifacts/Combat/" + name + ".png", image.EncodeToPNG());
        }
        finally { camera.targetTexture = target; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Destroy(image); }
    }
    IEnumerator Start()
    {
        Directory.CreateDirectory("Artifacts/Combat");
        Application.runInBackground = true;
        CampusRift.UI.UIStateManager.Instance?.EnterScene(true);
        Time.timeScale = 1;
        player = Object.FindAnyObjectByType<CampusExplorer>();
        brain = Object.FindAnyObjectByType<MonsterBrain>(); brain.enabled = false;
        combat = brain.GetComponent<MonsterCombat>(); sight = brain.GetComponent<MonsterPerception>(); sight.enabled = false;
        var nav = brain.GetComponent<MonsterNavigation>(); nav.Stop();
        player.enabled = false; player.GetComponent<CharacterAfterimageTrail>().enabled = false;
        health = player.GetComponent<PlayerMonsterHealth>(); blood = player.GetComponent<PlayerBloodFeedback>();
        animator = brain.GetComponentInChildren<Animator>();
        NavMesh.SamplePosition(new Vector3(8,0,-4), out var hit, 2, NavMesh.AllAreas); origin = hit.position;
        nav.Agent.Warp(origin); brain.transform.rotation = Quaternion.identity;
        PlacePlayer(1.1f);
        foreach (var r in player.GetComponentsInChildren<Renderer>()) r.enabled = true;
        camera = Camera.main; camera.transform.position = origin + new Vector3(3.1f,1.75f,2.9f);
        camera.transform.LookAt(origin + new Vector3(0,0.95f,0.65f)); camera.fieldOfView = 37;
        yield return new WaitForSeconds(1);
        float hp = health.CurrentHealth;
        Strike();
        Check(combat.WindingUp, "Attack starts in range");
        yield return new WaitForSeconds(0.18f);
        Check(health.CurrentHealth == hp && blood.BurstCount == 0, "Windup has no early damage or blood");
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), "Attack animation is playing");
        Capture("01-Windup");
        yield return new WaitForSeconds(0.22f);
        Check(health.CurrentHealth == hp - combat.config.Damage && combat.Hits == 1, "Impact applies damage exactly once");
        Check(blood.BurstCount == 1 && blood.LiveParticles > 0, "Impact emits visible blood particles");
        Capture("02-Impact");
        int bursts = blood.BurstCount;
        Check(!health.TryTakeDamage(10, player.transform.position, Vector3.forward) && blood.BurstCount == bursts, "Invulnerability rejects damage and blood");
        Strike(); Check(combat.Hits == 1 && !combat.WindingUp && combat.IsAttacking, "Recovery prevents a second strike");
        yield return new WaitForSeconds(0.8f);
        Check(!combat.IsAttacking && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"), "Animation recovers to original locomotion");
        Check(blood.LiveParticles == 0, "Blood particles expire");
        yield return new WaitForSeconds(0.7f);
        hp = health.CurrentHealth; Strike(); PlacePlayer(4);
        yield return new WaitForSeconds(0.5f);
        Check(health.CurrentHealth == hp && blood.BurstCount == bursts, "Dodging out of range prevents damage and blood");
        yield return new WaitForSeconds(1.4f);
        PlacePlayer(1.1f); Strike();
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Combat test wall";
        wall.transform.position = origin + new Vector3(0,1.2f,0.55f); wall.transform.localScale = new Vector3(3,3,0.12f); Physics.SyncTransforms();
        yield return new WaitForSeconds(0.5f);
        Check(health.CurrentHealth == hp && blood.BurstCount == bursts, "Wall introduced during windup prevents damage and blood");
        Destroy(wall);
        yield return new WaitForSeconds(1.4f);
        var original = combat.config; var slower = Instantiate(original); slower.AttackWindup = 0.7f; combat.config = slower;
        Strike();
        yield return new WaitForSeconds(0.48f);
        Check(health.CurrentHealth == hp && animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.39f, "Changing windup slows the animation and delays impact together");
        yield return new WaitForSeconds(0.3f);
        Check(health.CurrentHealth == hp - slower.Damage && blood.BurstCount == bursts + 1, "Retimed attack still causes one impact");
        combat.config = original; Destroy(slower);
        yield return new WaitForSeconds(0.7f);
        bursts = blood.BurstCount;
        bool lethal = health.TryTakeDamage(1000, player.transform.position + Vector3.up * 1.1f, Vector3.forward);
        Check(lethal && blood.BurstCount == bursts + 1 && blood.LiveParticles > 0, "Lethal hit emits blood before Game Over pauses the game");
        Check(!health.TryTakeDamage(10, player.transform.position, Vector3.forward) && blood.BurstCount == bursts + 1, "Defeated player rejects additional hits and effects");
        File.WriteAllText("Artifacts/Combat/DONE.txt", report.passed.Count + " passed; " + report.failed.Count + " failed");
    }
}
#endif
