using FishMode.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Hydrodynamics : Krill
{
    public override int Level => 3;
    public override Vector2 Position => new(60, 120);
    public override IReadOnlyList<Type> Requirements => [typeof(Bioluminescence)];
    public override void Apply(Player player)
    {
        player.GetModPlayer<FishPlayer>().hydrodynamics += 0.5f;
    }
}
