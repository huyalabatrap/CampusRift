namespace CampusRift.Combat
{
    // Anything that moves a monster and can be frozen in place by seals, freeze or stun.
    public interface IMotionHold
    {
        void HoldForSeal();
    }
}
