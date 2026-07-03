using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core;

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
        gd.DrawUserPrimitives(PrimitiveType.TriangleList, [..vertices], 0, vertices.Count / 3);

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

            points.Insert(0, points[0] - (points[1] - points[0]));
            points.Add(points[^1] + (points[^1] - points[^2]));

            float width = fplr.Body.particles[0].Radius * 2f; //this is just for the main body, extra padding will be added for more space
            float targetWidth = (float)PlayerRenderTarget.Target.Width;
            float widthRatio = targetWidth / width;

            float innerWidth = 0.15f; //how much space will be the actual main body
            float startX = 0.5f - (innerWidth / 2f);
            float endX = 0.5f + (innerWidth / 2f);

            float sideWidth = PlayerRenderTarget.frameWidth / 2;

            float startY = 0.4f;
            float endY = 0.6f;

            float heightIncrement = 1 / (float)(points.Count - 1);

            float xIncrement = PlayerRenderTarget.frameWidth / targetWidth;
            float xLeftBorder = xIncrement * PlayerRenderTarget.GetIndexFromPlayer(player.whoAmI);
            float x1 = xIncrement * PlayerRenderTarget.GetIndexFromPlayer(player.whoAmI) + startX * xIncrement;
            float x2 = x1 + (endX - startX) * xIncrement;
            float xRightBorder = xLeftBorder + xIncrement;

            if (fplr.Body.dead)
            {
                float deadHeightIncrement = 1f / (float)(particles.Count - 1);
                for (int i = 0; i < particles.Count; i++)
                {
                    var p = particles[i];
                    float rot = p.rotation;
                    var pos = p.Position;
                    Vector2 bottomLeft = pos + new Vector2(-sideWidth, width).RotatedBy(rot);
                    Vector2 topLeft = pos + new Vector2(-sideWidth, -width).RotatedBy(rot);
                    Vector2 topRight = pos + new Vector2(sideWidth, -width).RotatedBy(rot);
                    Vector2 bottomRight = pos + new Vector2(sideWidth, width).RotatedBy(rot);

                    float y1 = MathHelper.Lerp(startY, endY, (i + 1) * deadHeightIncrement);
                    float y2 = MathHelper.Lerp(startY, endY, (i) * deadHeightIncrement);

                    vertices.Add(new(new(bottomLeft.X, bottomLeft.Y, 0f), Color.White, new(xLeftBorder, y2)));
                    vertices.Add(new(new(topLeft.X, topLeft.Y, 0f), Color.White, new(xLeftBorder, y1)));
                    vertices.Add(new(new(bottomRight.X, bottomRight.Y, 0f), Color.White, new(xRightBorder, y2)));

                    vertices.Add(new(new(bottomRight.X, bottomRight.Y, 0f), Color.White, new(xRightBorder, y2)));
                    vertices.Add(new(new(topLeft.X, topLeft.Y, 0f), Color.White, new(xLeftBorder, y1)));
                    vertices.Add(new(new(topRight.X, topRight.Y, 0f), Color.White, new(xRightBorder, y1)));
                }
                continue;
            }

            Vector2 firstNormal = GetNormal(points, 0);
            Vector2 prev1 = points[0] + firstNormal * width;
            Vector2 prev2 = points[0] - firstNormal * width;
            Vector2 prevLeftExtension = prev1 + firstNormal * (width + sideWidth);
            Vector2 prevRightExtension = prev2 - firstNormal * (width + sideWidth);

            for (int i = 1; i < points.Count; i++)
            {
                var a = points[i];

                Vector2 normal = GetNormal(points, i);
                Vector2 a1 = a + normal * width;
                Vector2 a2 = a - normal * width;

                Vector2 leftExtensionA = a1 + normal * (sideWidth - width);
                Vector2 rightExtensionA = a2 - normal * (sideWidth - width);

                Color cP1 = FromVec3(Lighting.GetSubLight(prev1));
                Color cP2 = FromVec3(Lighting.GetSubLight(prev2));
                Color cA1 = FromVec3(Lighting.GetSubLight(a1));
                Color cA2 = FromVec3(Lighting.GetSubLight(a2));

                Color cPL = FromVec3(Lighting.GetSubLight(prevLeftExtension));
                Color cPR = FromVec3(Lighting.GetSubLight(prevRightExtension));
                Color cLA = FromVec3(Lighting.GetSubLight(leftExtensionA));
                Color cRA = FromVec3(Lighting.GetSubLight(rightExtensionA));

                float y1 = MathHelper.Lerp(startY, endY, i * heightIncrement);
                float y2 = MathHelper.Lerp(startY, endY, (i - 1) * heightIncrement);

                //main body, isolated to prevent too much wacky folding
                vertices.Add(new(new(a1.X, a1.Y, 0f), cA1, new(x1, y1)));
                vertices.Add(new(new(prev1.X, prev1.Y, 0f), cP1, new(x1, y2)));
                vertices.Add(new(new(prev2.X, prev2.Y, 0f), cP2, new(x2, y2)));

                vertices.Add(new(new(a1.X, a1.Y, 0f), cA1, new(x1, y1)));
                vertices.Add(new(new(prev2.X, prev2.Y, 0f), cP2, new(x2, y2)));
                vertices.Add(new(new(a2.X, a2.Y, 0f), cA2, new(x2, y1)));

                //left extension
                vertices.Add(new(new(leftExtensionA.X, leftExtensionA.Y, 0f), cLA, new(xLeftBorder, y1)));
                vertices.Add(new(new(prevLeftExtension.X, prevLeftExtension.Y, 0f), cPL, new(xLeftBorder, y2)));
                vertices.Add(new(new(a1.X, a1.Y, 0f), cA1, new(x1, y1)));

                vertices.Add(new(new(a1.X, a1.Y, 0f), cA1, new(x1, y1)));
                vertices.Add(new(new(prevLeftExtension.X, prevLeftExtension.Y, 0f), cPL, new(xLeftBorder, y2)));
                vertices.Add(new(new(prev1.X, prev1.Y, 0f), cP1, new(x1, y2)));

                //right extension
                vertices.Add(new(new(a2.X, a2.Y, 0f), cA2, new(x2, y1)));
                vertices.Add(new(new(prev2.X, prev2.Y, 0f), cP2, new(x2, y2)));
                vertices.Add(new(new(rightExtensionA.X, rightExtensionA.Y, 0f), cRA, new(xRightBorder, y1)));

                vertices.Add(new(new(rightExtensionA.X, rightExtensionA.Y, 0f), cRA, new(xRightBorder, y1)));
                vertices.Add(new(new(prev2.X, prev2.Y, 0f), cP2, new(x2, y2)));
                vertices.Add(new(new(prevRightExtension.X, prevRightExtension.Y, 0f), cPR, new(xRightBorder, y2)));

                (prev1, prev2) = (a1, a2);
                (prevLeftExtension, prevRightExtension) = (leftExtensionA, rightExtensionA);
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
