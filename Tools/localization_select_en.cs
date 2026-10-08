var settings=UnityEngine.Object.FindAnyObjectByType<CampusRift.UI.SettingsUI>();
var list=settings.Language.transform.Find("Dropdown List");
var item=System.Linq.Enumerable.First(list.GetComponentsInChildren<UnityEngine.UI.Toggle>(),x=>x.GetComponentInChildren<TMPro.TMP_Text>().text.StartsWith("EN"));
var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
UnityEngine.EventSystems.ExecuteEvents.Execute(item.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
settings.Apply();
return new {value=settings.Language.value,language=CampusRift.Localization.LocalizationService.Instance.Language.ToString(),saved=CampusRift.UI.SettingsManager.Instance.ReadSaved().Language.ToString()};
