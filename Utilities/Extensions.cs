global using static FishMode.Utilities.Extensions;
using FishMode.Content.KrillTree;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria.ModLoader;

namespace FishMode.Utilities;

public static class Extensions
{
    private static int GetTypeIDFromModType<T>() where T : ModType
    {
        var instance = ModContent.GetInstance<T>();
        return instance switch
        {
            ModItem m => m.Type,
            ModProjectile p => p.Type,
            ModNPC n => n.Type,
            ModTile t => t.Type,
            ModWall w => w.Type,
            ModCloud c => c.Type,
            ModBuff b => b.Type,
            ModPrefix pr => pr.Type,
            ModGore g => g.Type,
            ModRarity r => r.Type,
            ModDust d => d.Type,
            ModEmoteBubble e => e.Type,
            ModMount mo => mo.Type,
            _ => -1
        };
    }
    extension<T>(T) where T : ModType
    {
        public static int ID => GetTypeIDFromModType<T>();
    }
    extension<T>(T) where T : class
    {
        public static T? Instance => ModContent.GetInstance<T>();
    }
    extension<T>(T) where T : Krill
    {
        public static int Type => KrillTree.Krills.FirstOrDefault(k => k.Value.GetType() == typeof(T)).Key;
    }
    extension(Rectangle rect)
    {
        public Vector2 ClosestContactPoint(Vector2 p) => new(
        Math.Clamp(p.X, rect.Left, rect.Right),
        Math.Clamp(p.Y, rect.Top, rect.Bottom)
        );
        //closestpointinrect just snaps to each edge
    }
}
