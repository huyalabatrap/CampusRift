from pathlib import Path
p=Path('Assets/SkyBeast/Runtime/HeavenSwordCinematic.cs');s=p.read_text(encoding='utf-8').replace('FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)','FindObjectsByType<ParticleSystem>()').replace('FindObjectsByType<Canvas>(FindObjectsSortMode.None)','FindObjectsByType<Canvas>()');p.write_text(s,encoding='utf-8')
# Keep control and actor restoration on cancel; expired pill flags must not retain charges forever.
p=Path('Assets/Progression/Runtime/BuffSystem.cs');s=p.read_text(encoding='utf-8-sig').replace('if (uninterruptedCharges <= 0) return false;', 'if (uninterruptedCharges <= 0 || !HasFlag(ItemFlag.SwordUninterrupted)) return false;');p.write_text(s,encoding='utf-8')
