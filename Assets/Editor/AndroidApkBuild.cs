using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CampusRift.BuildTools
{
    public static class AndroidApkBuild
    {
        public const string OutputPath = "Artifacts/Android/CampusRift.apk";

        [MenuItem("Campus Rift/Build/Android APK")]
        public static void Build()
        {
            BuildTo(OutputPath, false);
        }

        public static void BuildTo(string outputPath, bool development)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before building the APK.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support and restart Unity.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Switch the active build platform to Android first.");

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (scenes.Length == 0 || scenes.Any(scene => !File.Exists(scene)))
                throw new InvalidOperationException("The enabled build scenes are missing.");

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.campusrift.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.splitApplicationBinary = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.development = development;
            AssetDatabase.SaveAssets();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(outputPath), "build-summary.txt"),
                "Result: " + summary.result + "\n" +
                "APK: " + Path.GetFullPath(outputPath) + "\n" +
                "APK bytes: " + (summary.result == BuildResult.Succeeded ? new FileInfo(outputPath).Length : 0) + "\n" +
                "Build report total bytes: " + summary.totalSize + "\n" +
                "Duration: " + summary.totalTime + "\n" +
                "Errors: " + summary.totalErrors + "\n" +
                "Warnings: " + summary.totalWarnings + "\n" +
                "Scenes: " + string.Join(", ", scenes) + "\n" +
                "Package: com.campusrift.game\nArchitecture: ARM64\nSigning: local debug keystore\n");
            if (summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Android APK build failed. See the Unity console and build-summary.txt.");
            Debug.Log("Android APK ready: " + Path.GetFullPath(outputPath));
        }
    }
}
