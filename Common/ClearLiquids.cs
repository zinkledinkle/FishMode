using MonoMod.Cil;
using System.Reflection;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

namespace FishMode.Common;

public class ClearLiquids : ILoadable
{
    public void Load(Mod mod)
    {
        const float waterLightDecay = 0.9f;
        const float liquidOpacityMultiplier = 0.3f;
        const float backgroundOpacityMultiplier = 2f;
        IL_TileLightScanner.ApplySurfaceLight += static (il) =>
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchLdcR4(0.175f));
            c.Next.Operand = waterLightDecay;
        };
        IL_LiquidRenderer.DrawNormalLiquids += static (il) =>
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchCall(typeof(Main).GetMethod("DrawTileInWater", BindingFlags.Public | BindingFlags.Static)));
            c.EmitLdloca(1);
            c.EmitDelegate((ref VertexColors verts) =>
            {
                verts.TopLeftColor *= liquidOpacityMultiplier;
                verts.TopRightColor *= liquidOpacityMultiplier;
                verts.BottomLeftColor *= liquidOpacityMultiplier;
                verts.BottomRightColor *= liquidOpacityMultiplier;
            });
        };
    }
    public void Unload() { }
}
