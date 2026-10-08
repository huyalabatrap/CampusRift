from pathlib import Path
import shutil,subprocess,os
folder=Path('Content/AR-Tech2/native-compile').resolve();folder.mkdir(parents=True,exist_ok=True)
(folder/'settings.gradle').write_text("pluginManagement { repositories { google(); mavenCentral(); gradlePluginPortal() } }\ndependencyResolutionManagement { repositories { google(); mavenCentral() } }\nrootProject.name='CampusRiftTech2NativeCompile'\n",encoding='utf-8')
(folder/'build.gradle').write_text("""plugins { id 'com.android.library' version '9.0.0' }
android { namespace 'com.campusrift.tech'; compileSdk 36
    defaultConfig { minSdk 28; targetSdk 36 }
    compileOptions { sourceCompatibility JavaVersion.VERSION_17; targetCompatibility JavaVersion.VERSION_17 }
}
dependencies {
    implementation 'com.google.mediapipe:tasks-vision:1.0.0'
    implementation 'com.alphacephei:vosk-android:0.3.75@aar'
    implementation 'net.java.dev.jna:jna:5.18.1@aar'
}
""",encoding='utf-8')
(folder/'gradle.properties').write_text('android.useAndroidX=true\norg.gradle.jvmargs=-Xmx2048m\n',encoding='utf-8')
shutil.copy2('Library/Bee/Android/Prj/IL2CPP/Gradle/local.properties',folder/'local.properties')
src=folder/'src/main/java';src.mkdir(parents=True,exist_ok=True)
for name in ['GestureBridge.kt','VoiceBridge.kt','ClipBridge.kt']:shutil.copy2(Path('Assets/Plugins/Android')/name,src/name)
(folder/'src/main/AndroidManifest.xml').write_text('<manifest xmlns:android="http://schemas.android.com/apk/res/android"><application /></manifest>',encoding='utf-8')
player=Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/PlaybackEngines/AndroidPlayer')
environment=os.environ.copy();environment['JAVA_HOME']=str(player/'OpenJDK')
launcher=next((player/'Tools/gradle/lib').glob('gradle-launcher-*.jar'))
args=[str(player/'OpenJDK/bin/java.exe'),'-classpath',str(launcher),'org.gradle.launcher.GradleMain','compileDebugKotlin','--console=plain','--no-daemon']
with Path('task/batch-1007/6-native-compile.log').open('w',encoding='utf-8') as log:
    result=subprocess.run(args,cwd=folder,env=environment,stdout=log,stderr=subprocess.STDOUT)
print('Native compilation exit',result.returncode)
print(Path('task/batch-1007/6-native-compile.log').read_text(encoding='utf-8')[-6000:])
