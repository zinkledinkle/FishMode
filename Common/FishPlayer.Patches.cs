using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common;

public partial class FishPlayer : ModPlayer
{
    private float startSwingRot = 0f;
    public override void Load()
    {
        On_Player.Teleport += (orig, self, pos, style, extraInfo) =>
        {
            orig(self, pos, style, extraInfo);
            var body = self.GetModPlayer<FishPlayer>().Body;
            if (body == null) return;
            body.Teleport(pos);
            foreach (var p in body.particles)
            {
                p.Velocity = Vector2.Zero;
            }
        };
        On_Player.HorizontalMovement += static (orig, self) => { if (self.GetModPlayer<FishPlayer>().disable) orig(self); };
        On_Player.JumpMovement += static (orig, self) => { if (self.GetModPlayer<FishPlayer>().disable) orig(self); };
        On_Player.DryCollision += (orig, self, fallthrough, ignorePlats) => { if (self.GetModPlayer<FishPlayer>().disable) orig(self, fallthrough, ignorePlats); };
        On_Player.CheckDrowning += DrownOverride;

        IL_DoorOpeningHelper.GetPlayerInfoForOpeningDoor += (il) =>
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchRet());
            c.Index--;
            c.EmitLdloca(10);
            c.EmitLdarg1();
            c.EmitDelegate((ref DoorOpeningHelper.PlayerInfoForOpeningDoors info, Player player) =>
            {
                info.hitboxToOpenDoor = PlayerRect(player);
            });
        };
        IL_DoorOpeningHelper.GetPlayerInfoForClosingDoor += (il) =>
        {
            var c = new ILCursor(il);
            c.GotoNext(i => i.MatchRet());
            c.Index--;
            c.EmitLdloca(0);
            c.EmitLdarg1();
            c.EmitDelegate((ref DoorOpeningHelper.PlayerInfoForClosingDoors info, Player player) =>
            {
                info.hitboxToNotCloseDoor = PlayerRect(player);
            });
        };
        IL_Player.Update += UpdateStuff;
        IL_Player.ApplyTouchDamage += SuffocateFix;
        IL_Player.Update_NPCCollision += NPCColliision;
        IL_Player.ItemCheck_ApplyUseStyle_Inner += UsestyleFix;
        IL_Projectile.HurtPlayer += ProjectileCollision;
    }

    private static Rectangle PlayerRect(Player player)
    {
        var body = player.GetModPlayer<FishPlayer>().Body;
        int x = (int)body.particles.Min(p => p.Position.X) - (int)body.particles[0].Radius;
        int x2 = (int)body.particles.Max(p => p.Position.X) + (int)body.particles[0].Radius;
        int y = (int)body.particles.Min(p => p.Position.Y) - (int)body.particles[0].Radius;
        int y2 = (int)body.particles.Max(p => p.Position.Y) + (int)body.particles[0].Radius;
        return new Rectangle(x, y, x2 - x, y2 - y);
    }
    #region IL
    private void SuffocateFix(ILContext il)
    {
        var c = new ILCursor(il);

        var suffocateField = typeof(TileID.Sets).GetField(nameof(TileID.Sets.Suffocate));
        if (suffocateField == null) return;
        if (!c.TryGotoNext(i => i.MatchLdsfld(suffocateField))) return;
        c.Index += 3;
        c.EmitLdarg0(); //player
        c.EmitLdarg2(); //x
        c.EmitLdarg3(); //y
        c.EmitDelegate((Player self, int x, int y) =>
        {
            var body = self.GetModPlayer<FishPlayer>().Body;
            if (body == null) return false;
            foreach (var particle in body.particles)
            {
                if (!particle.Grounded || !particle.suffocating) continue;
                    return true;
            }
            return false;
        });
        c.EmitAnd();
    }
    private void UpdateStuff(ILContext il)
    {
        var c = new ILCursor(il);

        var hurtTileMethod = typeof(Player).GetMethod(nameof(Player.GetHurtTile));
        if (hurtTileMethod == null) return;
        if (!c.TryGotoNext(i => i.MatchCall(hurtTileMethod))) return;
        c.Index--;
        c.RemoveRange(15);

        c.EmitLdarg0(); //player
        c.EmitDelegate((Player self) =>
        {
            var body = self.GetModPlayer<FishPlayer>().Body;
            if (body == null) return;
            foreach (var particle in body.particles)
            {
                if (!particle.Grounded) continue;
                self.ApplyTouchDamage(particle.touchingTileType, particle.tileX, particle.tileY);
            }
        });
    }
    private void UsestyleFix(ILContext il)
    {
        var c = new ILCursor(il);

        var skipVanillaSwingLabel = c.DefineLabel();

        c.GotoNext(i => i.MatchLdcI4(1)); //itemusestyleid.swing
        c.Index += 2;
        c.EmitBr(skipVanillaSwingLabel);
        c.GotoNext(i => i.MatchLdcI4(7)); //itemusestyleid.drinkold

        c.Index -= 3;
        c.MarkLabel(skipVanillaSwingLabel);

        c.EmitLdarg0(); //player
        c.EmitLdarg3(); //frame
        c.EmitDelegate((Player self, Rectangle frame) =>
        {
            float arc = MathHelper.Pi;
            var body = self.GetModPlayer<FishPlayer>().Body;
            Vector2 pos = body.particles[0].Position;
            Vector2 lookDir;
            if (LockOnHelper.AimedTarget != null && LockOnHelper.AimedTarget.active)
                lookDir = pos.DirectionTo(LockOnHelper.AimedTarget.Center);
            else lookDir = pos.DirectionTo(Main.MouseWorld);

            float itemAnim = self.itemAnimation;
            float p = MathF.Pow((itemAnim / (float)self.itemAnimationMax), 1.4f);
            if (p == 1) 
                self.GetModPlayer<FishPlayer>().startSwingRot = lookDir.ToRotation();
            var startRot = self.GetModPlayer<FishPlayer>().startSwingRot;
            self.direction = MathHelper.WrapAngle(startRot + MathHelper.PiOver2) > 0 ? 1 : -1;

            float rot = self.GetModPlayer<FishPlayer>().startSwingRot; 
            rot += MathHelper.PiOver2 * self.direction;
            rot += arc / 2f;
            rot += p * arc * -self.direction;
            int itemLength = frame.Height;
            pos += new Vector2(0, -itemLength * 0.5f).RotatedBy(rot);
            self.itemLocation = pos;// + frame.Size() / 2f;
            self.itemRotation = rot - MathHelper.PiOver4 * self.direction;
        });
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
    #endregion
    #region Detours
    private static void DrownOverride(On_Player.orig_CheckDrowning orig, Player self)
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
}