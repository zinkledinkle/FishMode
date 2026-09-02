using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Radiance : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(MagicFangs)];
    public override int Level => 3;
    public override Vector2 Position => new(180, 60);
    public override void Apply(Player player)
    {
        const float strength = 4f;
        Lighting.AddLight(player.Center, Vector3.One * strength);
    }
}