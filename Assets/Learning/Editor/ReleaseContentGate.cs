using System;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using CampusRift.Learning;
namespace CampusRift.BuildTools
{
    // Applies to all platforms and all build entry points, including development builds.
    public sealed class ReleaseContentGate : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report) { Check(Resources.Load<LearningCatalog>("LearningCatalog")); }
        public static void Check(LearningCatalog catalog)
        {
            var result = LearningContentValidation.Run(catalog);
            if(catalog!=null&&(catalog.courses==null||catalog.courses.Count==0))result.errors.Add("Learning catalog is empty.");
            if (catalog != null)
                foreach (var c in catalog.courses.Where(x => x != null))
                {
                    if (c.isPlaceholder) result.errors.Add("Placeholder chapter: " + c.id);
                    foreach (var l in c.lessons.Where(x => x != null)) if (l.isPlaceholder) result.errors.Add("Placeholder lesson: " + l.id);
                }
            if (result.errors.Count > 0) throw new BuildFailedException("Campus Rift content gate blocked build:\n" + string.Join("\n", result.errors));
        }
    }
}
