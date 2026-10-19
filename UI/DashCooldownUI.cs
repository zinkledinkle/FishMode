using FishMode.Common;
using FishMode.Common.VanillaChanges.VanillaAccessoryChanges;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace FishMode.UI;

public class DashCooldownUI : ModSystem
{
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(l => l.Name == "Vanilla: Resource Bars");
        if (index == -1) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("FishmodeDashCooldown", () =>
        {
            Draw(Main.spriteBatch);
            return true;
        }, InterfaceScaleType.Game));
    }
    private static void Draw(SpriteBatch sb)
    {
        var plr = Main.LocalPlayer;
        var dashPlayer = plr.GetModPlayer<ExtraJumpDashes>();
        if (!dashPlayer.AnyDashEquipped || dashPlayer.CurrentCooldown == int.MaxValue) return;
        var progress = dashPlayer.CurrentCooldown / (float)ExtraJumpDashes.dashRefreshTime;

        var body = plr.GetModPlayer<FishPlayer>().Body;
        var pos = Vector2.Zero;
        foreach (var p in body.particles)
            pos += p.Position;
        pos /= (float)body.particles.Count;
        pos += new Vector2(0, -60) - Main.screenPosition;
        int radius = 10;
        var rect = new Rectangle((int)pos.X - radius, (int)pos.Y - radius, radius * 2, radius * 2);
        var effect = Assets.Effects.RadialTimer.Asset.Value;
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

        effect.Parameters["uProgress"].SetValue(progress);
        effect.CurrentTechnique.Passes[0].Apply();
        sb.Draw(TextureAssets.MagicPixel.Value, rect, Color.White * 0.5f);

        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
}