using FishMode.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace FishMode.Core.Drawing;

public class BubbleDrawer : ModSystem
{
    private static Effect BubbleShader => Assets.Effects.Bubbles.Asset.Value;
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    public struct BubbleData(Vector2 pos, float radius)
    {
        [FieldOffset(0)]
        public Vector2 Position = pos;
        [FieldOffset(8)]
        public float Radius = radius;
    }
    private static readonly BubbleData[] bubbles = new BubbleData[MaxBubbles];
    private const int MaxBubbles = 128;
    private static int bubbleCount = 0;
    public static void QueueBubble(Vector2 position, float radius)
    {
        if (bubbles.Any(b => b.Position == position && b.Radius == radius)) return;
        if (bubbleCount < MaxBubbles)
            bubbles[bubbleCount++] = new BubbleData(position, radius);
    }
    private static unsafe void SupplyShaderData()
    {
        BubbleShader.Parameters["uBubbleCount"].SetValue(bubbleCount);
        BubbleShader.Parameters["uResolution"].SetValue(Main.ScreenSize.ToVector2());
        BubbleShader.Parameters["uScreenPos"].SetValue(Main.screenPosition);
        BubbleShader.Parameters["uZoom"].SetValue(Main.GameViewMatrix.Zoom);
        BubbleShader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        BubbleShader.Parameters["uBubbles"].SetValue(bubbles.Select(b => new Vector3(b.Position.X, b.Position.Y, b.Radius)).ToArray());
        Main.graphics.GraphicsDevice.Textures[1] = Assets.Textures.Noise.Perlin.Asset.Value;

        return;
        nint destination = BubbleShader.Parameters["uBubbles"].values;
        if (destination == 0) return;
        var byteCount = (uint)(bubbleCount * sizeof(BubbleData));
        fixed(BubbleData* source = bubbles)
        {
            Unsafe.CopyBlockUnaligned((byte*)destination, source, byteCount);
            //Buffer.MemoryCopy(source, (void*)destination, byteCount, byteCount);
        }
    }
    public override bool RequiresScreenTarget() => true;
    public override void Load()
    {
        On_Main.DrawProjectiles += static (orig, self) =>
        { 
            orig(self);
            if (bubbleCount > 0)
                Array.Clear(bubbles, 0, MaxBubbles);
            bubbleCount = 0;

            foreach(var p in Main.ActiveProjectiles)
            {
                if (p.type == SmallBubbleProjectile.ID)
                    QueueBubble(p.Center, SmallBubbleProjectile.Radius * p.scale * p.localAI[0]);
            }

            if (bubbleCount == 0)
                return;
            if (Main.finalScreenTarget == null) return;

            var gd = Main.graphics.GraphicsDevice;
            var prev = gd.GetRenderTargets();
            foreach (var target in prev)
                if (target.RenderTarget is RenderTarget2D rt) rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            Main.screenTargetSwap.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            gd.SetRenderTarget(Main.screenTargetSwap);
            gd.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, BubbleShader);
            SupplyShaderData();
            BubbleShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(Main.finalScreenTarget, Vector2.Zero, Color.White);

            Main.spriteBatch.End();

            gd.SetRenderTargets(prev);
            gd.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null);
            Main.spriteBatch.Draw(Main.screenTargetSwap, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        };
    }
}
