using FishMode.Content.KrillTree;
using FishMode.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace FishMode.Common;

public class KrillTreePlayer : ModPlayer
{
    public KrillTree KrillTree { get; init; } = new();
    public float KrillPoints { get; set; } = 0f;
    private readonly bool[] deactivatedForCombo = new bool[5];
    private Dictionary<int, int> NPCKillCounts = [];
    private static readonly Dictionary<int, float> EventClearedRewards = new() {
        { GameEventClearedID.DefeatedGoblinArmy, 2f },
        { GameEventClearedID.DefeatedSlimeKing, 2f },
        { GameEventClearedID.DefeatedEyeOfCthulu, 2f },
        { GameEventClearedID.DefeatedEaterOfWorldsOrBrainOfChtulu, 3f },
        { GameEventClearedID.DefeatedQueenBee, 3f },
        { GameEventClearedID.DefeatedDeerclops, 3f },
        { GameEventClearedID.DefeatedSkeletron, 4f },
        { GameEventClearedID.DefeatedWallOfFleshAndStartedHardmode, 6f },
        { GameEventClearedID.DefeatedQueenSlime, 5f },
        { GameEventClearedID.DefeatedPirates, 3f },
        { GameEventClearedID.DefeatedDestroyer, 5f },
        { GameEventClearedID.DefeatedTheTwins, 5f },
        { GameEventClearedID.DefeatedSkeletronPrime, 5f },
        { GameEventClearedID.DefeatedPlantera, 6f },
        { GameEventClearedID.DefeatedGolem, 5f },
        { GameEventClearedID.DefeatedMartians, 5f },
        { GameEventClearedID.DefeatedEmpressOfLight, 6f },
        { GameEventClearedID.DefeatedFishron, 6f },
        { GameEventClearedID.DefeatedAncientCultist, 7f },
        { GameEventClearedID.DefeatedMoonlord, 15f },
    };
    public override void Load()
    {
        On_NPC.OnGameEventClearedForTheFirstTime += static (orig, eventID) =>
        {
            orig(eventID);
            if (EventClearedRewards.TryGetValue(eventID, out var rewards))
                GetPoints(rewards, Main.LocalPlayer.Center);
        };
    }
    private static void GetPoints(float value, Vector2 position) => KrillPointBarSystem.Instance.GetPoints(value, position);
    public override void UpdateEquips()
    {
        if (PlayerInput.GetPressedKeys().Contains(Keys.LeftControl) && PlayerInput.GetPressedKeys().Contains(Keys.LeftShift) && PlayerInput.GetPressedKeys().Contains(Keys.K))
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
        if (target.life > 0 || target.immortal || target.friendly || target.lifeMax <= 5 || target.active || target.boss) return;
        float baseAmount = 0.25f;
        var killCount = NPCKillCounts.TryGetValue(target.type, out var value) ? value : 0;
        float denominator = MathF.Pow(killCount + 1, 1.2f);
        float total = baseAmount / denominator;
        total *= Main.rand.NextFloat(0.9f, 1.1f);
        if (total < 0.005f) return;
        GetPoints(total, target.Center);
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
        tag.Add("NPCKillCounts", JsonSerializer.Serialize(NPCKillCounts));
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
        if (tag.TryGet("NPCKillCounts", out string serializedKills))
        {
            var kills = JsonSerializer.Deserialize<Dictionary<int, int>>(serializedKills);
            if (kills == null) return;
            NPCKillCounts = kills;
        }
    }
}