using FishMode.Core.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core;

public class FishPlayer : ModPlayer
{
    public PlayerFishBody Body;
    public PlayerFishBody MeshDebug;

    public bool lastF = false;
    public bool lastG = false;

    public bool freeze = false;
    public bool disable = false;

    private int dashCooldown = 0;
    private int dashTime = 0;

    public int dashCooldownTime = 120;
    public int dashDuration = 5;
    public float dashSpeed = 40f;

    List<PlayerParticle> testParticles = [];

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
    }

    private void NPCColliision(ILContext il)
    {
        var c = new ILCursor(il);

        var loopIndexLocation = c.Body.Variables.Count + 1;
        var pointCountLocation = c.Body.Variables.Count + 2;

        if (!c.TryGotoNext(i => i.MatchLdloca(0))) return;
        c.RemoveRange(14);

        var loopLabel = il.DefineLabel();
        var endLoopLabel = il.DefineLabel();

        c.EmitLdcI4(0);
        c.EmitStloc(pointCountLocation); //the amount of points, will be assigned in the hitbox creation cause its just easiest
        c.EmitLdcI4(0);
        c.EmitStloc(loopIndexLocation); //for loop index
        c.EmitBr(endLoopLabel);

        c.MarkLabel(loopLabel); //start of loop

        c.EmitLdloca(0); //load rect address
        c.EmitLdloc(loopIndexLocation); //load index
        c.EmitLdarg0(); //load player instance
        c.EmitLdloca(pointCountLocation); //load point count address
        c.EmitDelegate((ref Rectangle rect, int index, Player self, ref int pointCount) =>
        {
            var body = self.GetModPlayer<FishPlayer>().Body;
            var point = body.particles[index];
            pointCount = body.particles.Count;
            var pos = point.Position;
            var radius = point.Radius;
            rect = new((int)(pos.X - radius), (int)(pos.Y - radius), (int)radius * 2, (int)radius * 2);
        });

        c.GotoNext(i => i.MatchRet());

        c.MarkLabel(endLoopLabel);
        c.EmitLdloc(loopIndexLocation);
        c.EmitLdloc(pointCountLocation);
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

    public override void OnEnterWorld()
    {
        Body = new PlayerFishBody(Player);
        disable = true;
    }
    public override void ResetEffects()
    {
        if (PlayerInput.GetPressedKeys().Contains(Keys.LeftControl) && Main.mouseRight && Main.mouseRightRelease) Body = new PlayerFishBody(Player);
        dashCooldownTime = 5;
        dashDuration = 15;
        dashSpeed = 5f;

        Body?.DebugGrab();

        dashCooldown = Math.Max(0, dashCooldown - 1);
        dashTime = Math.Max(0, dashTime - 1);
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
        Body.Update();

        if (Player.controlUp)
        {
            Body.Propel(2.4f * Player.moveSpeed, 0.3f);
        }
        if (dashTime > 0)
        {
            Body.Propel(dashSpeed, 0.8f);
        }
    }
    public override void PostUpdate()
    {
        if (disable) return;

        Vector2 fishPos = Body.particles[0].Position;
        Player.Center = fishPos;
        Player.velocity = Body.particles[0].Velocity;
        Player.direction = Body.particles[0].Velocity.X > 0 ? 1 : -1;
    }
    public override void DrawPlayer(Camera camera)
    {
        foreach (var particle in testParticles)
        {
            foreach(var constraint in particle.Constraints)
            {
                var px = TextureAssets.MagicPixel.Value;
                var width = 2f;
                var length = constraint.ParticleB.Position.Distance(particle.Position);

                var scale = new Vector2(width / px.Width, length / px.Height);
                var og = px.Size() / 2f;
                var rot = constraint.ParticleB.Position.DirectionTo(particle.Position).ToRotation() + MathHelper.PiOver2;
                Main.spriteBatch.Draw(px, (particle.Position + constraint.ParticleB.Position) / 2f - Main.screenPosition, null, Color.White, rot, og, scale, SpriteEffects.None, 0f);
            }

            var rect = new Rectangle((int)(particle.Position.X - particle.Radius - Main.screenPosition.X), (int)(particle.Position.Y - particle.Radius - Main.screenPosition.Y), (int)(particle.Radius * 2), (int)(particle.Radius * 2));
            Main.spriteBatch.Draw(TextureAssets.Extra[ExtrasID.MoonLordEye].Value, rect, Color.White);
        }
        if (PlayerInput.GetPressedKeys().Contains(Keys.RightAlt)) WorldGen.SaveAndQuit();
        return;
    }
}
