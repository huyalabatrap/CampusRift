using UnityEngine;
using UnityEngine.UI;
namespace CampusRift.UI
{
    public sealed class TutorialCircleHit : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 p, Camera camera)
        {
            var r = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, p, camera, out var local)) return false;
            return (local-r.rect.center).sqrMagnitude <= Mathf.Pow(Mathf.Min(r.rect.width,r.rect.height)*.5f,2);
        }
    }
}
