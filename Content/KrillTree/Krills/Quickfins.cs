using Microsoft.Xna.Framework;
using Terraria;

namespace FishMode.Content.KrillTree.Krills;

public class Quickfins : Krill
{
    public override int Level => 1;
    public override Vector2 Position => new(-60, -60);
    public override void Apply(Player player) => player.moveSpeed += 0.1f;
}
