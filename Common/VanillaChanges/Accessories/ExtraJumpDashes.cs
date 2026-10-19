using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using System.Reflection;
using Terraria.ID;
using Terraria.Audio;
using Terraria.GameContent.Drawing;

namespace FishMode.Common.VanillaChanges.VanillaAccessoryChanges;

public class ExtraJumpDashes : ModPlayer
{
    private static readonly Dictionary<int, int> dashCooldowns = [];
    private static readonly Dictionary<int, float> dashVelocityMultipliers = new()
    {
        { ExtraJump.CloudInABottle.Type, 1.4f }
    };

    public const int dashRefreshTime = 300;
    private int dashingTime = 0;
    private const float dashAccel = 12f;
    private const float maxDashSpeed = 18f;
    private const int dashCooldown = 30;
    private int dashCooldownTimer = 0;
    public bool AnyDashActive { get; private set; }
    public float DashProgress { get; private set; }
    public int CurrentCooldown { get; private set; }
    public bool AnyDashEquipped { get; private set; }
    private Vector2 dir;
    public override void Load()
    {
        var canShowVisualsMethod = typeof(ExtraJump).GetMethod("CanShowVisuals", BindingFlags.Public | BindingFlags.Instance);
        MonoModHooks.Add(canShowVisualsMethod, static (Func<ExtraJump, Player, bool> _, ExtraJump self, Player player) => true);
        var visualsMethod = typeof(ExtraJumpLoader).GetMethod("JumpVisuals", BindingFlags.Public | BindingFlags.Static);
        MonoModHooks.Add(visualsMethod, static (Action<Player> orig, Player player) =>
        {
            var prev = player.Bottom;
            player.Bottom = player.GetModPlayer<FishPlayer>().Body.particles[^1].Position;
            orig(player);
            player.Bottom = prev;
        });
    }
    public override bool CanShowExtraJumpVisuals(ExtraJump jump) => true;
    public override void PostUpdateEquips()
    {
        AnyDashActive = ExtraJumpLoader.OrderedJumps.Any(j => Player.GetJumpState(j).Active);
        dashCooldownTimer = AnyDashActive ? dashCooldown : Math.Max(dashCooldownTimer - 1, 0);

        int lowest = int.MaxValue;
        AnyDashEquipped = false;
        if (AnyDashActive || dashCooldownTimer > 0) return;
        foreach (var jump in ExtraJumpLoader.OrderedJumps)
        {
            ref var state = ref Player.GetJumpState(jump);
            if (!state.Enabled) continue;
            AnyDashEquipped = true;

            ref int cooldown = ref CollectionsMarshal.GetValueRefOrAddDefault(dashCooldowns, jump.Type, out _);
            var prev = cooldown;
            cooldown = Math.Max(cooldown - 1, 0);
            if (cooldown > 0) lowest = Math.Min(lowest, cooldown);

            if (cooldown == 0 && !state.Available)
            {
                state.Available = true;
                SoundEngine.PlaySound(SoundID.MaxMana, Player.Center);
                ParticleOrchestrator.Spawn_BestReforge(new() { IndexOfPlayerWhoInvokedThis = (byte)Player.whoAmI, PositionInWorld = Player.Center });
            }
        }
        CurrentCooldown = lowest;
    }
    public override void ModifyExtraJumpDurationMultiplier(ExtraJump jump, ref float duration)
    {
        duration *= jump switch
        {
            CloudInABottleJump => 2f,
            FartInAJarJump => 2f,
            _ => 1f
        };
    }
    public override void PreUpdateMovement()
    {
        base.PreUpdateMovement();
        foreach (var jump in ExtraJumpLoader.OrderedJumps)
        {
            ref var state = ref Player.GetJumpState(jump);
            if (!state.Enabled || !state.Active) continue;
            float duration = jump.GetDurationMultiplier(Player);
            PlayerLoader.ModifyExtraJumpDurationMultiplier(jump, Player, ref duration);
            int maxTime = (int)(Player.jumpHeight * duration);
            float t = 1 - MathHelper.Clamp(dashingTime / (float)maxTime, 0, 1);
            DashProgress = t;
            dashCooldowns[jump.Type] = dashRefreshTime;
            dashCooldownTimer = dashCooldown;

            var fplr = Player.GetModPlayer<FishPlayer>();
            if (t == 1) dir = fplr.lookDir;
            if (t == 0)
            {
                state._active = false;
                dashingTime = 0;
                Player.jump = 0;
                return;
            }
            var accel = t * dashAccel * Player.moveSpeed;
            var maxSpeed = maxDashSpeed * Player.moveSpeed;
            if (dashVelocityMultipliers.TryGetValue(jump.Type, out var speedMod))
            {
                accel *= speedMod;
                maxSpeed *= speedMod;
            }

            if (Vector2.Dot(dir, fplr.lookDir) < 0)
            {
                state.Stop();
                dashingTime = 0;
                return;
            }
            fplr.Body.Propel(accel / maxDashSpeed, maxSpeed, dir, 0.8f, false);
            dashingTime++;
            return;
        }
    }
}