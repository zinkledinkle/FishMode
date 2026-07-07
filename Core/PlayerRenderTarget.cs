using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Graphics;
using Terraria.DataStructures;
using MonoMod.Cil;
using System.Reflection;
using System;
using FishMode.Common;

namespace FishMode.Core;

public class PlayerRenderTarget : ILoadable
{
    public static RenderTarget2D Target { get; private set; }
    public const int frameWidth = 200;
    public const int frameHeight = 300;
    private static bool targetInUse = false;

    private static PlayerDrawSet curDrawSet;
    public void Load(Mod mod)
    {
        if (Main.dedServ) return;

        Main.QueueMainThreadAction(() => Target = new(Main.instance.GraphicsDevice, frameWidth * 10, frameHeight)); //10 is probably more than enough. I'll be damned if someone plays this mod with over 10 people

        On_LegacyPlayerRenderer.DrawPlayer += DrawPlayer;
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItem += (_, ref _) => { };
        On_PlayerDrawLayers.DrawPlayer_12_Skin_Composite += (_, ref _) => { };
        On_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite += (_, ref _) => { };
        IL_LegacyPlayerRenderer.DrawPlayerInternal += (il) =>
        {
            var c = new ILCursor(il);
            var method = typeof(PlayerDrawLayers).GetMethod(nameof(PlayerDrawLayers.DrawPlayer_RenderAllLayers), BindingFlags.Public | BindingFlags.Static);
            if (method ==  null) return;
            c.GotoNext(i => i.MatchCall(method));
            c.Index++;
            c.EmitLdloc0();
            c.EmitDelegate((PlayerDrawSet drawSet) =>
            {
                curDrawSet = drawSet;
            }); //yoink
        };

        //wouldn't have known to do this color stuff without slr!thanks slr
        On_Lighting.GetColorClamped += (orig, x, y, oldColor) => targetInUse ? oldColor : orig(x, y, oldColor);
        On_Player.GetHairColor += (orig, self, useLighting) => orig(self, useLighting & !targetInUse);
    }

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

        var _screenPos = Main.screenPosition;
        var _center = drawPlayer.Center;
        var _mountedCenter = drawPlayer.MountedCenter;
        var _pos = drawPlayer.position;
        var _heldProj = drawPlayer.heldProj;
        var _itemPos = drawPlayer.itemLocation;
        var _immuneAlpha = drawPlayer.immuneAlpha;

        Vector2 off = new(GetIndexFromPlayer(drawPlayer.whoAmI) * frameWidth + frameWidth / 2f, frameHeight / 2f);
        Main.screenPosition = Vector2.Zero;
        var newPos = off - drawPlayer.Size / 2f;
        drawPlayer.heldProj = -1;
        drawPlayer.itemLocation = Vector2.Zero;
        drawPlayer.position = _pos;
        drawPlayer.Center = _center - _pos + newPos;
        drawPlayer.MountedCenter = _mountedCenter - _pos + off;

        drawPlayer.headPosition = Vector2.Zero;
        drawPlayer.legPosition = Vector2.Zero;
        drawPlayer.bodyPosition = Vector2.Zero;
        drawPlayer.headRotation = 0f;
        drawPlayer.legRotation = 0f;
        drawPlayer.bodyRotation = 0f;
        if (drawPlayer.dead) drawPlayer.immuneAlpha = 0;

        orig(self, camera, drawPlayer, newPos, 0f, rotationOrigin, shadow, scale); //don't bother with rotation #lol!

        Main.screenPosition = _screenPos;
        drawPlayer.heldProj = _heldProj;
        drawPlayer.itemLocation = _itemPos;
        drawPlayer.Center = _center;
        drawPlayer.MountedCenter = _mountedCenter;
        drawPlayer.immuneAlpha = _immuneAlpha;

        gd.SetRenderTargets(prev);

        targetInUse = false;

        if (_heldProj != -1)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.instance.DrawProjDirect(Main.projectile[_heldProj]);
        }
        if (drawPlayer.GetModPlayer<FishPlayer>().disable)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, matrix);
            orig(self, camera, drawPlayer, position, rotation, rotationOrigin, shadow, scale);
        } if (drawPlayer.itemAnimation > 0)
        {
            curDrawSet.ItemLocation = _itemPos;
            int count = curDrawSet.DrawDataCache.Count;
            PlayerDrawLayers.DrawPlayer_27_HeldItem(ref curDrawSet); //draw the item later
            if (curDrawSet.DrawDataCache.Count == count) return; //nothing got added
            var draw = curDrawSet.DrawDataCache[^1];
            draw.color = Lighting.GetColor((int)_itemPos.X / 16, (int)_itemPos.Y / 16);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            draw.Draw(Main.spriteBatch);
        }
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
