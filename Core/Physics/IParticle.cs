using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FishMode.Core.Physics;

public interface IParticle
{
    #nullable enable
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public Vector2 Force { get; set; }
    public float Mass { get; set; }
    public float Radius { get; set; }
    public float Restitution { get; set; }
    public List<IConstraint> Constraints { get; set; }
    public void Update();
    public void SolveConstraints()
    {
        foreach (var constraint in Constraints)
        {
            constraint.Solve();
        }
    }
    public void Step()
    {
        Velocity += Force;
        Position += Velocity;
        Force = Vector2.Zero;
    }
    public void AddForce(Vector2 force)
    {
        Force += force;
    }
}
