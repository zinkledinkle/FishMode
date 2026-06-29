using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Graphics;

namespace FishMode.Core;

public class PlayerRenderTarget : ILoadable
{
    public static RenderTarget2D Target { get; private set; }
    public static int frameWidth = 200;
    public static int frameHeight = 300;
    private bool targetInUse = false;
    public void Load(Mod mod)
    {
        if (Main.dedServ) return;

        Main.QueueMainThreadAction(() => Target = new(Main.instance.GraphicsDevice, frameWidth * 10, frameHeight)); //10 is probably more than enough. I'll be damned if someone plays this mod with over 10 people

        On_LegacyPlayerRenderer.DrawPlayer += DrawPlayer;
        On_Lighting.GetColorClamped += WhiteForPlayer;
        On_Player.GetHairColor += WhiteHair;
    }

    private Color WhiteHair(On_Player.orig_GetHairColor orig, Player self, bool useLighting)
    {
        return orig(self, useLighting & !targetInUse);
    }

    private Color WhiteForPlayer(On_Lighting.orig_GetColorClamped orig, int x, int y, Color oldColor)
    {
        if (!targetInUse)
            return orig(x, y, oldColor);
        else
            return oldColor;
    }

    private Vector2 _screenPos;
    private Vector2 _itemPos;

    private void DrawPlayer(On_LegacyPlayerRenderer.orig_DrawPlayer orig, LegacyPlayerRenderer self, Camera camera, Player drawPlayer, Vector2 position, float rotation, Vector2 rotationOrigin, float shadow, float scale)
    {
        if (Main.gameMenu)
        {
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, scale); 
            return;
        }
        var gd = Main.graphics.GraphicsDevice;
        RenderTargetBinding[] prev = gd.GetRenderTargets();
        foreach (var target in prev)
        {
            if (target.RenderTarget is not RenderTarget2D rt)
                continue;
            rt.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        gd.SetRenderTarget(Target);
        gd.Clear(Color.Transparent);

        var matrix = Main.spriteBatch.transformMatrix;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);

        targetInUse = true;

        _screenPos = Main.screenPosition;
        _itemPos = drawPlayer.itemLocation;

        Vector2 off = new(GetIndexFromPlayer(drawPlayer.whoAmI) * frameWidth + frameWidth / 2f, frameHeight / 2f);
        drawPlayer.itemLocation = _itemPos;
        Main.screenPosition = Vector2.Zero;

        orig(self, camera, drawPlayer, off - drawPlayer.Size / 2f, 0f, rotationOrigin, shadow, scale); //don't bother with rotation #lol!

        Main.screenPosition = _screenPos;
        drawPlayer.itemLocation = _itemPos;

        gd.SetRenderTargets(prev);

        if (drawPlayer.GetModPlayer<FishPlayer>().disable)
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, scale);

        targetInUse = false;
    }

    public void Unload()
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(Target.Dispose);
    }
    public static int GetIndexFromPlayer(int player)
    {
        int ACTUALindex = 0;
        for (int i = 0; i < player; i++)
        {
            if (!Main.player[i].active) ACTUALindex--;
            ACTUALindex++;
        }
        return ACTUALindex;
    }
    public static Rectangle GetPlayerSource(int player) => new(frameWidth * GetIndexFromPlayer(player), 0, frameWidth, frameHeight);
}
