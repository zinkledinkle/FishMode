global using static FishMode.Utilities.Extensions;
using FishMode.Content.KrillTree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace FishMode.Utilities;

public static class Extensions
{
    public static void DrawLineFromPoints(this SpriteBatch spriteBatch, Texture2D texture, List<Vector2> points, float width, Color color = default, Rectangle? source = null)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            if (color == default) color = Color.White;
            Vector2 pointA = points[i];
            Vector2 pointB = points[i + 1];
            Vector2 drawPos = (pointA + pointB) * 0.5f;
            Vector2 delta = pointB - pointA;
            float angleTo = MathF.Atan2(delta.Y, delta.X);
            float dist = delta.Length();
            Vector2 scale = new(width / texture.Width, dist / texture.Height);
            Vector2 origin = (source?.Size() ?? texture.Size()) * 0.5f;
            spriteBatch.Draw(texture, drawPos, source, color, angleTo + MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0f);
        }
    }
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
