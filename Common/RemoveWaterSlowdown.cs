using Terraria;
using Terraria.ModLoader;

namespace FishMode.Common;

public class RemoveWaterSlowdown : ILoadable
{
    public void Load(Mod mod)
    {
        On_NPC.Collision_WaterCollision += (_, npc, _) =>
        {
            npc.wet = false;
            return false;
        };
        On_Projectile.SetDefaults_End += (orig, self, type) =>
        {
            orig(self, type);
            self.ignoreWater = true;
        };
    }
    public void Unload() { }
}
