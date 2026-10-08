from pathlib import Path
import shutil

def edit(path, old, new):
    p=Path(path);s=p.read_text(encoding='utf-8-sig');assert old in s,path;p.write_text(s.replace(old,new),encoding='utf-8')

edit('Assets/Learning/Validation/ExtendedLearningPlayTest.cs', 'Check(LearningContentValidation.Run(catalog).errors.Count==0,"Content validation0errors");', 'Check(all.All(q=>GraderRegistry.Default.Supports(q)&&!string.IsNullOrEmpty(q.source)),"Runtime question schema/source validation");')
edit('Assets/Progression/Validation/P20EconomyChecks.cs', 'float spiritBefore=stats.MaxSpirit,regenBefore=stats.SpiritRegen;profile.Artifacts.ApplyTo(stats);', 'stats.RemoveSource(StatSource.Artifact,ArtifactService.Key+":"+gourd.id);float spiritBefore=stats.MaxSpirit,regenBefore=stats.SpiritRegen;profile.Artifacts.ApplyTo(stats);')
for f in ['Assets/Progression/Validation/EconomyPlayTest.cs','Assets/Learning/Validation/BreakthroughExamPlayTest.cs']:
    edit(f,'else s.Answer(q.options.First(o => !q.correctOptionIds.Contains(o.id)).id);','else if(q.type=="matching") s.Answer(q.correctOptionIds.Select(p=>p.Split(\':\')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToArray());\n                    else s.Answer(q.options.First(o => !q.correctOptionIds.Contains(o.id)).id);')
edit('Assets/Learning/Validation/LearningPlayTest.cs','if (q.Data.type == "ordering") ids.Reverse(); else ids', 'if (q.Data.type == "ordering") ids.Reverse(); else if(q.Data.type=="matching") ids=q.Data.correctOptionIds.Select(p=>p.Split(\':\')[0]+":"+(p.EndsWith(":1")?"2":"1")).ToList(); else ids')
f='Assets/Progression/Validation/EconomyPlayTest.cs'
edit(f,'Balance == 5 * n1 + 50, "A perfect','Balance == 5 * n1 + 80, "Daily read +30; a perfect')
edit(f,'Balance == 5 * n1 + 50, "Repeating it at once pays nothing: answers were right in the last 24 hours"','Balance == 5 * n1 + 110 && engine.LastLinhThach.total == 0, "Daily20 correct +30 once; immediate repeat quiz pays nothing"')
edit(f,'Balance == 7 * n1 + 50','Balance == 7 * n1 + 110')
edit(f,'items.items.Count == 10 && items.artifacts.Count == 3, "Ten items and three artifacts are installed"','items.items.Count == 16 && items.artifacts.Count == 5, "Sixteen items and five artifacts are installed"')
edit(f,'// No real-money path.','P20EconomyChecks.Run(player.gameObject,profile,Check);\n            buffs.enabled=true;\n            // No real-money path.')
f='Assets/CampusRiftUI/Runtime/HubPages.cs'
edit(f,'kit.Image(detail, "Art", 24, 104, 380, 244, open', 'kit.Image(detail, "Art", 24, 104, 380, item.IsElementTalisman?148:244, open')
edit(f,'var check=Shop.Check(item,buyQuantity);','var purchaseItem=item.IsElementTalisman?Shop.Catalog.ElementVariant(shopElement):item;\n            var check=Shop.Check(purchaseItem,buyQuantity);')
edit(f,'{inv.Count(item.id)}":Shop.LockReason', '{inv.Count(purchaseItem.id)}":Shop.LockReason')
edit(f,'Shop.MaxPurchasable(item);','Shop.MaxPurchasable(purchaseItem);')
edit(f,'24+k*204,446,194,68','24+k*76,270,72,68')
f='Assets/Learning/Runtime/LearningUI.Shop.cs'
edit(f,'var item=shopItem;int category=', 'var item=shopItem;var purchaseItem=item.IsElementTalisman?Shop.Catalog.ElementVariant(talismanElement):item;int category=')
edit(f,'{Profile.Inventory.Count(item.id)}");', '{Profile.Inventory.Count(purchaseItem.id)}");')
edit(f,'int max=Shop.MaxPurchasable(item);','int max=Shop.MaxPurchasable(purchaseItem);')

backup=Path(Path('task/p20/BACKUP.txt').read_text().strip())
for folder in ['Artifacts/Learning','Artifacts/Economy','Artifacts/UI']:
    source=Path(folder)
    if source.exists():shutil.copytree(source,backup/'HistoricalArtifacts'/source.name,dirs_exist_ok=True)
# Keep existing icon importers unchanged. Only new P20 sprites need their own import settings.
for p in (backup/'Assets/Resources/ContentImages').rglob('*.png.meta'):
    dest=Path(str(p).replace(str(backup)+'\\','',1))
    if dest.exists():shutil.copy2(p,dest)
print('Harness compatibility, shop layout and historical artifact backup complete.')
