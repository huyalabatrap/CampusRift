#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CampusRift.Skills
{
    public static class SkillCatalogSetup
    {
        [MenuItem("Campus Rift/V2/Install Skill Catalog")]
        public static void Install()
        {
            const string path = "Assets/Skills/Core/Resources/SkillCatalog.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(path);
            if (catalog == null) { catalog = ScriptableObject.CreateInstance<SkillCatalog>(); AssetDatabase.CreateAsset(catalog, path); }
            var all = AssetDatabase.FindAssets("t:SkillDefinition").Select(g => AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null && !string.IsNullOrEmpty(s.id)).OrderByDescending(s => s.starter).ThenBy(s => s.unlockRealm).ThenBy(s => s.unlockTier).ThenBy(s => s.id).ToList();
            catalog.skills = all;
            EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssets();
            Debug.Log("Skill catalog: " + all.Count + " skills.");
        }
    }
}
#endif
