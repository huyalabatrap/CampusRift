var result=new System.Collections.Generic.List<string>();
foreach(var name in new[]{"UnityEditor.Audio.AudioMixerController","UnityEditor.Audio.AudioMixerGroupController","UnityEditor.Audio.AudioGroupParameterPath","UnityEditor.Audio.ExposedAudioParameter"})
{
var t=typeof(UnityEditor.Editor).Assembly.GetType(name);result.Add(name);
foreach(var p in t.GetProperties(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))result.Add(p.ToString());
foreach(var c in t.GetConstructors(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))result.Add(c.ToString());
foreach(var f in t.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance))if(f.Name.Contains("guid")||f.Name.Contains("name"))result.Add(f.ToString());
}return result;
