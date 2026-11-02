using Falezia.Sim;

/// <summary>Etichette condivise fra HUD e negozio (una sola verità per i nomi).</summary>
public static class UiLabels
{
    public static string FireLabel(WeaponDef def)
        => def.Melee ? "MISCHIA"
         : def.Modes == FireMode.Burst ? $"RAFFICA ×{def.BurstCount}"
         : def.Automatic ? "AUTO" : "SEMIAUTO";
}
