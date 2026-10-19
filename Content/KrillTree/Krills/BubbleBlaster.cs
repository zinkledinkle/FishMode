using FishMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class BubbleBlaster : Krill
{
    private const int bubbleDamage = 30;
    private const float bubbleKnockback = 3f;
    public override int Level => 3;
    public override Vector2 Position => new(120, -120);
    public override IReadOnlyList<Type> Requirements => [typeof(BubbleBlower)];
    public override void Apply(Player player)
    {
        player.GetModPlayer<BubbleBlaster_Player>().enabled = true;
    }
    public class BubbleBlaster_Player : ModPlayer
    {
        public bool enabled = false;
        private int count = 0;
        private const int cooldown = 5;
        public override void ResetEffects()
        {
            enabled = false;
        }
        public override void Load()
        {
            On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier += static (orig, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, mod) =>
            {
                int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2, mod);
                if (Type == BubbleBlasterProjectile.ID || Type == BubbleBombardierProjectile.ID) return result;
                var player = Main.player[Owner];
                var modPlayer = player.GetModPlayer<BubbleBlaster_Player>();
                if (Main.projectile[result].DamageType == DamageClass.Ranged && player.active && !player.dead && modPlayer.enabled)
                {
                    if (modPlayer.count++ % cooldown == 0)
                        BubbleBlasterProjectile.New(spawnSource, new(X, Y), new Vector2(SpeedX, SpeedY), bubbleDamage, bubbleKnockback, Owner, result);
                }
                return result;
            };
        }
    }
}

