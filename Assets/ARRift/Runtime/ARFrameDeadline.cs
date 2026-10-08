using System;
namespace CampusRift.AR
{
    // Sensor clock is used only for identity and capture deltas, never Unity age.
    public sealed class ARFrameDeadline
    {
        public double Next;public double LastSensor=double.NaN;
        public bool Due(double now,int hz){if(now+1e-9<Next)return false;double period=1.0/hz;Next+=Math.Max(1,Math.Floor((now-Next)/period+1e-9)+1)*period;return true;}
        public bool Unique(double sensor){if(!double.IsFinite(sensor)||sensor==LastSensor)return false;LastSensor=sensor;return true;}
        public void Reset(double now){Next=now;LastSensor=double.NaN;}
    }
}
