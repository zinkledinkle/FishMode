using FishMode.Common.Interfaces;
using FishMode.Common.VanillaChanges;
using FishMode.Common.Rendering;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.Projectiles;

public class BubbleBombardierProjectile : ModProjectile, IRadialHitboxProjectile
{
    public override string Texture => Assets.Textures.UI.Line.KEY;
    private NPC Target
    {
        get => Main.npc[(int)Projectile.ai[0]];
        set => Projectile.ai[0] = value.whoAmI;
    }
    private float VisualRadius => Radius * Projectile.scale * Projectile.localAI[0];
    public float Radius { get; set; } = 40f;
    private const float AOE = 220f;
    public override void SetStaticDefaults()
    {
        RemoveWaterSlowdown.ProjectileBypassIgnoreWater[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = (int)(Radius * 2);
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.scale = Main.rand.NextFloat(0.9f, 1.1f);
        Projectile.tileCollide = false;
        Projectile.timeLeft = 420;
        Projectile.ArmorPenetration = 25;
    }
    public static void New(IEntitySource source, Vector2 position, Vector2 velocity, int damage, float knockback, int owner, NPC target)
    {
        var proj = Projectile.NewProjectileDirect(source, position, velocity, BubbleBombardierProjectile.ID, damage, knockback, owner);
        (proj.ModProjectile as BubbleBombardierProjectile).Target = target;
    }
    public override void OnSpawn(IEntitySource source)
    {
        SoundEngine.PlaySound(Assets.Sounds.Projectiles.BubbleSpawn.Asset with { Volume = 0.7f, PitchVariance = 0.5f }, Projectile.Center);
    }
    public override void AI()
    {
        Projectile.localAI[0] = MathHelper.Lerp(Projectile.localAI[0], 1, 0.3f);
        if (Target?.active is not null)
        {
            var newTarget = Projectile.FindTargetWithinRange(600f);
            if (newTarget != null) Target = newTarget;
        }
        if (Target?.active is true)
        {
            const float maxSpeed = 24f;
            Vector2 idealVelocity = Projectile.DirectionTo(Target.Center).SafeNormalize(Vector2.Zero) * maxSpeed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.03f);
        }
        if (Main.rand.NextBool(7))
        {
            var pos = Projectile.Center + Main.rand.NextVector2Unit(-MathHelper.PiOver4, MathHelper.PiOver2).RotatedBy(Projectile.velocity.ToRotation() + MathHelper.Pi) * VisualRadius;
            Dust.NewDustPerfect(pos, DustID.JesterSparkleBlue, -Projectile.velocity * 0.1f);
        }
        if (Projectile.Hitbox.Intersects(Target?.Hitbox ?? Rectangle.Empty) && Projectile.localAI[1] > 30)
            Projectile.Kill();

        Projectile.localAI[1]++;
    }
    public override void OnKill(int timeLeft)
    {
        int amount = (int)VisualRadius;
        ParticleOrchestrator.SpawnLightningExplosionDust(Projectile.Center, Color.Cyan);
        Radius = AOE;
        Projectile.friendly = true;
        Projectile.Damage();
        SoundEngine.PlaySound(Assets.Sounds.Projectiles.BubbleImpact.Asset with { Volume = 0.8f, PitchVariance = 0.5f}, Projectile.Center);
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