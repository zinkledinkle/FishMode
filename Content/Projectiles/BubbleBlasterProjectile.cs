using FishMode.Common.Interfaces;
using FishMode.Common.VanillaChanges;
using FishMode.Common.Rendering;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.Projectiles;

public class BubbleBlasterProjectile : ModProjectile, IRadialHitboxProjectile
{
    public override string Texture => Assets.Textures.UI.Line.KEY;
    private const float AOE = 100f;
    public float Radius { get; set; } = 0;
    private Projectile Holder => Main.projectile[(int)Projectile.ai[1]];
    private float VisualRadius => Radius * Projectile.scale * Projectile.localAI[0];
    public override void SetStaticDefaults()
    {
        RemoveWaterSlowdown.ProjectileBypassIgnoreWater[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 0;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = Main.rand.NextFloat(0.9f, 1.1f);
        Projectile.tileCollide = false;
        Projectile.ArmorPenetration = 15;
    }
    public static void New(IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback, int owner, int holderProjectile)
    {
        var proj = Projectile.NewProjectileDirect(source, position, velocity, BubbleBlasterProjectile.ID, damage, knockback, owner);
        var hitbox = Main.projectile[holderProjectile].Hitbox.Size();
        var radius = MathF.Max(hitbox.X, hitbox.Y) * 1.5f; //technically you'd halve it but I want it to be big enough to envelop it
        radius = MathF.Max(radius, 10f);
        (proj.ModProjectile as BubbleBlasterProjectile)?.Radius = radius;
        proj.extraUpdates = Main.projectile[holderProjectile].extraUpdates;
        proj.ai[1] = holderProjectile;
    }
    public override void AI()
    {
        Projectile.localAI[0] = MathHelper.Lerp(Projectile.localAI[0], 1, 0.3f);
        if (!Holder.active) Projectile.Kill();
        else Projectile.timeLeft = 2;
        if (Main.rand.NextBool(7))
        {
            var pos = Projectile.Center + Main.rand.NextVector2Unit(-MathHelper.PiOver4, MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation() + MathHelper.Pi) * VisualRadius;
            Dust.NewDustPerfect(pos, DustID.JesterSparkleBlue, -Projectile.velocity * 0.1f);
        }
        Projectile.velocity = Holder.Center - Projectile.Center;
    }
    public override void OnKill(int timeLeft)
    {
        int amount = (int)VisualRadius;
        ParticleOrchestrator.Spawn_CattivaHit(new() { PositionInWorld = Projectile.Center, IndexOfPlayerWhoInvokedThis = (byte)Projectile.owner });
        Radius = AOE;
        Projectile.friendly = true;
        Projectile.Damage();
        SoundEngine.PlaySound(Assets.Sounds.Projectiles.BubbleImpactWeak.Asset with { Volume = 0.8f, PitchVariance = 1f}, Projectile.Center);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.9f);
    }
    public override bool PreDraw(Player player, ref Color lightColor)
    {
        BubbleDrawer.DrawBubble(Projectile.Center, VisualRadius);
        return false;
    }
}
public class BubbleBlasterProjectile_Holder : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.DamageType == DamageClass.Ranged;
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        foreach(var p in Main.ActiveProjectiles)
        {
            if (p.type != BubbleBlasterProjectile.ID || p.owner != projectile.owner || p.ai[1] != projectile.whoAmI) continue;
            p.Kill();
        }
        base.OnHitNPC(projectile, target, hit, damageDone);
    }
}