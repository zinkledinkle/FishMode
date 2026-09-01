using FishMode.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class PistolShrimp : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(BubbleBlower)];
    public override int Level => 3;
    public override Vector2 Position => new(150, -60);
    public const float multiplier = 1.6f;
    public override void Load()
    {
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float += static (orig, spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2) =>
        {
            int result = orig(spawnSource, X, Y, SpeedX, SpeedY, Type, Damage, KnockBack, Owner, ai0, ai1, ai2);
            var player = Main.player[Owner];
            var proj = Main.projectile[result];
            if (proj.DamageType == DamageClass.Ranged && player.GetModPlayer<KrillTreePlayer>().KrillTree.activated.Contains(PistolShrimp.Type)) proj.velocity *= multiplier;
            return result;
        };
    }
    public override void Apply(Player player) { }
}
