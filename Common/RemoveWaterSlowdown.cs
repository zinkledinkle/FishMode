using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FishMode.Common;

public class RemoveWaterSlowdown : ILoadable
{
    public static readonly bool[] ProjectileBypassIgnoreWater = ProjectileID.Sets.Factory.CreateNamedSet("FishMode", "ProjectileBypassIgnoreWater").RegisterBoolSet(false);
    public static readonly bool[] NPCBypassIgnoreWater = NPCID.Sets.Factory.CreateNamedSet("FishMode", "NPCBypassIgnoreWater").RegisterBoolSet(false, 
        55, 57, 58, 63, 64, 65, 67, 102, 103, 157, 220, 221, 241, 242, 362, 363, 364, 365, 461, 465, 542, 543, 544, 545, 586, 592, 607, 608, 609, 615, 616, 617, 620, 625, 626, 627, 692); 
    public void Load(Mod mod)
    {
        On_NPC.Collision_WaterCollision += (orig, npc, lava) =>
        {
            if (NPCBypassIgnoreWater[npc.type]) return orig(npc, lava);
            npc.wet = false;
            return false;
        };
        On_Projectile.SetDefaults_End += (orig, self, type) =>
        {
            orig(self, type);
            self.ignoreWater = !ProjectileBypassIgnoreWater[type];
        };
    }
    public void Unload() { }
}
