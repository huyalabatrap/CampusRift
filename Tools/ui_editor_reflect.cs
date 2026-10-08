var result=new System.Collections.Generic.List<string>();
foreach(var name in new[]{"UnityEditor.Audio.AudioMixerController","UnityEditor.Audio.AudioMixerGroupController","UnityEditor.Audio.AudioMixerSnapshotController","UnityEditor.GameViewSizes","UnityEditor.GameViewSize","UnityEditor.GameView"})
{
var t=typeof(UnityEditor.Editor).Assembly.GetType(name);result.Add(name+" = "+t);
if(t!=null)foreach(var m in t.GetMembers(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Static))
if(m.Name.Contains("Create") || m.Name.Contains("Group") || m.Name.Contains("Expose") || m.Name.Contains("Volume") || m.Name.Contains("Snapshot") || m.Name.Contains("Size") || m.Name=="instance")result.Add(m.ToString());
}return result;
