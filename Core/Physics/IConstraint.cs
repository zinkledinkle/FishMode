namespace FishMode.Core.Physics;

public interface IConstraint
{
    public IParticle Particle { get; }
    public void Solve();
}