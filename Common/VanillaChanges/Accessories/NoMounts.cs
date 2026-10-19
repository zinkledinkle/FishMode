using Daybreak.Hooks.V1;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace FishMode.Common.VanillaChanges.Accessories;

public static class NoMounts
{
    [OnLoad]
    private static void Load()
    {
        //IL_ItemSlot.SwapEquip_ItemArray_int_int += (il) =>
        //{
        //    var c = new ILCursor(il);
        //    if (!c.TryGotoNext(i => i.MatchLdfld(typeof(Item).GetField(nameof(Item.mountType), BindingFlags.Public | BindingFlags.Instance)))) return;
        //    if (!c.TryGotoNext(i => i.MatchCall(typeof(ItemSlot).GetMethod(nameof(ItemSlot.EquipSwap), BindingFlags.Public | BindingFlags.Instance)))) return;
        //    ILLabel? skip = null;
        //    if (!c.TryGotoPrev(i => i.MatchBrfalse(out skip))) return;
        //    c.Index++;
        //    c.EmitDelegate(() => SoundEngine.PlaySound(SoundID.NPCDeath64));
        //    c.EmitBr(skip!);
        //};
        MonoModHooks.Add(typeof(ItemLoader).GetMethod(nameof(ItemLoader.CanEquipAccessory), BindingFlags.Public | BindingFlags.Static, [typeof(Item), typeof(int), typeof(bool)]), static (Func<Item, int, bool, bool> orig, Item item, int slot, bool modded) => {
            if (item.mountType > -1)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath64);
                return false;
            }
            return orig(item, slot, modded);
        });
        On_ItemSlot.Draw_SpriteBatch_ItemArray_int_int_Vector2_Color += static (orig, spriteBatch, inv, context, slot, position, color) =>
        {
            orig(spriteBatch, inv, context, slot, position, color);
            if (inv != Main.LocalPlayer.miscEquips || slot < 2 || slot > 3) return;
            var texture = TextureAssets.Cd.Value;
            var origin = texture.Size() / 2f;
            var offset = TextureAssets.InventoryBack.Size() / 2f;
            offset -= new Vector2(4, 4);
            spriteBatch.Draw(texture, position + offset, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        };
        On_Mount.CanMount += static (orig, self, m, player) => { SoundEngine.PlaySound(SoundID.NPCDeath64); return false; };
    }
}
