using System;
using System.Collections.Generic;
using UnityEngine;

namespace CampusRift
{
    [Serializable]
    public sealed class ElevatorDoorLeaf
    {
        public Transform transform;
        public Vector3 closedLocalPosition;
        public Vector3 openOffset;

        public void SetOpen(float amount)
        {
            if (transform != null)
                transform.localPosition = closedLocalPosition + openOffset * Mathf.SmoothStep(0f, 1f, amount);
        }
    }

    [Serializable]
    public sealed class ElevatorLanding
    {
        public float height;
        public Vector3[] entrances;
        public Bounds[] doorways;
        public ElevatorDoorLeaf[] doors;
        public TextMesh display;
    }

    [DefaultExecutionOrder(-100)]
    public sealed class CampusElevator : MonoBehaviour
    {
        public enum LiftState { Closed, Opening, Open, Closing, Moving }

        public string building;
        public Transform cabin;
        public Rigidbody cabinBody;
        public Vector3 cabinBasePosition;
        public Bounds[] cabinSpaces;
        public ElevatorDoorLeaf[] cabinDoors;
        public ElevatorLanding[] floors;
        public Vector3 outward;
        public CampusExplorer player;
        public Material displayMaterial;
        [Min(0.1f)] public float travelSpeed = 2.8f;
        [Min(0.1f)] public float acceleration = 2f;
        [Min(0.1f)] public float doorDuration = 1.1f;
        [Min(1f)] public float holdOpenSeconds = 5f;

        public int CurrentFloor { get; private set; }
        public int TargetFloor { get; private set; }
        public LiftState State { get; private set; }
        public float DoorAmount { get; private set; }
        public bool IsMoving => State == LiftState.Moving;
        public int FloorCount => floors == null ? 0 : floors.Length;
        public float CabinOffset => cabin == null ? 0f : cabin.position.y - cabinBasePosition.y;

        // Non-passenger bodies (e.g. Shaban) keep the doors from closing on them. They are
        // never carried, so a body inside the car holds it open until it steps out again.
        static readonly List<Collider> occupants = new List<Collider>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOccupants() => occupants.Clear();
        public static void RegisterOccupant(Collider body) { if (body != null && !occupants.Contains(body)) occupants.Add(body); }
        public static void UnregisterOccupant(Collider body) => occupants.Remove(body);

        readonly Queue<int> requests = new Queue<int>();
        readonly HashSet<int> queuedFloors = new HashSet<int>();
        CharacterController passengerController;
        CampusExplorer passenger;
        float openTimer;
        float speed;
        int lastDisplayFloor = -1;
        LiftState lastDisplayState;
        AudioSource arrivalAudio;
        Material runtimeDisplayMaterial;
        static AudioClip chime;

        void Awake()
        {
            if (player == null) player = FindAnyObjectByType<CampusExplorer>();
            if (player != null) passengerController = player.GetComponent<CharacterController>();
            State = LiftState.Closed;
            CurrentFloor = TargetFloor = 0;
            if (cabin == null || FloorCount == 0) { enabled = false; return; }
            cabin.position = cabinBasePosition;
            foreach (var floor in floors) foreach (var leaf in floor.doors) leaf.SetOpen(0f);
            SetDoors(0f);
            arrivalAudio = cabin.GetComponent<AudioSource>();
            if (arrivalAudio != null) arrivalAudio.clip = GetChime();
            if (displayMaterial != null)
            {
                runtimeDisplayMaterial = new Material(displayMaterial);
                foreach (var floor in floors)
                    if (floor.display != null) floor.display.GetComponent<Renderer>().sharedMaterial = runtimeDisplayMaterial;
                RefreshFontAtlas(null);
                Font.textureRebuilt += RefreshFontAtlas;
            }
            UpdateDisplays();
        }

        void Update() => Tick(Mathf.Min(Time.deltaTime, 0.05f));

        public bool RequestFloor(int index)
        {
            if (index < 0 || index >= FloorCount) return false;
            if (!IsMoving && index == CurrentFloor)
            {
                State = LiftState.Opening;
                openTimer = holdOpenSeconds;
                return true;
            }
            if (IsMoving && index == TargetFloor) return true;
            if (queuedFloors.Add(index)) requests.Enqueue(index);
            if (State == LiftState.Closed) StartNextTrip();
            else if (State == LiftState.Open) openTimer = 0f;
            return true;
        }

        // The same step is used by Update and deterministic scene integration checks.
        public void Tick(float dt)
        {
            if (dt <= 0f || cabin == null || FloorCount == 0) return;
            switch (State)
            {
                case LiftState.Opening:
                    SetDoors(Mathf.MoveTowards(DoorAmount, 1f, dt / doorDuration));
                    if (DoorAmount >= 1f) { State = LiftState.Open; openTimer = holdOpenSeconds; }
                    break;
                case LiftState.Open:
                    if (IsDoorwayBlocked()) openTimer = holdOpenSeconds;
                    else openTimer -= dt;
                    if (openTimer <= 0f) State = LiftState.Closing;
                    break;
                case LiftState.Closing:
                    if (IsDoorwayBlocked()) { State = LiftState.Opening; break; }
                    SetDoors(Mathf.MoveTowards(DoorAmount, 0f, dt / doorDuration));
                    if (DoorAmount <= 0f) StartNextTrip();
                    break;
                case LiftState.Moving:
                    MoveCabin(dt);
                    break;
            }
            UpdateDisplays();
        }

        void StartNextTrip()
        {
            if (DoorAmount > 0f) { State = LiftState.Closing; return; }
            if (requests.Count == 0) { State = LiftState.Closed; return; }
            TargetFloor = requests.Dequeue();
            queuedFloors.Remove(TargetFloor);
            if (TargetFloor == CurrentFloor) { State = LiftState.Opening; return; }
            passenger = player != null && IsInside(player.transform.position) ? player : null;
            if (passenger != null) passenger.BeginElevatorRide(this);
            speed = 0f;
            State = LiftState.Moving;
        }

        void MoveCabin(float dt)
        {
            float destination = cabinBasePosition.y + floors[TargetFloor].height - floors[0].height;
            float remaining = Mathf.Abs(destination - cabin.position.y);
            float desiredSpeed = Mathf.Min(travelSpeed, Mathf.Sqrt(2f * acceleration * remaining));
            speed = Mathf.MoveTowards(speed, desiredSpeed, acceleration * dt);
            Vector3 next = cabin.position;
            next.y = Mathf.MoveTowards(next.y, destination, Mathf.Max(0.01f, speed) * dt);
            Vector3 delta = next - cabin.position;

            // Kinematic cabin and passenger use exactly the same displacement, in the same frame.
            if (cabinBody != null) cabinBody.position = next;
            cabin.position = next;
            Physics.SyncTransforms();
            if (passenger != null && passenger.RidingElevator == this)
                passenger.MoveWithElevator(delta);

            if (Mathf.Abs(next.y - destination) < 0.001f)
            {
                CurrentFloor = TargetFloor;
                speed = 0f;
                if (passenger != null) passenger.EndElevatorRide(this);
                passenger = null;
                State = LiftState.Opening;
                if (arrivalAudio != null) arrivalAudio.Play();
            }
        }

        void SetDoors(float amount)
        {
            DoorAmount = amount;
            foreach (var leaf in cabinDoors) leaf.SetOpen(amount);
            foreach (var leaf in floors[CurrentFloor].doors) leaf.SetOpen(amount);
            Physics.SyncTransforms();
        }

        public bool IsInside(Vector3 feet)
        {
            Vector3 test = feet + Vector3.up * 0.85f - Vector3.up * CabinOffset;
            foreach (var space in cabinSpaces) if (space.Contains(test)) return true;
            return false;
        }

        public bool IsDoorwayBlocked()
        {
            if (IsOccupantBlocking()) return true;
            if (passengerController == null || !passengerController.enabled) return false;
            foreach (var doorway in floors[CurrentFloor].doorways)
                if (doorway.Intersects(passengerController.bounds)) return true;
            return false;
        }

        bool IsOccupantBlocking()
        {
            for (int i = occupants.Count - 1; i >= 0; i--)
            {
                var body = occupants[i];
                if (body == null) { occupants.RemoveAt(i); continue; }
                if (!body.enabled || !body.gameObject.activeInHierarchy) continue;
                Bounds bounds = body.bounds;
                if (IsInside(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z))) return true;
                foreach (var doorway in floors[CurrentFloor].doorways)
                    if (doorway.Intersects(bounds)) return true;
            }
            return false;
        }

        public bool IsQueued(int floor) => queuedFloors.Contains(floor) || (IsMoving && TargetFloor == floor);

        void UpdateDisplays()
        {
            int displayedFloor = CurrentFloor;
            if (IsMoving)
            {
                float height = floors[0].height + CabinOffset;
                float best = float.MaxValue;
                for (int i = 0; i < FloorCount; i++)
                    if (Mathf.Abs(floors[i].height - height) < best)
                    { best = Mathf.Abs(floors[i].height - height); displayedFloor = i; }
            }
            if (lastDisplayFloor == displayedFloor && lastDisplayState == State) return;
            lastDisplayFloor = displayedFloor;
            lastDisplayState = State;
            string direction = IsMoving ? (TargetFloor > CurrentFloor ? "  ^" : "  v") : "";
            foreach (var floor in floors)
                if (floor.display != null) floor.display.text = building + "  " + (displayedFloor + 1).ToString("00") + direction;
        }

        void OnDisable()
        {
            if (passenger != null) passenger.EndElevatorRide(this);
        }

        void RefreshFontAtlas(Font rebuilt)
        {
            if (runtimeDisplayMaterial == null || FloorCount == 0 || floors[0].display == null) return;
            Font font = floors[0].display.font;
            if (font != null && (rebuilt == null || rebuilt == font))
                runtimeDisplayMaterial.mainTexture = font.material.mainTexture;
        }

        void OnDestroy()
        {
            Font.textureRebuilt -= RefreshFontAtlas;
            if (runtimeDisplayMaterial != null) Destroy(runtimeDisplayMaterial);
        }

        static AudioClip GetChime()
        {
            if (chime != null) return chime;
            const int sampleRate = 22050;
            var samples = new float[sampleRate / 2];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)sampleRate;
                float frequency = t < 0.23f ? 880f : 660f;
                float localTime = t < 0.23f ? t : t - 0.23f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * Mathf.Exp(-12f * localTime)
                    * Mathf.Clamp01(localTime * 120f) * 0.22f;
            }
            chime = AudioClip.Create("Elevator arrival", samples.Length, 1, sampleRate, false);
            chime.SetData(samples, 0);
            return chime;
        }
    }
}
