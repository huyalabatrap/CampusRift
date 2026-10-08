using UnityEngine;
namespace CampusRift.Skills
{
    [DefaultExecutionOrder(220)]
    public sealed class SkillCastPose : MonoBehaviour
    {
        Transform spine,arm;Quaternion spineBase,armBase,spineApplied,armApplied;
        float duration,age,lean,raise;bool applied;
        void Awake()
        {
            var e=GetComponent<CampusExplorer>();var a=e!=null?e.characterAnimator:null;if(a==null||!a.isHuman)return;
            spine=a.GetBoneTransform(HumanBodyBones.Spine);arm=a.GetBoneTransform(HumanBodyBones.RightUpperArm);
        }
        public void Play(float seconds,float torso,float hand){duration=seconds;age=0;lean=torso;raise=hand;}
        void LateUpdate()
        {
            // Animator overwrites animated bones; undo only a previous offset on an unanimated bone.
            if(applied){if(spine!=null&&Quaternion.Angle(spine.localRotation,spineApplied)<.01f)spine.localRotation=spineBase;if(arm!=null&&Quaternion.Angle(arm.localRotation,armApplied)<.01f)arm.localRotation=armBase;applied=false;}
            if(duration<=0||age>=duration)return;age+=Time.deltaTime;float weight=Mathf.Sin(Mathf.Clamp01(age/duration)*Mathf.PI);
            if(spine!=null){spineBase=spine.localRotation;spineApplied=spineBase*Quaternion.Euler(lean*weight,0,0);spine.localRotation=spineApplied;}
            if(arm!=null){armBase=arm.localRotation;armApplied=armBase*Quaternion.Euler(0,0,raise*weight);arm.localRotation=armApplied;}applied=true;
        }
    }
}
