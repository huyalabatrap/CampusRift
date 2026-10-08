var settings=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.SettingsUI>();
settings.Language.Show();
CampusRift.Localization.LocalizationService.Instance.Discover();
UnityEngine.Canvas.ForceUpdateCanvases();
UnityEngine.ScreenCapture.CaptureScreenshot("Artifacts/Localization/05-Language-Dropdown.png");
var list=settings.Language.transform.Find("Dropdown List");
return new {expanded=settings.Language.IsExpanded, items=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(list.GetComponentsInChildren<UnityEngine.UI.Toggle>(),x=>new {name=x.name,text=x.GetComponentInChildren<TMPro.TMP_Text>().text,selected=x.isOn})), label=settings.transform.Find("Settings Card/Language Label").GetComponent<TMPro.TMP_Text>().text};
