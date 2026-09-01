using FishMode.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;
public class Waves : Krill
{
    public override IReadOnlyList<Type> Requirements => [];
    public override int Level => 1;
    public override Vector2 Position => Vector2.Zero;
    public override void Apply(Player player)
    {
        player.GetModPlayer<FishPlayer>().hasWave = true;
    }
}