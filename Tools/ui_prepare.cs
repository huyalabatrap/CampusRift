var packages = System.IO.Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", System.IO.SearchOption.AllDirectories);
if (packages.Length > 0 && !System.IO.Directory.Exists("Assets/TextMesh Pro")) UnityEditor.AssetDatabase.ImportPackage(packages[0], false);
var camera = UnityEngine.Camera.main;
return new {tmpPackage=packages, cameraPosition=camera.transform.position.ToString(), cameraRotation=camera.transform.eulerAngles.ToString(), playerPosition=UnityEngine.Object.FindAnyObjectByType<CampusRift.CampusExplorer>().transform.position.ToString()};
