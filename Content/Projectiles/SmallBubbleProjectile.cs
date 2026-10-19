using FishMode.Common.Interfaces;
using FishMode.Common.VanillaChanges;
using FishMode.Common.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.Projectiles;

public class SmallBubbleProjectile : ModProjectile, IRadialHitboxProjectile
{
    public override string Texture => Assets.Textures.UI.Line.KEY;
    public float Radius { get; set; } = 16f;
    private float VisualRadius => Radius * Projectile.scale * Projectile.localAI[0];
    public override void SetStaticDefaults()
    {
        RemoveWaterSlowdown.ProjectileBypassIgnoreWater[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 0;
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
        Projectile.tileCollide = false;
        Projectile.ArmorPenetration = 5;
    }
    public override void AI()
    {
        base.AI();
        Projectile.localAI[0] = MathHelper.Lerp(Projectile.localAI[0], 1, 0.3f);
        Projectile.tileCollide = Projectile.localAI[0] > 0.5f;
        Projectile.velocity *= Projectile.wet ? Vector2.One * 0.99f : new Vector2(0.9f, 0.97f);
        Projectile.velocity += Main.rand.NextVector2Circular(0.1f, 0.1f);
        if (Projectile.scale <= 0f)
            Projectile.Kill();
        if (Main.rand.NextBool(7))
        {
            var pos = Projectile.Center + Main.rand.NextVector2Unit(-MathHelper.PiOver4, MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation() + MathHelper.Pi) * VisualRadius;
            Dust.NewDustPerfect(pos, DustID.JesterSparkleBlue, -Projectile.velocity * 0.1f);
        }
    }
    public override void OnKill(int timeLeft)
    {
        int amount = (int)VisualRadius;
        for (int i = 0; i < Main.rand.Next(amount - 3, amount + 3); i++)
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(VisualRadius, VisualRadius), DustID.BubbleBurst_White);
        SoundEngine.PlaySound(SoundID.Item85 with { Pitch = 1, PitchVariance = 1 }, Projectile.Center);
    }
    public override bool PreDraw(Player player, ref Color lightColor)
    {
        BubbleDrawer.DrawBubble(Projectile.Center, VisualRadius);
        return false;
    }
}
