namespace CampusRift.AR
{
    public sealed class ARRecoveryGate
    {
        public bool Running {get;private set;}public double Started {get;private set;}
        public bool Begin(double now){if(Running)return false;Running=true;Started=now;return true;}
        public void Complete(){Running=false;}
        public bool Overdue(double now)=>Running&&now-Started>2000;
        public static bool Watchdog(double now,double submitted,bool waiting)=>waiting&&now-submitted>=1000;
        // Graph/delegate creation can exceed an inference frame's one-second budget.
        public static bool InitializationOverdue(double now,double started)=>now-started>=5000;
    }
}
