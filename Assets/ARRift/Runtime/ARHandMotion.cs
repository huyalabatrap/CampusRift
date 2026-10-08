using System;
using UnityEngine;
namespace CampusRift.AR
{
    public enum ARHandMotionState { Idle, Pinching, Swiping, Released }
    // Dynamic motions own their release rules. No model label is trained or synthesized.
    public sealed class ARHandMotion
    {
        public ARHandMotionState State {get;private set;}
        public bool BlocksStatic {get;private set;}
        public bool CompletedThisFrame {get;private set;}
        public string Reason {get;private set;}="idle";
        public Vector3 Velocity {get;private set;}
        public event Action PinchStarted;
        public event Action<Vector3,bool> PinchReleased;
        public event Action<int> Swiped;
        int epoch=-1,releaseCount;long lastId=-1;double stamp=-1,born,releaseAt,quietAt;
        Vector3 prior,start,releaseVelocity;double movingAt;Quaternion reference;Vector3 priorCamera;Quaternion priorRotation;
        bool hadPoint,pinchArmed=true,swipeArmed=true;
        static Vector3 W(GestureFrame f,int i)=>new Vector3(f.worldLandmarks[3*i],f.worldLandmarks[3*i+1],f.worldLandmarks[3*i+2]);
        public void Cancel()
        {
            if(State==ARHandMotionState.Pinching)PinchReleased?.Invoke(Vector3.zero,false);
            // Invalid data or lifecycle cancellation is not evidence of a dynamic motion.
            State=ARHandMotionState.Released;BlocksStatic=false;CompletedThisFrame=false;Reason="cancelled";Velocity=releaseVelocity=Vector3.zero;hadPoint=false;stamp=-1;quietAt=-1;releaseCount=0;pinchArmed=swipeArmed=false;
        }
        public void Process(GestureFrame f,Camera camera,ARModeSettings settings,bool allowed,bool actions)
        {
            CompletedThisFrame=false;
            long id=f.frameId>0?f.frameId:f.timestampMs;double now=f.sensorTimestamp>0?f.sensorTimestamp:f.timestampMs*.001;
            if(f.epoch<epoch)return;
            if(epoch!=f.epoch){Cancel();epoch=f.epoch;lastId=-1;stamp=-1;}
            if(id<=lastId||now<=stamp)return;
            double dt=stamp<0?0:now-stamp;stamp=now;lastId=id;
            if(!allowed||camera==null||f.acquireMs>0&&f.consumeMs-f.acquireMs>250){Cancel();Reason=!allowed?"paused":camera==null?"no-camera":"stale";return;}
            bool hand=f.handPresent||f.landmarks!=null&&f.landmarks.Length==63;
            if(!hand)
            {
                Reason="no-hand";
                if(State==ARHandMotionState.Pinching)Cancel();hadPoint=false;Velocity=Vector3.zero;
                if(quietAt<0)quietAt=now;
                if(now-quietAt>=.2){State=ARHandMotionState.Idle;BlocksStatic=false;Reason="idle";pinchArmed=swipeArmed=true;}
                return;
            }
            var geometry=GestureGeometry.Evaluate(f,settings);
            if(!geometry.inFrame||!geometry.quality||dt>.15){Cancel();Reason=dt>.15?"capture-gap":geometry.reason;return;}
            float size=Vector3.Distance(W(f,5),W(f,17));
            float ratio=Vector3.Distance(W(f,4),W(f,8))/Mathf.Max(.001f,size);
            // A folded fist's close fingertips are not a pinch.
            bool pinched=ratio<.32f&&Vector3.Distance(W(f,8),W(f,0))>size*.85f&&Vector3.Distance(W(f,4),W(f,0))>size*.6f;
            var posePosition=f.hasCameraPose?f.cameraPosition:camera.transform.position;
            var poseRotation=f.hasCameraPose?f.cameraRotation:camera.transform.rotation;
            Vector2 a=GestureCoordinates.Viewport(f,new Vector2(f.landmarks[15],f.landmarks[16]));
            Vector2 b=GestureCoordinates.Viewport(f,new Vector2(f.landmarks[51],f.landmarks[52]));
            float width=Vector2.Distance(a*new Vector2(Screen.width,Screen.height),b*new Vector2(Screen.width,Screen.height));
            float focal=Screen.height/(2*Mathf.Tan(camera.fieldOfView*.5f*Mathf.Deg2Rad));
            float depth=Mathf.Clamp(focal*size/Mathf.Max(8,width),.18f,1.5f);
            Vector2 pixel=geometry.palm*new Vector2(Screen.width,Screen.height);
            // Reconstruct a palm in AR world space before computing motion, removing camera yaw/translation.
            var point=posePosition+poseRotation*new Vector3((pixel.x-Screen.width*.5f)*depth/focal,(pixel.y-Screen.height*.5f)*depth/focal,depth);
            if(hadPoint&&(Vector3.Distance(posePosition,priorCamera)>.25f||Quaternion.Angle(poseRotation,priorRotation)>25)){Cancel();Reason="camera-jump";return;}
            Velocity=hadPoint&&dt>0?(point-prior)/(float)dt:Vector3.zero;
            var local=Quaternion.Inverse(poseRotation)*Velocity;
            bool moving=hadPoint&&Mathf.Abs(local.x)>.45f&&Mathf.Abs(local.x)>Mathf.Abs(local.y)*1.5f;
            prior=point;priorCamera=posePosition;priorRotation=poseRotation;hadPoint=true;
            if(State==ARHandMotionState.Pinching)
            {
                BlocksStatic=true;Reason="pinching";
                if(Velocity.magnitude>=.55f&&ratio<=.5f){releaseVelocity=Velocity;movingAt=now;}
                if(ratio>.5f){if(releaseCount++==0)releaseAt=now;if(releaseCount>=2&&now-releaseAt>=.05){State=ARHandMotionState.Released;BlocksStatic=false;CompletedThisFrame=true;quietAt=-1;PinchReleased?.Invoke(now-movingAt<=.18?releaseVelocity:Vector3.zero,actions);}}
                else releaseCount=0;
                return;
            }
            if(pinched)
            {
                BlocksStatic=pinchArmed;Reason=pinchArmed?"pinch":"pinch-unarmed";quietAt=-1;
                if(pinchArmed){pinchArmed=false;State=ARHandMotionState.Pinching;releaseCount=0;releaseVelocity=Vector3.zero;movingAt=-1;if(actions)PinchStarted?.Invoke();}
                return;
            }
            if(moving)
            {
                BlocksStatic=swipeArmed;Reason=swipeArmed?"swipe":"swipe-unarmed";quietAt=-1;
                if(State!=ARHandMotionState.Swiping&&swipeArmed){State=ARHandMotionState.Swiping;born=now;start=point-Velocity*(float)dt;reference=poseRotation;}
                if(State==ARHandMotionState.Swiping)
                {
                    var travel=Quaternion.Inverse(reference)*(point-start);
                    if(now-born>.3){State=ARHandMotionState.Released;BlocksStatic=false;swipeArmed=false;}
                    else if(Mathf.Abs(travel.x)>=.16f&&Mathf.Abs(travel.x)>Mathf.Abs(travel.y)*2)
                    {State=ARHandMotionState.Released;BlocksStatic=false;CompletedThisFrame=true;swipeArmed=false;if(actions)Swiped?.Invoke(travel.x>0?1:-1);}
                }
                return;
            }
            // Below the motion thresholds there is no evidence that static input is owned.
            if(State==ARHandMotionState.Swiping)State=ARHandMotionState.Released;
            BlocksStatic=false;Reason="quiet";
            if(quietAt<0)quietAt=now;
            if(now-quietAt>=.2){State=ARHandMotionState.Idle;BlocksStatic=false;Reason="idle";pinchArmed=swipeArmed=true;}
        }
    }
}
