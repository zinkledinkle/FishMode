using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Core;

internal class PlayerPatches : ILoadable
{
    public void Load(Mod mod)
    {
        On_Player.Teleport += (orig, self, pos, style, extraInfo) =>
        {
            orig(self, pos, style, extraInfo);
            var body = self.GetModPlayer<FishPlayer>().Body;
            body.Teleport(pos);
            foreach (var p in body.particles)
            {
                p.Velocity = Vector2.Zero;
            }
        };
        On_Player.HorizontalMovement += (orig, self) => { if (!self.GetModPlayer<FishPlayer>().disable) orig(self); };
        On_Player.JumpMovement += (orig, self) => { if (!self.GetModPlayer<FishPlayer>().disable) orig(self); };
        On_Player.CheckDrowning += DrownOverride;
        IL_Player.Update += UpdateStuff;
        IL_Player.ApplyTouchDamage += SuffocateFix;
        IL_Player.Update_NPCCollision += NPCColliision;
        IL_Projectile.HurtPlayer += ProjectileCollision;
    }

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

    public void Unload() { }
}
