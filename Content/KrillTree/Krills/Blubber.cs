using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Blubber : Krill
{
    public override int Level => 2;
    public override IReadOnlyList<Type> Requirements => [typeof(Quickfins)];
    public override Vector2 Position => new(-30, -120);
    public override void Apply(Player player)
    {
        player.moveSpeed -= 0.15f;
        player.endurance += 0.25f;
    }
}
