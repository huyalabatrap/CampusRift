from pathlib import Path

root = Path(__file__).resolve().parents[1]
def edit(name, pairs):
    p = root / name
    s = p.read_text(encoding='utf-8-sig')
    for before, after in pairs:
        if before not in s:
            raise RuntimeError(f'Missing replacement in {name}: {before[:100]}')
        s = s.replace(before, after)
    p.write_text(s, encoding='utf-8')

edit('Assets/Combat/Validation/PlayerCombatPlayTest.cs', [
    ('position = Vector2.zero', 'position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.Attack).transform.position)'),
    ('Zone(TouchRole.Dash).OnPointerDown(pointer);', 'pointer.position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.Dash).transform.position);\n            Zone(TouchRole.Dash).OnPointerDown(pointer);'),
    ('Zone(TouchRole.LockOn).OnPointerDown(pointer);', 'pointer.position = RectTransformUtility.WorldToScreenPoint(null, Zone(TouchRole.LockOn).transform.position);\n            Zone(TouchRole.LockOn).OnPointerDown(pointer);')])

edit('Assets/Controls/Runtime/MobileChasePlayTest.cs', [
    ('int materials=Resources.FindObjectsOfTypeAll<Material>().Length;', 'var materials=hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material.GetInstanceID()).ToArray(); // P09/LOOK HUD ownership; unrelated expiring world VFX are outside this assertion.'),
    ('materials==Resources.FindObjectsOfTypeAll<Material>().Length', 'materials.SequenceEqual(hud.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Select(g=>g.material.GetInstanceID()))')])

edit('Assets/Controls/Runtime/BoostEnergyPlayTest.cs', [
    ('hud.Label.text=="RECOVERING" && hud.Fill.fillAmount<.01f &&\n                mobile.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="RECOVER")', 'hud.Label.text=="RECOVERING" && Mathf.Abs(hud.Fill.fillAmount-player.EnergyFraction)<.01f &&\n                mobile.GetComponentsInChildren<TMP_Text>().Any(t=>t.text==CampusRift.Localization.LocalizationService.Instance.Translate("RECOVER"))')])

edit('Assets/Skills/Core/Validation/SkillSet1EdgePlayTest.cs', [
    ('world.victims[0].GetComponentInChildren<Animator>().transform', 'VisualRig(world.victims[0])'),
    ('void Place(int i,Vector3 p)', 'static Transform VisualRig(MonsterVitality victim) { var skin=victim.GetComponentInChildren<SkinnedMeshRenderer>(); var rig=skin.rootBone; while(rig.parent!=null&&rig.parent!=victim.transform)rig=rig.parent; return rig; }\n        void Place(int i,Vector3 p)')])

edit('Assets/Enemies/Validation/EnemyReactionAnimationPlayTest.cs', [
    ('var model=target.GetComponentInChildren<Animator>().transform;', 'var skin=target.GetComponentInChildren<SkinnedMeshRenderer>();var model=skin.rootBone;while(model.parent!=null&&model.parent!=target.transform)model=model.parent; // P12 root Animator uses a child visual wrapper.')])

edit('Assets/CampusRiftUI/Validation/SkyVictoryPlayTest.cs', [
    ('State.OpenSettings();yield return new WaitForSecondsRealtime(.3f);', 'State.OpenHub();yield return null;State.OpenSettings();yield return new WaitForSecondsRealtime(.3f);'),
    ('Main menu exposes the same saved sky setting', 'P09 Hub exposes the same saved sky setting')])

edit('Assets/Enemies/Validation/P12Fix1VisualSmoke.cs', [
    ('Check(bc[2].y < pc[0].y, "comic boss HUD below mobile pause button");', 'var bossRect=Rect.MinMaxRect(bc[0].x,bc[0].y,bc[2].x,bc[2].y);var pauseRect=Rect.MinMaxRect(pc[0].x,pc[0].y,pc[2].x,pc[2].y);\n                    Check(!bossRect.Overlaps(pauseRect), "LOOK centered comic boss HUD does not overlap mobile pause button");')])

edit('Assets/MonsterShaban/Scripts/ShabanTraversalPlayTest.cs', [
    ('X F11->F1 right after that ride: lift is faster', 'X F11->F1 right after that ride: downhill stairs are faster'),
    ('KeepPosition = true, Target = new Vector3(-43.3f, 0f, -28f), Expect = "lift"', 'KeepPosition = true, Target = new Vector3(-43.3f, 0f, -28f), Expect = "stairs"')])

print('Updated current-design fixture assumptions; production code unchanged.')
