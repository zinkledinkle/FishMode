using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class Electrogenesis : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(Jellyfish)];
    public override int Level => 4;
    public override Vector2 Position => new(210, 180);
    public const float radius = 350;
    public const float combinedRadius = 500;
    private const int combinedDPS = 40;
    private bool jellyfishCombined = false;
    private const int timeMax = 8;
    private const int tickDamage = (int)(combinedDPS / (60 / (float)timeMax));
    private int time = 0;
    public override IReadOnlyList<Type> CombinesWith => [typeof(Jellyfish)];
    public override void Apply(Player player)
    {
        time = Math.Max(time - 1, 0);
        const float strength = 0.4f;
        Lighting.AddLight(player.Center, Vector3.One * strength);

        float effectiveRadius = jellyfishCombined ? combinedRadius : radius;

        foreach (var npc in Main.ActiveNPCs)
        {
            var doDmg = npc.lifeMax > 5 && !npc.dontTakeDamage && !npc.friendly && npc.active;
            var inside = npc.Hitbox.ClosestContactPoint(player.Center).DistanceSQ(player.Center) < effectiveRadius * effectiveRadius;
            if (doDmg && inside)
            {
                npc.AddBuff(Electrified_NPC.ID, 180);
                if (time > 0 || !jellyfishCombined) continue;
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
        float effectiveRadius = jellyfishCombined ? combinedRadius : radius;
        for (int i = 0; i < count; i++)
        {
            float angle = (float)Main.timeForVisualEffects / 120f + (i / (float)count) * MathF.Tau;
            Vector2 pos = angle.ToRotationVector2() * effectiveRadius;
            Dust.NewDustPerfect(player.Center + pos, DustID.Electric);
        }
        jellyfishCombined = false;
    }
    public override void CombinationEffect(int otherType, Player player, ref bool deactivateOtherEffect)
    {
        if (otherType == Jellyfish.Type) jellyfishCombined = true;
        deactivateOtherEffect = true;
    }
}
public class Electrified_NPC : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Electrified}";
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
    }
    public override void Update(NPC npc, ref int buffIndex)
    {
        var dps = (int)MathHelper.Clamp(npc.velocity.Length() * 25f, 4, 80);
        npc.lifeRegen -= dps * 2;
        if (Main.rand.NextBool(5))
            Dust.NewDust(npc.position, npc.width, npc.height, DustID.Electric);
    }
}