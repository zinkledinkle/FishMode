using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;

namespace FishMode.Common;

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
    protected override void OnTileCollision(ImpactInfo impact)
    {
        bool impactFrame = impact.VelAlongNormal > 35f;
        if (!impactFrame)
        {
            base.OnTileCollision(impact);
            FallDamage(impact.VelAlongNormal);
        }
        else Impact = impact; //queue for after impact frame
        if (impactFrame)
        {
            ImpactFrame.Initiate(particles[0].Position, Vector2.Normalize(impact.Normal));
            return;
        }
        if (impact.VelAlongNormal > 22f)
        {
            float strength = MathF.Min(impact.VelAlongNormal + (timeInAir * 0.2f), 100f);
            int time = (int)impact.VelAlongNormal + (int)(timeInAir * 0.4f);
            float cycles = 5f;
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Player.Center, Main.rand.NextVector2CircularEdge(1f, 1f), strength, cycles, time, 500f, "splatShake"));
        }
    }
    private void FallDamage(float magnitude)
    {
        if (dead) return;
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
        if (Impact != null)
        {
            base.OnTileCollision(Impact.Value);
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Player.Center, Main.rand.NextVector2CircularEdge(1f, 1f), 100f, 4f, 180, 500f, "splatShake"));
            Impact = null;
        }
        if (Main.instance.CameraModifiers._modifiers.FirstOrDefault(m => m.UniqueIdentity == "splatShake") is PunchCameraModifier shake) shake._direction = Main.rand.NextVector2CircularEdge(1f, 1f);
        base.Update();
        timeInAir = Grounded || particles[0].Velocity.Y < -4 ? 0 : timeInAir + 1;
        jumpCooldown = Math.Max(jumpCooldown - 1, 0);
    }
}