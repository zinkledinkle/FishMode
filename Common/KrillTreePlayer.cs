using FishMode.Content.KrillTree;
using FishMode.UI;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FishMode.Common;

public class KrillTreePlayer : ModPlayer
{
    public KrillTree KrillTree { get; init; } = new();
    public float KrillPoints { get; set; } = 0f;
    public override void UpdateEquips()
    {
        if (Player.controlDown)
        {
            KrillTree.ClearUnlocks();
        }
        for (int i = 0; i < 5; i++)
        {
            var type = KrillTree.activated[i];
            if (type == -1) continue;
            KrillTree.Krills[type].Apply(Player);
        }
    }
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (Player.dead)
        {
            ModContent.GetInstance<KrillTreeUISystem>().Toggle(false);
            return;
        }
        if (Keybinds.OpenKrillTree.JustReleased)
            ModContent.GetInstance<KrillTreeUISystem>().Toggle();
    }
    public override void ResetEffects()
    {
        if (Main.mouseMiddle && Main.mouseMiddleRelease) KrillPoints ++;
    }
    public override void SaveData(TagCompound tag)
    {
        tag.Add("KrillTreeUnlocks", KrillTree.SerializeForSaving());
        tag.Add("KrillTreePoints", KrillPoints);
        tag.Add("ActivatedKrills", KrillTree.activated);
    }
    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet("KrillTreeUnlocks", out int[] unlocks))
            KrillTree.LoadSaveData(unlocks);
        if (tag.TryGet("KrillTreePoints", out float points))
            KrillPoints = points;
        if (tag.TryGet("ActivatedKrills", out int[] krills))
        {
            for (int i = 0; i < KrillTree.activated.Length; i++)
                KrillTree.activated[i] = krills[i];
        }
    }
}