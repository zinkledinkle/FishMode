using FishMode.Common;
using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core.Drawing;

public class PlayerFishDrawer : ILoadable
{
    private static BasicEffect effect;
    public void Load(Mod mod)
    {
        if (Main.dedServ) return;
        Main.QueueMainThreadAction(() =>
        {
            effect = new(Main.graphics.GraphicsDevice)
            {
                VertexColorEnabled = true,
                TextureEnabled = true
            };
        });
        On_Main.DrawPlayers_AfterProjectiles += Draw;
    }

    private void Draw(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        orig(self);
        BuildPlayerMeshes();
        if (vertices.Count == 0) return;

        effect.Projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1f, 1f);
        effect.World = Matrix.CreateTranslation(new Vector3(-Main.screenPosition, 0f));
        effect.View = Main.GameViewMatrix.TransformationMatrix;

        var gd = Main.graphics.GraphicsDevice;

        effect.Texture = PlayerRenderTarget.Target;
        effect.TextureEnabled = true;
        effect.CurrentTechnique.Passes[0].Apply();
        gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, [..vertices], 0, vertices.Count - 2);

        if (!ModContent.GetInstance<FishModeConfig>().DebugDraw) return;

        Main.spriteBatch.Begin();
        Main.spriteBatch.transformMatrix = Main.GameViewMatrix.TransformationMatrix;
        foreach (var player in Main.ActivePlayers)
        {
            var fplr = player.GetModPlayer<FishPlayer>();
            foreach (var particle in fplr.Body.particles)
            {
                foreach (var constraint in particle.Constraints)
                {
                    if (constraint is not DistanceConstraint dc) continue;
                    var px = TextureAssets.MagicPixel.Value;
                    var width = 2f;
                    var length = dc.ParticleB.Position.Distance(particle.Position);

                    var scale = new Vector2(width / px.Width, length / px.Height);
                    var og = px.Size() / 2f;
                    var rot = dc.ParticleB.Position.DirectionTo(particle.Position).ToRotation() + MathHelper.PiOver2;
                    Main.spriteBatch.Draw(px, (particle.Position + dc.ParticleB.Position) / 2f - Main.screenPosition, null, Color.White, rot, og, scale, SpriteEffects.None, 0f);
                }

                var rect = new Rectangle((int)(particle.Position.X - particle.Radius - Main.screenPosition.X), (int)(particle.Position.Y - particle.Radius - Main.screenPosition.Y), (int)(particle.Radius * 2), (int)(particle.Radius * 2));
                Main.spriteBatch.Draw(TextureAssets.Extra[ExtrasID.MoonLordEye].Value, rect, Color.White * 1f);
            }
        }
        Main.spriteBatch.End();
    }

    public void Unload() { }
    private static List<VertexPositionColorTexture> vertices = [];
    //this sucks
    private static Color FromVec3(Vector3 vector3) => new(vector3.X, vector3.Y, vector3.Z, 1f);
    private static void BuildPlayerMeshes()
    {
        vertices = [];
        foreach (var player in Main.ActivePlayers)
        {
            var fplr = player.GetModPlayer<FishPlayer>();
            var particles = fplr.Body.particles;
            var points = particles.Select(p => p.Position).ToList();
            var source = PlayerRenderTarget.GetPlayerSource(player.whoAmI);

            points.Insert(0, points[0] - (points[1] - points[0]));
            points.Insert(0, points[0] - (points[1] - points[0]));
            points.Add(points[^1] + (points[^1] - points[^2]));
            points.Add(points[^1] + (points[^1] - points[^2]));

            float width = source.Width;
            float height = source.Height;
            float startX = source.X / PlayerRenderTarget.Target.Width;
            float endX = (source.X + width) / PlayerRenderTarget.Target.Width;
            float startY = 0f;
            float endY = 1f;

            width *= 0.5f;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var normal = GetNormal(points, i);
                var point = points[i];
                point -= normal.RotatedBy(MathHelper.PiOver2) * 0.5f;

                var left = point + normal * width;
                var right = point - normal * width;
                var colorLeft = FromVec3(Lighting.GetSubLight(left));
                var colorRight = FromVec3(Lighting.GetSubLight(right));

                var texCoordY = i / (float)(points.Count - 1);
                texCoordY = MathHelper.Lerp(startY, endY, texCoordY);

                vertices.Add(new(new(left, 0f), colorLeft, new(startX, texCoordY)));
                vertices.Add(new(new(right, 0f), colorRight, new(endX, texCoordY)));

                var nextNormal = GetNormal(points, i + 1);
                var nextPoint = points[i + 1];
                    
                var nextLeft = nextPoint + nextNormal * width;
                var nextRight = nextPoint - nextNormal * width;
                var nextColorLeft = FromVec3(Lighting.GetSubLight(nextLeft));
                var nextColorRight = FromVec3(Lighting.GetSubLight(nextRight));

                var nextTexCoordY = (i + 1) / (float)(points.Count - 1);
                nextTexCoordY = MathHelper.Lerp(startY, endY, nextTexCoordY);

                vertices.Add(new(new(nextLeft, 0f), nextColorLeft, new(startX, nextTexCoordY)));
                vertices.Add(new(new(nextRight, 0f), nextColorRight, new(endX, nextTexCoordY)));
            }
        }
    }
    private static Vector2 GetNormal(List<Vector2> points, int index)
    {
        if (index == 0)
        {
            Vector2 d = (points[1] - points[0]).SafeNormalize(Vector2.Zero);
            return new(-d.Y, d.X);
        }
        else if (index == points.Count - 1)
        {
            Vector2 d = (points[index] - points[index - 1]).SafeNormalize(Vector2.Zero);
            return new(-d.Y, d.X);
        } else
        {
            Vector2 delta = (points[index + 1] - points[index - 1]).SafeNormalize(Vector2.Zero);
            return new(-delta.Y, delta.X);
        }
    }
}
