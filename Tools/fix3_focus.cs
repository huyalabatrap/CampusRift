var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
UnityEditor.EditorWindow.GetWindow(type).Focus();
UnityEngine.Application.runInBackground=true;
return "GameView focused; background rendering enabled";
