using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace FishMode.Core;

public partial class FishPlayer : ModPlayer
{
    public PlayerFishBody? Body;
    private const int baseBodyLength = 5;
    private int _bodyLength = baseBodyLength;
    public int BodyLength
    {
        get => _bodyLength;

        set => _bodyLength = MathHelper.Clamp(value, 2, 10);
    }
    private static FishModeConfig.MovementType MoveMode => ModContent.GetInstance<FishModeConfig>().MovementMode;

    public bool lastF = false;
    public bool lastG = false;

    public bool freeze = false;
    public bool disable = false;

    private int dashCooldown;
    private int dashTime;

    public int dashCooldownTime;
    public int dashDuration;
    public float dashSpeed;

    private int wasdSineTimer;
    private float averageDir;
    private int waveTimer;
    private int waveDecayTimer;
    private bool waveUp;
    private float waveAccel;

    private readonly List<PlayerParticle> testParticles = [];

    public override void OnEnterWorld()
    {
        Body = new(Player, BodyLength);
    }
    public override void OnRespawn()
    {
        Vector2 spawn;
        if (Player.SpawnX == -1)
        {
            spawn = new(Main.spawnTileX * 16 + 8, Main.spawnTileY * 16 - 24);
        } else spawn = new(Player.SpawnX * 16 + 8, Player.SpawnY * 16 - 24);
        Player.Center = spawn;
        Body = new(Player, BodyLength);
    }
    public override void ResetEffects()
    {
        Main.SetCameraLerp(0.05f, 100);
        if (PlayerInput.GetPressedKeys().Contains(Keys.LeftControl) && Main.mouseRight && Main.mouseRightRelease) Body = new PlayerFishBody(Player, BodyLength);
        if (!Player.controlJump) wasdSineTimer = 0; else wasdSineTimer++;

        dashCooldownTime = 120;
        dashDuration = 20;
        dashSpeed = 3f;
        BodyLength = baseBodyLength;

        Body?.SetEnviromentalValues(); //reset to defaults

        dashCooldown = Math.Max(0, dashCooldown - 1);
        dashTime = Math.Max(0, dashTime - 1);

        waveDecayTimer = Math.Max(0, waveDecayTimer - 1);
        waveTimer = Math.Max(0, waveTimer - 1);
        if (waveDecayTimer == 0) waveAccel = MathF.Max(0f, waveAccel - 0.01f);
    }
    public override void UpdateDead()
    {
        if (!Body.dead) Body.Kill();
        Body.Update();
    }
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Player.dead) return;
        bool F = PlayerInput.GetPressedKeys().Contains(Keys.F);
        if (F && !lastF)
        {
            freeze = !freeze;
        }
        lastF = F;

        bool G = PlayerInput.GetPressedKeys().Contains(Keys.G);
        if (G && !lastG)
        {
            disable = !disable;
            if (disable) Player.velocity = Vector2.Zero;
        }
        lastG = G;

        if (triggersSet.Jump && Body.Grounded)
        {
            Body.Jump(10f + Player.jumpSpeedBoost);
        }

        if (Keybinds.LockBind.JustReleased && MoveMode == FishModeConfig.MovementType.LookAndLock)
        {
            LockOnHelper.ForceUsability = true;
            PlayerInput.Triggers.JustReleased.LockOn = true;
            LockOnHelper.SetActive(true);
        } else if (MoveMode == FishModeConfig.MovementType.WASD)
        {
            LockOnHelper.ForceUsability = false;
            LockOnHelper.SetActive(false);
        }
    }
    public override void PreUpdateMovement()
    {
        if (freeze) return;

        Body.SetEnviromentalValues(gravity: Player.gravity * Player.gravDir);
        while (Body.particles.Count < BodyLength)
            Body.AddSegment();
        while (Body.particles.Count > BodyLength)
            Body.RemoveSegment();

        Body.Update();

        float moveSpeed = 3f * (1 + waveAccel) * MathF.Pow(Player.maxRunSpeed - 2f, 2f);
        if (MoveMode == FishModeConfig.MovementType.LookAndLock)
        {
            if (Player.controlUp)
            {
                Body.Propel(moveSpeed, 0.3f);
            }
            var dir = Main.MouseWorld - Body.particles[0].Position;
            if (Body.Submerged && Player.controlUp) WaveShit(dir);
        }
        else
        {
            var right = Player.controlRight.ToDirectionInt();
            var left = Player.controlLeft.ToDirectionInt();
            var down = Player.controlDown.ToDirectionInt();
            var up = Player.controlUp.ToDirectionInt();
            Vector2 input = new(right - left, down - up);
            if (input == Vector2.Zero) return;
            if (Body.Submerged)
            {
                var sine = MathF.Sin(wasdSineTimer / MathF.PI / 60f * 40f);
                input = input.RotatedBy(sine * 0.4f);
            }

            Body.Propel(moveSpeed, 0.3f, Body.particles[0].Position + input);
            WaveShit(input);
        }
    }
    private void WaveShit(Vector2 dir)
    {
        var angleTo = MathF.Atan2(dir.Y, dir.X);
        var angleDelta = MathHelper.WrapAngle(angleTo - averageDir);
        averageDir = MathHelper.WrapAngle(averageDir + angleDelta * 0.05f);

        float threshold = 0.15f;
        float increment = 0.15f;

        if (waveTimer > 0) return;

        if (angleDelta > threshold && waveUp)
        {
            waveTimer = 10;
            waveDecayTimer = 40;
            waveAccel = MathF.Min(waveAccel + increment, 1f);
            waveUp = false;
        }
        if (angleDelta < -threshold && !waveUp)
        {
            waveTimer = 10;
            waveDecayTimer = 40;
            waveAccel = MathF.Min(waveAccel + increment, 1f);
            waveUp = true;
        }
    }
    public override void PostUpdate()
    {
        if (disable || Player.dead) return;

        Vector2 fishPos = Body.particles[0].Position;
        Player.Center = fishPos;
        Player.velocity = Body.particles[0].Velocity;

        if (Math.Abs(Body.particles[0].Velocity.X) > 1) Player.direction = Math.Sign(Body.particles[0].Velocity.X);
    }
    public override void DrawPlayer(Camera camera)
    {
        if (!ModContent.GetInstance<FishModeConfig>().DebugDraw) return;
        var px = TextureAssets.MagicPixel.Value;
        var width = 2f;
        var center = Body.particles[0].Position;
        var length = center.Distance(Main.MouseWorld);
        var avgDirPos = averageDir.ToRotationVector2() * length + center;

        var scale = new Vector2(width / px.Width, length / px.Height);
        var og = px.Size() / 2f;
        var rot = averageDir + MathHelper.PiOver2;
        Main.spriteBatch.Draw(px, (avgDirPos + center) / 2f - Main.screenPosition, null, Color.Red, rot, og, scale, SpriteEffects.None, 0f);
    }
}
