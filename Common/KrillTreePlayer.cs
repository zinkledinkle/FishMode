using FishMode.Content.KrillTree;
using FishMode.UI;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Achievements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FishMode.Common;

public class KrillTreePlayer : ModPlayer
{
    public KrillTree KrillTree { get; init; } = new();
    public float KrillPoints { get; set; } = 0f;
    private readonly bool[] deactivatedForCombo = new bool[5];
    private readonly Dictionary<int, int> NPCKillCounts = [];
    public override void Load()
    {
        On_AchievementManager.AchievementCompleted += static (orig, self, achievement) =>
        {
            orig(self, achievement);
            KrillPointBarSystem.Instance.GetPoints(1f, Main.LocalPlayer.Center);
        };
    }
    public override void UpdateEquips()
    {
        if (Player.controlDown)
        {
            KrillTree.ClearUnlocks();
            KrillTree.activated[0] = -1;
            KrillTree.activated[1] = -1;
            KrillTree.activated[2] = -1;
            KrillTree.activated[3] = -1;
            KrillTree.activated[4] = -1;
        }
        for (int i = 0; i < 5; i++)
        {
            deactivatedForCombo[i] = false;
            var type = KrillTree.activated[i];
            if (type == -1) continue;
            KrillTree.Krills[type].Reset();
        }
        for (int i = 0; i < 5; i++)
        {
            var type = KrillTree.activated[i];
            if (type == -1) continue;
            var krill = KrillTree.Krills[type];
            if (krill.Combinations.Count == 0) continue;
            for (int j = 0; j < 5; j++)
            {
                if (j == i) continue;
                if (!krill.Combinations.Contains(KrillTree.activated[j])) continue;
                if (j > i) continue; //let the higher level one do it
                bool deactivate = false;
                krill.CombinationEffect(KrillTree.activated[j], Player, ref deactivate);
                deactivatedForCombo[j] = deactivate;
            }
        }
        for (int i = 0; i < 5; i++)
        {
            var type = KrillTree.activated[i];
            if (type == -1 || deactivatedForCombo[i]) continue;
            KrillTree.Krills[type].Apply(Player);
        }
    }
    public override void UpdateVisibleAccessories()
    {
        for (int i = 0; i < 5; i++)
        {
            var type = KrillTree.activated[i];
            if (type == -1 || deactivatedForCombo[i]) continue;
            KrillTree.Krills[type].Visuals(Player);
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
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.life > 0 || target.immortal || target.friendly || target.lifeMax <= 5 || target.active) return;
        float baseAmount = 0.25f;
        if (target.boss) baseAmount *= 5;
        var killCount = NPCKillCounts.TryGetValue(target.type, out var value) ? value : 0;
        float denominator = MathF.Pow(killCount + 1, 1.2f);
        KrillPointBarSystem.Instance.GetPoints(baseAmount / denominator, target.Center);
        if (!NPCKillCounts.TryAdd(target.type, 1)) NPCKillCounts[target.type] += 1;
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