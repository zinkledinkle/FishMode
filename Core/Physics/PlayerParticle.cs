using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace FishMode.Core.Physics;

public class EntityParticle(Vector2 position, float mass, float radius) : IParticle
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
        MaxInstances = 1,
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
    public bool Grounded { get; set; } = false;
    public float Restitution { get; set; } = 0.3f;
    public List<IConstraint> Constraints { get; set; } = [];
    public void AddConstraint(IConstraint constraint) => Constraints.Add(constraint);
    public void AddForce(Vector2 force) => Force += force / Mass;

    private float airDrag = 0.01f;
    private float waterDrag = 0.07f;
    private float lavaDrag = 0.1f;
    private float honeyDrag = 0.2f;
    private float shimmerDrag = 0f;

    private float gravity = 0.5f;

    private const float maxXSpeed = 30f;
    private const float maxYSpeed = 30f;

    public int touchingTileType = -1;
    public int tileX = 0;
    public int tileY = 0;

    public bool suffocating = false;
    public float rotation = 0;
    private float angleVel = 0;
    public bool dead = false;

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
        this.Restitution = bounce;
        this.gravity = gravity;
    }
    public void ApplyEnviromentalForces()
    {
        if (GetLiquid() == -1)
        {
            Force += Vector2.UnitY * gravity;
        }
        float drag = GetDrag();
        Velocity -= Velocity * drag;
        angleVel = Velocity.X / 15f; //ded
    }
    public void Update()
    {
        Force = Vector2.Zero;
        if (dead && Main.rand.NextBool(2))
        {
            Vector2 vel = Main.rand.NextVector2Circular(10f, 10f);
            float scale = Main.rand.NextFloat(2f);
            Dust.NewDust(Position, 1, 1, DustID.Blood, vel.X, vel.Y, Scale: scale);
        }
        Grounded = false;
        suffocating = false;
        timeSinceLastSplat++;

        if (Velocity.LengthSquared() > 0.1f) Frozen = false;

        if (Frozen) return;

        if (Math.Abs(Velocity.X) > maxXSpeed) Velocity = new(Math.Sign(Velocity.X) * maxXSpeed, Velocity.Y);
        if (Math.Abs(Velocity.Y) > maxYSpeed) Velocity = new(Velocity.X, Math.Sign(Velocity.Y) * maxYSpeed);
    }
    public void Step()
    {
        Velocity += Force;
        Position += Velocity;
        rotation += angleVel;
    }
    public void OnHitGround(float magnitude, Vector2 normal, int surroundingSolidTiles, int tileType, int tileX, int tileY)
    {
        Grounded = true;
        SplatNoise(magnitude);
        CollideDust(magnitude, normal);
        suffocating = surroundingSolidTiles >= 4;
        touchingTileType = tileType;
        this.tileX = tileX;
        this.tileY = tileY;
    }
    private void SplatNoise(float magnitude)
    {   
        if (magnitude < 6f) return;
        int strength = magnitude switch
        {
            < 12f => 1,
            < 22f => 2,
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
    private void CollideDust(float magnitude, Vector2 normal)
    {
        int tries = (int)magnitude / 2;
        int chanceForBlood = (30 - MathHelper.Clamp((int)magnitude, 0, 29));
        normal = -normal.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.5f,1.5f) * magnitude;
        for (int i = 0; i < tries; i++)
        {
            if (Main.rand.NextBool(3)) 
                Dust.NewDust(Position - new Vector2(Radius, Radius), (int)Radius * 2, (int)Radius * 2, DustID.Water, normal.X, normal.Y, Scale: MathHelper.Clamp(magnitude / 6f, 1f, 3f));
            if (Main.rand.NextBool(chanceForBlood) && chanceForBlood < 15)
                Dust.NewDust(Position - new Vector2(Radius, Radius), (int)Radius * 2, (int)Radius * 2, DustID.Blood, normal.X * 0.4f, normal.Y * 0.4f, Scale: MathHelper.Clamp(magnitude / 10f, 0f, 2f));
        }
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
