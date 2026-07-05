using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;
public class Bioluminescence1 : Krill
{
    public override List<string> Requires => ["Waves"];
    public override int Level => 2;
    public override Vector2 Position => new(60, 60);
    public override void Apply(Player player)
    {
        float strength = 1f;
        Lighting.AddLight(player.Center, Vector3.One * strength);
    }
}