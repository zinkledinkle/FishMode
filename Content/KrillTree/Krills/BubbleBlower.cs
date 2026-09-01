using FishMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class BubbleBlower : Krill
{
    private const int bubbleDamage = 6;
    private const float bubbleKnockback = 1.4f;
    public override int Level => 2;
    public override Vector2 Position => new(60, -60);
    public override void Apply(Player player)
    {
        player.GetModPlayer<BubbleBlower_Player>().enabled = true;
    }
    public class BubbleBlower_Player : ModPlayer
    {
        public bool enabled = false;
        private int time = 0;
        private const int timeMax = 16;
        public override void ResetEffects()
        {
            enabled = false;
            time = Math.Max(0, time - 1);
        }
        public override void Load()
        {
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += static (orig, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2) =>
            {
                int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2);
                var player = Main.player[Owner];
                if (Main.projectile[result].DamageType == DamageClass.Ranged && player.active && !player.dead && player.GetModPlayer<BubbleBlower_Player>().enabled && player.GetModPlayer<BubbleBlower_Player>().time == 0)
                {
                    player.GetModPlayer<BubbleBlower_Player>().time = timeMax;
                    Projectile.NewProjectile(spawnSource, new(X, Y), new Vector2(SpeedX, SpeedY).RotatedByRandom(0.1f) * Main.rand.NextFloat(1.4f, 2f), SmallBubbleProjectile.ID, bubbleDamage, bubbleKnockback, Owner);
                }
                return result;
            };
        }
    }
}
