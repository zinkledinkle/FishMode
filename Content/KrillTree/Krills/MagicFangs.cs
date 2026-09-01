using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Content.KrillTree.Krills;

public class MagicFangs : Krill
{
    public override IReadOnlyList<Type> Requirements => [typeof(Bioluminescence)];
    public override int Level => 3;
    public override Vector2 Position => new(120, 30);
    public override void Apply(Player player)
    {
        player.GetModPlayer<MagicFangs_Player>().enabled = true;
    }
    public class MagicFangs_Player : ModPlayer
    {
        public bool enabled = false;
        private int time;
        private const int timeMax = 5; //limit regen rate
        public override void ResetEffects()
        {
            enabled = false;
            time = Math.Max(time - 1, 0);
        }
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.DamageType != DamageClass.Magic || !enabled || time > 0) return;
            int amount = damageDone / 15;
            Player.statMana = MathHelper.Clamp(Player.statMana + amount, Math.Min(Player.statMana + 1, Player.statManaMax2), Player.statManaMax2);
            Player.ManaEffect(Math.Min(amount + Player.statMana, Player.statManaMax2) - Player.statMana);
            time = timeMax;
        }
    }
}
