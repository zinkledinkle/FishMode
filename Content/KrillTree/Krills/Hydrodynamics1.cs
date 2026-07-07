using FishMode.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Hydrodynamics1 : Krill
{
    public override int Level => 3;
    public override Vector2 Position => new(120, 90);
    public override List<string> Requires => ["Bioluminescence1"];
    public override void Apply(Player player)
    {
        player.GetModPlayer<FishPlayer>().hydrodynamics += 0.5f;
    }
}
