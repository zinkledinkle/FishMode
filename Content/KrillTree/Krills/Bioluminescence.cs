using Microsoft.Xna.Framework;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;
public class Bioluminescence : Krill
{
    public override int Level => 1;
    public override Vector2 Position => new(60, 60);
    public override void Apply(Player player)
    {
        const float strength = 1f;
        Lighting.AddLight(player.Center, Vector3.One * strength);
    }
}