using FishMode.Common;
using FishMode.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.Projectiles;

public class SmallBubbleProjectile : ModProjectile
{
    public override string Texture => AssetReferences.Content.Projectiles.Bubble_Small.KEY;
    public override void SetStaticDefaults()
    {
        RemoveWaterSlowdown.BypassIgnoreWater[Type] = true;
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
    }
    public override void AI()
    {
        base.AI();
        Projectile.velocity *= Projectile.wet ? Vector2.One * 0.99f : new Vector2(0.9f, 0.97f);
    }
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < Main.rand.Next(4); i++)
        Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.BreatheBubble);
    }
}
