using FishMode.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.Projectiles;

public class SmallBubbleProjectile : ModProjectile
{
    public override string Texture => Assets.Textures.UI.Line.KEY;
    public const float Radius = 16f;
    public override void SetStaticDefaults()
    {
        RemoveWaterSlowdown.ProjectileBypassIgnoreWater[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = Main.rand.NextFloat(0.75f, 1.25f);
        Projectile.timeLeft = Main.rand.Next(160, 200);
        Projectile.hide = true;
    }
    public override void AI()
    {
        base.AI();
        Projectile.localAI[0] = MathHelper.Lerp(Projectile.localAI[0], 1, 0.3f);
        Projectile.velocity *= Projectile.wet ? Vector2.One * 0.99f : new Vector2(0.9f, 0.97f);
        Projectile.velocity += Main.rand.NextVector2Circular(0.1f, 0.1f);
        foreach(var p in Main.ActiveProjectiles)
        {
            if (p == Projectile || p.type != Type || p.DistanceSQ(Projectile.Center) > 500) continue;
            float strength = 1 / p.DistanceSQ(Projectile.Center);
            strength = MathHelper.Clamp(strength * 4, 0, 5);
            var dir = p.DirectionTo(Projectile.Center);
            p.velocity -= dir * strength;
            Projectile.velocity += dir * strength;
        }
        if (Projectile.scale <= 0f)
            Projectile.Kill();
    }
    public override void OnKill(int timeLeft)
    {
        float effectiveRadius = Radius * Projectile.scale;
        int amount = (int)effectiveRadius;
        for (int i = 0; i < Main.rand.Next(amount - 3, amount + 3); i++)
        Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(effectiveRadius, effectiveRadius), DustID.BreatheBubble);
        SoundEngine.PlaySound(SoundID.Item85 with { Pitch = 1, PitchVariance = 1 }, Projectile.Center);
    }
}
