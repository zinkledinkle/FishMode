using FishMode.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FishMode.UI;


public class KrillPointBarSystem : ModSystem
{
    private UserInterface uInterface;
    private KrillPointBar uiState;
    private GameTime _gameTime;
    public override void Load()
    {
        if (Main.dedServ) return;
        uInterface = new();
        uiState = new();
        uInterface.SetState(uiState);
    }
    public override void UpdateUI(GameTime gameTime)
    {
        _gameTime = gameTime;
        uInterface.Update(gameTime);
        base.UpdateUI(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(l => l.Name == "Vanilla: Resource Bars");
        if (index == -1) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("KrillPointBarSystem", () =>
        {
            uInterface.Draw(Main.spriteBatch, _gameTime);
            return true;
        }, InterfaceScaleType.UI));
    }
    public void GetPoints(float amount, Vector2 position) => uiState.SpawnParticles(amount, position);
}
public class KrillPointBar : UIState
{
    private GameTime GameTime { get; set; }
    private static UIPanel meter;
    private static float glow = 0f;
    private static float meterLevel = 0f;
    private static float krillPointsLerped = 0f;
    private static float enter = 0f;
    private static SoundStyle Plink = Assets.Sounds.UI.KrillParticle.Asset with { Type = SoundType.Sound, Volume = 0.4f, MaxInstances = 3, PitchVariance = 1.5f };
    private static SoundStyle Spawn = Assets.Sounds.UI.SpawnKrillParticle.Asset with { Type = SoundType.Sound, Volume = 0.45f, MaxInstances = 1, PitchVariance = 0.2f };
    private static SoundStyle NewPoint = Assets.Sounds.UI.GetKrillPoint.Asset with { Type = SoundType.Sound, Volume = 0.6f, MaxInstances = 1, PitchVariance = 0f };
    public override void OnInitialize()
    {
        meter = new UIPanel();
        meter.Width.Set(64, 0f);
        meter.Height.Set(256, 0f);
        meter.HAlign = 0.95f;
        meter.VAlign = 0.5f; 
        meter.BackgroundColor = meter.BorderColor = Color.Transparent;
        Append(meter);
    }
    private static KrillTreePlayer KTP => Main.LocalPlayer.GetModPlayer<KrillTreePlayer>();
    public void SpawnParticles(float amount, Vector2 position)
    {
        SoundEngine.PlaySound(Spawn);
        var values = SplitValue(amount * 100);
        foreach(var value in values)
            particles.Add(new(position, Main.rand.NextVector2Circular(10f, 10f), value));
    }
    public static List<float> SplitValue(float amount)
    {
        float[] stages = [100f, 20f, 5f, 1f];
        var pieces = new List<float>();
        var remainder = amount;
        for (int i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            int count = (int)(remainder / stage + float.Epsilon);
            for (int j = 0; j < count; j++) pieces.Add(stage);
            remainder -= count * stage;
        }

        if (remainder > 0f) pieces.Add(remainder);
        return pieces;
    }
    public override void Update(GameTime gameTime)
    {
        if (Main.gamePaused) return;
        glow *= 54f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        GameTime = gameTime;
        enter = MathHelper.Clamp(enter + (float)gameTime.ElapsedGameTime.TotalSeconds * (particles.Count > 0 ? 1 : -1), 0, 1);
        for (int i = particles.Count - 1; i  >= 0; i--)
        {
            var particle = particles[i];
            if (!particle.Update((float)gameTime.ElapsedGameTime.TotalSeconds))
            {
                particles.RemoveAt(i);
                glow = 1f;
                var prev = KTP.KrillPoints;
                KTP.KrillPoints += particle.Value / 100f;
                if ((int)KTP.KrillPoints > (int)prev)
                {
                    SoundEngine.PlaySound(NewPoint);
                    var request = new AdvancedPopupRequest()
                    {
                        Text = Language.GetOrRegister(FishMode.Instance.GetLocalizationKey("UI.KrillPointPopup"), () => "+1 Krill Point!").Value,
                        Color = Color.Cyan,
                        DurationInFrames = 120,
                        Velocity = new Vector2(0, -4f)
                    };
                    PopupText.NewText(request, Main.LocalPlayer.Center);
                }
                SoundEngine.PlaySound(Plink);
            }
            else particles[i] = particle;
        }
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (GameTime == null) return;
        float dt = (float)GameTime.ElapsedGameTime.TotalSeconds;

        var rect = meter.GetDimensions().ToRectangle();
        int shift = (int)((1 - Math.Pow(enter, 0.4f)) * 200);
        rect.X += shift;
        int xPadding = 16;
        int yPadding = 16;

        var bar = Assets.Textures.UI.KrillMeter.Asset.Value;
        var pixel = TextureAssets.MagicPixel.Value;
        var perlin = Assets.Textures.Noise.Perlin.Asset.Value;
        var vein = Assets.Textures.Noise.Vein.Asset.Value;
        var shader = Assets.Effects.MeterShader.Asset.Value;

        krillPointsLerped = MathHelper.Lerp(krillPointsLerped, KTP.KrillPoints, dt * 5f);
        meterLevel = krillPointsLerped % 1f;

        spriteBatch.Draw(bar, rect, null, Color.White);

        int availablePoints = (int)KTP.KrillPoints;
        var textPos = rect.Bottom() + new Vector2(0, 50);
        var text = availablePoints.ToString();
        var font = FontAssets.DeathText.Value;
        var origin = font.MeasureString(text) / 2f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, Color.White, Color.Blue with { A = 0 } * 0.5f, 0f, origin, Vector2.One, -1, 4f);

        rect.X += xPadding;
        rect.Width -= xPadding * 2;
        rect.Y += yPadding;
        rect.Height -= yPadding * 2;

        var rasterizer = spriteBatch.rasterizerState;
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, rasterizer, Assets.Effects.MeterShader.Asset.Value, Main.UIScaleMatrix);

        spriteBatch.graphicsDevice.Textures[1] = vein;
        spriteBatch.graphicsDevice.Textures[2] = perlin;
        shader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        shader.Parameters["uSize"].SetValue(rect.Size());
        shader.Parameters["uProgress"].SetValue(meterLevel);
        shader.CurrentTechnique.Passes[0].Apply();

        var color = Color.Lerp(new Color(0f, 0.3f, 0.8f), Color.White, glow * 0.5f);
        spriteBatch.Draw(pixel, rect, null, color);

        spriteBatch.End();

        DrawParticles(spriteBatch);
    }
    private void DrawParticles(SpriteBatch spriteBatch)
    {
        if (particles.Count == 0)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return;
        }
        var glowShader = Assets.Effects.KrillBallGlow.Asset.Value;
        var particleShader = Assets.Effects.KrillBall.Asset.Value;
        var perlin = Assets.Textures.Noise.Perlin.Asset.Value;
        var grain = Assets.Textures.Noise.Grainy.Asset.Value;
        var circle = Assets.Textures.UI.Circle.Asset.Value;
        var glow = Assets.Textures.UI.Glow.Asset.Value;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, glowShader, Main.GameViewMatrix.TransformationMatrix);

        spriteBatch.graphicsDevice.Textures[1] = grain;
        spriteBatch.graphicsDevice.Textures[1] = perlin;
        glowShader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        glowShader.CurrentTechnique.Passes[0].Apply();
        Color color = new(0.4f, 0.7f, 1f);
        var glorig = glow.Size() / 2f;
        foreach (var particle in particles)
        {
            float scale = particle.Radius / (float)glow.Width;
            scale *= 1.65f;
            scale *= MathF.Pow(MathHelper.Clamp(particle.Time / 30f, 0, 1), 0.4f);
            var pos = particle.Position - Main.screenPosition;
            spriteBatch.Draw(glow, pos, null, color with { A = 0 }, 0f, glorig, scale * particle.Scale, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, particleShader, Main.GameViewMatrix.TransformationMatrix);

        spriteBatch.graphicsDevice.Textures[1] = perlin;
        particleShader.Parameters["uColor"].SetValue(color.ToVector3());
        particleShader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        particleShader.CurrentTechnique.Passes[0].Apply();
        var orig = circle.Size() / 2f;
        foreach (var particle in particles)
        {
            float scale = particle.Radius / (float)circle.Width;
            scale *= MathF.Pow(MathHelper.Clamp(particle.Time / 30f, 0, 1), 0.4f);
            var pos = particle.Position - Main.screenPosition;
            spriteBatch.Draw(circle, pos, null, new(particle.NoiseOffset % 1, 0f, 0f), 0f, orig, scale * particle.Scale, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }
    private readonly List<KrillParticle> particles = new(128);
    private record struct KrillParticle(Vector2 Position, Vector2 Velocity, float Value, float Time = 0)
    {
        public readonly float Radius => Value switch
        {
            < 5f => 10f,
            < 20f => 15f,
            < 100f => 20f,
            _ => 30f
        };
        public float NoiseOffset { get; init; } = Main.rand.NextFloat(0.5f, 1f);
        public float Scale { get; init; } = Main.rand.NextFloat(0.9f, 1.1f);
        public bool Update(float time)
        {
            Position += Velocity;
            var transformedPosition = Vector2.Transform(Position, Main.GameViewMatrix.TransformationMatrix);
            var dimensions = meter.GetDimensions().ToRectangle();
            var targetPos = dimensions.Bottom() + Main.screenPosition;
            targetPos.Y -= 16;
            targetPos.Y -= meterLevel * 224;
            float timeToGo = 40f;
            if (Time < timeToGo)
            {
                Velocity *= 0.97f;
                Time = Math.Min(Time, 60);
            } else
            {
                Vector2 dir = transformedPosition.DirectionTo(targetPos);
                Vector2 targetVel = dir * ((Time - timeToGo) / 120f) * 40f;
                Velocity = Vector2.Lerp(Velocity, targetVel, 0.06f);
                if (Velocity.LengthSquared() > 30f * 30f) Velocity = Vector2.Normalize(Velocity) * 30f;
            }
            Time += time * 60;
            var relativeDimensions = dimensions;
            relativeDimensions.X += (int)Main.screenPosition.X;
            relativeDimensions.Y += (int)Main.screenPosition.Y;
            bool inside = (transformedPosition.X > relativeDimensions.Left && transformedPosition.X < relativeDimensions.Right && transformedPosition.Y > relativeDimensions.Top && transformedPosition.Y < relativeDimensions.Bottom);
            var dist = transformedPosition.DistanceSQ(relativeDimensions.ClosestContactPoint(transformedPosition));
            bool intersect = Velocity.LengthSquared() > dist;
            float padding = 400f;
            bool offScreen = transformedPosition.X < Main.screenPosition.X - padding || transformedPosition.X > Main.screenPosition.X + Main.screenWidth + padding || transformedPosition.Y < Main.screenPosition.Y - padding || transformedPosition.Y > Main.screenPosition.Y + Main.screenHeight + padding;
            return !inside && !intersect && !offScreen;
        }
    }
}