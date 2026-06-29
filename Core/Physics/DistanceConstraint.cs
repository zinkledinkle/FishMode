using Microsoft.Xna.Framework;
using Terraria;

namespace FishMode.Core.Physics;

public class DistanceConstraint(IParticle particleA, IParticle particleB, float distance, float stretchResistance = 0.1f, float squishResistance = 0.04f, int iterations = 1) : IConstraint
{
    public IParticle ParticleA { get; } = particleA;
    public IParticle ParticleB { get; } = particleB;
    public float Distance { get; } = distance;
    public float StretchResistance { get; } = stretchResistance;
    public float SquishResistance { get; } = squishResistance;
    public int IterationCount { get; } = iterations;
    public void Apply()
    {
        float distance = Vector2.Distance(ParticleA.Position, ParticleB.Position);
        float difference = distance - Distance;
        if (difference > 0.01f)
        {
            Vector2 dir = (ParticleB.Position - ParticleA.Position).SafeNormalize(Vector2.Zero);
            if (dir == Vector2.Zero)
            {
                dir = Main.rand.NextVector2CircularEdge(1f, 1f);
            }
            ParticleA.AddForce(dir * (difference * StretchResistance));
            ParticleB.AddForce(-dir * (difference * StretchResistance));
        }
        else if (difference < -0.01f)
        {
            Vector2 dir = (ParticleB.Position - ParticleA.Position).SafeNormalize(Vector2.Zero);
            if (dir == Vector2.Zero)
            {
                dir = Main.rand.NextVector2CircularEdge(1f, 1f);
            }
            ParticleA.AddForce(dir * (difference * SquishResistance));
            ParticleB.AddForce(-dir * (difference * SquishResistance));
        }
    }
}
