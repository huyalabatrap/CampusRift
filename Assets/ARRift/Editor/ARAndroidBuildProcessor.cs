#if UNITY_EDITOR
using System.IO;
using System.Xml;
using UnityEditor.Android;
namespace CampusRift.AR.Editor
{
    public sealed class ARAndroidBuildProcessor : IPostGenerateGradleAndroidProject
    {
        public int callbackOrder=>200;
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Some Unity patch versions do not import .kt as an Android source plug-in.
            // Update the one emitted source on incremental builds as well. A stale
            // previous bridge would retain ByteArray and the old constructor.
            var emitted=Directory.GetFiles(Path.Combine(path,"src"),"GestureBridge.kt",SearchOption.AllDirectories);
            if(emitted.Length>1)throw new System.InvalidOperationException("Duplicate GestureBridge Kotlin sources");
            string target=emitted.Length==1?emitted[0]:Path.Combine(path,"src/main/java/com/campusrift/gesture/GestureBridge.kt");
            Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy("Assets/Plugins/Android/GestureBridge.kt",target,true);
            foreach(var name in new[]{"VoiceBridge.kt","ClipBridge.kt"})
            {
                var copies=Directory.GetFiles(Path.Combine(path,"src"),name,SearchOption.AllDirectories);
                if(copies.Length>1)throw new System.InvalidOperationException("Duplicate Kotlin source: "+name);
                var destination=copies.Length==1?copies[0]:Path.Combine(path,"src/main/java/com/campusrift/tech",name);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy("Assets/Plugins/Android/"+name,destination,true);
            }
            File.WriteAllText(Path.Combine(path,"proguard-ar.pro"),"-keep class com.campusrift.gesture.** { *; }\n-keep class com.campusrift.tech.** { *; }\n-keep class org.vosk.** { *; }\n-keep class com.sun.jna.** { *; }\n-keep class com.google.mediapipe.** { *; }\n");
            // Unity 6 emits userLandscape for landscape-only AutoRotation.
            // Use the explicit sensorLandscape activity requested by the AR build;
            // the scene itself locks a fixed landscape orientation before XR.
            var manifestPath=Path.Combine(path,"src/main/AndroidManifest.xml");
            var manifest=new XmlDocument();manifest.Load(manifestPath);
            const string android="http://schemas.android.com/apk/res/android";
            bool found=false;
            if(manifest.SelectSingleNode("/manifest/uses-permission[@android:name='android.permission.VIBRATE']",Namespaces(manifest))==null)
            {var permission=manifest.CreateElement("uses-permission");permission.SetAttribute("name",android,"android.permission.VIBRATE");manifest.DocumentElement.AppendChild(permission);}
            foreach(var name in new[]{"android.permission.RECORD_AUDIO","android.permission.FOREGROUND_SERVICE","android.permission.FOREGROUND_SERVICE_MEDIA_PROJECTION"})
            {
                if(manifest.SelectSingleNode("/manifest/uses-permission[@android:name='"+name+"']",Namespaces(manifest))!=null)continue;
                var permission=manifest.CreateElement("uses-permission");permission.SetAttribute("name",android,name);manifest.DocumentElement.AppendChild(permission);
            }
            // Never let Android request the optional microphone at application launch.
            var application=(XmlElement)manifest.SelectSingleNode("/manifest/application");
            var skip=application.SelectSingleNode("meta-data[@android:name='unityplayer.SkipPermissionsDialog']",Namespaces(manifest)) as XmlElement;
            if(skip==null){skip=manifest.CreateElement("meta-data");skip.SetAttribute("name",android,"unityplayer.SkipPermissionsDialog");application.AppendChild(skip);}skip.SetAttribute("value",android,"true");
            var clipActivity=application.SelectSingleNode("activity[@android:name='com.campusrift.tech.ClipConsentActivity']",Namespaces(manifest)) as XmlElement;
            if(clipActivity==null){clipActivity=manifest.CreateElement("activity");clipActivity.SetAttribute("name",android,"com.campusrift.tech.ClipConsentActivity");application.AppendChild(clipActivity);}
            clipActivity.SetAttribute("exported",android,"false");clipActivity.SetAttribute("theme",android,"@android:style/Theme.Translucent.NoTitleBar");clipActivity.SetAttribute("excludeFromRecents",android,"true");clipActivity.SetAttribute("screenOrientation",android,"sensorLandscape");
            var clipService=application.SelectSingleNode("service[@android:name='com.campusrift.tech.ClipRecordingService']",Namespaces(manifest)) as XmlElement;
            if(clipService==null){clipService=manifest.CreateElement("service");clipService.SetAttribute("name",android,"com.campusrift.tech.ClipRecordingService");application.AppendChild(clipService);}
            clipService.SetAttribute("exported",android,"false");clipService.SetAttribute("foregroundServiceType",android,"mediaProjection");
            foreach(XmlElement activity in manifest.SelectNodes("/manifest/application/activity"))
            {
                var name=activity.GetAttribute("name",android);
                if(name!="com.unity3d.player.UnityPlayerGameActivity"&&name!="com.unity3d.player.UnityPlayerActivity")continue;
                activity.SetAttribute("screenOrientation",android,"sensorLandscape");found=true;
            }
            if(!found)throw new System.InvalidOperationException("Unity player activity missing from generated Android manifest");
            manifest.Save(manifestPath);
        }
        static XmlNamespaceManager Namespaces(XmlDocument document){var ns=new XmlNamespaceManager(document.NameTable);ns.AddNamespace("android","http://schemas.android.com/apk/res/android");return ns;}
    }
}
#endif
