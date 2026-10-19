using FishMode.Common;
using FishMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class BubbleBombardier : Krill
{
    private const float bubbleDamagePercentage = 0.25f;
    private const int bubbleDamageFlat = 15;
    private const float bubbleKnockback = 4.5f;
    private const int chanceDenominator = 3;
    public override int Level => 5;
    public override Vector2 Position => new(120, -180);
    public override IReadOnlyList<Type> Requirements => [typeof(BubbleBlaster)];
    public override void Apply(Player player) { }
    public class BubbleBombardier_GlobalProjectile : GlobalProjectile
    {
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.DamageType != DamageClass.Ranged
                || !Main.player[projectile.owner].GetModPlayer<KrillTreePlayer>().KrillTree.IsActivated(BubbleBombardier.ID)
                || !Main.rand.NextBool(chanceDenominator)
                || projectile.type == BubbleBombardierProjectile.ID
                || projectile.type == BubbleBlasterProjectile.ID) return;
            int damage = (int)(damageDone * bubbleDamagePercentage) + bubbleDamageFlat;
            Vector2 offset = projectile.Center - target.Center;
            BubbleBombardierProjectile.New(Main.player[projectile.owner].GetSource_FromThis(), target.Center + offset, Vector2.Normalize(offset) * 15f, damage, bubbleKnockback, projectile.owner, target);
        }
    }
}