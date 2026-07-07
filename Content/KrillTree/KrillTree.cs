using System.Collections.Generic;
using System.Linq;

namespace FishMode.Content.KrillTree;

public class KrillTree
{
    private static readonly Dictionary<int, Krill> _krills = [];
    public static IReadOnlyDictionary<int, Krill> Krills => _krills;

    private readonly HashSet<int> _unlocked = [];
    public IReadOnlyCollection<int> Unlocked => _unlocked;
    public readonly int[] activated = [-1, -1, -1, -1, -1];
    public bool IsActivated(int id) => activated.Contains(id);
    public void Toggle(int id) => activated[Krills[id].Level - 1] = (activated[Krills[id].Level - 1] == id ? -1 : id);
    private static readonly Dictionary<string, int> nameToID = [];
    internal static void Register(Krill krill)
    {
        krill.ID = _krills.Count;
        _krills.Add(krill.ID, krill);
        nameToID.Add(krill.Name, krill.ID);
    }
    public static void EvaluateUnlocks()
    {
        foreach (var krill in Krills)
        {
            krill.Value.IDRequirements.Clear();
            foreach (var requirement in krill.Value.Requires)
            {
                var requireID = nameToID[requirement];
                krill.Value.IDRequirements.Add(requireID);
                Krills[requireID].Unlocks.Add(krill.Key);
            }
        }
    }
    public void ClearUnlocks() => _unlocked.Clear();
    public bool CanUnlock(int id)
    {
        if (!_krills.TryGetValue(id, out Krill? krill) || _unlocked.Contains(id)) return false;
        return krill.IDRequirements.All(_unlocked.Contains);
    }
    public void Unlock(int id) => _unlocked.Add(id);
    public static IEnumerable<(int from, int to)> Connections()
    {
        foreach (var k in _krills)
            foreach (var to in k.Value.Unlocks)
                yield return (k.Key, to);
    }
    public int[] SerializeForSaving() => [.. _unlocked];
    public void LoadSaveData(int[] unlocks)
    {
        _unlocked.Clear();
        foreach (var id in unlocks)
            if (_krills.ContainsKey(id))
                _unlocked.Add(id);
    }
}