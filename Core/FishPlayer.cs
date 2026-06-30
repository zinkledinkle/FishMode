using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Data;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core;

public class FishPlayer : ModPlayer
{
    public PlayerFishBody Body;
    private const int baseBodyLength = 5;
    private int _bodyLength = baseBodyLength;
    public int BodyLength
    {
        get => _bodyLength;

        set => _bodyLength = MathHelper.Clamp(value, 2, 10);
    }

    public bool lastF = false;
    public bool lastG = false;

    public bool freeze = false;
    public bool disable = false;

    private int dashCooldown = 0;
    private int dashTime = 0;

    public int dashCooldownTime;
    public int dashDuration;
    public float dashSpeed;

    //wavey stuff
    private float averageDir;
    private int waveTimer;
    private int waveDecayTimer;
    private bool waveUp;
    private float waveAccel;

    private readonly List<PlayerParticle> testParticles = [];

    #region edits
    public override void Load()
    {
        On_Player.Teleport += (orig, self, pos, style, extraInfo) =>
        {
            orig(self, pos, style, extraInfo);
            var body = self.GetModPlayer<FishPlayer>().Body;
            body.Teleport(pos);
            foreach(var p in body.particles)
            {
                p.Velocity = Vector2.Zero;
            }
        };
        On_Player.HorizontalMovement += (orig, self) => { if (!disable) orig(self); };
        On_Player.JumpMovement += (orig, self) => { if (!disable) orig(self); };
        On_Player.CheckDrowning += DrownOverride;
        IL_Player.Update_NPCCollision += NPCColliision;
        IL_Projectile.HurtPlayer += ProjectileCollision;
    }

    private void ProjectileCollision(ILContext il)
    {
        var c = new ILCursor(il);

        var loopIndexA = c.Body.Variables.Count;
        il.Body.Variables.Add(new VariableDefinition(il.Import(typeof(int))));

        c.GotoNext(i => i.MatchLdloc0());

        var loopLabel = il.DefineLabel();
        var endLoopLabel = il.DefineLabel();

        c.EmitLdcI4(0);
        c.EmitStloc(loopIndexA); //for loop index
        c.EmitBr(endLoopLabel);

        c.MarkLabel(loopLabel); //start of loop

        c.GotoNext(i => i.MatchRet());
        c.Remove();
        c.EmitBr(endLoopLabel); //continue instead of return

        c.GotoNext(i => i.MatchLdarga(1));
        c.Index++;
        c.RemoveRange(3);
        c.EmitLdloc(0); //player
        c.EmitLdloc(loopIndexA);
        c.EmitDelegate((ref Rectangle hitbox, Player plr, int index) =>
        {
            var body = plr.GetModPlayer<FishPlayer>().Body;
            var point = body.particles[index];
            var pos = point.Position;
            var radius = point.Radius;
            Rectangle rect = new((int)(pos.X - radius), (int)(pos.Y - radius), (int)radius * 2, (int)radius * 2);
            return rect.Intersects(hitbox);
        });

        c.GotoNext(i => i.MatchRet());
        c.Remove();
        c.EmitBr(endLoopLabel); //continue instead of return

        c.Index = c.Instrs.Count - 1;

        c.MarkLabel(endLoopLabel);

        c.EmitLdloc(loopIndexA);
        c.EmitLdcI4(1);
        c.EmitAdd();
        c.EmitStloc(loopIndexA);

        c.EmitLdloc(loopIndexA);
        c.EmitLdarg0();
        c.EmitDelegate((Player self) => self.GetModPlayer<FishPlayer>().Body.particles.Count);
        c.EmitBlt(loopLabel);
    }
    private void NPCColliision(ILContext il)
    {
        var c = new ILCursor(il);

        var loopIndexA = c.Body.Variables.Count;
        il.Body.Variables.Add(new VariableDefinition(il.Import(typeof(int)))); 

        if (!c.TryGotoNext(i => i.MatchLdloca(0))) return;
        c.RemoveRange(14);

        var loopLabel = il.DefineLabel();
        var endLoopLabel = il.DefineLabel();

        c.EmitLdcI4(0);
        c.EmitStloc(loopIndexA); //for loop index
        c.EmitBr(endLoopLabel);

        c.MarkLabel(loopLabel); //start of loop

        c.EmitLdloca(0); //load rect address
        c.EmitLdloc(loopIndexA); //load index
        c.EmitLdarg0(); //load player instance
        c.EmitDelegate((ref Rectangle rect, int index, Player self) =>
        {
            var body = self.GetModPlayer<FishPlayer>().Body;
            var point = body.particles[index];
            var pos = point.Position;
            var radius = point.Radius;
            rect = new((int)(pos.X - radius), (int)(pos.Y - radius), (int)radius * 2, (int)radius * 2);

            //Dust.QuickBox(rect.TopLeft(), rect.BottomRight(), 0, Color.White, null);
        });

        c.GotoNext(i => i.MatchRet());

        c.MarkLabel(endLoopLabel);

        c.EmitLdloc(loopIndexA);
        c.EmitLdcI4(1);
        c.EmitAdd();
        c.EmitStloc(loopIndexA);

        c.EmitLdloc(loopIndexA);
        c.EmitLdarg0();
        c.EmitDelegate((Player self) => self.GetModPlayer<FishPlayer>().Body.particles.Count);
        c.EmitBlt(loopLabel);
    }
    private void DrownOverride(On_Player.orig_CheckDrowning orig, Player self)
    {
        var body = self.GetModPlayer<FishPlayer>().Body;
        bool shouldDrown = body.particles[0].GetLiquid() == -1; //in air
        bool lungs = self.gills;

        if (lungs && shouldDrown)
            shouldDrown = false;
        if (self.shimmering)
            shouldDrown = false;

        if (Main.myPlayer == self.whoAmI)
        {
            if (shouldDrown)
            {
                self.breathCD++;
                if (self.breathCD >= self.breathCDMax)
                {
                    self.breathCD = 0;
                    self.breath--;
                    if (self.breath == 0)
                    {
                        SoundEngine.PlaySound(23);
                    }
                    if (self.breath <= 0)
                    {
                        self.lifeRegenTime = 0f;
                        self.breath = 0;
                        self.statLife -= 2;
                        if (self.statLife <= 0)
                        {
                            self.statLife = 0;
                            self.KillMe(PlayerDeathReason.ByOther(1), 10.0, 0);
                        }
                    }
                }
            }
            else
            {
                self.breath += 3;
                if (self.breath > self.breathMax)
                {
                    self.breath = self.breathMax;
                }
                self.breathCD = 0;
            }
        }
    }
    #endregion

    public override void OnEnterWorld()
    {
        Body = new PlayerFishBody(Player, BodyLength);
        disable = true;
    }
    public override void ResetEffects()
    {
        Main.NewText(waveAccel);
        Main.NewText(waveTimer);
        Main.NewText(waveUp ? "up" : "down");
        if (PlayerInput.GetPressedKeys().Contains(Keys.LeftControl) && Main.mouseRight && Main.mouseRightRelease) Body = new PlayerFishBody(Player, BodyLength);

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
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
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

        if (Keybinds.DashBind.JustPressed && dashCooldown == 0)
        {
            dashTime = dashDuration;
            dashCooldown = dashCooldownTime + dashDuration;
        }
        if (triggersSet.Jump && Body.Grounded)
        {
            Body.Jump(10f + Player.jumpSpeedBoost);
        }

        //if (Main.mouseMiddle && Main.mouseMiddleRelease)
        //{
        //    var a = new PlayerParticle(Main.MouseWorld - Vector2.UnitX * 15, 3f, 15f);
        //    var b = new PlayerParticle(Main.MouseWorld, 3f, 15f);
        //    var c = new PlayerParticle(Main.MouseWorld + Vector2.UnitX * 15, 3f, 15f);

        //    int i = 7;
        //    float dist = 60f;
        //    float strength = 0.1f;

        //    a.AddConstraint(new DistanceConstraint(a, b, dist, strength, strength, i));
        //    b.AddConstraint(new DistanceConstraint(b, c, dist, strength, strength, i));

        //    b.AddConstraint(new DistanceConstraint(b, a, dist, strength, strength, i));
        //    c.AddConstraint(new DistanceConstraint(c, b, dist, strength, strength, i));

        //    //a.AddConstraint(new DistanceConstraint(a, c, dist * 2, 0f, strength, i));
        //    //c.AddConstraint(new DistanceConstraint(c, a, dist * 2, 0f, strength, i));

        //    testParticles.Add(a);
        //    testParticles.Add(b);
        //    testParticles.Add(c);
        //}
        //if (Main.mouseMiddle && Main.mouseLeft)
        //{
        //    testParticles.Clear();
        //}

        //foreach (var p in testParticles)
        //{
        //    if (triggersSet.Down || Main.mouseLeft && Main.mouseLeftRelease)
        //    {
        //        p.Update();
        //        foreach(var c in p.Constraints)
        //        {
        //            c.Apply();
        //        }
        //        p.Step();
        //    }
        //}
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

        if (Player.controlUp)
        {
            Body.Propel(3f * (1 + waveAccel) * MathF.Pow(Player.maxRunSpeed - 2f, 2f), 0.3f);
        }
        if (dashTime > 0)
        {
            Body.Propel(dashSpeed, 0.8f);
        }

        if (Body.Submerged) WaveShit();
    }
    private void WaveShit()
    {
        var front = Body.particles[0].Position;
        var delta = Main.MouseWorld - front;

        var angleTo = MathF.Atan2(delta.Y, delta.X);
        var angleDelta = MathHelper.WrapAngle(angleTo - averageDir);
        averageDir = MathHelper.WrapAngle(averageDir + angleDelta * 0.05f);

        float threshold = 0.2f;
        float increment = 0.15f;

        if (waveTimer > 0) return;

        if (angleDelta > threshold && waveUp)
        {
            waveTimer = 15;
            waveDecayTimer = 40;
            waveAccel = MathF.Min(waveAccel + increment, 1f);
            waveUp = false;
            for (int i = 0; i < 10; i++) Dust.NewDust(front, 16, 16, DustID.AncientLight);
        }
        if (angleDelta < -threshold && !waveUp)
        {
            waveTimer = 15;
            waveDecayTimer = 40;
            waveAccel = MathF.Min(waveAccel + increment, 1f);
            waveUp = true;
            for (int i = 0; i < 10; i++) Dust.NewDust(front, 16, 16, DustID.AncientLight);
        }
    }
    public override void PostUpdate()
    {
        if (disable) return;

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
        Main.spriteBatch.Draw(px, (avgDirPos + center) / 2f - Main.screenPosition, null, Color.White, rot, og, scale, SpriteEffects.None, 0f);
    }
}
