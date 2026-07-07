using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

internal class ArmoredScales1 : Krill
{
    public override int Level => 2;
    public override Vector2 Position => new(-120, 90);
    public override List<string> Requires => ["Swordfish"];
    public override void Apply(Player player)
    {
        player.endurance += 0.1f;
    }
}
