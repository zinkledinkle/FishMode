using FishMode.Common;
using FishMode.Content.KrillTree;
using Humanizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Core.Utils;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.UI;

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
    private static SoundStyle UnlockKrill = new("FishMode/Assets/Sounds/UnlockKrill", SoundType.Sound)
    {
        Volume = 0.6f,
        MaxInstances = 1,
        PitchVariance = 0.2f
    };
    private static readonly List<KrillNode> nodes = [];
    internal static int HoveringNodeIndex = -1;
    internal static readonly Dictionary<int, Vector2> NodePositions = [];
    internal static List<InterfaceParticle> particles = [];
    public static float Zoom { get; private set; } = 1f;
    public static Vector2 Pan { get; private set; } = Vector2.Zero;
    private Vector2 oldMouse;
    private UIPanel panel;
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

        panel = new UIPanel();
        panel.Width.Set(0, 0.55f);
        panel.Height.Set(0, 0.4f);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        panel.OnLeftClick += LeftClick;
        panel.OnScrollWheel += ScrollWheel;

        Append(panel);

        foreach(var krill in KrillTree.Krills)
        {
            var node = new KrillNode(krill.Key, krill.Value.Position);
            NodePositions.Add(krill.Key, krill.Value.Position);

            if (PlayerTree.Unlocked.Contains(krill.Key))
                node.unlocked = true;
            if (PlayerTree.IsActivated(krill.Key))
                node.activated = true;
            if (node.unlocked) node.bubbleTimer = 0f;

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

        if (Main.mouseLeft && HoveringNodeIndex == -1)
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
        Zoom += evt.ScrollWheelValue / 600f;
        Zoom = MathF.Floor(Zoom * 10) / 10;
        Zoom = MathHelper.Clamp(Zoom, 0.5f, 2f);
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
        }
        else if (PlayerTree.CanUnlock(node.ID) && ModPlayer.KrillPoints >= 1f)
        {
            ModPlayer.KrillPoints--;
            PlayerTree.Unlock(node.ID);
            node.unlocked = true;
            node.bubbleTimer = 1f;
            node.bubbleScale = 0.7f;
            SoundEngine.PlaySound(UnlockKrill);

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
        base.Draw(spriteBatch);

        var rasterizer = new RasterizerState()
        {
            ScissorTestEnable = true
        };
        if (panel == null) return;
        var scissorRect = panel.GetInnerDimensions().ToRectangle();
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizer, null, Main.UIScaleMatrix);
        spriteBatch.GraphicsDevice.ScissorRectangle = scissorRect;

        foreach (var node in nodes)
            node.DrawConnections(spriteBatch);
        foreach (var node in nodes)
            node.Draw(spriteBatch);

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

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.Default, Main.Rasterizer, null, Main.UIScaleMatrix);
    }
}

public class KrillNode(int id, Vector2 position)
{
    public int ID { get; } = id;
    public float scale = 1f;
    public bool activated = false;
    public bool unlocked = false;
    public float bubbleTimer = 1f;
    public float bubbleScale = 1f;
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

            spriteBatch.Draw(line, midPoint + center, null, Color.White, lineRot, line.Size() / 2f, new Vector2(zoom, distance), SpriteEffects.None, 0f);
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        var texture = KrillTree.Krills[ID].TextureValue;
        var frame = Assets.Textures.UI.KrillFrame.Asset.Value;
        var highlight = Assets.Textures.UI.KrillHighlight.Asset.Value;
        var bubble = Assets.Textures.UI.bubl.Asset.Value;

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
            UICommon.TooltipMouseText(KrillTree.Krills[ID].DisplayName.Value);
            hovering = true;
        }

        var dt = (float)Main.gameTimeCache.ElapsedGameTime.TotalSeconds;
        scale = MathHelper.Lerp(scale, hovering ? 1.2f : 1f, dt * 20f);
        bubbleScale = MathHelper.Lerp(bubbleScale, 1f, dt * 20f);

        var krillOrig = texture.Size() / 2f;
        var frameOrig = frame.Size() / 2f;
        var highlightOrig = highlight.Size() / 2f;
        var bubbleOrig = bubble.Size() / 2f;

        float offsetForSine = ID;

        float bob = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 2f;
        float bob2 = (float)Math.Sin(Main.timeForVisualEffects / 40f + 5f + offsetForSine) * 2f;
        Vector2 bubbleOffset = new(bob, bob2);
        Vector2 krillOffset = new(0, bob);

        float rot = (float)Math.Sin(Main.timeForVisualEffects / 60f + offsetForSine) * 0.1f;

        pos += center;

        spriteBatch.Draw(frame, pos, null, Color.White, 0f, frameOrig, (scale * 0.5f + 0.5f) * zoom, SpriteEffects.None, 0f);
        spriteBatch.Draw(texture, pos + krillOffset * zoom, null, Color.White, rot, krillOrig, scale * zoom, SpriteEffects.None, 0f);
        if (activated) spriteBatch.Draw(highlight, pos, null, Color.White, 0f, highlightOrig, (scale * 0.5f + 0.5f) * zoom, SpriteEffects.None, 0f);
        if (unlocked) bubbleTimer = Math.Max(bubbleTimer - dt * 60f / 30f, 0f);
        if (!unlocked || bubbleTimer > 0f)
        {
            float pow = MathF.Pow(bubbleTimer, 3f);
            spriteBatch.Draw(bubble, pos + bubbleOffset * zoom, null, Color.White with { A = 0 } * pow, -rot, bubbleOrig, (1 + (1 - pow)) * scale * bubbleScale * zoom, SpriteEffects.None, 0f);
        }
    }
}