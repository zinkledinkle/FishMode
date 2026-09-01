using FishMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace FishMode.UI;

public record struct InterfaceParticle(int Type, Vector2 Position, Vector2 Velocity, float Scale, float Alpha, float Spin)
{
    public Vector2 Position { get; set; } = Position;
    public Vector2 Velocity { get; set; } = Velocity;
    public float Rotation { get; set; }
    private float rotVel = Spin;
    public float Scale { get; set; } = Scale;
    public float Alpha { get; set; } = Alpha;
    public int Type { get; } = Type;
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
public record struct PointCountParticle(int Count)
{
    private Vector2 position;
    private Vector2 velocity = new(Main.rand.NextFloatDirection() * 0.2f, -3f);
    private float rotation;
    private float rotVel = Main.rand.NextFloatDirection() * 0.1f;
    public float alpha = 1f;
    public void Update(float dt)
    {
        position += velocity;
        velocity *= new Vector2(0.99f, 1f);
        velocity += Vector2.UnitY * 0.1f;
        alpha -= 0.02f * dt;
        rotation += rotVel;
        rotVel -= rotVel * 0.02f * dt;
    }
    public readonly void Draw(Vector2 originalPosition, SpriteBatch spriteBatch)
    {
        var text = Count.ToString();
        var font = FontAssets.DeathText.Value;
        var origin = font.MeasureString(text) / 2f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, originalPosition + position, Color.White * alpha * 0.5f, Color.Blue with { A = 0 } * 0.25f * alpha, 0f, origin, Vector2.One, -1, 4f);
    }
}