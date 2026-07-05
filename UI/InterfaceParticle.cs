using FishMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace FishMode.UI;

public struct InterfaceParticle(int type, Vector2 position, Vector2 velocity, float scale, float alpha, float spin)
{
    public Vector2 Position { get; set; } = position;
    public Vector2 Velocity { get; set; } = velocity;
    public float Rotation { get; set; }
    private float rotVel = spin;
    public float Scale { get; set; } = scale;
    public float Alpha { get; set; } = alpha;
    public int Type { get; } = type;
    public void Update(float dt)
    {
        Position += Velocity;
        Velocity -= Velocity * 0.08f * dt;
        Scale -= 0.005f * dt;
        Alpha -= 0.01f * dt;
        Rotation += rotVel;
        rotVel -= rotVel * 0.02f * dt;
    }
    public void Draw(SpriteBatch spriteBatch, Vector2 pan, float zoom)
    {
        var rect = new Rectangle(0, Type * 16, 16, 16);
        var orig = Vector2.One * 8f;
        var tex = AssetReferences.UI.InterfaceParticle.Asset.Value;
        spriteBatch.Draw(tex, (Position + pan) * zoom + Main.ScreenSize.ToVector2()/2f, rect, Color.White with { A = 0 } * Alpha, Rotation, orig, Scale * zoom, SpriteEffects.None, 0f);
    }
}
