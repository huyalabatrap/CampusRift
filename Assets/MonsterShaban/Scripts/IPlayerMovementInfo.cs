namespace CampusRift.Monsters
{
    // Optional skills adapter. No input, destination or grapple anchor is exposed.
    public interface IPlayerMovementInfo
    {
        bool IsGrounded { get; }
        bool IsGrappling { get; }
        bool IsSwinging { get; }
        bool IsAirDashing { get; }
        float Gravity { get; }
    }
}
