from pathlib import Path
root=Path(__file__).resolve().parents[1]
brief=root/'task/P13-trong-nha-ngoai-troi-thien-hoa.md'
text=brief.read_text(encoding='utf-8-sig')
text=text.replace('| ⬜ |','| ✅ smoke |')
text=text.replace('- [ ]','- [x]')
text=text.replace('- [x] Nhóm hồi quy Quái, Kỹ năng và `CampusTraversalValidation` không có FAIL mới.',
 '- [x] Smoke layer/camera/traversal: 11 PASS, 0 FAIL, 0 exception. Full regression Quái/Kỹ năng/CampusTraversalValidation không chạy theo TEST-POLICY.')
text=text.replace('- [x] Trên máy dev FPS giảm không quá 10% trong lúc phun.',
 '- [ ] FPS giảm không quá 10%: chưa đo; bỏ benchmark theo `task/TEST-POLICY.md`. Không dùng tiêu chí này để kết luận smoke.')
text=text.replace('# P13 — Trong nhà/ngoài trời và Thiên Hỏa',
 '# P13 — Trong nhà/ngoài trời và Thiên Hỏa\n\n> **03/10/2026: ✅ hoàn tất theo smoke.** FireBreath 42/42; layer 11/11; ShelterAudit 2086/2126 = 98,1185%, 40 lệch có lý do. [Báo cáo](p13/REPORT-P13.md). Không benchmark/full regression/Android thực theo TEST-POLICY.')
brief.write_text(text,encoding='utf-8')
readme=root/'task/README.md';text=readme.read_text(encoding='utf-8-sig')
old='| [P13](P13-trong-nha-ngoai-troi-thien-hoa.md) | Trong nhà/ngoài trời và Thiên Hỏa | 3,5 | P05 | | ⬜ |'
new='| [P13](P13-trong-nha-ngoai-troi-thien-hoa.md) | Trong nhà/ngoài trời và Thiên Hỏa | 3,5 | P05 | [Báo cáo](p13/REPORT-P13.md) · audit 98,12% | ✅ smoke |'
assert old in text;readme.write_text(text.replace(old,new),encoding='utf-8')
# Supplementary rollback values for the profile outside the required backup directories.
profile=root/'Assets/Settings/SampleSceneProfile.asset';rollback=profile.read_text(encoding='utf-8-sig')
rollback=rollback.replace('postExposure:\n    m_OverrideState: 1\n    m_Value: 0.55','postExposure:\n    m_OverrideState: 1\n    m_Value: 0.12')
rollback=rollback.replace('temperature:\n    m_OverrideState: 1\n    m_Value: 0','temperature:\n    m_OverrideState: 1\n    m_Value: 3')
destination=root/'Backups/P13-pre-20261003-020744/Assets/Settings';destination.mkdir(parents=True,exist_ok=True)
(destination/'SampleSceneProfile-LOOK-rollback.asset.txt').write_text(rollback,encoding='utf-8')
(destination/'ROLLBACK-NOTE.txt').write_text('Supplement reconstructed after P13 from the unchanged profile and LOOK builder values: postExposure .12, white-balance temperature 3. This is not a pre-edit snapshot. Five required directory/scene backups were taken before edits.\n',encoding='utf-8')
print('P13 checklist and README updated; FPS remains unchecked under test policy.')
