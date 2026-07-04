using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;

namespace FishMode.Core.Physics;

public class PlayerFishBody : FishBody
{
    public override float Width => ModContent.GetInstance<FishModeConfig>().BodyWidth;
    public override float SegmentLength => ModContent.GetInstance<FishModeConfig>().BaseSegmentLength;
    private Player Player { get; set; }

    private int timeInAir;
    private int jumpCooldown = 0;
    public PlayerFishBody(Player player, int segments)
    {
        Player = player;
        Setup(player.Top, segments);
    }
    protected override void OnTileCollision(IParticle particle, float velAlongNormal, Vector2 normal, int surroundingSolidTiles, int tileType, int tileX, int tileY)
    {
        base.OnTileCollision(particle, velAlongNormal, normal, surroundingSolidTiles, tileType, tileX, tileY);
        FallDamage(velAlongNormal);
    }
    private void FallDamage(float magnitude)
    {
        if (dead) return;
        if (magnitude > 22f)
        {
            float strength = MathF.Min(magnitude + (timeInAir * 0.2f), 100f);
            int time = (int)magnitude + (int)(timeInAir * 0.4f);
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Player.Center, Main.rand.NextVector2Circular(1f, 1f), strength, 5f, time, 500f, "splatShake"));
        }
        if (Player.noFallDmg || Submerged) return;
        float thresh = 12f;
        float mult = 20f;
        float timeMultiplier = 1f;
        if (magnitude < thresh) return;
        int timeDmg = Math.Max((int)(timeInAir * timeMultiplier - 100), 0);
        int dmg = (int)((magnitude - thresh) * mult) + timeDmg;
        Player.Hurt(PlayerDeathReason.ByOther(0), dmg, 0);
        timeInAir = 0;
    }
    public void Jump(float strength)
    {
        if (jumpCooldown > 0) return;
        foreach (var particle in particles)
        {
            particle.Frozen = false;
            particle.AddForce(Vector2.UnitY * (particle.Grounded ? -strength : -strength * 0.5f));
        }
        jumpCooldown = 5;
    }
    public override void Update()
    {
        base.Update();
        timeInAir = Grounded || particles[0].Velocity.Y < -4 ? 0 : timeInAir + 1;
        jumpCooldown = Math.Max(jumpCooldown - 1, 0);
    }
}