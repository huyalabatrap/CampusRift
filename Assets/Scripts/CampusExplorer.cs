using UnityEngine;
using UnityEngine.InputSystem;

namespace CampusRift
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class CampusExplorer : MonoBehaviour
    {
        [Header("References")]
        public Animator characterAnimator;
        public Camera followCamera;

        [Header("Movement")]
        [Min(0.1f)] public float walkSpeed = 6f;
        [Min(0.1f)] public float runSpeed = 10f;
        [Min(0.1f)] public float acceleration = 30f;
        [Min(0f)] public float jumpHeight = 1.05f;
        public float gravity = -25f;
        public Vector3 spawnPosition;
        public float spawnYaw;

        [Header("Boost energy")]
        [Min(1)] public float maxEnergy = 100f;
        [Min(0.1f)] public float boostEnergyPerSecond = 20f;
        [Min(0.1f)] public float energyRecoveryPerSecond = 15f;
        [Min(0)] public float energyRecoveryDelay = 1.5f;
        [Range(0.05f,1)] public float exhaustedRecoveryFraction = 0.3f;
        public float Energy { get; private set; }
        public float EnergyFraction => Mathf.Clamp01(Energy / Mathf.Max(1,maxEnergy));
        public bool BoostExhausted { get; private set; }
        public bool CanBoost => Progression.DevMode.Active || Energy > 0 && !BoostExhausted;
        // Spends stamina for an ability (dodge). Returns false, without spending, when there is not enough.
        public bool TrySpendEnergy(float amount)
        {
            if(Progression.DevMode.Active || amount<=0)return true;
            if(Energy+0.0001f<amount || BoostExhausted)return false;
            Energy=Mathf.Max(0,Energy-amount);energyRecoveryWait=energyRecoveryDelay;
            if(Energy<=0)BoostExhausted=true;
            return true;
        }
        // Full stamina and no exhaustion: item pickups and harnesses.
        public void RefillEnergy(){Energy=maxEnergy;BoostExhausted=false;}
        public float Yaw => yaw;
#if UNITY_EDITOR || (DEVELOPMENT_BUILD && P10_BENCH)
        public void SetValidationCameraOrbit(float validationYaw,float validationPitch){yaw=validationYaw;pitch=validationPitch;}
#endif
        // World direction for a stick/keyboard vector relative to the camera.
        public Vector3 InputToWorld(Vector2 input)=>Quaternion.Euler(0f,yaw,0f)*new Vector3(input.x,0f,input.y);
        // Face a direction while standing or attacking; movement input takes over again immediately.
        public void FaceDirection(Vector3 direction)
        {
            direction.y=0;
            if(direction.sqrMagnitude<.001f||CurrentSpeed>walkSpeed*.6f)return;
            transform.rotation=Quaternion.LookRotation(direction.normalized);
        }
        // Dash: a short burst along a flat direction. Collision still stops it; it ends early when it hits a wall.
        public bool IsDashing => dashRemaining>0;
        public float SkillMoveMultiplier {get;set;}=1f;
        public float EnemyMoveMultiplier {get;set;}=1f;
        public Vector3 DashVelocity => dashRemaining>0?dashVelocity:Vector3.zero;
        public float LastDashDistance {get;private set;}
        Vector3 dashVelocity;float dashRemaining,dashTotal;Vector3 dashStart;
        public void Dash(Vector3 direction,float distance,float duration)
        {
            direction.y=0;if(direction.sqrMagnitude<.001f||duration<=0||distance<=0)return;
            direction.Normalize();dashVelocity=direction*(distance/duration);
            dashRemaining=dashTotal=duration;dashStart=transform.position;LastDashDistance=0;
            transform.rotation=Quaternion.LookRotation(direction);
        }
        public void CancelDash(){dashRemaining=0;dashTotal=0;planarVelocity=Vector3.zero;}
        public void SetProgressionMaxEnergy(float value)
        {
            value=Mathf.Max(1,value);
            if(Mathf.Approximately(maxEnergy,value))return;
            float fraction=EnergyFraction;
            maxEnergy=value;Energy=fraction*maxEnergy;
        }
        float energyRecoveryWait;

        [Header("Camera")]
        public float cameraDistance = 3.4f;
        public float cameraHeight = 1.35f;
        public float mouseSensitivity = 0.12f;
        public float zoomSensitivity = 0.003f;
        public bool invertY;
        public float minCameraDistance = 1.2f;
        public float maxCameraDistance = 6f;
        public LayerMask cameraObstacles = ~0;
        public bool showControls = true;

        static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        static readonly int RunRate = Animator.StringToHash("RunRate");
        readonly RaycastHit[] cameraHits = new RaycastHit[32];
        CharacterController controller;
        Controls.CampusInput controls;
        Vector3 planarVelocity;
        Vector3 cameraPivot;
        float verticalVelocity;
        float yaw;
        float pitch = 14f;
        float resolvedDistance;
        bool cameraInitialized;
        Renderer[] characterRenderers;
        bool closeCameraHidden;
        [System.NonSerialized] GUIStyle titleStyle;
        [System.NonSerialized] GUIStyle controlsStyle;
        bool interfaceOpen;
        public CampusElevator RidingElevator { get; private set; }

        public float CurrentSpeed { get; private set; }
        public Vector3 PlanarVelocity => planarVelocity;
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller != null && controller.isGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controls = GetComponent<Controls.CampusInput>() ?? gameObject.AddComponent<Controls.CampusInput>();
            if (followCamera == null) followCamera = Camera.main;
            if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>();
            if (characterAnimator != null) characterAnimator.applyRootMotion = false;
            characterRenderers = characterAnimator != null ? characterAnimator.GetComponentsInChildren<Renderer>() : new Renderer[0];
            yaw = spawnYaw;
            resolvedDistance = cameraDistance;
            Energy=maxEnergy;
        }

        void Start()
        {
            SetCursorCaptured(true);
            UpdateCamera(0f, true);
        }

        void Update()
        {
            var uiState = UI.UIStateManager.Instance;
            if (uiState != null && (!uiState.GameplayInputEnabled || uiState.EscapeConsumedFrame == Time.frameCount))
            {
                IsSprinting=false;
                // A lift panel leaves the game running; pausing and Game Over freeze energy.
                if(uiState.State==UI.UIState.Modal)UpdateEnergy(false,Time.deltaTime);
                return;
            }
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (uiState == null && !interfaceOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetCursorCaptured(false);
            else if (uiState == null && !interfaceOpen && mouse != null && mouse.leftButton.wasPressedThisFrame)
                SetCursorCaptured(true);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
                showControls = !showControls;
#endif

            bool captured = !interfaceOpen && (Controls.CampusInput.Mobile || Cursor.lockState == CursorLockMode.Locked);
            Vector2 input = Vector2.zero;
            bool sprint = false;
            bool jump = false;
            if (captured && (RidingElevator == null || !RidingElevator.IsMoving))
            {
                input = controls.Move;sprint = controls.Sprint;jump = controls.Pressed(Controls.CampusAction.Jump);
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (controls.Pressed(Controls.CampusAction.Respawn)) ReturnToSpawn();
#endif
            }

            if (captured)
            {
                // Mouse delta is already measured per frame, not per second.
                Vector2 look = controls.TakeLook();float sensitivity=Controls.CampusInput.Mobile?1:mouseSensitivity;
                yaw = Mathf.Repeat(yaw + look.x * sensitivity, 360f);
                // The sky beasts pass overhead; preserve the ordinary orbit/FOV while allowing a full upward look.
                pitch = Mathf.Clamp(pitch - look.y * sensitivity * (invertY ? -1f : 1f), -75f, 65f);
                if(!Controls.CampusInput.Mobile && mouse!=null && GetComponent<Skills.VoidWallSkill>()?.IsPreviewing!=true)
                {
                    bool avatar=GetComponent<Skills.MartialAvatarRuntime>()?.Active??false;
                    cameraDistance = Mathf.Clamp(cameraDistance - mouse.scroll.ReadValue().y * zoomSensitivity,
                        avatar?7.5f:minCameraDistance, avatar?Mathf.Max(9.5f,maxCameraDistance):maxCameraDistance);
                }
            }

            MoveCharacter(input, sprint, jump, dt);
            if (transform.position.y < -10f) ReturnToSpawn();
        }

        void MoveCharacter(Vector2 input, bool sprint, bool jump, float dt)
        {
            // Skill timers use elapsed frame time. Advance a dash with the same clock so
            // a slow frame still completes its requested distance; CharacterController.Move
            // sweeps the displacement and continues to stop at solid geometry.
            if (dashRemaining > 0f) dt = Mathf.Max(dt, Time.deltaTime);
            sprint=sprint && CanBoost && input.sqrMagnitude>.01f;
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            Vector3 desiredVelocity = direction * (sprint ? runSpeed : walkSpeed) * Mathf.Clamp01(SkillMoveMultiplier) * Mathf.Clamp01(EnemyMoveMultiplier);
            if(dashRemaining>0)
            {
                // The dash overrides walking; it ends when its time is up or a wall stops it.
                float dashStep=Mathf.Min(dt,dashRemaining);
                planarVelocity=dt>0?dashVelocity*(dashStep/dt):Vector3.zero;
                dashRemaining-=dashStep;
            }
            else planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, acceleration * dt);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            if (jump && controller.isGrounded)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, -50f);

            if (dashRemaining<=0 && direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction), 1f - Mathf.Exp(-14f * dt));

            Vector3 before = transform.position;
            CollisionFlags flags = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;

            Vector3 displacement = transform.position - before;
            displacement.y = 0f;
            if(dashRemaining>0||dashTotal>0)
            {
                var travelled=transform.position-dashStart;travelled.y=0;LastDashDistance=travelled.magnitude;
                // Blocked by a wall: stop pushing instead of sliding along it.
                if(dashRemaining>0 && dt>0 && displacement.magnitude<dashVelocity.magnitude*dt*.25f)dashRemaining=0;
                if(dashRemaining<=0){dashTotal=0;planarVelocity=desiredVelocity;}
            }
            CurrentSpeed = dt > 0f ? displacement.magnitude / dt : 0f;
            IsSprinting = sprint && input.sqrMagnitude > 0.01f && CurrentSpeed > walkSpeed + 0.2f;
            UpdateEnergy(IsSprinting,Time.deltaTime);
            if (characterAnimator != null)
            {
                // Wall contact stops the run cycle; slower travel reuses the original clip at a slower rate.
                characterAnimator.SetFloat(MoveSpeed, CurrentSpeed, 0.10f, dt);
                characterAnimator.SetFloat(RunRate, Mathf.Clamp(CurrentSpeed / 4.6f, 0.45f, 2.2f));
            }
        }

        void UpdateEnergy(bool boosting,float dt)
        {
            if (Progression.DevMode.Active) { RefillEnergy(); return; }
            if(dt<=0)return;
            if(boosting)
            {
                Energy=Mathf.Max(0,Energy-boostEnergyPerSecond*dt);
                energyRecoveryWait=energyRecoveryDelay;
                if(Energy<=0)BoostExhausted=true;
            }
            else
            {
                float recoveryTime=Mathf.Max(0,dt-energyRecoveryWait);
                energyRecoveryWait=Mathf.Max(0,energyRecoveryWait-dt);
                Energy=Mathf.Min(maxEnergy,Energy+energyRecoveryPerSecond*recoveryTime);
                if(BoostExhausted && Energy>=maxEnergy*exhaustedRecoveryFraction)BoostExhausted=false;
            }
        }

        void LateUpdate()
        {
            if (UI.UIStateManager.Instance != null && !UI.UIStateManager.Instance.GameplayInputEnabled
                && UI.UIStateManager.Instance.State != UI.UIState.Modal) return;
            UpdateCamera(Time.deltaTime, false);
        }

        void UpdateCamera(float dt, bool snap)
        {
            if (followCamera == null) return;
            Vector3 targetPivot = transform.position + Vector3.up * cameraHeight;
            if (snap || !cameraInitialized)
            {
                cameraPivot = targetPivot;
                cameraInitialized = true;
            }
            else cameraPivot = Vector3.Lerp(cameraPivot, targetPivot, 1f - Mathf.Exp(-18f * dt));

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rotation * Vector3.back;
            float safeDistance = cameraDistance;
            int count = Physics.SphereCastNonAlloc(cameraPivot, 0.18f, back, cameraHits,
                cameraDistance, cameraObstacles, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (cameraHits[i].collider.transform.IsChildOf(transform)) continue;
                if (cameraHits[i].collider.GetComponent<Skills.VoidWall>() != null) continue;
                if (cameraHits[i].collider.GetComponentInParent<Monsters.MonsterVitality>() != null) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(0.20f, cameraHits[i].distance - 0.08f));
            }
            resolvedDistance = snap || safeDistance < resolvedDistance ? safeDistance
                : Mathf.Lerp(resolvedDistance, safeDistance, 1f - Mathf.Exp(-8f * dt));
            followCamera.transform.SetPositionAndRotation(cameraPivot + back * resolvedDistance, rotation);
            for(int i=0;i<Monsters.MonsterVitality.Active.Count;i++)Monsters.MonsterVitality.Active[i].AvoidCameraClipping(followCamera.transform.position);
            // Tight cabins can push the camera into the character's head. Hide the local visual
            // at that distance so the player can still see the doors and their destination.
            bool hide = resolvedDistance < (closeCameraHidden ? 1.05f : 0.85f);
            if (hide != closeCameraHidden)
            {
                closeCameraHidden = hide;
                foreach (var renderer in characterRenderers)
                    if (renderer != null) renderer.forceRenderingOff = hide;
            }
        }

        public void ReturnToSpawn()
        {
            GetComponent<CharacterAfterimageTrail>()?.ClearTrail();
            RidingElevator = null;dashRemaining=0;dashTotal=0;
            controller.enabled = false;
            transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(0f, spawnYaw, 0f));
            controller.enabled = true;
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            CurrentSpeed = 0f;
            IsSprinting = false;
            yaw = spawnYaw;
            pitch = 14f;
            UpdateCamera(0f, true);
        }

        public void SetInterfaceOpen(bool open)
        {
            interfaceOpen = open;
            planarVelocity = Vector3.zero;
            if (open)
            {
                CurrentSpeed = 0f;
                IsSprinting = false;
                if (characterAnimator != null) characterAnimator.SetFloat(MoveSpeed, 0f);
            }
            if (UI.UIStateManager.Instance != null)
            {
                if (open) UI.UIStateManager.Instance.OpenModal(() => GetComponent<ElevatorInteraction>()?.ClosePanel());
                else UI.UIStateManager.Instance.CloseModal();
                return;
            }
            SetCursorCaptured(!open);
        }

        public void BeginElevatorRide(CampusElevator elevator)
        {
            RidingElevator = elevator;
            planarVelocity = Vector3.zero;
            verticalVelocity = -2f;
        }

        public void EndElevatorRide(CampusElevator elevator)
        {
            if (RidingElevator == elevator) RidingElevator = null;
        }

        public void MoveWithElevator(Vector3 displacement)
        {
            // Carry the passenger once; do not let the rising floor resolve the same displacement a second time.
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position += displacement;
            controller.enabled = wasEnabled;
            verticalVelocity = -2f;
        }

        static void SetCursorCaptured(bool captured)
        {
            if (UI.UIStateManager.Instance != null) return;
            Cursor.lockState = captured ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !captured;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused) SetCursorCaptured(false);
        }

        void OnDisable()
        {
            IsSprinting = false;
            SetCursorCaptured(false);
            if (characterRenderers != null)
                foreach (var renderer in characterRenderers)
                    if (renderer != null) renderer.forceRenderingOff = false;
            closeCameraHidden = false;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void OnGUI()
        {
            if (UI.UIStateManager.Instance != null) return;
            if (!showControls) return;
            if (titleStyle == null || titleStyle.fontSize != 16)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                titleStyle.normal.textColor = new Color(0.55f, 0.87f, 1f);
                controlsStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
                controlsStyle.normal.textColor = Color.white;
            }
            float width = Mathf.Min(410f, Screen.width - 24f);
            var oldColor = GUI.color;
            GUI.color = new Color(0.025f, 0.045f, 0.07f, 0.86f);
            GUI.DrawTexture(new Rect(12f, Screen.height - 112f, width, 100f), Texture2D.whiteTexture);
            GUI.color = oldColor;
            GUI.Label(new Rect(24f, Screen.height - 108f, width - 24f, 25f), "CAMPUS RIFT  /  EXPLORER", titleStyle);
            GUI.Label(new Rect(24f, Screen.height - 80f, width - 24f, 65f),
                "WASD / Arrows: move   |   Shift: run   |   Space: jump\n"
                + "Mouse: look   |   E: elevator   |   R: return   |   F1: help\n"
                + (Cursor.lockState == CursorLockMode.Locked ? "Esc: release mouse" : "Click the Game view to explore"), controlsStyle);
        }
#endif
    }
}
