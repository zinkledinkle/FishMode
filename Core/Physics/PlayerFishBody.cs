using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Animations;
using Terraria.Graphics.CameraModifiers;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

namespace FishMode.Core.Physics;

public class PlayerFishBody
{
    private const float baseSegmentMass = 3f;
    private const float constraintSquishResist = 0.1f;
    private const float constraintStretchResist = 0.3f;
    public static float Width => ModContent.GetInstance<FishModeConfig>().BodyWidth;
    public static float SegmentLength => ModContent.GetInstance<FishModeConfig>().BaseSegmentLength;
    public readonly List<PlayerParticle> particles = [];
    public bool Grounded => particles.Count(p => p.Grounded) > particles.Count / 2;
    public bool Submerged => particles.Count(p => p.GetLiquid() > -1) > particles.Count / 2;
    private Player Player { get; set; }
    public bool dead = false;

    private int timeInAir;
    private int constraintIterations = 0;
    private int jumpCooldown = 0;
    public PlayerFishBody(Player player, int segments)
    {
        Player = player;
        for (int i = 0; i < segments; i++)
        {
            float mass = i == 0 ? baseSegmentMass * 1.5f : baseSegmentMass;
            PlayerParticle particle = new(player.Top + Vector2.UnitY * i * SegmentLength, mass, Width / 2f);
            particles.Add(particle);
        }

        constraintIterations = (int)(segments / 4f);

        for(int i = 0; i < segments - 1; i++)
        {
            PlayerParticle particleA = particles[i];
            for (int j = i + 1; j < segments; j++)
            {
                PlayerParticle particleB = particles[j];
                int diff = j - i;
                DistanceConstraint constraint = new(particleA, particleB, SegmentLength * diff, constraintStretchResist, constraintSquishResist);
                particleA.AddConstraint(constraint);
            }
        }
        foreach(var particle in particles)
        {
            var tileCollide = new TileConstraint(particle);
            particle.AddConstraint(tileCollide);
            tileCollide.TileCollision += OnTileCollision;
            //to make sure tile collision happens AFTER distance constraint
        }
    }

    private void OnTileCollision(IParticle particle, float velAlongNormal, Vector2 normal, int surroundingSolidTiles, int tileType, int tileX, int tileY)
    {
        if (tileType == -1) return;
        (particle as PlayerParticle).OnHitGround(velAlongNormal, normal, surroundingSolidTiles, tileType, tileX, tileY);
        FallDamage(velAlongNormal);
    }

    public void SetEnviromentalValues(
    float airDrag = 0.01f,
    float waterDrag = 0.07f,
    float lavaDrag = 0.1f,
    float honeyDrag = 0.2f,
    float shimmerDrag = 0f,
    float bounce = 0.3f,
    float gravity = 0.5f)
    {
        foreach(var particle in particles)
            particle.SetEnviromentalValues(
                airDrag,
                waterDrag,
                lavaDrag,
                honeyDrag,
                shimmerDrag,
                bounce, 
                gravity
            );
    }
    public void AddSegment()
    {
        if (particles.Count >= 50) return;

        var delta = particles[^1].Position - particles[^2].Position;
        PlayerParticle particle = new(particles[^1].Position + delta, baseSegmentMass, Width / 2f);
        particles.Add(particle);
        var tileCollide = new TileConstraint(particle);
        particle.AddConstraint(tileCollide);
        tileCollide.TileCollision += OnTileCollision;

        for (int i = 0; i < particles.Count - 1; i++)
        {
            PlayerParticle particleB = particles[i];
            int diff = particles.Count - 1 - i;
            DistanceConstraint constraint = new(particleB, particle, SegmentLength * diff, constraintStretchResist, constraintSquishResist);
            particleB.AddConstraint(constraint);
        }

        constraintIterations = (int)(particles.Count);
    }
    public void RemoveSegment()
    {
        if (particles.Count <= 1) return;
        foreach (var particle in particles)
            particle.Constraints.RemoveAll(c => c is DistanceConstraint dc && dc.ParticleB == particles[^1]);
        particles.RemoveAt(particles.Count - 1);
        constraintIterations = (int)(particles.Count);
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
    private void FallDamage(float magnitude)
    {
        if (dead) return;
        if (magnitude > 22f)
        {
            float strength = MathF.Min(magnitude + (timeInAir * 0.2f), 100f);
            int time = (int)magnitude + (int)(timeInAir * 0.4f);
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Player.Center, Main.rand.NextVector2Circular(1f, 1f), strength, 5f, time, 500f, "splatShake"));
        }
        if (Player.noFallDmg || Submerged) return;
        float thresh = 12f;
        float mult = 20f;
        float timeMultiplier = 1f;
        if (magnitude < thresh) return;
        int timeDmg = Math.Max((int)(timeInAir * timeMultiplier - 100), 0);
        int dmg = (int)((magnitude - thresh) * mult) + timeDmg;
        Player.Hurt(PlayerDeathReason.ByOther(0), dmg, 0);
        timeInAir = 0;
    }
    public void Kill()
    {
        dead = true;
        foreach (var particle in particles)
        {
            particle.Constraints.RemoveAll(c => c is DistanceConstraint); //lol
            particle.dead = true;
            float speed = 20f;
            particle.Force = Main.rand.NextVector2Circular(speed, speed);
        }
    }
    public void Propel(float speed, float falloff = 0.4f, Vector2? direction = null)
    {
        if (dead) return;
        direction ??= Main.MouseWorld;
        foreach (var particle in particles)
        {
            var dir = particle.Position.DirectionTo(direction.Value).SafeNormalize(Vector2.Zero);
            float air = Submerged ? 1f : 0.7f;
            var dot = (1 - Vector2.Dot(-Vector2.UnitY, dir)) * 0.5f;
            dot *= 0.5f; dot += 0.5f;
            if (!Submerged) air *= dot;
            var vel = dir * speed * air;
            vel.Y *= air;
            particle.AddForce(vel);
            speed *= falloff; //all particles contribute but less
            particle.Frozen = false;
        }
    }
    public void Teleport(Vector2 pos)
    {
        Vector2 center = new(particles.Average(p => p.Position.X), particles.Average(p => p.Position.Y));
        List<Vector2> offsets = [];
        foreach (var particle in particles)
        {
            offsets.Add(particles[0].Position - particle.Position);
        }
        for (int i = 0; i < particles.Count; i++)
        {
            var particle = particles[i];
            particle.Frozen = false;
            particle.Position = pos - offsets[i];
        }
    }
    public void Jump(float strength)
    {
        if (jumpCooldown > 0) return;
        foreach (var particle in particles)
        {
            particle.Frozen = false;
            particle.Position -= Vector2.UnitY * 1;
            particle.Velocity += Vector2.UnitY * (particle.Grounded ? -strength : -strength * 0.5f);
        }
        jumpCooldown = 20;
    }
    public void Update()
    {
        jumpCooldown = Math.Max(jumpCooldown - 1, 0);
        foreach (var particle in particles)
        {
            particle.ApplyEnviromentalForces();
            particle.Step();
            particle.Update();
        }
        timeInAir = Grounded || particles[0].Velocity.Y < -4 ? 0 : timeInAir + 1;
        for (int i = 0; i < constraintIterations; i++)
        {
            foreach (var particle in particles)
                (particle as IParticle).SolveConstraints();
        }
    }
}