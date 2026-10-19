using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common.VanillaChanges;

public class Potions : GlobalBuff
{
    public override void Update(int type, Player player, ref int buffIndex)
    {
        switch(type)
        {
            case BuffID.Flipper:
                player.moveSpeed += 0.1f;
                return;
        }
        base.Update(type, player, ref buffIndex);
    }
}
