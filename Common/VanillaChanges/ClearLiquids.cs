using Microsoft.Xna.Framework;
using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common.VanillaChanges;

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
            c.Next.Operand = waterLightDecay;
        };
        IL_WaterfallManager.DrawWaterfall_int_float += static (il) =>
        {
            var c = new ILCursor(il);
            var waterfallsArray = typeof(WaterfallManager).GetField("waterfalls", BindingFlags.NonPublic | BindingFlags.Instance);
            var waterfallData = typeof(WaterfallManager.WaterfallData);
            var waterfallType = waterfallData.GetField("type", BindingFlags.Public | BindingFlags.Instance);

            c.GotoNext(i => i.MatchLdarg0());
            c.EmitLdarg0();
            c.EmitLdfld(waterfallsArray);
            c.EmitLdloc(10);
            c.EmitLdarga(2);
            c.EmitDelegate((WaterfallManager.WaterfallData[] data, int i, ref float alpha) =>
            {
                alpha *= (data[i].type switch
                {
                    LiquidID.Water => waterOpacity,
                    LiquidID.Honey => honeyOpacity,
                    LiquidID.Lava => lavaOpacity,
                    LiquidID.Shimmer => shimmerOpacity,
                    _ => 1f,
                } + backGroundOpacity);
            });
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
