using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class Jellyfish : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(MagicFangs)];
    public override int Level => 3;
    public override Vector2 Position => new(150, 150);
    public const float radius = 200;
    private const int targetDPS = 20;
    private const int timeMax = 20;
    private const int tickDamage = (int)(targetDPS / (60 / (float)timeMax));
    private int time = 0;
    public override void Apply(Player player)
    {
        time = Math.Max(time - 1, 0);
        const float strength = 0.4f;
        Lighting.AddLight(player.Center, Vector3.One * strength);

        if (time > 0) return;
        foreach (var npc in Main.ActiveNPCs)
        {
            var doDmg = npc.lifeMax > 5 && !npc.dontTakeDamage && !npc.friendly && npc.active;
            var inside = npc.Hitbox.ClosestContactPoint(player.Center).DistanceSQ(player.Center) < radius * radius;
            if (doDmg && inside)
            {
                NPC.HitModifiers modifiers = new()
                {
                    HitDirection = Math.Sign((npc.Center - player.Center).X),
                    DamageType = DamageClass.Default,
                    Defense = new(0f, 0f, 0f, 0f)
                };
                npc.StrikeNPC(modifiers.ToHitInfo(tickDamage, false, 0f));
                time = timeMax;
            }
        }
    }
    public override void Visuals(Player player)
    {
        const int count = 30;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)Main.timeForVisualEffects / 120f + (i / (float)count) * MathF.Tau;
            Vector2 pos = angle.ToRotationVector2() * radius;
            Dust.NewDustPerfect(player.Center + pos, DustID.MagnetSphere);
        }
    }
}