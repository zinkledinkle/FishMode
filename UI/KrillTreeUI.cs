using FishMode.Common;
using FishMode.Content.KrillTree;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace FishMode.UI;

public class KrillTreeUISystem : ModSystem
{
    private UserInterface uInterface;
    private KrillTreeUI uiState;
    private GameTime _gameTime;
    private bool on = false;
    private bool initialized = false;
    public override void Load()
    {
        if (Main.dedServ) return;
        uInterface = new();
        uiState = new();
        uInterface.SetState(uiState);
    }
    public void Toggle(bool? value = null)
    {
        value ??= !on;
        if (value.Value)
        {
            if (!initialized)
            {
                uiState.Initialize(); initialized = true;
            }
            uiState.Initialize();
            uiState.Activate();
            KrillTreeUI.RecheckUnlocks();
            uInterface.SetState(uiState);
        }
        else
        {
            uiState.Deactivate();
            uInterface.SetState(null);
        }
        on = value.Value;
    }
    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.mapFullscreen)
        {
            uiState.Deactivate();
            uInterface.SetState(null);
            return;
        }
        _gameTime = gameTime;
        uInterface.Update(gameTime);
        base.UpdateUI(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        var index = layers.FindIndex(l => l.Name == "Vanilla: Map / Minimap");
        if (index == -1) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("KrillTree", () =>
        {
            uInterface.Draw(Main.spriteBatch, _gameTime);
            return true;
        }, InterfaceScaleType.UI));
    }
}
public class KrillTreeUI : UIState
{
    private static Player Player => Main.LocalPlayer;
    private static KrillTreePlayer? ModPlayer => Player.TryGetModPlayer<KrillTreePlayer>(out var plr) ? plr : null;
    private static KrillTree? PlayerTree => ModPlayer?.KrillTree;
    private static SoundStyle UnlockKrill = new("FishMode/Assets/Sounds/UI/UnlockKrill", SoundType.Sound)
    {
        Volume = 0.6f,
        MaxInstances = 1,
        PitchVariance = 0.2f
    };
    private static SoundStyle Click = new("FishMode/Assets/Sounds/UI/Click", 4, SoundType.Sound)
    {
        Volume = 0.5f,
        MaxInstances = 1,
        PitchVariance = 0.15f
    };

    private static readonly List<KrillNode> nodes = [];
    internal static int HoveringNodeIndex = -1;
    internal static readonly Dictionary<int, Vector2> NodePositions = [];
    internal static List<InterfaceParticle> particles = [];
    public static float Zoom { get; private set; } = 1f;
    private static float targetZoom = 1f;
    public static Vector2 Pan { get; private set; } = Vector2.Zero;
    private Vector2 oldMouse;
    private static UIPanel panel;
    private static UIPanel selected;
    private static UIPanel meter;
    private static float meterLevel = 0f;
    private static float meterGlow = 0f;
    internal static float hover = 0f;
    internal static string hoverName;
    internal static string hoverTooltip;
    internal static int hoverLevel;
    private readonly List<float> equipScales = [1,1,1,1,1];
    public static void RecheckUnlocks()
    {
        foreach (var node in nodes)
        {
            node.unlocked = PlayerTree.Unlocked.Contains(node.ID);
            node.activated = PlayerTree.activated.Contains(node.ID);
            if (node.unlocked) node.bubbleTimer = 0f; else node.bubbleTimer = 1f;
        }
    }
    public override void OnInitialize()
    {
        particles.Clear();
        NodePositions.Clear();
        nodes.Clear();
        RemoveAllChildren();
        if (PlayerTree == null) return;

        meterLevel = MathHelper.Clamp(ModPlayer.KrillPoints, 0, 1);

        panel = new UIPanel();
        panel.LoadTextures();
        panel.Width.Set(0, 0.55f);
        panel.Height.Set(0, 0.7f);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        panel.OnLeftClick += LeftClick;
        panel.OnScrollWheel += ScrollWheel;

        Append(panel);

        selected = new UIPanel();
        selected.Width.Set(272 * 2, 0f);
        selected.Height.Set(64 * 2, 0f);
        selected.HAlign = 0.5f;
        selected.VAlign = 1f;
        selected.BackgroundColor = selected.BorderColor = Color.Transparent;
        panel.Append(selected);

        meter = new UIPanel();
        meter.Width.Set(64, 0f);
        meter.Height.Set(256, 0f);
        meter.HAlign = 1f;
        meter.VAlign = 0.5f;
        meter.BackgroundColor = selected.BorderColor = Color.Transparent;
        panel.Append(meter);

        foreach (var krill in KrillTree.Krills)
        {
            var node = new KrillNode(krill.Key, krill.Value.Position);
            NodePositions.Add(krill.Key, krill.Value.Position);

            if (PlayerTree.Unlocked.Contains(krill.Key))
                node.unlocked = true;
            if (PlayerTree.IsActivated(krill.Key))
                node.activated = true;
            if (node.unlocked) node.bubbleTimer = 0f;
            if (PlayerTree.CanUnlock(node.ID) && ModPlayer.KrillPoints >= 1f)
                node.canUnlock = true;

            nodes.Add(node);
        }
    }
    public override void OnDeactivate()
    {
        particles.Clear();
    }
    public override void Update(GameTime gameTime)
    {
        if (panel == null) return;

        var rect = panel.GetInnerDimensions().ToRectangle();
        if (Main.MouseScreen.Between(rect.TopLeft(), rect.BottomRight()))
        {
            Player.mouseInterface = true; 
            PlayerInput.LockVanillaMouseScroll("KrillTree");
        }

        if (Main.mouseLeft && HoveringNodeIndex == -1 && Player.mouseInterface)
        {
            Pan += (Main.MouseScreen - oldMouse) / Zoom;
            oldMouse = Main.MouseScreen;
        } else if (!Main.mouseLeft)
        {
            oldMouse = Main.MouseScreen;
        }

        float xRange = 300f;
        float yRange = 200f;
        Pan = Vector2.Clamp(Pan, new(-xRange, -yRange), new(xRange, yRange));
    }
    public static void ScrollWheel(UIScrollWheelEvent evt, UIElement _)
    {
        targetZoom += evt.ScrollWheelValue / 600f;
        targetZoom = MathF.Floor(targetZoom * 10) / 10;
        targetZoom = MathHelper.Clamp(targetZoom, 0.5f, 2f);
    }
    public static void LeftClick(UIMouseEvent evt, UIElement _)
    {
        if (HoveringNodeIndex == -1) return;
        var node = nodes[HoveringNodeIndex];
        if (node.unlocked)
        {
            node.activated = !node.activated;
            PlayerTree.Toggle(node.ID);
            foreach (var toDeactivate in nodes.Where(n => KrillTree.Krills[n.ID].Level == KrillTree.Krills[node.ID].Level && n.ID != node.ID))
                toDeactivate.activated = false;
            node.scale = 0.9f;
            SoundEngine.PlaySound(Click);
        }
        else if (PlayerTree.CanUnlock(node.ID) && ModPlayer.KrillPoints >= 1f)
        {
            ModPlayer.KrillPoints--;
            PlayerTree.Unlock(node.ID);
            node.unlocked = true;
            node.bubbleTimer = 1f;
            node.bubbleScale = 0.7f;
            node.canUnlock = false;
            SoundEngine.PlaySound(UnlockKrill);
            foreach (var n in nodes)
                n.canUnlock = PlayerTree.CanUnlock(n.ID) && ModPlayer.KrillPoints >= 1f;

            for (int i = 0; i < Main.rand.Next(15,25); i++)
            {
                var p = new InterfaceParticle(Main.rand.Next(4), node.Position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.NextFloat(0.4f, 1f), Main.rand.NextFloat(0.5f, 1f), Main.rand.NextFloat(-0.1f, 0.1f));
                particles.Add(p);
            }
        }
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        HoveringNodeIndex = -1;
        if (panel == null) return;
        if (panel._borderTexture == null) return;
        if (panel._backgroundTexture == null) return;
        panel?.DrawPanel(spriteBatch, panel._borderTexture.Value, panel.BorderColor);
        panel?.DrawPanel(spriteBatch, panel._backgroundTexture.Value, panel.BackgroundColor);

        Zoom = MathHelper.Lerp(Zoom, targetZoom, (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds * 10f);

        var rasterizer = new RasterizerState()
        {
            ScissorTestEnable = true
        };
        var scissorRect = panel.GetDimensions().ToRectangle();
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, Assets.Effects.UIOceanShader.Asset.Value, Main.UIScaleMatrix);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

        DrawOcean(spriteBatch);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.UIScaleMatrix);

        foreach (var node in nodes)
            node.DrawConnections(spriteBatch);
        foreach (var node in nodes)
            node.Draw(spriteBatch);

        hover += (HoveringNodeIndex != -1 ? 10f : -10f) * (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds;
        hover = MathHelper.Clamp(hover, 0, 1);

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            var particle = particles[i];
            particle.Update((float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds * 60f);
            particle.Draw(spriteBatch, Pan, Zoom);
            if (particle.Scale <= 0f || particle.Alpha <= 0f)
            {
                particles.RemoveAt(i);
            }
            else
            {
                particles[i] = particle;
            }
        }

        DrawSelected(spriteBatch);

        DrawMeter(spriteBatch);

        DrawTooltip(spriteBatch);
    }
    private void DrawSelected(SpriteBatch spriteBatch)
    {
        if (selected == null)
            return;
        var rect = selected.GetDimensions().ToRectangle();
        var selectedTexture = Assets.Textures.UI.Selected.Asset.Value;

        spriteBatch.Draw(selectedTexture, rect, null, Color.White);

        Vector2 firstSlot = new Vector2(31.5f, 31.5f) * 2 + rect.TopLeft();
        for (int i = 0; i < 5; i++)
        {
            var pos = firstSlot + Vector2.UnitX * i * 52 * 2;
            int krillID = PlayerTree.activated[i];
            if (krillID == -1) continue;

            float targetScale = Main.MouseScreen.DistanceSQ(pos) < 400f ? 1.2f : 1f;
            equipScales[i] = MathHelper.Lerp(equipScales[i], targetScale, (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds * 20f);

            var texture = KrillTree.Krills[krillID].TextureValue;

            float offsetForSine = krillID;
            float bob = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 2f;
            Vector2 krillOffset = new(0, bob);

            float rot = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 0.1f;
            var scale = equipScales[i] * 2;
            spriteBatch.Draw(texture, pos + krillOffset, null, Color.White, rot, texture.Size() / 2f, scale, SpriteEffects.None, 0f);
        }
    }
    private void DrawMeter(SpriteBatch spriteBatch)
    {
        var rect = meter.GetDimensions().ToRectangle();
        int xPadding = 16;
        int yPadding = 16;

        var bar = Assets.Textures.UI.KrillMeter.Asset.Value;
        var pixel = TextureAssets.MagicPixel.Value;
        var perlin = Assets.Textures.Noise.Perlin.Asset.Value;
        var vein = Assets.Textures.Noise.Vein.Asset.Value;
        var shader = Assets.Effects.MeterShader.Asset.Value;

        float dt = (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds;
        meterLevel = MathHelper.Lerp(meterLevel, MathHelper.Clamp(ModPlayer.KrillPoints, 0f, 1f), dt * 5f);
        meterGlow = MathHelper.Lerp(meterGlow, ModPlayer.KrillPoints >= 1 ? 1f : 0f, dt * 3f);

        if (meterGlow > 0.1f)
        {
            int count = 8;
            for (int i = 0; i < count; i++)
            {
                float time = (float)Main.timeForVisualEffects / 60f;
                var newRect = rect;
                float offset = (i / (float)count) * MathHelper.TwoPi;
                time += offset;
                float dist = 7f;
                int xOff = (int)(Math.Sin(time) * (int)(meterGlow * dist));
                int yOff = (int)(Math.Cos(time) * (int)(meterGlow * dist));
                newRect.X += xOff;
                newRect.Y += yOff;
                spriteBatch.Draw(bar, newRect, null, Color.LightBlue with { A = 0 } * meterGlow * (1 / (float)count) * 2);
            }
        }

        spriteBatch.Draw(bar, rect, null, Color.White);

        int availablePoints = (int)ModPlayer.KrillPoints;
        var textPos = rect.Bottom() + new Vector2(0, 50);
        var text = availablePoints.ToString();
        var font = FontAssets.DeathText.Value;
        var origin = font.MeasureString(text) / 2f;
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, text, textPos, Color.White, Color.Navy, 0f, origin, Vector2.One);

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

        spriteBatch.Draw(pixel, rect, null, new Color(0f, 0.3f, 0.8f));
    }
    private static void DrawTooltip(SpriteBatch spriteBatch)
    {
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.UIScaleMatrix);
        if (hover == 0f || string.IsNullOrEmpty(hoverName) || string.IsNullOrEmpty(hoverTooltip)) return;

        Color color = hoverLevel switch
        {
            1 => Color.White,
            2 => Color.LightGreen,
            3 => Color.Cyan,
            4 => Color.Orange,
            _ => Color.Red
        };

        var pos = Main.MouseScreen;
        pos.Y -= 5;
        pos.X += 40;
        float eased = MathF.Pow(hover, 0.5f);
        var font = FontAssets.DeathText.Value;
        var font2 = FontAssets.MouseText.Value;

        float titleScale = 0.6f;
        float tooltipScale = 1f;
        int textLength = (int)(font.MeasureString(hoverName).X * titleScale); //add space for stars
        int paddedLengthForStars = textLength + 150;
        textLength = Math.Max(textLength, 200);

        var leftEdge = new Rectangle(0, 0, 10, 20);
        var rightEdge = new Rectangle(12, 0, 10, 20);
        var middle = new Rectangle(10, 0, 2, 20);

        var rect = new Rectangle((int)pos.X + leftEdge.Width, (int)pos.Y, (int)((paddedLengthForStars - leftEdge.Width) * eased), leftEdge.Height);

        var bar = Assets.Textures.UI.TitleBar.Asset.Value;
        var gradient = Assets.Textures.UI.Gradient.Asset.Value;
        int gradientHeight = 150;

        var gradientRect = rect;
        gradientRect.Y += leftEdge.Height;
        gradientRect.X -= leftEdge.Width;
        gradientRect.Width += leftEdge.Width;
        gradientRect.Height = gradientHeight;
        spriteBatch.Draw(gradient, gradientRect, null, Color.White);

        spriteBatch.Draw(bar, pos, leftEdge, Color.White);
        spriteBatch.Draw(bar, rect, middle, Color.White);
        spriteBatch.Draw(bar, pos + Vector2.UnitX * (rect.Width + rightEdge.Width), rightEdge, Color.White);

        var textPos = pos + new Vector2(10, 25);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font, hoverName, textPos, Color.Black * eased, color * eased, 0f, Vector2.Zero, Vector2.One * titleScale);

        string tooltip = font.CreateWrappedText(hoverTooltip, textLength / tooltipScale * 2);
        var tooltipPos = textPos + new Vector2(0, 40);
        ChatManager.DrawColorCodedStringWithShadow(spriteBatch, font2, tooltip, tooltipPos, Color.White * eased, Color.Black * eased, 0f, Vector2.Zero, Vector2.One * tooltipScale);

        List<Vector2> starPositions = hoverLevel switch
        {
            1 => [Vector2.Zero],
            2 => [new(-20, 0), new(20, 0)],
            3 => [new(-20, -10), new(0, 10), new(20, -10)],
            4 => [new(-20, -5), new(20, -5), new(-20, 25), new(20, 25)],
            _ => [new(-40, -5), new(0, -5), new(40, -5), new(-20, 20), new(20, 20)],
        };
        var basePos = pos + new Vector2(paddedLengthForStars - 60, 40);
        var star = Assets.Textures.UI.Star.Asset.Value;
        float time = (float)Main.timeForVisualEffects / 60f;
        foreach(var offset in starPositions)
        {
            var starpos = basePos + offset;
            float rot = MathF.Sin(time * 3.2f + offset.Y + offset.X) * 0.1f;
            float bob = MathF.Sin(time * 2.6f + offset.Y + offset.X) * 2f;
            starpos.Y += bob;
            float alpha = Math.Max(0, eased * 3 - 2);
            spriteBatch.Draw(star, starpos, null, color * alpha, rot, star.Size() / 2f, 1f, SpriteEffects.None, 0f);
        }
    }
    private static void DrawOcean(SpriteBatch spriteBatch)
    {
        var rect = panel.GetInnerDimensions().ToRectangle();

        var pixel = TextureAssets.MagicPixel.Value;
        var perlin = Assets.Textures.Noise.Perlin.Asset.Value;
        var vein = Assets.Textures.Noise.Vein.Asset.Value;
        var shader = Assets.Effects.UIOceanShader.Asset.Value;

        var color = new Color(0.2f, 0.02f, 0.6f);

        spriteBatch.graphicsDevice.Textures[1] = vein;
        spriteBatch.graphicsDevice.Textures[2] = perlin;
        shader.Parameters["uTime"].SetValue((float)Main.timeForVisualEffects / 60f);
        shader.Parameters["uSize"].SetValue(rect.Size());
        shader.Parameters["uZoom"].SetValue(Zoom);
        shader.Parameters["uPosition"].SetValue(Pan / rect.Size());
        shader.CurrentTechnique.Passes[0].Apply();

        spriteBatch.Draw(pixel, rect, null, color);
    }
}

public class KrillNode(int id, Vector2 position)
{
    public int ID { get; } = id;
    public float scale = 1f;
    public bool activated = false;
    public bool unlocked = false;
    public bool canUnlock = false;
    public float bubbleTimer = 1f;
    public float bubbleScale = 1f;
    private float glowAlpha = 0f;
    public Vector2 Position { get; } = position;
    public void DrawConnections(SpriteBatch spriteBatch)
    {
        float zoom = KrillTreeUI.Zoom;
        Vector2 pan = KrillTreeUI.Pan;
        Vector2 pos = (Position + pan) * zoom;
        Vector2 center = Main.ScreenSize.ToVector2() / 2f;
        foreach (var unlockID in KrillTree.Krills[ID].Unlocks)
        {
            var line = Assets.Textures.UI.Line.Asset.Value;
            var arrow = Assets.Textures.UI.Line_arrow.Asset.Value;

            Vector2 otherPos = ((KrillTreeUI.NodePositions[unlockID] + pan) * zoom);
            float distance = otherPos.Distance(pos);
            Vector2 midPoint = (otherPos + pos) / 2f;
            float lineRot = (otherPos - pos).ToRotation() + MathHelper.PiOver2;

            if (unlocked)
            {
                float alpha = MathF.Sin((float)Main.timeForVisualEffects / MathHelper.TwoPi / 2f) * 0.25f + 0.75f;

                float distanceBetweenArrows = 20f * zoom;
                int numberOfArrows = Math.Max(1, (int)Math.Floor(distance / distanceBetweenArrows));
                float ticksToCycleArrows = 20f;
                float spacingNormalized = 1f / numberOfArrows;
                float speedPerTick = spacingNormalized / ticksToCycleArrows;
                float tick = (float)Main.timeForVisualEffects;
                float offset = (tick * speedPerTick) % 1f;
                for (int i = 0; i < numberOfArrows; i++)
                {
                    float p = (i * spacingNormalized + offset) % 1f;
                    Vector2 arrowPos = Vector2.Lerp(pos, otherPos, p);
                    spriteBatch.Draw(arrow, arrowPos + center, null, Color.White * alpha, lineRot, arrow.Size() / 2f, zoom, SpriteEffects.None, 0f);
                }
            }

            float dim = unlocked ? 1f : 0.5f;
            spriteBatch.Draw(line, midPoint + center, null, Color.White * dim, lineRot, line.Size() / 2f, new Vector2(zoom, distance), SpriteEffects.None, 0f);
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        var krill = KrillTree.Krills[ID];
        var texture = krill.TextureValue;
        var frame = Assets.Textures.UI.KrillFrame.Asset.Value;
        var highlight = Assets.Textures.UI.KrillHighlight.Asset.Value;
        var bubble = Assets.Textures.UI.bubl.Asset.Value;
        var glow = Assets.Textures.UI.Glow.Asset.Value;
        var shadow = Assets.Textures.UI.Shadow.Asset.Value;

        bool hovering = false;
        float zoom = KrillTreeUI.Zoom;
        Vector2 pan = KrillTreeUI.Pan;
        Vector2 pos = Position + pan;
        Vector2 center = Main.ScreenSize.ToVector2() / 2f;
        pos *= zoom;
        float distSq = Main.MouseScreen.DistanceSQ(pos +center);
        float radius = 20f * zoom;
        if (distSq < radius*radius)
        {
            KrillTreeUI.HoveringNodeIndex = ID;
            KrillTreeUI.hoverName = krill.DisplayName.Value;
            KrillTreeUI.hoverTooltip = krill.Tooltip.Value;
            KrillTreeUI.hoverLevel = krill.Level;
            hovering = true;
        }

        var dt = (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds;
        scale = MathHelper.Lerp(scale, hovering ? 1.2f : 1f, dt * 20f);
        bubbleScale = MathHelper.Lerp(bubbleScale, 1f, dt * 15f);

        var krillOrig = texture.Size() / 2f;
        var frameOrig = frame.Size() / 2f;
        var glowOrig = glow.Size() / 2f;
        var highlightOrig = highlight.Size() / 2f;
        var bubbleOrig = bubble.Size() / 2f;

        float offsetForSine = ID;

        float bob = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 2f;
        float bob2 = (float)Math.Sin(Main.timeForVisualEffects / 40f + 5f + offsetForSine) * 2f;
        Vector2 bubbleOffset = new(bob, bob2);
        Vector2 krillOffset = new(0, bob);

        float rot = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 0.1f;

        pos += center;

        glowAlpha = MathHelper.Lerp(glowAlpha, canUnlock ? 1f : 0f, dt * 2f);
        float actualGlowAlphaLol = glowAlpha * (MathF.Sin(dt * 10) * 0.25f + 0.75f);

        spriteBatch.Draw(shadow, pos, null, Color.Black * (1 - glowAlpha), 0f, glowOrig, 0.3f * zoom, SpriteEffects.None, 0f);
        if (canUnlock)
        {
            spriteBatch.blendState = BlendState.Additive;
            spriteBatch.Draw(glow, pos, null, Color.LightCyan * actualGlowAlphaLol, 0f, glowOrig, 0.3f * zoom, SpriteEffects.None, 0f);
            spriteBatch.blendState = BlendState.AlphaBlend;
        }
        spriteBatch.Draw(frame, pos, null, Color.White, 0f, frameOrig, (scale * 0.5f + 0.5f) * zoom, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, pos + krillOffset * zoom, null, Color.White, rot, krillOrig, scale * zoom, SpriteEffects.None, 0f);
        if (activated) spriteBatch.Draw(highlight, pos, null, Color.White, 0f, highlightOrig, (scale * 0.5f + 0.5f) * zoom, SpriteEffects.None, 0f);
        if (unlocked) bubbleTimer = Math.Max(bubbleTimer - dt * 60f / 30f, 0f);
        if (!unlocked || bubbleTimer > 0f)
        {
            float pow = MathF.Pow(bubbleTimer, 3f);
            spriteBatch.Draw(bubble, pos + bubbleOffset * zoom, null, Color.White with { A = 0 } * pow, -rot + (bubbleScale * 10 - 10), bubbleOrig, (1 + (1 - pow)) * scale * bubbleScale * zoom, SpriteEffects.None, 0f);
        }
    }
}