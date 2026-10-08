using UnityEngine;
using UnityEngine.InputSystem;
using CampusRift.Monsters;
using CampusRift.UI;

namespace CampusRift.Skills
{
    [DefaultExecutionOrder(100), DisallowMultipleComponent, RequireComponent(typeof(CampusExplorer))]
    public sealed class VoidWallSkill : MonoBehaviour
    {
        public VoidWallConfig config;
        public VoidWall wallPrefab;
        public int MaxCharges {get;private set;}
        public int Charges {get;private set;}
        public bool IsPreviewing {get;private set;}
        public WallPlacement Placement {get;private set;}
        public float PreviewDistance {get;private set;}
        public string Feedback {get;private set;}="";
        public float FeedbackUntil {get;private set;}
        public VoidWall LastDeployed {get;private set;}
        public System.Action Changed;
        VoidWall[] pool;
        VoidWallPlacement validator;
        CampusExplorer explorer;
        Controls.CampusInput controls;
        public float CooldownRemaining => CampusRift.Progression.DevMode.NoCooldown ? 0 : Mathf.Max(0,nextUse-Time.time);
        public void AdvanceCooldown(float seconds){seconds=Mathf.Max(0,seconds);nextUse=Mathf.Max(Time.time,nextUse-seconds);if(rechargeAt>0)rechargeAt=Mathf.Max(Time.time,rechargeAt-seconds);}
        VoidWallRuntime runtime;
        public float RechargeSeconds => config.rechargeSeconds*(runtime!=null?runtime.CooldownMultiplier:1);
        public float DeployCooldown=>config.deployCooldown*(runtime!=null?runtime.CooldownMultiplier:1);
        public float RechargeRemaining => Charges<MaxCharges && rechargeAt>0 ? Mathf.Max(0,rechargeAt-Time.time) : 0;
        float rechargeAt;
        PlayerMonsterHealth health;
        Collider[] ownerColliders;
        GameObject preview;
        MeshRenderer previewRenderer;
        MaterialPropertyBlock properties;
        AudioSource sound;
        float nextUse,nextFeedback,holdStart,pendingUntil,threatScan;
        int poolIndex;
        // Quick cast: tap Q deploys at the smart placement, holding Q past this reveals the aim preview.
        const float HoldToAim=0.18f,BufferWindow=0.45f;
        bool heldByKey,previewShown,snapPreview,keyWasHeld;
        MonsterBrain[] monsters=new MonsterBrain[0];
        public bool CastQueued=>pendingUntil>Time.time;
        public bool IsUnlocked => Learning.LearningSkillGate.Allows(this);
        bool InputAllowed => IsUnlocked && health.CurrentHealth>0 && Time.timeScale>0 &&
            (UIStateManager.Instance==null || UIStateManager.Instance.GameplayInputEnabled);

        void Awake()
        {
            explorer=GetComponent<CampusExplorer>();health=GetComponent<PlayerMonsterHealth>();
            controls=GetComponent<Controls.CampusInput>();
            runtime=GetComponent<VoidWallRuntime>();
            MaxCharges=Mathf.Clamp(config.charges,1,20);
            Charges=MaxCharges;
            ownerColliders=GetComponentsInChildren<Collider>();validator=new VoidWallPlacement();properties=new MaterialPropertyBlock();
            // Walls outlive a recharge, so keep a few spare bodies beyond the charge count.
            pool=new VoidWall[MaxCharges+2];
            for(int i=0;i<pool.Length;i++){pool[i]=Instantiate(wallPrefab);pool[i].name="Void Wall "+(i+1);pool[i].gameObject.SetActive(false);}
            preview=new GameObject("Void Wall Placement");
            var mf=preview.AddComponent<MeshFilter>();mf.sharedMesh=wallPrefab.surface.GetComponent<MeshFilter>().sharedMesh;
            previewRenderer=preview.AddComponent<MeshRenderer>();previewRenderer.sharedMaterial=config.wallMaterial;
            previewRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;previewRenderer.receiveShadows=false;
            preview.SetActive(false);
            sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=0;sound.outputAudioMixerGroup=config.output;sound.volume=0.35f;SkillAudio.Apply(sound);
        }
        void Update()
        {
            Recharge();
            if(!InputAllowed){CancelPreview();pendingUntil=0;keyWasHeld=false;return;}
            if(controls==null)controls=GetComponent<Controls.CampusInput>();
            // Edge from the held state too, so a press is never lost to frame/event timing (low FPS, background editor).
            bool keyHeld=controls.Holding(Controls.CampusAction.Wall);
            bool wallPressed=controls.Pressed(Controls.CampusAction.Wall)||(keyHeld&&!keyWasHeld);
            keyWasHeld=keyHeld;
            if(wallPressed)
            {
                if(IsPreviewing)Confirm();
                else if(Charges>0 && CooldownRemaining>0)pendingUntil=Time.time+BufferWindow;   // mashed during cooldown: cast as soon as it ends
                else if(BeginPreview(false)){heldByKey=!Controls.CampusInput.Mobile;holdStart=Time.unscaledTime;}
            }
            else if(IsPreviewing && controls.Pressed(Controls.CampusAction.Confirm))Confirm();
            if(IsPreviewing && controls.Pressed(Controls.CampusAction.Cancel))CancelPreview();
            if(IsPreviewing && heldByKey)
            {
                if(!controls.Holding(Controls.CampusAction.Wall))Confirm();   // tap or end of aim: release deploys
                else if(!previewShown && Time.unscaledTime-holdStart>=HoldToAim)ShowPreview();
            }
            if(!IsPreviewing && CastQueued && CooldownRemaining<=0){pendingUntil=0;QuickCast();}
            if(IsPreviewing)
            {
                if(!Controls.CampusInput.Mobile && Mouse.current!=null)
                    PreviewDistance=Mathf.Clamp(PreviewDistance+Mouse.current.scroll.ReadValue().y*0.005f,
                        config.minimumPlacementDistance,config.maximumPlacementDistance);
                RefreshPreview();
            }
        }
        public bool BeginPreview() => BeginPreview(true);
        public bool BeginPreview(bool visible)
        {
            if (CampusRift.Progression.DevMode.NoCooldown) RefillCharges();
            if(!InputAllowed)return false;
            if(Charges<=0){Notify("NO CHARGES — 0 / "+MaxCharges,config.empty);return false;}
            if(CooldownRemaining>0)return false;
            GetComponent<PhantomDecoySkill>()?.CancelPreview();GetComponent<GiantHandSkill>()?.CancelPreview();
            PreviewDistance=Mathf.Clamp(config.placementDistance,config.minimumPlacementDistance,config.maximumPlacementDistance);
            IsPreviewing=true;previewShown=false;heldByKey=false;RefreshPreview();
            if(visible)ShowPreview();
            Changed?.Invoke();return true;
        }
        void ShowPreview()
        {
            previewShown=true;snapPreview=true;preview.SetActive(true);RefreshPreview();
            if(config.preview!=null)sound.PlayOneShot(config.preview,0.35f);
            Changed?.Invoke();
        }
        /// <summary>One-press cast at the smart placement (keyboard tap, buffered press).</summary>
        public bool QuickCast() => BeginPreview(false) && Confirm();
        public void CancelPreview()
        {if(!IsPreviewing)return;IsPreviewing=false;heldByKey=false;previewShown=false;if(preview!=null)preview.SetActive(false);Changed?.Invoke();}
        Vector3 Aim => controls!=null?controls.WallDirection(explorer.followCamera):transform.forward;
        public WallPlacement Evaluate(Vector3 origin,Vector3 aim) => Evaluate(origin,aim,config.placementDistance);
        public WallPlacement Evaluate(Vector3 origin,Vector3 aim,float distance)
        {validator.threat=NearestThreat(origin);return validator.Evaluate(origin,aim,transform,config,distance);}
        Transform NearestThreat(Vector3 origin)
        {
            for(int pass=0;pass<2;pass++)
            {
                Transform best=null;float closest=float.PositiveInfinity;
                foreach(var m in monsters)
                    if(m!=null && m.gameObject.activeInHierarchy){float d=(m.transform.position-origin).sqrMagnitude;if(d<closest){closest=d;best=m.transform;}}
                if(best!=null || pass==1 || Time.unscaledTime<threatScan)return best;
                // Nothing cached is alive: rescan (throttled) so a respawned or newly enabled monster is picked up.
                threatScan=Time.unscaledTime+.5f;monsters=FindObjectsByType<MonsterBrain>();
            }
            return null;
        }
        public void RefreshPreview()
        {
            float distance=PreviewDistance;
            if(Controls.CampusInput.Mobile && controls!=null && controls.Aiming==Controls.CampusAction.Wall)
            {
                float vertical=controls.SkillDrag.y;
                distance=Mathf.Clamp(config.placementDistance+vertical*(vertical<0?
                    config.placementDistance-config.minimumPlacementDistance:
                    config.maximumPlacementDistance-config.placementDistance),
                    config.minimumPlacementDistance,config.maximumPlacementDistance);
            }
            PreviewDistance=distance;
            Placement=Evaluate(transform.position,Aim,distance);
            // The ghost glides to the solved spot instead of jittering between frames; deployment uses the exact spot.
            var t=preview.transform;float k=snapPreview?1:1-Mathf.Exp(-22*Time.unscaledDeltaTime);snapPreview=false;
            t.SetPositionAndRotation(Vector3.Lerp(t.position,Placement.feet,k),Quaternion.Slerp(t.rotation,Placement.rotation,k));
            t.localScale=Vector3.Lerp(t.localScale,new Vector3(Placement.width,config.height,1),k);
            properties.SetFloat("_Preview",Placement.valid?1:-1);properties.SetFloat("_Reveal",1);properties.SetFloat("_Fade",0);properties.SetFloat("_Age",Time.time);properties.SetFloat("_Integrity",1);
            previewRenderer.SetPropertyBlock(properties);
        }
        public bool Confirm()
        {
            if(!IsPreviewing || !InputAllowed || Charges<=0 || CooldownRemaining>0)return false;
            RefreshPreview();
            if(!Placement.valid){Notify(Placement.reason,config.empty);return false;}
            if(!SkillSpirit.TryPay(this,"hu-khong-ket-gioi")){CancelPreview();Notify("NOT ENOUGH SPIRIT",config.empty);return false;}
            LastDeployed=NextWall();
            // V2: the barrier is worth 40% of the caster's maximum health.
            var stats=GetComponent<Combat.PlayerStats>();
            LastDeployed.Deploy(Placement.feet,Placement.rotation,Placement.width,ownerColliders,stats!=null?stats.MaxHealth*config.healthShare*(runtime!=null?runtime.EffectMultiplier:1):0,gameObject,runtime!=null&&runtime.Mastered);
            Charges--;nextUse=Time.time+DeployCooldown;
            GetComponent<PlayerSoundEmitter>()?.Combat();
            CancelPreview();Notify("VOID WALL DEPLOYED",null);Changed?.Invoke();return true;
        }
        // Full refill: harnesses, and later pickups/items.
        public void RefillCharges(){Charges=MaxCharges;rechargeAt=0;Changed?.Invoke();}
        public void ReadyOnRestEquip(){nextUse=0;RefillCharges();}
        void Recharge()
        {
            if(CampusRift.Progression.DevMode.NoCooldown) { RefillCharges(); return; }
            if(Charges>=MaxCharges || RechargeSeconds<=0){rechargeAt=0;return;}
            if(rechargeAt<=0)rechargeAt=Time.time+RechargeSeconds;
            if(Time.time<rechargeAt)return;
            Charges++;rechargeAt=Charges<MaxCharges?Time.time+RechargeSeconds:0;Changed?.Invoke();
        }
        // Prefer an idle wall; otherwise recycle the oldest one.
        VoidWall NextWall()
        {
            for(int i=0;i<pool.Length;i++)
            {
                var candidate=pool[(poolIndex+i)%pool.Length];
                if(!candidate.gameObject.activeSelf){poolIndex=(poolIndex+i+1)%pool.Length;return candidate;}
            }
            var oldest=pool[poolIndex];poolIndex=(poolIndex+1)%pool.Length;return oldest;
        }
        void Notify(string text,AudioClip clip)
        {
            Feedback=text;FeedbackUntil=Time.unscaledTime+1.5f;
            if(clip!=null && Time.unscaledTime>=nextFeedback){sound.PlayOneShot(clip,0.45f);nextFeedback=Time.unscaledTime+0.4f;}
            Changed?.Invoke();
        }
        void OnDisable(){CancelPreview();}
        void OnDestroy()
        {
            if(preview!=null)Destroy(preview);
            if(pool!=null)foreach(var wall in pool)if(wall!=null)Destroy(wall.gameObject);
        }
    }
}
