using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common;

public class RemoveWaterSlowdown : ILoadable
{
    public static readonly bool[] BypassIgnoreWater = ProjectileID.Sets.Factory.CreateNamedSet("FishMode", "BypassIgnoreWater").RegisterBoolSet(false);
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
            self.ignoreWater = !BypassIgnoreWater[type];
        };
    }
    public void Unload() { }
}
