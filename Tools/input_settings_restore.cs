UnityEngine.InputSystem.InputSettings settings;
if(!UnityEditor.EditorBuildSettings.TryGetConfigObject<UnityEngine.InputSystem.InputSettings>("com.unity.input.settings",out settings)||settings==null)
{
 var paths=UnityEditor.AssetDatabase.FindAssets("t:InputSettings");
 settings=paths.Length>0?UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputSettings>(UnityEditor.AssetDatabase.GUIDToAssetPath(paths[0])):UnityEngine.ScriptableObject.CreateInstance<UnityEngine.InputSystem.InputSettings>();
}
UnityEngine.InputSystem.InputSystem.settings=settings;
return new{settings.name,path=UnityEditor.AssetDatabase.GetAssetPath(settings)};
