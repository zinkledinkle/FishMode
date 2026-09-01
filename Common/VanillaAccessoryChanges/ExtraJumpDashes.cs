using System;
using Terraria.ModLoader;

namespace FishMode.Common.VanillaAccessoryChanges;

public class ExtraJumpDashes : ModPlayer
{
    private int dashCooldown;
    private int dashTime;

    public int dashCooldownTime;
    public int dashDuration;
    public float dashSpeed;
    public override void OnExtraJumpStarted(ExtraJump jump, ref bool playSound)
    {
        var fplr = Player.GetModPlayer<FishPlayer>();
        fplr.Body.Propel(10f, fplr.lookDir, 0.2f);
    }
    public override void ResetEffects()
    {
        dashCooldownTime = 30;
        dashDuration = 20;
        dashSpeed = 3f;

        dashCooldown = Math.Max(0, dashCooldown - 1);
        dashTime = Math.Max(0, dashTime - 1);
    }
}
