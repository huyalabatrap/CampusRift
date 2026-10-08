var ui=CampusRift.UI.LoadoutUI.Instance;int cards=0;
foreach(var b in ui.GetComponentsInChildren<UnityEngine.UI.Button>(true))if(b.name.StartsWith("Skill ")&&b.name!="Skill List")cards++;
string selected=string.Join(",",ui.Slots);
System.IO.File.WriteAllText("Artifacts/Skills/Set2/P18-ui-proof.json","{\"cards\":"+cards+",\"suggestedLevel10\":\""+selected+"\"}");
CampusRift.Progression.ProfileService.Instance.EndTransient();
return "Final UI cards="+cards+"; level10="+selected;
