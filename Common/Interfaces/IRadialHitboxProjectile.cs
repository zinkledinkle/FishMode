using Daybreak.Hooks.V1;
using Daybreak.Rendering.V1;
using DragonLens.Content.Tools.Visualization;
using DragonLens.Core.Loaders.UILoading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace FishMode.Common.Interfaces;

public interface IRadialHitboxProjectile
{
    public float Radius { get; set; }
}

public class RadialHitboxProjectileManager
{
    [OnLoad]
    public static void Load()
    {
        IL_Projectile.Damage_PVE_Inner += Patch;
        IL_Projectile.Damage_EVP += Patch;
        IL_Projectile.Damage_PVP += Patch;
        void Patch(ILContext il)
        {
            var c = new ILCursor(il);
            var collideMethod = typeof(Projectile).GetMethod(nameof(Projectile.Colliding), BindingFlags.Public | BindingFlags.Instance);
            if (collideMethod is null) return;
            while (c.TryGotoNext(MoveType.Before, i => i.MatchCall(collideMethod)))
            {
                var label = c.DefineLabel();
                c.EmitBr(label);
                c.Index++;
                c.MarkLabel(label);
                c.EmitDelegate((Projectile self, Rectangle projHitbox, Rectangle targetHitbox) =>
                {
                    if (self.ModProjectile is not IRadialHitboxProjectile radial) return self.Colliding(projHitbox, targetHitbox);
                    return CircleVAABBCollision(self.Center, radial.Radius, targetHitbox);
                });
            }
        }
    }
    private static bool CircleVAABBCollision(Vector2 circleCenter, float radius, Rectangle aabb)
    {
        var off = circleCenter - aabb.Center.ToVector2();
        if (Math.Abs(off.X) > aabb.Width / 2f + radius
        || Math.Abs(off.Y) > aabb.Height / 2f + radius) return false;
        if (Math.Abs(off.X) < aabb.Width / 2f || Math.Abs(off.Y) < aabb.Height / 2f) return true;
        var distSq = 
            (off.X - aabb.Width / 2f) * (off.X - aabb.Width / 2f) +
            (off.Y - aabb.Height / 2f) * (off.Y - aabb.Height / 2f);
        return distSq < radius * radius;
    }
}
[JITWhenModsEnabled("DragonLens")]
public class RadialHitboxDragonLensSupport
{
    private record struct RadialHitbox(Vector2 Center, float Radius);
    private static readonly List<RadialHitbox> radialHitboxes = [];
    [OnLoad]
    private static void Load()
    {
        if (!ModLoader.TryGetMod("DragonLens", out Mod dragonLens)) return;
        var hbSystem = typeof(HitboxSystem);
        if (hbSystem is null) return;
        var drawHitboxesMethod = hbSystem.GetMethod(nameof(HitboxSystem.DrawHitboxes), BindingFlags.NonPublic | BindingFlags.Instance);

        //stop radial hitbox projectiles from being added to the hitbox list, and add to a new list
        UILoader.GetUIState<HitboxWindow>().ProjectileOption.getBoxes = () =>
        {
            var list = new List<Rectangle>();
            radialHitboxes.Clear();
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.ModProjectile is IRadialHitboxProjectile radial)
                {
                    radialHitboxes.Add(new(proj.Center, radial.Radius));
                    continue;
                }
                Rectangle hitbox = proj.Hitbox;
                hitbox.Offset((-Main.screenPosition).ToPoint());
                list.Add(hitbox);
            }
            return list;
        };
        UILoader.GetUIState<HitboxWindow>().PlayerOption.getBoxes = () =>
        {
            var list = new List<Rectangle>();
            foreach(var player in Main.ActivePlayers)
            {
                if (player.dead) continue;
                list.AddRange(player.GetModPlayer<FishPlayer>().Body.particles.Select(p => new Rectangle(
                    (int)(p.Position.X - p.Radius - Main.screenPosition.X),
                    (int)(p.Position.Y - p.Radius - Main.screenPosition.Y),
                    (int)p.Radius,
                    (int)p.Radius)));
            }
            return list;
        };

        //draw after normal hitboxes
        MonoModHooks.Modify(drawHitboxesMethod, (il) =>
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdfld(typeof(HitboxWindow).GetField(nameof(HitboxWindow.ProjectileOption), BindingFlags.Public | BindingFlags.Instance)),
                i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.spriteBatch), BindingFlags.Public | BindingFlags.Static)),
                i => i.MatchCallvirt(typeof(HitboxOption).GetMethod(nameof(HitboxOption.DrawBoxes), BindingFlags.Public | BindingFlags.Instance))))
                return;

            var shader = Assets.Effects.RadialHitboxTexture.Asset.Value;
            var texture = dragonLens.Assets.Request<Texture2D>("Assets/Themes/BoxProviders/SimpleBoxes/Box");
            const float pxWidth = 12;

            c.EmitLdloc0();
            c.EmitLdfld(typeof(HitboxWindow).GetField(nameof(HitboxWindow.ProjectileOption), BindingFlags.Public | BindingFlags.Instance)); //for draw settings
            c.EmitDelegate((HitboxOption hitboxOption) =>
            {
                if (hitboxOption.boxState == HitboxOption.BoxType.none || radialHitboxes.Count == 0) return;

                var color = hitboxOption.BoxColor;
                var sample = FishModeConfig.Instance.DragonLensPixelateRadialHitboxes ? SamplerState.PointClamp : SamplerState.LinearClamp;
                var fill = hitboxOption.boxState == HitboxOption.BoxType.filled;

                var sb = Main.spriteBatch;
                var scope = sb.Scope();
                {
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, sample, DepthStencilState.Default, Main.Rasterizer, shader, Main.GameViewMatrix.TransformationMatrix);
                    shader.Parameters["uThickness"].SetValue(pxWidth);
                    shader.Parameters["uTexSampleX"].SetValue(0.5f); //center of the texture
                    shader.Parameters["uColor"].SetValue(color.ToVector3()); //we can use the actual color sampling as radii, since color is always global across hitboxes
                    shader.Parameters["uFill"].SetValue(fill); //we can use the actual color sampling as radii, since color is always global across hitboxes
                    shader.Parameters["uPixelate"].SetValue(FishModeConfig.Instance.DragonLensPixelateRadialHitboxes); //we can use the actual color sampling as radii, since color is always global across hitboxes 
                    shader.CurrentTechnique.Passes[0].Apply();

                    foreach (var radial in radialHitboxes)
                    {
                        var rect = new Rectangle(
                            (int)(radial.Center.X - radial.Radius - Main.screenPosition.X),
                            (int)(radial.Center.Y - radial.Radius - Main.screenPosition.Y),
                            (int)(radial.Radius * 2),
                            (int)(radial.Radius * 2));

                        //encode the radius in the color, up to 2^16
                        uint encoded = (uint)Math.Round(radial.Radius * (float)(ushort.MaxValue + 1));
                        byte b0 = (byte)(encoded >> 24);
                        byte b1 = (byte)(encoded >> 16);
                        byte b2 = (byte)(encoded >> 8);
                        byte b3 = (byte)encoded;
                        var colorData = new Color(b0, b1, b2, b3);
                        sb.Draw(texture.Value, rect, colorData);
                    }
                    sb.End();
                }
                scope.Dispose();
            });
        });
    }
}