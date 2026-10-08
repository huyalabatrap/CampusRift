#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CampusRift.Combat;
using CampusRift.Controls;
using CampusRift.Enemies;
using CampusRift.Levels;
using CampusRift.Skills;
using CampusRift.SkyBeast;
using CampusRift.UI;

namespace CampusRift.Validation
{
    // Small review capture/smoke, deliberately without balance/statistical trials.
    public sealed class P12Fix1VisualSmoke : P12PlayTest
    {
        const string Root = "task/p12/screens/fix1/";
        readonly List<Renderer> hidden = new List<Renderer>();
        IEnumerator Shot(string name)
        {
            System.IO.Directory.CreateDirectory(Root);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Root + name + ".png");
            yield return new WaitForEndOfFrame();
            yield return null;
            Measure("image " + Root + name + ".png");
        }
        float Dissolve(EnemyInstance enemy)
        {
            var block = new MaterialPropertyBlock();
            float value = 0;
            foreach (var r in enemy.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                r.GetPropertyBlock(block);
                value = Mathf.Max(value, block.GetFloat("_Dissolve"));
            }
            return value;
        }
        Bounds DragonBounds(SkyBeastController dragon)
        {
            var renderers = dragon.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }
        void Track(SkyBeastController dragon)
        {
            var delta = DragonBounds(dragon).center - world.player.followCamera.transform.position;
            world.Look(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg,
                -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg);
        }
        protected override IEnumerator Run()
        {
            Begin(); world.Mode(false); world.Lighting(false); world.Look(90, 14);
            foreach (var r in world.player.GetComponentsInChildren<Renderer>())
                if (!r.forceRenderingOff) { hidden.Add(r); r.forceRenderingOff = true; }
            foreach (var sword in FindObjectsByType<FlyingSword>())
                foreach (var r in sword.GetComponentsInChildren<Renderer>())
                    if (!r.forceRenderingOff) { hidden.Add(r); r.forceRenderingOff = true; }
            var roster = LevelCatalog.Instance.Get(10).spawnTable.roster;
            var live = new List<EnemyInstance>();
            int index = 0;
            foreach (var id in new[] { "hoa-trung", "thiet-giap-nguu", "bao-thi" })
            {
                var archetype = roster.First(x => x.archetype.id == id).archetype;
                var e = EnemyPool.Ensure().Spawn(archetype, world.origin + new Vector3(6, 0, (index++ - 1) * 2.1f), EnemyScaling.Default);
                e.Brain.enabled = false; e.Motor.Stop(); live.Add(e);
            }
            yield return new WaitForSeconds(3.1f);
            foreach (var e in live) Check(e.Alive && Dissolve(e) == 0, e.archetype.id + " live model has zero dissolve");
            yield return Shot("living-light");
            world.Lighting(true); yield return null; yield return Shot("living-dark");
            var victim = live[0]; float hp = victim.Vitality.Health;
            world.player.GetComponent<PlayerCombat>().ResolveHit(victim.Vitality, 1, true);
            Check(victim.Vitality.Health < hp, "real sword hit responds on reviewed model");
            victim.Vitality.ApplyDamage(DamageInfo.Create(100000, Element.None, DamageSource.Melee,
                victim.transform.position, Vector3.forward, world.player.gameObject));
            yield return new WaitForSeconds(victim.Animation.DeathSeconds + .3f);
            Check(!victim.Alive && Dissolve(victim) > 0, "death starts dissolve after death pose");
            EnemyPool.Instance.ReleaseAll();
            var reused = EnemyPool.Instance.Spawn(victim.archetype, world.origin + Vector3.right * 6, EnemyScaling.Default);
            reused.Brain.enabled = false; reused.Motor.Stop();
            Check(reused.Alive && Dissolve(reused) == 0, "pool reuse restores a clean living model");
            EnemyPool.Instance.ReleaseAll(); RiftPortal.CloseAll();

            foreach (var id in new[] { "020", "023", "026" })
            {
                world.Lighting(id != "026");
                var dragon = SkyBeastPresence.Spawn(id);
                dragon.SeekFlightForValidation(0);
                float until = Time.time + .5f;
                while (Time.time < until) { Track(dragon); yield return null; }
                var bounds = DragonBounds(dragon);
                var center = world.player.followCamera.WorldToViewportPoint(bounds.center);
                Check(bounds.size.magnitude > 20 && center.z > 0 && center.x > .1f && center.x < .9f && center.y > .15f && center.y < .85f,
                    "dragon " + id + " tens of metres and visible from ground during low pass");
                Measure("dragon " + id + " scale=" + dragon.definition.scale + " bounds=" + bounds.size + " lowPassRoot=" + dragon.transform.position);
                yield return Shot("dragon-" + id + "-lowpass");
                dragon.Roar(); int first = dragon.LastRoarVariant; dragon.Roar();
                Check(dragon.NextRoarAt - Time.time >= 20 && dragon.NextRoarAt - Time.time <= 45 && first != dragon.LastRoarVariant,
                    "dragon " + id + " roar responds, next interval20-45s, distinct variants");
                SkyBeastPresence.StopAll(); yield return null;
            }
            world.PlacePlayer(world.origin); world.Look(90, 14); world.Mode(true);
            foreach (int level in new[] { 5, 7 })
            {
                world.Lighting(level == 7);
                var definition = LevelCatalog.Instance.Get(level);
                var e = EnemyPool.Instance.Spawn(definition.bosses[0], world.origin + Vector3.right * 6, definition.Scaling);
                var boss = e.GetComponent<BossController>(); boss.Force(BossAttack.Roar);
                yield return new WaitForSeconds(.5f);
                Check(boss.Busy && boss.WarningAt < Time.time && Time.time < boss.WarningAt + 1.2f,
                    "boss" + level + " actual roar windup visible");
                yield return Shot("boss-" + level + "-telegraph-hud");
                if (level == 7)
                {
                    var pause = FindObjectsByType<MobileTouchZone>().First(x => x.role == TouchRole.Pause);
                    var panel = GameObject.Find("Boss panel").GetComponent<RectTransform>();
                    var pc = new Vector3[4]; var bc = new Vector3[4];
                    ((RectTransform)pause.transform).GetWorldCorners(pc); panel.GetWorldCorners(bc);
                    var bossRect=Rect.MinMaxRect(bc[0].x,bc[0].y,bc[2].x,bc[2].y);var pauseRect=Rect.MinMaxRect(pc[0].x,pc[0].y,pc[2].x,pc[2].y);
                    Check(!bossRect.Overlaps(pauseRect), "LOOK centered comic boss HUD does not overlap mobile pause button");
                    var input = new PointerEventData(EventSystem.current) { position = (pc[0] + pc[2]) * .5f };
                    var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(input, hits);
                    Check(hits.Count > 0 && (hits[0].gameObject == pause.gameObject || hits[0].gameObject.transform.IsChildOf(pause.transform)), "mobile pause remains raycast accessible");
                    pause.OnPointerDown(input); Check(UIStateManager.Instance.State == UIState.Paused, "mobile pause button responds");
                    UIStateManager.Instance.Resume();
                }
                boss.Cancel(); EnemyPool.Instance.ReleaseAll(); yield return null;
            }
            world.Look(90, 14);
            float brightness = SettingsManager.Instance.Sky.Brightness;
            StartCoroutine(SkyBeastEnding.Play());
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Shot("ending7");
            Check(SkyBeastEnding.Playing && FindObjectsByType<Canvas>().Where(c => c.enabled).All(c => c.name == "Ending skip"),
                "ending hides skills, hints and synergy HUD even when they update");
            var skip = FindObjectsByType<Button>().First(x => x.transform.parent != null && x.transform.parent.name == "Ending skip");
            skip.onClick.Invoke(); yield return null; yield return null;
            Check(SkyBeastEnding.Skipped && !SkyBeastEnding.Playing && world.player.enabled, "ending touch skip restores flow");
            Check(FindObjectsByType<Canvas>().Any(c => c.enabled && c.name != "Ending skip"), "HUD restored after ending");
            Check(Mathf.Approximately(SettingsManager.Instance.Sky.Brightness, brightness) && GameObject.Find("Ending facade fill") == null,
                "ending restores brightness and removes temporary facade fill");
        }
        protected override void Cleanup()
        {
            SkyBeastEnding.Cancel();
            foreach (var r in hidden) if (r != null) r.forceRenderingOff = false;
            base.Cleanup();
        }
    }
}
#endif
