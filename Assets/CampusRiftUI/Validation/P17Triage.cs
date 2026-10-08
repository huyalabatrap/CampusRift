#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
using CampusRift.Skills;
using CampusRift.Monsters;
using CampusRift.Combat;
namespace CampusRift.Validation
{
    // Two direct probes, without replaying or altering the legacy harness.
    public sealed class P17Triage : MonoBehaviour
    {
        IEnumerator Start()
        {
            Application.runInBackground=true;
            var world=new SkillSet1TestWorld{GameplayCamera=true};world.Begin();
            yield return new WaitForSecondsRealtime(1);
            world.Arrange();var skill=world.player.GetComponent<LightningFlashRuntime>();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=world.origin+Vector3.right*4+Vector3.up;wall.transform.localScale=new Vector3(.3f,3,5);Physics.SyncTransforms();
            var cc=world.player.GetComponent<CharacterController>();
            string details="CC collision="+cc.detectCollisions+" wall layer="+wall.layer+" ignored="+Physics.GetIgnoreCollision(cc,wall.GetComponent<Collider>())+"\n";
            world.player.GetComponent<SpiritPower>().Refill();bool accepted=skill.CastAt(world.origin+Vector3.right*10);
            yield return new WaitForSecondsRealtime(2);
            details+="Flash actual gameplay camera accepted="+accepted+" wallDistance="+world.player.LastDashDistance+" position="+world.player.transform.position+" wallStops="+(world.player.LastDashDistance<4)+"\n";
            Destroy(wall);yield return null;
            world.Arrange();var victim=world.victims[0];
            var skin=victim.GetComponentInChildren<SkinnedMeshRenderer>(true);var wrapper=skin.rootBone;
            while(wrapper.parent!=null&&wrapper.parent!=victim.transform)wrapper=wrapper.parent;
            Vector3 before=wrapper.localPosition;Quaternion rotation=wrapper.localRotation;
            var hole=world.player.GetComponent<BlackHoleRuntime>();world.player.GetComponent<SpiritPower>().Refill();hole.ResetCooldownForValidation();bool cast=hole.CastAt(world.origin+Vector3.right*6);
            yield return new WaitForSeconds(1.4f);
            details+="BlackHole actual rig="+wrapper.name+" AnimatorRoot="+(victim.GetComponentInChildren<Animator>().transform==victim.transform)+" cast="+cast+" lifted="+(wrapper.localPosition.y>before.y+.2f)+"\n";
            yield return new WaitForSeconds(2.4f);
            details+="BlackHole wrapper restored="+(Vector3.Distance(wrapper.localPosition,before)<.001f&&Quaternion.Angle(wrapper.localRotation,rotation)<.01f)+" navFailures="+hole.NavigationFailures+"\n";
            File.WriteAllText("task/p17/triage.txt",details);world.End();File.WriteAllText("task/p17/triage-DONE.txt","DONE");Destroy(gameObject);
        }
    }
}
#endif
