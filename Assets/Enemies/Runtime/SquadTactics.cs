using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using CampusRift.Monsters;
using CampusRift.Combat;
using CampusRift.SkyBeast;

namespace CampusRift.Enemies
{
    public enum SquadRole { None, Chaser, FlankerLeft, FlankerRight, Interceptor, Ambusher, Ranged, Support, Flyer }
    public enum SquadPhase { Form, Close }

    // The Director owns one prediction and a time-sliced path queue. Reservations affect candidate
    // costs, never baked NavMesh areas. All paths/buffers are reused after the first pool spawn.
    public sealed class SquadTactics
    {
        public sealed class Assignment
        {
            public EnemyInstance enemy;
            public SquadRole role;
            public Vector3 target, waypoint;
            public bool ready, via, guarding;
            public float signalUntil, nextSignal, score;
            internal float targetUntil;
            internal Vector3 targetHeading;
            public int revision, appliedRevision=-1, leg, selected;
            internal readonly NavMeshPath[] paths=new NavMeshPath[12];
            internal int bank, committedBank;
            internal Vector3 wanted, detour, bestTarget, bestWaypoint;
            internal float bestScore;
            internal float angle, directLength;
            internal bool approaching;
            internal Vector3 approachGate;
            internal Vector3 landingTarget;
            internal bool landingGuard;
            internal float landingUntil,landingSign;
            public int sector;
            internal int best, candidate, stage, slot;
            internal Assignment(EnemyInstance e){enemy=e;for(int i=0;i<paths.Length;i++)paths[i]=new NavMeshPath();}
        }
        readonly EnemyDirector director;
        readonly Dictionary<EnemyInstance,Assignment> assignments=new Dictionary<EnemyInstance,Assignment>(48);
        readonly List<Assignment> squad=new List<Assignment>(48);
        readonly Dictionary<Vector3Int,int> reservations=new Dictionary<Vector3Int,int>(2048);
        readonly Vector3[] corners=new Vector3[128],escapeCorners=new Vector3[128];
        readonly NavMeshPath escapePath;
        readonly int[] exitUses=new int[12];
        readonly float[] actorAngles=new float[128];
        RoomGraph graph;CampusExplorer explorer;Transform player;
        Vector3 lastPlayer,velocity,heading,center;
        Vector3 watchedEscapeStart,watchedEscapeDirection;
        bool watchingEscape;
        float nextPlan,nextRoles,lastSample,stationarySince,formationSince,verticalSpeed;
        float closeSince,escapeUntil,escapeBearing;
        Vector3 sectorHeading;
        int cursor,escapeCandidate,escapeCornerCount,preferredExit=-1;
        bool planning,planningEscape;
        AITierProfile.Tier profile;
        int tier;
        public bool Enabled {get;set;}=true;
        public bool HasEscape {get;private set;}
        public Vector3 EscapePoint {get;private set;}
        public Vector3 Prediction {get;private set;}
        public Vector3 Heading=>heading;
        public bool Herding {get;private set;}
        public SquadPhase Phase {get;private set;}
        public float ActualCoverage {get;private set;}
        public int CloseCount {get;private set;}
        public int PathsThisFrame {get;private set;}
        public int MaxPathsPerFrame {get;private set;}
        public int PlanCount {get;private set;}
        public int SplitCount {get;private set;}
        public int AdaptedExitCount {get;private set;}
        public int Signals {get;private set;}
        public IReadOnlyList<Assignment> Members=>squad;
        public bool ReservePathQuery()
        {
            if(PathsThisFrame>=3)return false;
            PathsThisFrame++;MaxPathsPerFrame=Mathf.Max(MaxPathsPerFrame,PathsThisFrame);return true;
        }
        public SquadTactics(EnemyDirector owner){director=owner;escapePath=new NavMeshPath();}
        public void Register(EnemyInstance e)
        {
            if(!assignments.TryGetValue(e,out var a))assignments.Add(e,a=new Assignment(e));
            a.ready=false;a.role=SquadRole.None;a.appliedRevision=-1;a.signalUntil=0;a.nextSignal=0;
            a.targetUntil=0;
            a.approaching=a.landingGuard=false;a.target=a.waypoint=e.transform.position;
            if(director.Active.Count<=1){formationSince=Time.time;Phase=SquadPhase.Form;escapeUntil=0;}
            nextPlan=nextRoles=0;
        }
        public void Unregister(EnemyInstance e)
        {if(assignments.TryGetValue(e,out var a)){a.ready=false;a.role=SquadRole.None;a.signalUntil=0;}nextRoles=0;}
        public void Reset()
        {
            foreach(var a in assignments.Values){a.ready=false;a.role=SquadRole.None;a.signalUntil=0;a.nextSignal=0;a.appliedRevision=-1;a.approaching=a.landingGuard=false;a.targetUntil=0;}
            squad.Clear();reservations.Clear();System.Array.Clear(exitUses,0,exitUses.Length);
            planning=planningEscape=false;nextPlan=nextRoles=lastSample=0;stationarySince=formationSince=Time.time;
            HasEscape=Herding=watchingEscape=false;preferredExit=-1;PlanCount=SplitCount=AdaptedExitCount=Signals=MaxPathsPerFrame=CloseCount=0;
            Phase=SquadPhase.Form;ActualCoverage=0;escapeUntil=0;sectorHeading=Vector3.zero;
        }
        bool Eligible(EnemyInstance e)=>e!=null&&e.Alive&&e.archetype!=null&&!e.archetype.isBoss&&e.scaling.aiTier>0&&
            e.Brain!=null&&e.Brain.enabled&&e.Brain.Faction==CombatFaction.Hostile&&e.Brain.State==MinionState.Chase&&
            !e.Motor.Held&&!director.Holds(e)&&!(e.GetComponent<EnemyAbilityRunner>()?.Busy??false)&&!(e.GetComponent<ExpandedEnemyRuntime>()?.Busy??false);
        public bool TryGet(EnemyInstance e,out Assignment a)
        {return assignments.TryGetValue(e,out a)&&Enabled&&a.ready&&Eligible(e)&&director.TierFor(e.scaling.aiTier).squad;}
        // Read-only observers must retain a role during its attack/windup. Movement eligibility
        // intentionally rejects token holders, which otherwise hid successful frontal arrivals.
        public bool TryInspect(EnemyInstance e,out Assignment a)
        {return assignments.TryGetValue(e,out a)&&Enabled&&a.ready&&e!=null&&e.Alive;}
        public float FormationSpeed(EnemyInstance e)
        {
            if(!TryGet(e,out var a)||!director.TierFor(e.scaling.aiTier).phasedEncirclement||a.role==SquadRole.Chaser||
                a.role==SquadRole.Ranged||a.role==SquadRole.Support)return 1;
            Vector3 relative=Flat(e.transform.position-center);
            if(!a.approaching&&Vector3.Angle(relative,Direction(a.angle))<30&&relative.magnitude<6&&Vector3.Distance(e.transform.position,a.target)<1.2f)return 1;
            // A telegraphed run to the assigned wing, never a speed-up during windup/strike/retreat.
            return Mathf.Clamp(director.TierFor(e.scaling.aiTier).formationSpeed,1,2);
        }
        public bool CanAttack(EnemyInstance e)
        {
            if(!TryGet(e,out var a)||!director.TierFor(e.scaling.aiTier).phasedEncirclement||squad.Count<5||a.role==SquadRole.Chaser||e.archetype.ranged)return true;
            // Finish the wing before accepting a melee token. A running player must not draw
            // the forward guard into a windup behind them before the interception is complete.
            Vector3 relative=Flat(e.transform.position-center);
            return !a.approaching&&Phase==SquadPhase.Close&&Vector3.Angle(relative,Direction(a.angle))<45&&
                (a.role!=SquadRole.Interceptor||velocity.magnitude<1.5f);
        }
        bool Member(EnemyInstance e)=>e!=null&&e.Alive&&e.archetype!=null&&!e.archetype.isBoss&&e.scaling.aiTier>0&&
            e.Brain!=null&&e.Brain.enabled&&e.Brain.Faction==CombatFaction.Hostile&&e.Brain.State!=MinionState.Dead&&
            (e.Brain.State!=MinionState.Spawn||director.TierFor(e.scaling.aiTier).phasedEncirclement);
        bool CanPlan(EnemyInstance e)=>Eligible(e)||Member(e)&&e.Brain.State==MinionState.Spawn&&!e.Motor.Held;
        public bool TryDestination(EnemyInstance e,out Vector3 point)
        {if(TryGet(e,out var a)){point=a.via&&a.leg==0?a.waypoint:a.target;return true;}point=default;return false;}
        public bool Move(EnemyInstance e)
        {
            if(e.GetComponent<FlyingMotor>()!=null)return false;
            if(!TryGet(e,out var a))
            {
                if(Enabled&&Eligible(e)&&director.TierFor(e.scaling.aiTier).squad&&player!=null&&(e.transform.position-player.position).sqrMagnitude<6400)
                {e.Motor.Stop();return true;}
                return false;
            }
            var motor=e.Motor;
            if(Time.time<a.signalUntil){motor.Stop();motor.FaceTowards(center);return true;}
            if(a.via&&a.leg==0&&Vector3.Distance(e.transform.position,a.waypoint)<1.3f)
            {a.leg=1;a.appliedRevision=-1;if(a.approaching)a.approaching=false;}
            Vector3 goal=a.via&&a.leg==0?a.waypoint:a.target;
            if(Vector3.Distance(e.transform.position,goal)<(a.guarding?.65f:.5f)){motor.Stop();motor.FaceTowards(center);return true;}
            if(a.appliedRevision!=a.revision||!motor.Agent.hasPath)
                if(motor.FollowPath(a.paths[a.committedBank+a.selected*2+(a.via?a.leg:0)]))a.appliedRevision=a.revision;
            bool flank=a.role==SquadRole.FlankerLeft||a.role==SquadRole.FlankerRight||a.role==SquadRole.Ranged;
            motor.Agent.updateRotation=!flank||Vector3.Distance(e.transform.position,center)>12;
            if(!motor.Agent.updateRotation)motor.FaceTowards(center);
            return true;
        }
        public void Tick()
        {
            PathsThisFrame=0;if(!Enabled)return;
            player=director.FindPlayer();if(player==null)return;
            if(!planning&&Time.time>=nextPlan)BeginPlan();
            int budget=Mathf.Clamp(profile.pathsPerFrame,1,3);
            while(planning&&PathsThisFrame<budget)
            {
                if(planningEscape){PlanEscapeStep();continue;}
                if(cursor>=squad.Count){planning=false;break;}
                var a=squad[cursor];if(!CanPlan(a.enemy)){cursor++;continue;}
                if(a.role==SquadRole.Flyer){CommitFlying(a);cursor++;continue;}
                EvaluateStep(a);
            }
            MaxPathsPerFrame=Mathf.Max(MaxPathsPerFrame,PathsThisFrame);
        }
        void BeginPlan()
        {
            center=player.position;if(explorer==null)explorer=player.GetComponent<CampusExplorer>();
            float dt=Time.time-lastSample;
            Vector3 fullMeasured=lastSample>0&&dt>.01f?(center-lastPlayer)/dt:Vector3.zero;
            verticalSpeed=fullMeasured.magnitude<14?fullMeasured.y:0;
            Vector3 measured=Flat(fullMeasured);
            if(measured.magnitude>14)measured=Vector3.zero;
            velocity=explorer!=null&&explorer.PlanarVelocity.sqrMagnitude>.25f?explorer.PlanarVelocity:measured;
            velocity=Vector3.ClampMagnitude(Flat(velocity),12);
            if(velocity.magnitude>1){stationarySince=Time.time;heading=velocity.normalized;}
            else if(heading.sqrMagnitude<.5f)heading=Flat(player.forward).normalized;
            if(watchingEscape)
            {
                Vector3 travelled=Flat(center-watchedEscapeStart);
                if(travelled.magnitude>4)
                {
                    if(Vector3.Dot(travelled.normalized,watchedEscapeDirection)>.7f)
                    {int sector=Sector(watchedEscapeDirection);if(exitUses[sector]<20)exitUses[sector]++;}
                    watchingEscape=false;
                }
            }
            lastPlayer=center;lastSample=Time.time;
            if(graph==null)graph=ShelterGraphReference.Graph;
            int previousCount=squad.Count;squad.Clear();tier=1;
            foreach(var e in director.Active)
            {
                if(e!=null&&(e.transform.position-center).sqrMagnitude>=6400&&assignments.TryGetValue(e,out var far)){far.ready=false;far.role=SquadRole.None;}
                if(Member(e)&&director.TierFor(e.scaling.aiTier).squad&&(e.transform.position-center).sqrMagnitude<6400&&assignments.TryGetValue(e,out var a))
                {squad.Add(a);tier=Mathf.Max(tier,e.scaling.aiTier);}
            }
            if(squad.Count==0){nextPlan=Time.time+.5f;HasEscape=false;return;}
            profile=director.TierFor(tier);
            nextPlan=Time.time+Mathf.Clamp(profile.squadRefresh,.5f,1);
            Prediction=center+Vector3.ClampMagnitude(velocity*Mathf.Clamp(profile.predictionSeconds,2,4),28);
            Prediction+=Vector3.up*Mathf.Clamp(verticalSpeed*profile.predictionSeconds,-4,4);
            bool newMember=false;foreach(var a in squad)if(a.role==SquadRole.None){newMember=true;break;}
            if(newMember||squad.Count!=previousCount||sectorHeading.sqrMagnitude<.5f||Vector3.Angle(sectorHeading,heading)>50||!profile.phasedEncirclement&&Time.time>=nextRoles)
            {AssignRoles();sectorHeading=heading;nextRoles=Time.time+3.2f;}
            ActualCoverage=Coverage();
            if(profile.phasedEncirclement)
            {
                if(Phase==SquadPhase.Form&&ActualCoverage>=profile.closeCoverage){Phase=SquadPhase.Close;closeSince=Time.time;CloseCount++;}
                else if(Phase==SquadPhase.Close&&ActualCoverage<profile.closeCoverage-45){Phase=SquadPhase.Form;formationSince=Time.time;}
            }
            preferredExit=-1;
            if(profile.herding&&squad.Count>=5)
                for(int i=0;i<exitUses.Length;i++)if(exitUses[i]>=2&&(preferredExit<0||exitUses[i]>exitUses[preferredExit]))preferredExit=i;
            Herding=profile.herding&&squad.Count>=5;
            reservations.Clear();cursor=escapeCandidate=escapeCornerCount=0;HasEscape=false;
            planning=planningEscape=true;PlanCount++;
            foreach(var a in squad)
            {
                if(a.approaching&&Vector3.Distance(a.enemy.transform.position,a.approachGate)<1.3f)a.approaching=false;
                a.bank=a.committedBank==0?6:0;a.bestScore=float.PositiveInfinity;a.best=-1;a.candidate=a.stage=0;a.directLength=0;
            }
        }
        static Vector3 Flat(Vector3 p){p.y=0;return p;}
        static int Sector(Vector3 dir)=>Mathf.RoundToInt(Mathf.Repeat(Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,360)/30)%12;
        Vector3 Direction(float degrees)=>Quaternion.Euler(0,degrees,0)*heading;
        float Coverage()
        {
            int count=0;
            foreach(var a in squad)
            {
                Vector3 d=Flat(a.enemy.transform.position-center);if(d.sqrMagnitude<.1f||count>=actorAngles.Length)continue;
                float angle=Mathf.Repeat(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,360);int i=count++;
                while(i>0&&actorAngles[i-1]>angle){actorAngles[i]=actorAngles[i-1];i--;}actorAngles[i]=angle;
            }
            if(count<2)return 0;float gap=0;
            for(int i=0;i<count;i++)gap=Mathf.Max(gap,Mathf.Repeat(actorAngles[(i+1)%count]-actorAngles[i],360));return 360-gap;
        }
        void AssignRoles()
        {
            if(profile.phasedEncirclement){AssignSectors();return;}
            // Re-select 1–2 pressure units first; keep heavy/slow units on that job.
            foreach(var a in squad)if(a.role==SquadRole.Chaser)a.slot=0;
            for(int n=0;n<Mathf.Clamp(profile.chasers,1,2);n++)
            {
                Assignment best=null;float score=float.PositiveInfinity;
                foreach(var a in squad)
                {
                    var e=a.enemy;if(e.archetype.ranged||e.GetComponent<FlyingMotor>()!=null||a.slot<=-100||a.role==SquadRole.Interceptor&&Time.time<a.targetUntil)continue;
                    float s=Vector3.Distance(e.transform.position,center)+e.Speed*3;
                    if(s<score){score=s;best=a;}
                }
                if(best!=null){best.slot=-100-n;best.role=SquadRole.Chaser;}
            }
            int interceptors=0,ambushers=0,flank=0,fly=0,range=0;
            foreach(var a in squad)if(a.role==SquadRole.Interceptor&&Time.time<a.targetUntil)interceptors++;
            foreach(var a in squad)
            {
                var e=a.enemy;var local=director.TierFor(e.scaling.aiTier);SquadRole old=a.role,role;int slot;
                if(e.GetComponent<FlyingMotor>()!=null){role=SquadRole.Flyer;slot=fly++;}
                else if(e.archetype.id=="trieu-hon-su"){role=SquadRole.Support;slot=range++;}
                else if(e.archetype.ranged){role=SquadRole.Ranged;slot=range++;}
                else if(a.role==SquadRole.Interceptor&&Time.time<a.targetUntil){role=SquadRole.Interceptor;slot=a.slot;}
                else if(a.slot<=-100){role=SquadRole.Chaser;slot=a.slot;}
                else if(local.interception&&velocity.magnitude>1.5f&&interceptors<(e.scaling.aiTier>=3?2:1)&&e.Speed>=4.5f){role=SquadRole.Interceptor;slot=interceptors++;}
                else if(local.ambush&&squad.Count>=5&&ambushers<1){role=SquadRole.Ambusher;slot=ambushers++;}
                else{role=flank%2==0?SquadRole.FlankerLeft:SquadRole.FlankerRight;slot=flank++/2;}
                SetRole(a,old,role,slot);
            }
        }
        void AssignSectors()
        {
            // Stable slots cover all four quadrants. Combat states remain members, so taking a
            // token cannot donate an occupied sector to another actor or create extra chasers.
            foreach(var a in squad)a.slot=0;
            for(int n=0;n<Mathf.Min(2,profile.chasers);n++)
            {
                Assignment best=null;float score=float.PositiveInfinity;
                foreach(var a in squad)
                {
                    var e=a.enemy;if(e.archetype.ranged||e.GetComponent<FlyingMotor>()!=null||a.slot<0)continue;
                    float s=Vector3.Dot(Flat(e.transform.position-center),heading)+e.Speed*3;
                    if(s<score){score=s;best=a;}
                }
                if(best!=null)best.slot=-100-n;
            }
            // Give the two closest fast actors the forward wings, preserving their current side.
            // Reserving the nearest actors for rear pressure made the front arrive a second late.
            for(int n=0;n<2;n++)
            {
                Assignment best=null;float score=float.PositiveInfinity;float angle=n==0?-22.5f:22.5f;
                foreach(var a in squad)
                {
                    var e=a.enemy;if(a.slot!=0||e.archetype.ranged||e.GetComponent<FlyingMotor>()!=null)continue;
                    Vector3 delta=Flat(e.transform.position-center);
                    float s=Vector3.Distance(e.transform.position,center+Direction(angle)*4)/Mathf.Max(1,e.Speed);
                    if(Vector3.Dot(delta,Direction(n==0?-90:90))<0)s+=1;
                    if(s<score){score=s;best=a;}
                }
                if(best!=null)best.slot=50+n;
            }
            int wing=0,ranged=0,fly=0;
            foreach(var a in squad)
            {
                var e=a.enemy;var old=a.role;SquadRole role;float angle;
                if(e.GetComponent<FlyingMotor>()!=null){role=SquadRole.Flyer;angle=(fly++%2==0?-1:1)*45;}
                else if(e.archetype.id=="trieu-hon-su"){role=SquadRole.Support;angle=135+ranged++*35;}
                else if(e.archetype.ranged){role=SquadRole.Ranged;angle=(ranged++%2==0?-1:1)*90;}
                else if(a.slot<0){role=SquadRole.Chaser;angle=a.slot==-100?-157.5f:157.5f;}
                else if(a.slot>=50){role=SquadRole.Interceptor;angle=a.slot==50?-22.5f:22.5f;}
                else
                {
                    int w=wing++;angle=w==0?-112.5f:w==1?-67.5f:w==2?67.5f:112.5f+(w-3)*20;
                    role=w==0?SquadRole.Ambusher:angle<0?SquadRole.FlankerLeft:SquadRole.FlankerRight;
                }
                int slot=a.slot<0?a.slot:role==SquadRole.Interceptor?a.slot-50:wing-1;
                SetRole(a,old,role,slot);a.angle=angle;a.sector=Mathf.FloorToInt(Mathf.Repeat(angle+180,360)/90);
                a.approaching=role==SquadRole.Interceptor&&Vector3.Dot(Flat(e.transform.position-center),heading)<0;
                if(a.approaching)
                {
                    Vector3 want=e.transform.position+Direction(angle<0?-90:90)*3.2f+heading*2;
                    if(NavMesh.SamplePosition(want,out var gate,1.8f,NavMesh.AllAreas))
                    {
                        var start=e.transform.position;
                        if(NavMesh.SamplePosition(start,out var from,1.5f,NavMesh.AllAreas)&&NavMesh.Raycast(from.position,gate.position,out var wall,NavMesh.AllAreas))
                            a.approachGate=wall.position+Flat(from.position-wall.position).normalized*.2f;
                        else a.approachGate=gate.position;
                    }
                    else a.approachGate=e.transform.position;
                }
            }
            // The two forward wings enter the path queue first; rear pressure can use a shorter path.
            for(int i=0;i<squad.Count;i++)if(squad[i].role==SquadRole.Interceptor)
            {var a=squad[i];squad.RemoveAt(i);squad.Insert(0,a);}
        }
        void SetRole(Assignment a,SquadRole old,SquadRole role,int slot)
        {
            a.role=role;a.slot=slot;
            if(old!=role)a.targetUntil=0;
            if(role!=old&&role!=SquadRole.Chaser&&Time.time>=a.nextSignal&&a.enemy.Animation!=null)
            {
                // Plan during the existing spawn warning, without moving or cancelling Spawn.
                // Its normal Idle_Alert transition already announces the first formation order.
                if(a.enemy.Brain.State==MinionState.Spawn){a.signalUntil=0;a.nextSignal=Time.time+5;Signals++;return;}
                a.enemy.Motor.Stop();a.enemy.Animation.Play(a.enemy.Animation.HasState("Taunt")?"Taunt":"Idle_Alert",1,.25f);
                a.signalUntil=Time.time+.25f;a.nextSignal=Time.time+5;Signals++;
            }
        }
        // Validate a physical escape BEFORE reserving enemy paths. Warning uses P16's nearest shelter.
        void PlanEscapeStep()
        {
            bool warning=director.Tactics!=null&&director.Tactics.WarningActive&&director.Tactics.Entrances.Count>0;
            if(profile.phasedEncirclement&&Time.time>=escapeUntil)
            {escapeBearing=Mathf.Repeat(Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg+(PlanCount%2==0?135:-135),360);escapeUntil=Time.time+7;}
            float angle=profile.phasedEncirclement?Mathf.DeltaAngle(Mathf.Atan2(heading.x,heading.z)*Mathf.Rad2Deg,escapeBearing)+EscapeAngle(escapeCandidate):EscapeAngle(escapeCandidate);
            if(preferredExit>=0&&Vector3.Angle(Direction(angle),Quaternion.Euler(0,preferredExit*30,0)*Vector3.forward)<profile.escapeDegrees*.5f)angle+=90;
            Vector3 want=warning?director.Tactics.FreeEntrance:center+Direction(angle)*8;
            escapeCandidate++;
            if(Sample(center,out var start)&&Sample(want,out var end))
            {
                PathsThisFrame++;
                if(NavMesh.CalculatePath(start,end,NavMesh.AllAreas,escapePath)&&escapePath.status==NavMeshPathStatus.PathComplete)
                {
                    float clearance=100;foreach(var a in squad)clearance=Mathf.Min(clearance,Flat(a.enemy.transform.position-end).magnitude);
                    if(warning||clearance>2.2f)
                    {
                        HasEscape=true;EscapePoint=end;escapeCornerCount=escapePath.GetCornersNonAlloc(escapeCorners);planningEscape=false;
                        if(!watchingEscape){watchedEscapeStart=center;watchedEscapeDirection=Flat(end-center).normalized;watchingEscape=true;}
                        foreach(var a in squad)a.wanted=Target(a);return;
                    }
                }
            }
            if(escapeCandidate>=12){planningEscape=false;foreach(var a in squad){a.ready=false;a.wanted=a.enemy.transform.position;}}
        }
        float EscapeAngle(int i)=>i==0?0:(i%2==1?1:-1)*((i+1)/2)*30;
        Vector3 Target(Assignment a)
        {
            var e=a.enemy;var local=director.TierFor(e.scaling.aiTier);
            if(local.phasedEncirclement)return SectorTarget(a,local);
            float side=a.role==SquadRole.FlankerLeft?-1:1;
            float close=Time.time-stationarySince>Mathf.Max(5,local.stationarySeconds)?1:Mathf.Clamp01((Time.time-formationSince)/10);
            float radius=local.flankRadius>0?local.flankRadius:6;
            if(Flat(e.transform.position-center).magnitude<12&&!director.Adapting(e))
                radius=Mathf.Lerp(radius,Mathf.Max(2.1f,e.archetype.attackRange*.9f),close);
            if(director.Adapting(e))radius=Mathf.Max(radius,8);
            Vector3 point;a.guarding=false;
            if((a.role==SquadRole.Interceptor||a.role==SquadRole.FlankerLeft||a.role==SquadRole.FlankerRight)&&a.ready&&
                Time.time<a.targetUntil&&Vector3.Angle(a.targetHeading,heading)<45&&Mathf.Abs(a.target.y-center.y)<1.5f&&
                Vector3.Distance(a.enemy.transform.position,a.target)>1.5f)
                return ProtectEscape(a.target,local.escapeDegrees);
            switch(a.role)
            {
                case SquadRole.Chaser:
                    point=center+Direction(a.slot==-100?-160:160)*Mathf.Max(1.3f,e.archetype.attackRange*.8f);break;
                case SquadRole.Interceptor:
                    point=Topology(Prediction,heading,false)+Direction(a.slot==0?90:-90)*2.2f;a.guarding=true;break;
                case SquadRole.Ambusher:
                    point=Topology(center+heading*Mathf.Max(8,velocity.magnitude*3),heading,true);a.guarding=true;break;
                case SquadRole.Support:
                    point=center+Direction(150-a.slot*18)*Mathf.Max(10,e.archetype.preferredMin+1);break;
                case SquadRole.Ranged:
                    point=center+Direction((a.slot%2==0?-1:1)*(95+a.slot/2*18))*Mathf.Clamp((e.archetype.preferredMax+e.archetype.preferredMin)*.5f,5,14);break;
                case SquadRole.Flyer:
                    point=center+Direction((a.slot%2==0?-1:1)*(70+(a.slot/2*37+PlanCount*8)%70))*(radius+2);break;
                default:
                    float angle=e.scaling.aiTier==1?side*(65+a.slot*20):side*(85-close*20+a.slot*18);
                    point=center+Direction(angle)*radius+velocity*(e.scaling.aiTier==1?.25f:1.5f);break;
            }
            if(Herding&&preferredExit>=0&&(a.role==SquadRole.Ambusher||a.role==SquadRole.Interceptor))
            {
                Vector3 d=Quaternion.Euler(0,preferredExit*30,0)*Vector3.forward;
                if(Vector3.Angle(d,Flat(EscapePoint-center))>local.escapeDegrees*.5f){point=Topology(center+d*9,d,true);AdaptedExitCount++;}
            }
            return ProtectEscape(point,local.escapeDegrees);
        }
        Vector3 SectorTarget(Assignment a,AITierProfile.Tier local)
        {
            Vector3 relative=Flat(a.enemy.transform.position-center);
            if(a.landingGuard)
            {
                if(Time.time<a.landingUntil&&(center.y-a.landingTarget.y)*a.landingSign<1&&Flat(center-a.landingTarget).magnitude<16)
                {a.guarding=true;return ProtectEscape(a.landingTarget,local.escapeDegrees);}
                a.landingGuard=false;
            }
            // One rear unit holds a shelter door during Warning; the other owns the middle of
            // their two rear slots. Keep the side wings rather than leaving an empty rear quadrant.
            if(a.role==SquadRole.Chaser)a.angle=director.Tactics.WarningActive&&director.Tactics.BlockedCount==1?180:a.slot==-100?-157.5f:157.5f;
            bool arrived=Vector3.Angle(relative,Direction(a.angle))<35&&relative.magnitude<7;
            float close=Phase==SquadPhase.Close?Mathf.Clamp01((Time.time-closeSince)/2):0;
            float radius=a.role==SquadRole.Chaser?2.4f:Mathf.Lerp(4.3f,Mathf.Max(2.7f,a.enemy.archetype.attackRange*1.15f),close);
            if(a.role==SquadRole.Ranged||a.role==SquadRole.Support)radius=Mathf.Max(7,a.enemy.archetype.preferredMin+1);
            if(a.role==SquadRole.Flyer)radius=7;
            bool spreading=director.Adapting(a.enemy)&&a.role!=SquadRole.Chaser;
            if(spreading)radius=Mathf.Max(radius,8);
            float forward=Vector3.Dot(Direction(a.angle),heading);
            float eta=Mathf.Clamp(relative.magnitude/Mathf.Max(1,a.enemy.Speed*local.formationSpeed-velocity.magnitude),2,local.predictionSeconds);
            float lead=a.role==SquadRole.Chaser?.2f:arrived?.35f:forward>0?eta:.35f;
            float approachRadius=a.role==SquadRole.Interceptor&&!arrived&&!spreading?2.8f:radius;
            Vector3 point=center+velocity*lead+Direction(a.angle)*approachRadius;
            if(a.role==SquadRole.Interceptor&&Mathf.Abs(verticalSpeed)>.3f)
            {
                Vector3 landing=NextLanding();
                if(landing.y!=center.y)
                {
                    a.landingGuard=true;a.landingUntil=Time.time+8;a.landingSign=Mathf.Sign(verticalSpeed);
                    a.landingTarget=landing+Direction(a.angle)*.8f;a.guarding=true;
                    return ProtectEscape(a.landingTarget,local.escapeDegrees);
                }
            }
            // A fixed ahead point is consumed as an interception, then the actor tracks its own
            // quadrant instead of running past it or surrendering it to the nearest chaser.
            if(!arrived&&(a.role==SquadRole.Interceptor||a.role==SquadRole.Ambusher))
                point=SectorTopology(point,Direction(a.angle),a.role==SquadRole.Interceptor);
            if(!spreading&&a.role==SquadRole.Interceptor&&a.ready&&Time.time<a.targetUntil&&
                Mathf.Abs(a.target.y-center.y)<1.5f&&Vector3.Dot(Flat(a.target-center),heading)>2.5f&&Vector3.Distance(a.enemy.transform.position,a.target)>1)
                point=a.target;
            if(Herding&&preferredExit>=0&&a.role==SquadRole.Ambusher)
            {Vector3 d=Quaternion.Euler(0,preferredExit*30,0)*Vector3.forward;point=SectorTopology(center+d*7,d,false);AdaptedExitCount++;}
            a.guarding=a.role==SquadRole.Interceptor||a.role==SquadRole.Ambusher;
            return ProtectEscape(point,local.escapeDegrees);
        }
        Vector3 NextLanding()
        {
            if(graph==null)return center;Vector3 expected=center+Vector3.up*Mathf.Sign(verticalSpeed)*4,best=center;float score=14;
            foreach(var n in graph.Nodes)
            {
                if(n.Kind!=RoomNodeKind.StairLanding)continue;Vector3 d=n.WorldPosition-center;
                if(d.y*Mathf.Sign(verticalSpeed)<1.5f||Mathf.Abs(d.y)>5||Flat(d).magnitude>12)continue;
                float s=Vector3.Distance(n.WorldPosition,expected);if(s<score){score=s;best=n.WorldPosition;}
            }
            return best;
        }
        Vector3 SectorTopology(Vector3 expected,Vector3 direction,bool intercept)
        {
            if(graph==null)return expected;Vector3 best=expected;float score=3.5f;
            foreach(var n in graph.Nodes)
            {
                if(n.Kind!=RoomNodeKind.Door&&n.Kind!=RoomNodeKind.StairLanding&&n.Kind!=RoomNodeKind.Junction&&n.Kind!=RoomNodeKind.Exit)continue;
                Vector3 d=n.WorldPosition-center;if(Mathf.Abs(d.y)>1.5f&&Mathf.Abs(verticalSpeed)<.3f)continue;
                if(Mathf.Abs(d.y)>5||Vector3.Angle(Flat(d),direction)>55)continue;
                float s=Vector3.Distance(n.WorldPosition,expected);if(s<score){score=s;best=n.WorldPosition;}
            }
            return best;
        }
        Vector3 Topology(Vector3 expected,Vector3 direction,bool corner)
        {
            if(graph==null)return expected;
            Vector3 best=expected;float score=corner?12:8;
            foreach(var n in graph.Nodes)
            {
                if(n.Kind!=RoomNodeKind.Door&&n.Kind!=RoomNodeKind.StairLanding&&n.Kind!=RoomNodeKind.Junction&&n.Kind!=RoomNodeKind.Exit)continue;
                Vector3 d=n.WorldPosition-center;
                if(d.magnitude<4||d.magnitude>30||Mathf.Abs(d.y)>5||Mathf.Abs(d.y)>1.5f&&Mathf.Abs(verticalSpeed)<.3f||Vector3.Dot(Flat(d).normalized,direction)<.3f)continue;
                float s=Vector3.Distance(n.WorldPosition,expected)-(n.Kind==RoomNodeKind.StairLanding?3:corner?2:0);
                if(s<score){score=s;best=n.WorldPosition;}
            }
            return best;
        }
        Vector3 ProtectEscape(Vector3 point,float degrees)
        {
            if(!HasEscape)return point;
            float floorHeight=point.y;
            Vector3 d=Flat(point-center),escape=Flat(EscapePoint-center);
            float angle=Vector3.SignedAngle(escape,d,Vector3.up),gap=Mathf.Max(30,degrees);
            // Keep a wide nearby opening, and a 2m passage further out. Extending the full angular
            // cone to 14m continually pushed distant guards away as the player approached them.
            float halfGap=d.magnitude<=4?gap*.5f:Mathf.Atan2(2,d.magnitude)*Mathf.Rad2Deg;
            if(d.magnitude<14&&Mathf.Abs(angle)<halfGap)
                point=center+Quaternion.Euler(0,angle<0?-halfGap*1.3f:halfGap*1.3f,0)*escape.normalized*Mathf.Max(2.8f,d.magnitude);
            if(director.Tactics.WarningActive&&Flat(point-director.Tactics.FreeEntrance).magnitude<4.5f)
                point=director.Tactics.FreeEntrance+(Flat(point-director.Tactics.FreeEntrance).sqrMagnitude>.1f?Flat(point-director.Tactics.FreeEntrance).normalized:Direction(100))*5;
            point.y=floorHeight;return point;
        }
        bool Sample(Vector3 want,out Vector3 point)
        {if(NavMesh.SamplePosition(want,out var h,1.8f,NavMesh.AllAreas)&&Mathf.Abs(h.position.y-want.y)<1.5f){point=h.position;return true;}point=default;return false;}
        void CommitFlying(Assignment a)
        {if(HasEscape&&FlyingMotor.OutdoorPoint(a.wanted,out var p)){a.target=p;a.ready=true;a.revision++;}}
        void EvaluateStep(Assignment a)
        {
            if(!HasEscape){a.ready=false;cursor++;return;}
            var e=a.enemy;if(a.candidate>=3){Commit(a);cursor++;return;}
            int c=a.candidate;
            if(c==0&&a.approaching){a.candidate++;return;}
            if(a.stage==0)
            {
                if(!Sample(CandidateTarget(a,c),out var target)||!Sample(e.transform.position,out var start)){a.candidate++;return;}
                Vector3 waypoint=c==0?target:Detour(a,c);
                if(!Sample(waypoint,out waypoint)){a.candidate++;return;}
                PathsThisFrame++;
                bool valid=NavMesh.CalculatePath(start,waypoint,e.Motor.Agent.areaMask,a.paths[a.bank+c*2])&&a.paths[a.bank+c*2].status==NavMeshPathStatus.PathComplete;
                if(!valid){a.candidate++;return;}
                a.detour=waypoint;
                if(c==0){Score(a,c,target);a.candidate++;}else a.stage=1;
            }
            else
            {
                if(!Sample(CandidateTarget(a,c),out var target)){a.candidate++;a.stage=0;return;}
                PathsThisFrame++;
                bool valid=NavMesh.CalculatePath(a.detour,target,e.Motor.Agent.areaMask,a.paths[a.bank+c*2+1])&&a.paths[a.bank+c*2+1].status==NavMeshPathStatus.PathComplete;
                if(valid)Score(a,c,target);a.candidate++;a.stage=0;
            }
        }
        Vector3 CandidateTarget(Assignment a,int candidate)
        {
            if((a.role==SquadRole.Ranged||a.role==SquadRole.Support)&&candidate>0)
                return ProtectEscape(center+Quaternion.Euler(0,candidate==1?25:-25,0)*(a.wanted-center),director.TierFor(a.enemy.scaling.aiTier).escapeDegrees);
            return a.wanted;
        }
        Vector3 Detour(Assignment a,int candidate)
        {
            if(profile.phasedEncirclement)
            {
                if(a.approaching)return a.approachGate;
                Vector3 wing=center+Direction(a.angle)*(candidate==1?6:8)+velocity*.4f;
                return ProtectEscape(candidate==2?SectorTopology(wing,Direction(a.angle),false):wing,profile.escapeDegrees);
            }
            float side=a.role==SquadRole.FlankerLeft||a.slot%2==0?-1:1;
            float radius=Mathf.Max(6,director.TierFor(a.enemy.scaling.aiTier).flankRadius+3);
            Vector3 want=center+Direction(side*(candidate==1?115:70))*radius+velocity*.5f;
            if(candidate==2&&graph!=null)
            {
                float best=14;
                foreach(var n in graph.Nodes)
                {
                    var d=n.WorldPosition-center;
                    if(Mathf.Abs(d.y)>2||d.magnitude<4||d.magnitude>25||Vector3.Dot(Flat(d),Direction(side*90))<2)continue;
                    float score=Vector3.Distance(n.WorldPosition,want)+(n.Kind==RoomNodeKind.Junction||n.Kind==RoomNodeKind.Door?0:2);
                    if(score<best){best=score;want=n.WorldPosition;}
                }
            }
            return ProtectEscape(want,profile.escapeDegrees);
        }
        void Score(Assignment a,int c,Vector3 target)
        {
            float length=PathLength(a.paths[a.bank+c*2]);if(c>0)length+=PathLength(a.paths[a.bank+c*2+1]);
            if(c==0)a.directLength=length;
            if(profile.phasedEncirclement&&c>0&&a.directLength>0&&length>a.directLength*1.3f+1)return;
            float score=PathCost(a.paths[a.bank+c*2],a.enemy);
            if(c>0)score+=PathCost(a.paths[a.bank+c*2+1],a.enemy)+1;
            if(a.role==SquadRole.Chaser)score+=c*6;
            if(a.role==SquadRole.Ranged||a.role==SquadRole.Support)
            {
                if(!CombatLine.Clear(target+Vector3.up,center+Vector3.up,a.enemy.transform,true))score+=18;
                for(int i=0;i<director.Active.Count;i++){var other=director.Active[i];if(other!=null&&other!=a.enemy&&other.Alive&&DistanceToSegment(other.transform.position,target,center)<.8f)score+=4;}
            }
            if(score<a.bestScore){a.bestScore=score;a.best=c;a.bestTarget=target;a.bestWaypoint=a.detour;}
        }
        float PathLength(NavMeshPath path)
        {int n=path.GetCornersNonAlloc(corners);float length=0;for(int i=1;i<n;i++)length+=Vector3.Distance(corners[i-1],corners[i]);return length;}
        float PathCost(NavMeshPath path,EnemyInstance e)
        {
            int n=path.GetCornersNonAlloc(corners);float cost=0;
            for(int i=1;i<n;i++)
            {
                float length=Vector3.Distance(corners[i-1],corners[i]);cost+=length;
                int samples=Mathf.CeilToInt(length/2);
                for(int j=1;j<=samples;j++)
                {
                    Vector3 p=Vector3.Lerp(corners[i-1],corners[i],(float)j/Mathf.Max(1,samples));
                    if(reservations.TryGetValue(Cell(p),out int used))cost+=used*profile.routePenalty;
                    if(Flat(p-center).magnitude<6)
                        for(int k=1;k<escapeCornerCount;k++)if(DistanceToSegment(p,escapeCorners[k-1],escapeCorners[k])<1.7f){cost+=12;break;}
                }
            }
            if(n>1)foreach(var other in squad)if(other.enemy!=e&&other.enemy.Alive)
            {
                Vector3 d=other.enemy.transform.position-e.transform.position;
                if(d.sqrMagnitude<4&&Vector3.Dot(Flat(d).normalized,Flat(corners[1]-corners[0]).normalized)>.75f){cost+=18;SplitCount++;}
            }
            return cost;
        }
        static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x/3),Mathf.RoundToInt(p.y/2),Mathf.FloorToInt(p.z/3));
        static float DistanceToSegment(Vector3 p,Vector3 from,Vector3 to)
        {
            if(Mathf.Abs(p.y-from.y)>2&&Mathf.Abs(p.y-to.y)>2)return 100;
            Vector3 d=Flat(to-from);return Flat(p-from-d*Mathf.Clamp01(Vector3.Dot(Flat(p-from),d)/Mathf.Max(.001f,d.sqrMagnitude))).magnitude;
        }
        void Reserve(NavMeshPath path)
        {
            int n=path.GetCornersNonAlloc(corners);
            for(int i=1;i<n;i++)
            {
                int samples=Mathf.CeilToInt(Vector3.Distance(corners[i-1],corners[i])/2);
                for(int j=0;j<=samples;j++){var cell=Cell(Vector3.Lerp(corners[i-1],corners[i],(float)j/Mathf.Max(1,samples)));reservations.TryGetValue(cell,out int used);reservations[cell]=used+1;}
            }
        }
        void Commit(Assignment a)
        {
            if(a.best<0){a.ready=false;return;}
            a.selected=a.best;a.committedBank=a.bank;a.score=a.bestScore;a.target=a.bestTarget;a.waypoint=a.bestWaypoint;
            if(Time.time>=a.targetUntil||Vector3.Angle(a.targetHeading,heading)>=45)
            {a.targetUntil=Time.time+(a.role==SquadRole.Interceptor?4:2);a.targetHeading=heading;}
            a.via=a.best>0;a.leg=0;a.revision++;a.appliedRevision=-1;a.ready=true;
            Reserve(a.paths[a.committedBank+a.selected*2]);if(a.via)Reserve(a.paths[a.committedBank+a.selected*2+1]);
        }
    }
}
