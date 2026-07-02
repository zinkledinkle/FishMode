using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
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
    public PlayerFishBody(Player player, int segments)
    {
        Player = player;
        for (int i = 0; i < segments; i++)
        {
            float mass = i == 0 ? baseSegmentMass * 1.5f : baseSegmentMass;
            PlayerParticle particle = new(player.Top + Vector2.UnitY * i * SegmentLength, mass, Width / 2f);
            particles.Add(particle);
            particle.OnHitGround += FallDamage;
        }

        int iterations = (int)(segments * 1.5f);

        for(int i = 0; i < segments - 1; i++)
        {
            PlayerParticle particleA = particles[i];
            for(int j = i + 1; j < segments; j++)
            {
                PlayerParticle particleB = particles[j];
                int diff = j - i;
                DistanceConstraint constraint = new(particleA, particleB, SegmentLength * diff, constraintStretchResist, constraintSquishResist, iterations);
                particleA.AddConstraint(constraint);
            }
        }
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
        particle.OnHitGround += FallDamage;

        int iterations = (int)(particles.Count * 2);

        for (int i = 0; i < particles.Count - 1; i++)
        {
            PlayerParticle particleB = particles[i];
            int diff = particles.Count - 1 - i;
            DistanceConstraint constraint = new(particleB, particle, SegmentLength * diff, constraintStretchResist, constraintSquishResist, iterations);
            foreach (var c in particle.Constraints)
                c.IterationCount = iterations;
            particleB.AddConstraint(constraint);
        }
    }
    public void RemoveSegment()
    {
        if (particles.Count <= 1) return;
        foreach (var particle in particles)
            particle.Constraints.RemoveAll(c => c.ParticleB == particles[^1]);
        particles[^1].OnHitGround -= FallDamage;
        particles.RemoveAt(particles.Count - 1);
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
        if (magnitude > 15f)
        {
            float strength = MathF.Min(magnitude + (timeInAir * 0.2f), 100f);
            int time = (int)magnitude + (int)(timeInAir * 0.4f);
            Main.instance.CameraModifiers.Add(new PunchCameraModifier(Player.Center, Main.rand.NextVector2Circular(1f, 1f), strength, 5f, time, 500f, "splatShake"));
        }
        if (Player.noFallDmg || Submerged) return;
        float thresh = 12f;
        int mult = 7;
        float timeMultiplier = 2f;
        if (magnitude < thresh) return;
        int dmg = (int)(magnitude - thresh) * mult + (int)(timeInAir * timeMultiplier - 100);
        Player.Hurt(PlayerDeathReason.ByOther(0), dmg, 0);
        if (Player.statLife <= 0)
        {
            Kill();
        }
        timeInAir = 0;
    }
    private void Kill()
    {
        dead = true;
        foreach (var particle in particles)
        {
            particle.Constraints.Clear(); //lol
            particle.dead = true;
            float speed = 15f;
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
        foreach (var particle in particles)
        {
            particle.Frozen = false;
            particle.Position -= Vector2.UnitY * 2;
            particle.Velocity += Vector2.UnitY * (particle.Grounded ? -strength : -strength * 0.5f);
        }
    }
    public void Update()
    {
        timeInAir = Grounded || particles[0].Velocity.Y < 0 ? 0 : timeInAir + 1;
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