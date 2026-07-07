using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace FishMode.Core.Physics;

public abstract class FishBody
{
    protected virtual float BaseSegmentMass => 3f;
    protected virtual float ConstraintSquishResist => 0.1f;
    protected virtual float ConstraintStretchResist => 0.3f;
    public virtual float Width { get; set; }
    public virtual float SegmentLength { get; set; }
    public virtual int ConstraintIterations { get; set; } = 1;
    public readonly List<EntityParticle> particles = [];
    public bool Grounded => particles.Count(p => p.Grounded) > particles.Count / 2;
    public bool Submerged => particles.Count(p => p.GetLiquid() > -1) > particles.Count / 2;
    public bool dead = false;
    protected virtual void Setup(Vector2 pos, int segments)
    {
        for (int i = 0; i < segments; i++)
        {
            float mass = i == 0 ? BaseSegmentMass * 1.5f : BaseSegmentMass;
            EntityParticle particle = new(pos + Vector2.UnitY * i * SegmentLength, mass, Width / 2f);
            particles.Add(particle);
        }

        for (int i = 0; i < segments - 1; i++)
        {
            EntityParticle particleA = particles[i];
            for (int j = i + 1; j < segments; j++)
            {
                EntityParticle particleB = particles[j];
                int diff = j - i;
                DistanceConstraint constraint = new(particleA, particleB, SegmentLength * diff, ConstraintStretchResist, ConstraintSquishResist);
                particleA.AddConstraint(constraint);
            }
        }
        foreach (var particle in particles)
        {
            var tileCollide = new TileConstraint(particle);
            particle.AddConstraint(tileCollide);
            tileCollide.TileCollision += OnTileCollision;
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
        foreach (var particle in particles)
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
    public void AddEnviromentalValues(
    float airDrag = 0f,
    float waterDrag = 0f,
    float lavaDrag = 0f,
    float honeyDrag = 0f,
    float shimmerDrag = 0f,
    float bounce = 0f,
    float gravity = 0f)
    {
        foreach (var particle in particles)
            particle.AddEnviromentalValues(
                airDrag,
                waterDrag,
                lavaDrag,
                honeyDrag,
                shimmerDrag,
                bounce,
                gravity
            );
    }
    public void MultiplyEnviromentalValues(
    float airDrag = 1f,
    float waterDrag = 1f,
    float lavaDrag = 1f,
    float honeyDrag = 1f,
    float shimmerDrag = 1f,
    float bounce = 1f,
    float gravity = 1f)
    {
        foreach (var particle in particles)
            particle.MultiplyEnviromentalValues(
                airDrag,
                waterDrag,
                lavaDrag,
                honeyDrag,
                shimmerDrag,
                bounce,
                gravity
            );
    }
    protected virtual void OnTileCollision(IParticle particle, float velAlongNormal, Vector2 normal, int surroundingSolidTiles, int tileType, int tileX, int tileY)
    {
        if (tileType == -1) return;
        (particle as EntityParticle)?.OnHitGround(velAlongNormal, normal, surroundingSolidTiles, tileType, tileX, tileY);
    }
    public void AddSegment()
    {
        if (particles.Count >= 50) return;

        var delta = particles[^1].Position - particles[^2].Position;
        EntityParticle particle = new(particles[^1].Position + delta, BaseSegmentMass, Width / 2f);
        particles.Add(particle);
        var tileCollide = new TileConstraint(particle);
        particle.AddConstraint(tileCollide);
        tileCollide.TileCollision += OnTileCollision;

        for (int i = 0; i < particles.Count - 1; i++)
        {
            EntityParticle particleB = particles[i];
            int diff = particles.Count - 1 - i;
            DistanceConstraint constraint = new(particleB, particle, SegmentLength * diff, ConstraintStretchResist, ConstraintSquishResist);
            particleB.AddConstraint(constraint);
        }
    }
    public void RemoveSegment()
    {
        if (particles.Count <= 1) return;
        foreach (var particle in particles)
            particle.Constraints.RemoveAll(c => c is DistanceConstraint dc && dc.ParticleB == particles[^1]);
        particles.RemoveAt(particles.Count - 1);
    }
    public virtual void Kill()
    {
        dead = true;
        foreach (var particle in particles)
        {
            particle.Constraints.RemoveAll(c => c is DistanceConstraint); //#loloutloud
            particle.dead = true;
            float speed = 20f;
            particle.Force = Main.rand.NextVector2Circular(speed, speed);
        }
    }
    public virtual void Propel(float speed, Vector2 target, float falloff = 0.4f)
    {
        if (dead) return;
        foreach (var particle in particles)
        {
            var dir = particle.Position.DirectionTo(target).SafeNormalize(Vector2.Zero);
            float air = Submerged ? 1f : 0.3f;
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
    public virtual void Update()
    {
        foreach (var particle in particles)
        {
            particle.ApplyEnviromentalForces();
            particle.Step();
            particle.Update();
        }
        for (int i = 0; i < ConstraintIterations; i++)
        {
            foreach (var particle in particles)
                (particle as IParticle).SolveConstraints();
        }
    }
}
