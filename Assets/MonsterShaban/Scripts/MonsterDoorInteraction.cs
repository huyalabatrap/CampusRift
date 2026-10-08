using UnityEngine;

namespace CampusRift.Monsters
{
    public sealed class MonsterDoorInteraction : MonoBehaviour
    {
        CampusAutomaticDoor[] doors;
        MonsterNavigation navigation;
        float nextCheck;
        void Awake()
        {
            doors = FindObjectsByType<CampusAutomaticDoor>();
            navigation = GetComponent<MonsterNavigation>();
        }
        void Update()
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 0.1f;
            if (navigation == null || !navigation.Ready) return;
            bool blocked = false;
            foreach (var door in doors)
            {
                if (door == null || !door.isActiveAndEnabled) continue;
                // A descending capsule can intersect the leaf above its feet.
                if (transform.position.y + 1.7f < door.doorway.min.y + .15f || transform.position.y + .25f > door.doorway.max.y - .15f) continue;
                Vector3 delta = door.doorway.ClosestPoint(transform.position + Vector3.up) - (transform.position + Vector3.up);
                if (delta.sqrMagnitude > 25) continue;
                // Opening is a local interaction; it discloses no player information.
                if (Vector3.Dot(navigation.Agent.desiredVelocity, delta) > 0 || delta.sqrMagnitude < 2.25f)
                {
                    door.RequestOpenFrom(transform.position);
                    if (delta.sqrMagnitude < 1.44f && door.OpenAmount < 0.95f) blocked = true;
                }
            }
            navigation.HoldForDoor(blocked);
        }
        void OnDisable() { if (navigation != null) navigation.HoldForDoor(false); }
    }
}
