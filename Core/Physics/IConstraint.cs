namespace FishMode.Core.Physics;

public interface IConstraint
{
    public IParticle ParticleA { get; }
    public IParticle ParticleB { get; }
    public int IterationCount { get; set; }
    public void Apply();
}
