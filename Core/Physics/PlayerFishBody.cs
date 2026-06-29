using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core.Physics;

public class PlayerFishBody
{
    public static int ParticleCount => ModContent.GetInstance<FishModeConfig>().BodySegmentCount;
    public static float Width => ModContent.GetInstance<FishModeConfig>().BodyWidth;
    public static float Height => ModContent.GetInstance<FishModeConfig>().BodyLength;
    public readonly List<PlayerParticle> particles = [];
    public bool Grounded => particles.Any(p => p.Grounded);
    public PlayerFishBody(Player player)
    {
        float segmentDistance = Height / (float)ParticleCount;
        float totalMass = 12f;
        float headMassRatio = 0.35f; //takes 35% of the mass

        for (int i = 0; i < ParticleCount; i++)
        {
            float mass = i == 0 ? totalMass * headMassRatio : (totalMass * (1 - headMassRatio) / (float)(ParticleCount - 2));
            PlayerParticle particle = new(player.Bottom - Vector2.UnitY * i * segmentDistance, mass, Width / 2f);
            particles.Add(particle);
        }

        float squish = 0.2f;
        float stretch = 0.3f;
        int iterations = (int)(ParticleCount * 2);

        for(int i = 0; i < ParticleCount - 1; i++)
        {
            PlayerParticle particleA = particles[i];
            PlayerParticle particleB = particles[i + 1];
            DistanceConstraint constraint = new(particleA, particleB, segmentDistance, stretch, squish, iterations);
            particleA.AddConstraint(constraint);
        }
        for (int i = ParticleCount - 1; i > 0; i--)
        {
            PlayerParticle particleA = particles[i];
            PlayerParticle particleB = particles[i - 1];
            DistanceConstraint constraint = new(particleA, particleB, segmentDistance, stretch, squish, iterations);
            particleA.AddConstraint(constraint);
        }
    }
    public void DebugGrab()
    {
        foreach (var particle in particles)
        {
            float distance = Vector2.Distance(particle.Position, Main.MouseWorld);
            if ((distance < particle.Radius || particle.Grabbed) && Main.mouseLeft)
            {
                particle.Velocity = Vector2.Zero;
                particle.Position = Main.MouseWorld;
                particle.Frozen = true;
                particle.Grabbed = true;
                break;
            }
            else
            {
                particle.Grabbed = false;
                particle.Frozen = false;
            }
        }
    }
    public void Propel(float speed, float falloff = 0.4f)
    {
        foreach (var particle in particles)
        {
            float air = particle.GetLiquid() == 0 ? 1f : 0.8f;
            var dir = particle.Position.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.Zero) * speed * air;
            dir.Y *= air;
            particle.AddForce(dir);
            speed *= falloff; //all particles contribute but less
            particle.Frozen = false;
        }
    }
    public void Teleport(Vector2 pos)
    {
        Vector2 center = new(particles.Average(p => p.Position.X), particles.Average(p => p.Position.Y));
        foreach (var particle in particles)
        {
            particle.Frozen = false;
            particle.Position = pos;
        }
    }
    public void Jump(float strength)
    {
        foreach (var particle in particles)
        {
            particle.Frozen = false;
            particle.Position -= Vector2.UnitY * 2;
            particle.Velocity += Vector2.UnitY * (particle.Grounded ? -strength : -strength * 0.5f);
        }
    }
    public void Update()
    {
        foreach (var particle in particles)
        {
            particle.Update();
        }

        SolveConstraints();

        foreach (var particle in particles)
            particle.Step();
    }
    public void SolveConstraints()
    {
        int iterations = 0;
        int maxIterations = 1;

        while(iterations < maxIterations)
        {
            foreach (var particle in particles)
            {
                foreach (var constraint in particle.Constraints)
                {
                    if (constraint.IterationCount <= iterations) continue;
                    constraint.Apply();
                    if (constraint.IterationCount > maxIterations)
                        maxIterations = constraint.IterationCount;
                }
                iterations++;
            }
        }
    }
}
public class PlayerParticle(Vector2 position, float mass, float radius) : IParticle
{
#nullable enable
    public Vector2 Position { get; set; } = position;
    public Vector2 Velocity { get; set; }
    public Vector2 Force { get; set; }
    public float Mass { get; set; } = mass;
    public float Radius { get; set; } = radius;
    public bool Frozen { get; set; } = false;
    public bool Grabbed { get; set; } = false;
    public bool Grounded { get; set; } = false;
    public List<IConstraint> Constraints { get; set; } = [];
    public void AddConstraint(IConstraint constraint) => Constraints.Add(constraint);
    public void AddForce(Vector2 force) => Force += force / Mass;

    private static float airDrag = 0.01f;
    private static float waterDrag = 0.07f;
    private static float lavaDrag = 0.1f;
    private static float honeyDrag = 0.2f;
    private static float shimmerDrag = 0f;

    private static float bounce = 0.7f;

    private static float gravity = 0.5f;

    private static float maxXSpeed = 30f;
    private static float maxYSpeed = 20f;

    public void Update()
    {
        bounce = 0.35f;

        Grounded = false;

        if (Velocity.LengthSquared() > 0.1f) Frozen = false;

        if (Frozen) return;

        if (GetLiquid() == -1)
        {
            Force += Vector2.UnitY * gravity;
        }

        float drag = GetDrag();
        Velocity -= Velocity * drag;

        TileCollide();

        if (Math.Abs(Velocity.X) > maxXSpeed) Velocity = new(Math.Sign(Velocity.X) * maxXSpeed, Velocity.Y);
        if (Math.Abs(Velocity.Y) > maxYSpeed) Velocity = new(Velocity.X, Math.Sign(Velocity.Y) * maxYSpeed);
    }
    public void Step()
    {
        Velocity += Force;
        Position += Velocity;

        Force = Vector2.Zero;
    }
    //remove
    private static Vector2 GetLineRectangleIntersect(Vector2 p1, Vector2 p2, Rectangle rect)
    {
        Vector2 delta = p2 - p1;
        float tMin = 0f;
        float tMax = 1f;
        for (int i = 0; i < 2; i++)
        {
            float s = i == 0 ? p1.X : p1.Y;
            float d = i == 0 ? delta.X : delta.Y;

            float min = i == 0 ? rect.Left : rect.Top;
            float max = i == 0 ? rect.Right : rect.Bottom;

            if (Math.Abs(d) < 0.0001f)
            {
                if (s < min || s > max)
                    return p2;
            }
            else
            {
                float t1 = (min - s) / d;
                float t2 = (max - s) / d;
                if (t1 > t2) (t1, t2) = (t2, t1); //switcheroo
                tMin = Math.Max(tMin, t1);
                tMax = Math.Min(tMax, t2);

                if (tMin > tMax)
                    return p2;
            }
        }
        return p1 + delta * Math.Clamp(tMin, 0f, 1f);
    }
    private static Vector2 ClosestPointOnRect(Vector2 p, Rectangle rect)
    {
        return new Vector2(
            Math.Clamp(p.X, rect.Left, rect.Right),
            Math.Clamp(p.Y, rect.Top, rect.Bottom)
        ); //closestpointinrect doesn't work because it just uses the actual sides
    }
    public bool TileCollide()
    {
        int tileX = (int)(Position.X / 16f);
        int tileY = (int)(Position.Y / 16f);

        int padding = (int)(Radius / 8f) + 1;

        tileX += Math.Sign(Velocity.X);
        tileY += Math.Sign(Velocity.Y);

        Grounded = false;

        List<Rectangle> candidates = [];

        for (int x = tileX - padding; x <= tileX + padding; x++)
        {
            for (int y = tileY - padding; y <= tileY + padding; y++)
            {
                Tile tile = Main.tile[x, y];
                bool solid = Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (!tile.HasTile || !solid) continue;

                Rectangle tileRect = new(x * 16, y * 16, 16, 16);

                candidates.Add(tileRect);
            }
        }

        foreach (var tile in candidates.OrderBy(t => ClosestPointOnRect(Position, t).DistanceSQ(Position)))
        {
            Vector2 closestPoint = ClosestPointOnRect(Position, tile);
            Vector2 delta = (closestPoint - Position);
            float dist = delta.Length();

            Vector2 normal = delta.SafeNormalize(Vector2.Zero);
            float velAlongNormal = Vector2.Dot(Velocity, normal);

            if (dist > Radius)
            {
                //check if it'll intersect NEXT frame
                float velMagnitude = Velocity.Length();
                float distToContact = dist - Radius;

                if (velAlongNormal <= distToContact) continue;

                float normalPenetration = velAlongNormal - Math.Max(0f, distToContact);
                Position -= normalPenetration * normal;

                Velocity = (Velocity - 2f * velAlongNormal * normal) * bounce;
                Grounded = true;
                break;
            }

            float penetration = Radius - dist;
            Position -= penetration * normal;
            Velocity = (Velocity - 2f * velAlongNormal * normal) * bounce;
            Grounded = true;
            break;
        }

        return false;
    }
    public float GetDrag()
    {
        int type = GetLiquid();

        return type switch
        {
            LiquidID.Water => waterDrag,
            LiquidID.Lava => lavaDrag,
            LiquidID.Honey => honeyDrag,
            LiquidID.Shimmer => shimmerDrag,
            _ => airDrag
        };
    }
    public int GetLiquid()
    {
        int tileX = (int)(Position.X / 16f);
        int tileY = (int)(Position.Y / 16f);
        float yOffset = Position.Y % 16f;

        Tile tile = Main.tile[tileX, tileY];
        float liquidAmount = tile.LiquidAmount / 255f * 16f;
        if (liquidAmount + Radius < yOffset || tile.LiquidAmount == 0)
            return -1;
        return tile.LiquidType;
    }
}
