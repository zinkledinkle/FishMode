using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria.GameContent.Liquid;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace FishMode.Common;

public class ClearLiquids : ILoadable
{
    const float waterLightDecay = 0.8f;
    private static readonly Vector3 waterLightColorDecay = new(0.99f, 0.998f, 1f);
    private static readonly Vector3 honeyLightColorDecay = new(0.998f, 0.999f, 0.99f);
    const float waterOpacity = 0.1f;
    const float honeyOpacity = 0.7f;
    const float lavaOpacity = 0.7f;
    const float shimmerOpacity = 0.15f;
    const float backGroundOpacity = 0.25f;
    public void Load(Mod mod)
    {
        IL_TileLightScanner.ApplySurfaceLight += static (il) =>
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchLdcR4(0.175f));
            //c.Next.Operand = waterLightDecay;
            c.Remove();
            c.EmitDelegate(() => waterLightDecay);
        };
        LiquidRenderer.DEFAULT_OPACITY = [waterOpacity, lavaOpacity, honeyOpacity, shimmerOpacity];
        On_LiquidRenderer.DrawNormalLiquids += static (orig, self, sb, off, style, alpha, bg, waterOnly) =>
        {
            if (bg) alpha *= backGroundOpacity;
            orig(self, sb, off, style, alpha, bg, waterOnly);
        };
        var getLightDecayThroughWaterMethod = typeof(LightMap).GetProperty(nameof(LightMap.LightDecayThroughWater)).GetGetMethod();
        MonoModHooks.Add(getLightDecayThroughWaterMethod, static (orig_get_LightDecayThroughWater orig, LightMap self) =>
        {
            return waterLightColorDecay;
        });
        var getLightDecayThroughHoneyMethod = typeof(LightMap).GetProperty(nameof(LightMap.LightDecayThroughHoney)).GetGetMethod();
        MonoModHooks.Add(getLightDecayThroughHoneyMethod, static (orig_get_LightDecayThroughHoney orig, LightMap self) =>
        {
            return honeyLightColorDecay;
        });
    }
    private delegate Vector3 orig_get_LightDecayThroughWater(LightMap self);
    private delegate Vector3 orig_get_LightDecayThroughHoney(LightMap self);
    public void Unload() { }
}
