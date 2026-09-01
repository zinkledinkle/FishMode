using FishMode.Common;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Slither : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(Swordfish)];
    public override int Level => 2;
    public override Vector2 Position => new(-30, 120);
    public override void Apply(Player player)
    {
        player.GetModPlayer<FishPlayer>().BodyLength += 1;
    }
}