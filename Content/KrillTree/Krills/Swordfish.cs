using FishMode.Common;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;
public class Swordfish : Krill
{
    public override List<string> Requires => ["Waves"];
    public override int Level => 2;
    public override Vector2 Position => new(-60, 60);
    public override void Apply(Player player)
    {
        var speed = player.GetModPlayer<FishPlayer>().Body.particles[0].Velocity.Length();
        speed = MathHelper.Clamp(speed * 0.05f, 0f, 0.15f);
        player.GetDamage(DamageClass.Generic) += speed;
    }
}