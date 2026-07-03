using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace FishMode.Core.Physics;

public class TileConstraint(IParticle particle) : IConstraint
{
    public IParticle Particle { get; } = particle;
    /// <summary>
    /// parameters in order are: velocity along normal, normal vector, surrounding solid tile count, tile type of the tile that was hit, tile X coordinate, tile Y coordinate
    /// </summary>
    public event Action<IParticle, float, Vector2, int, int, int, int> TileCollision = delegate { };
    private int totalSurroundingTiles = 0;
    public void Solve()
    {
        totalSurroundingTiles = 0;

        int tileX = (int)(Particle.Position.X / 16f);
        int tileY = (int)(Particle.Position.Y / 16f);

        int padding = (int)(Particle.Radius / 8f) + 1;

        int addX = Math.Sign(Particle.Velocity.X);
        int addY = Math.Sign(Particle.Velocity.Y);

        List<Rectangle> candidates = [];

        for (int x = tileX - padding + addX; x <= tileX + padding + addX; x++)
        {
            for (int y = tileY - padding + addY; y <= tileY + padding + addY; y++)
            {
                Tile tile = Main.tile[x, y];
                bool solid = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (!tile.HasTile || !solid) continue;

                int xDiff = Math.Abs(x - tileX);
                int yDiff = Math.Abs(y - tileY);
                if (xDiff <= 1 && yDiff <= 1) totalSurroundingTiles++;

                Rectangle tileRect = new(x * 16, y * 16, 16, 16);

                candidates.Add(tileRect);
            }
        }

        foreach (var tile in candidates.OrderBy(t => ClosestPointOnRect(Particle.Position, t).DistanceSQ(Particle.Position)))
        {
            Vector2 closestPoint = ClosestPointOnRect(Particle.Position, tile);
            Vector2 delta = (closestPoint - Particle.Position);
            float dist = delta.Length();

            Vector2 normal = delta.SafeNormalize(Vector2.Zero);
            float velAlongNormal = Vector2.Dot(Particle.Velocity, normal);

            if (dist > Particle.Radius)
            {
                //check if it'll intersect NEXT frame
                float velMagnitude = Particle.Velocity.Length();
                float distToContact = dist - Particle.Radius;

                if (velAlongNormal <= distToContact) continue;

                float normalPenetration = velAlongNormal - Math.Max(0f, distToContact);

                ResolveCollision(normal, normalPenetration, tile);
                break;
            }

            float penetration = Particle.Radius - dist;
            ResolveCollision(normal, penetration, tile);
            break;
        }
        TileCollision(Particle, 0f, Vector2.Zero, totalSurroundingTiles, -1, 0, 0);
    }

    private void ResolveCollision(Vector2 normal, float penetration, Rectangle rect)
    {
        float velAlongNormal = Vector2.Dot(Particle.Velocity, normal);

        Particle.Position -= penetration * normal;

        float i = Particle.Mass * Particle.Radius * Particle.Radius / 2f;
        float j = -2f * velAlongNormal;
        Particle.Velocity += j * normal;
        Particle.Velocity *= Particle.Restitution;

        var tileX = rect.X / 16;
        var tileY = rect.Y / 16;
        var tile = Main.tile[tileX, tileY];
        var type = tile.HasTile ? tile.TileType : -1;

        TileCollision(Particle, velAlongNormal, normal, totalSurroundingTiles, type, tileX, tileY);
    }
    private static Vector2 ClosestPointOnRect(Vector2 p, Rectangle rect) =>
    new(
        Math.Clamp(p.X, rect.Left, rect.Right),
        Math.Clamp(p.Y, rect.Top, rect.Bottom)
    ); //closestpointinrect doesn't work because it just uses the actual sides

}
