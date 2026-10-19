using Daybreak.Hooks.V1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Map;
using Terraria.ModLoader;

namespace FishMode.Common;

public class ImpactFrame
{
    private static int time = 0;
    private static int maxTime = 9;
    private static Vector2 position;
    private readonly static float[] angles = new float[8];
    private readonly static float[] widths = new float[8];
    private static Effect Shader => Assets.Effects.ImpactFrame.Asset.Value;
    [OnLoad]
    private static void Load()
    {
        On_Main.DoUpdateInWorld += static (orig, self, sw) =>
        {
            if (time <= 0) orig(self, sw);
            time--;
        };
    }

    public static void Initiate(Vector2 pos, Vector2 dir)
    {
        if (time > -30) return;
        maxTime = 12;
        position = pos;
        time = maxTime;
        for (int i = 0; i < Main.rand.Next(4,9); i++)
        {
            angles[i] = dir.ToRotation() + Main.rand.NextFloat(-0.8f, 0.8f);
            widths[i] = Main.rand.NextFloat(0.04f);
        }
    }
    [ScreenFilter(EffectPriority.Medium)]
    public static bool DrawImpactFrame(SpriteBatch sb, GraphicsDevice device, RenderTarget2D screen, RenderTarget2D screenSwap)
    {
        if (time <= 0) return false;
        device.SetRenderTarget(screenSwap);
        device.Clear(Color.Transparent);
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, Shader);
        Shader.Parameters["uScreenResolution"].SetValue(Main.ScreenSize.ToVector2());
        Shader.Parameters["uZoom"].SetValue(Main.GameViewMatrix.Zoom);
        Shader.Parameters["uScreenPosition"].SetValue(Main.screenPosition);
        Shader.Parameters["uPosition"].SetValue(position);
        bool doImpactLines = time >= maxTime / 4 && time < maxTime / 4 * 2;
        Shader.Parameters["uAngles"].SetValue(doImpactLines ? angles : new float[8]);
        Shader.Parameters["uWidths"].SetValue(doImpactLines ? widths : new float[8]);
        Shader.Parameters["uIntensity"].SetValue(1 - MathF.Pow(MathF.Sin((time / (float)maxTime) * MathHelper.TwoPi) * 0.5f + 0.5f, 5f));
        Shader.CurrentTechnique.Passes[0].Apply();
        sb.Draw(screen, Vector2.Zero, Color.White);
        sb.End();
        return true;
    }
}
