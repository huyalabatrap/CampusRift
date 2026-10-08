using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CampusRift.AR
{
    public sealed class MockGestureSource : MonoBehaviour,IGestureSource
    {
        public static readonly string[] Labels={"None","Open_Palm","Closed_Fist","Pointing_Up","Victory","Thumb_Down"};
        public event Action<GestureFrame> Result;
        static readonly Key[] Keys={Key.Digit0,Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5};
        public string Label="None";float next;readonly float[] points=new float[63];
        public void Emit(string label,Vector2 palm,float score=1,float size=.18f)
        {
            var hand=label=="None"?null:GestureSyntheticLandmarks.Create(label,new Vector2(palm.x,1-palm.y),size);
            Result?.Invoke(new GestureFrame{label=label,score=score,handed="Right",landmarks=hand,timestampMs=(long)(Time.realtimeSinceStartupAsDouble*1000),width=Screen.width,height=Screen.height,screenCoordinates=true});
        }
        void Update()
        {
#if UNITY_EDITOR
            if(Keyboard.current!=null)for(int i=0;i<=5;i++)if(Keyboard.current[Keys[i]].wasPressedThisFrame)Label=Labels[i];
            if(Time.unscaledTime<next)return;next=Time.unscaledTime+1f/15;
            Vector2 p=Mouse.current!=null?Mouse.current.position.ReadValue():new Vector2(Screen.width*.5f,Screen.height*.5f);Emit(Label,new Vector2(p.x/Screen.width,1-p.y/Screen.height));
#endif
        }
    }
}
