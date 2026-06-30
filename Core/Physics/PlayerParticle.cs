using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace FishMode.Core.Physics;

public class PlayerParticle(Vector2 position, float mass, float radius) : IParticle
{
#nullable enable
    #region sounds
    private static readonly SoundStyle SplatWeak = new("FishMode/Assets/Sounds/SplatSmall", 3, SoundType.Sound)
    {
        Volume = 0.5f,
        MaxInstances = 0,
        PitchVariance = 0.5f,
    };
    private static readonly SoundStyle SplatMedium = new("FishMode/Assets/Sounds/SplatMedium", 3, SoundType.Sound)
    {
        Volume = 0.6f,
        MaxInstances = 0,
        PitchVariance = 0.3f,
    };
    private static readonly SoundStyle SplatHard = new("FishMode/Assets/Sounds/SplatHard", 3, SoundType.Sound)
    {
        Volume = 0.8f,
        MaxInstances = 0,
        PitchVariance = 0.2f,
    };
    private int timeSinceLastSplat = 0;
    #endregion

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

    private float airDrag = 0.01f;
    private float waterDrag = 0.07f;
    private float lavaDrag = 0.1f;
    private float honeyDrag = 0.2f;
    private float shimmerDrag = 0f;

    private float bounce = 0.3f;

    private float gravity = 0.5f;

    private const float maxXSpeed = 30f;
    private const float maxYSpeed = 20f;

    public void SetEnviromentalValues(
        float airDrag = 0.01f,
        float waterDrag = 0.07f,
        float lavaDrag = 0.1f,
        float honeyDrag = 0.2f,
        float shimmerDrag = 0f,
        float bounce = 0.3f,
        float gravity = 0.5f)
    {
        this.airDrag = airDrag;
        this.waterDrag = waterDrag;
        this.lavaDrag = lavaDrag;
        this.honeyDrag = honeyDrag;
        this.shimmerDrag = shimmerDrag;
        this.bounce = bounce;
        this.gravity = gravity;
    }

    public void Update()
    {
        Grounded = false;
        timeSinceLastSplat++;

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
    private static Vector2 ClosestPointOnRect(Vector2 p, Rectangle rect) => 
        new(
            Math.Clamp(p.X, rect.Left, rect.Right),
            Math.Clamp(p.Y, rect.Top, rect.Bottom)
        ); //closestpointinrect doesn't work because it just uses the actual sides

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
                SplatNoise(velAlongNormal);
                break;
            }

            float penetration = Radius - dist;
            Position -= penetration * normal;
            Velocity = (Velocity - 2f * velAlongNormal * normal) * bounce;
            Grounded = true;
            SplatNoise(velAlongNormal);
            break;
        }

        return false;
    }
    private void SplatNoise(float magnitude)
    {
        if (magnitude < 5f) return;
        int strength = magnitude switch
        {
            < 10f => 1,
            < 20f => 2,
            _ => 3
        };
        int requiredCooldown = strength * 15;
        if (timeSinceLastSplat < requiredCooldown) return;
        SoundStyle sound = strength switch
        {
            1 => SplatWeak,
            2 => SplatMedium,
            _ => SplatHard
        };
        SoundEngine.PlaySound(sound, Position);
    }
    private float GetDrag()
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
