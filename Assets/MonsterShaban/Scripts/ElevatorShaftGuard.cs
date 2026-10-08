using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusRift.Monsters
{
    // The baked NavMesh ignores the lift hierarchy, so some shafts (ground floors) keep
    // walkable polygons behind closed landing doors. A carving obstacle removes that area
    // whenever the car is not standing at the landing with its doors open, so no agent
    // walks through shut doors into an empty shaft. Created once at runtime for any monster.
    [DefaultExecutionOrder(-90)]
    public sealed class ElevatorShaftGuard : MonoBehaviour
    {
        sealed class Guard { public CampusElevator Lift; public int Floor; public NavMeshObstacle Obstacle; }
        readonly List<Guard> guards = new List<Guard>();
        static ElevatorShaftGuard instance;
        public static int GuardCount => instance != null ? instance.guards.Count : 0;

        public static void Ensure(int agentTypeID)
        {
            if (instance != null) return;
            var go = new GameObject("Elevator Shaft Guard");
            instance = go.AddComponent<ElevatorShaftGuard>();
            instance.Build(agentTypeID);
        }

        void Build(int agentTypeID)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = agentTypeID, areaMask = NavMesh.AllAreas };
            foreach (var lift in FindObjectsByType<CampusElevator>())
            {
                if (lift.cabinSpaces == null || lift.floors == null || lift.floors.Length == 0) continue;
                float baseHeight = lift.floors[0].height;
                for (int f = 0; f < lift.FloorCount; f++)
                    foreach (var space in lift.cabinSpaces)
                    {
                        // cabinSpaces are authored for the base floor; shift them to this landing.
                        Vector3 center = space.center + Vector3.up * (lift.floors[f].height - baseHeight);
                        Vector3 floorPoint = new Vector3(center.x, lift.floors[f].height, center.z);
                        if (!NavMesh.SamplePosition(floorPoint + Vector3.up * 0.05f, out var hit, 0.45f, filter)) continue;
                        if (Mathf.Abs(hit.position.y - floorPoint.y) > 0.35f || !space.Contains(new Vector3(hit.position.x, space.center.y, hit.position.z))) continue;
                        var go = new GameObject("Shaft " + lift.building + " F" + (f + 1).ToString("00"));
                        go.transform.SetParent(transform, false);
                        go.transform.position = floorPoint + Vector3.up * 1f;
                        var obstacle = go.AddComponent<NavMeshObstacle>();
                        obstacle.shape = NavMeshObstacleShape.Box;
                        // Cover the car floor and its threshold up to the landing door line.
                        Vector3 size = space.size; size.y = 2f;
                        Vector3 outward = new Vector3(Mathf.Abs(lift.outward.x), 0, Mathf.Abs(lift.outward.z));
                        size += outward * 0.5f;
                        obstacle.size = size;
                        obstacle.center = lift.outward * 0.25f;
                        obstacle.carving = true;
                        obstacle.carveOnlyStationary = true;
                        guards.Add(new Guard { Lift = lift, Floor = f, Obstacle = obstacle });
                    }
            }
            Refresh();
        }

        void Update() => Refresh();

        void Refresh()
        {
            foreach (var guard in guards)
            {
                if (guard.Lift == null || guard.Obstacle == null) continue;
                bool carPresent = !guard.Lift.IsMoving && guard.Lift.CurrentFloor == guard.Floor && guard.Lift.DoorAmount > 0.35f;
                if (guard.Obstacle.enabled == carPresent) guard.Obstacle.enabled = !carPresent;
            }
        }

        void OnDestroy() { if (instance == this) instance = null; }
    }
}
