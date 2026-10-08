UnityEngine.Application.runInBackground=true;
var loc=CampusRift.Localization.LocalizationService.Instance;
loc.Discover();
var state=CampusRift.UI.UIStateManager.Instance;
var settings=CampusRift.UI.SettingsManager.Instance;
var result=new {language=loc.Language.ToString(),saved=settings.ReadSaved().Language.ToString(),scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,state=state.State.ToString(),testRunner=UnityEngine.Object.FindAnyObjectByType<CampusRift.Localization.LocalizationPlayTest>()!=null,breakthroughs=CampusRift.Learning.LearningService.Instance.Engine.Progress.breakthroughs};
UnityEngine.ScreenCapture.CaptureScreenshot("Artifacts/Localization/06-MainMenu-VN.png");
return result;
