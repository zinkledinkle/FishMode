using Microsoft.Xna.Framework;
using Terraria;

namespace FishMode.Core.Physics;

public class DistanceConstraint(IParticle particleA, IParticle particleB, float distance, float stretchResistance = 0.1f, float squishResistance = 0.04f) : IConstraint
{
    public IParticle Particle { get; } = particleA;
    public IParticle ParticleB { get; } = particleB;
    public float Distance { get; } = distance;
    public float StretchResistance { get; } = stretchResistance;
    public float SquishResistance { get; } = squishResistance;
    public void Solve()
    {
        float positionalCorrectionRatio = 0.5f;
        float distance = Vector2.Distance(Particle.Position, ParticleB.Position);
        float difference = distance - Distance;
        Vector2 dir = (ParticleB.Position - Particle.Position).SafeNormalize(Main.rand.NextVector2CircularEdge(1f, 1f));

        var force = dir * difference;
        if (difference > 0.01f) force *= StretchResistance;
        else if (difference < -0.01f) force *= SquishResistance;

        Particle.Velocity += force * (1 - positionalCorrectionRatio);
        ParticleB.Velocity -= force * (1 - positionalCorrectionRatio);
        Particle.Position += force * positionalCorrectionRatio;
        ParticleB.Position -= force * positionalCorrectionRatio;
    }
}
