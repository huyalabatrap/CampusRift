#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift
{
    public sealed class CampusTraversalProbe : MonoBehaviour
    {
        public readonly HashSet<string> blockers = new HashSet<string>();
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < 0.45f) blockers.Add(hit.collider.name);
        }
    }
}
#endif
