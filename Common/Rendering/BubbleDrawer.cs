using Daybreak.Hooks.V1;
using Daybreak.Rendering.Buffers.V1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace FishMode.Common.Rendering;

public class BubbleDrawer : ModSystem
{
    private static Effect BubbleShader => Assets.Effects.Bubbles.Asset.Value;
    private static Texture2D BubbleTexture => Assets.Textures.Bubble.Asset.Value;
    private static Texture2D NoiseTexture => Assets.Textures.Noise.Perlin.Asset.Value;
    public record struct Bubble(Vector2 Position, float Radius) { }
    private static readonly List<Bubble> bubbles = [];
    public static void DrawBubble(Vector2 position, float radius) => bubbles.Add(new(position, radius));
    private static float radius = 400f;
    public override void Load()
    {
        On_Main.DrawProjectiles += static (orig, self) =>
        {
            bubbles.Clear();
            orig(self);
        };
    }
    public override bool RequiresScreenTarget() => true;
    [ScreenFilter(EffectPriority.VeryHigh)]
    private static bool DrawBubbles(SpriteBatch sb, GraphicsDevice device, RenderTarget2D screen, RenderTarget2D screenSwap)
    {
        if (bubbles.Count == 0) return false;

        using var target = ScreenspaceTargetProvider.Shared.Create(device);

        device.SetRenderTarget(target.Target);
        device.Clear(Color.Transparent);

        var blendState = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.BlendFactor,
            ColorBlendFunction = BlendFunction.Max,
            AlphaSourceBlend = Blend.SourceAlpha,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add
        };
        sb.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        var origin = BubbleTexture.Size() / 2f;
        var textureRadius = BubbleTexture.Width * 0.5f;
        foreach (var bubble in bubbles)
        {
            var scale = bubble.Radius / textureRadius;
            Color color = new(1f, 1f, bubble.Radius / 256f, 1f);
            sb.Draw(BubbleTexture, bubble.Position - Main.screenPosition, null, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        sb.End();

        device.SetRenderTarget(screenSwap);
        device.Clear(Color.Transparent);

        sb.Begin();
        sb.Draw(screen, Vector2.Zero, Color.White);
        sb.End();

        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, BubbleShader);
        device.Textures[1] = screen;
        device.Textures[2] = NoiseTexture;
        BubbleShader.Parameters["uResolution"].SetValue(Main.ScreenSize.ToVector2());
        BubbleShader.Parameters["uScreenPos"].SetValue(Main.screenPosition);
        BubbleShader.Parameters["uZoom"].SetValue(Main.GameViewMatrix.Zoom);
        BubbleShader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        BubbleShader.CurrentTechnique.Passes[0].Apply();
        sb.Draw(target.Target, Vector2.Zero, Color.White);
        sb.End();

        return true;
    }
}   
