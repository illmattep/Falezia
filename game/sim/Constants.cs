namespace Falezia.Sim;

/// <summary>
/// Tutti i numeri vengono dai documenti di design (doc 05 = movimento).
/// NESSUN riferimento a Godot: questo file deve compilare in una console .NET pura.
/// </summary>
public static class Constants
{
    public const int TickRate = 64;
    public const float Dt = 1f / 64f;
    public const int InputBuffer = 256;

    // --- movimento (doc 05)
    public const float WalkSpeed = 4.2f;
    public const float SprintSpeed = 6.5f;
    public const float CrouchSpeed = 2.2f;
    public const float AdsMult = 0.55f;

    public const float StaminaMax = 10f;
    public const float StaminaRecovery = 8f;

    public const float Gravity = 18f;
    public const float JumpVelocity = 6f;
    public const float JumpCooldown = 0.15f;

    public const float GroundAccel = 8f;
    public const float AirAccel = 2f;
    public const float Friction = 8f;
    public const float MaxAirWishSpeed = 0.3f;

    public const float Radius = 0.30f;
    public const float Height = 1.80f;
    public const float CrouchHeight = 1.20f;

    public const float CorrectionThreshold = 0.01f;
    public const float WorldFloorY = -20f;

    // --- rete
    public const int ServerPort = 27015;
    public const string ServerHost = "127.0.0.1";
    public const int SnapshotEvery = 2;   // 32 Hz su un tick di 64 Hz
}
