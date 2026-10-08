namespace CampusRift.Combat
{
    // P19 stealth implementations can honour this reveal lease; no hidden model is hardcoded here.
    public interface IEnemyConcealment { void RevealFor(float seconds); }
}
