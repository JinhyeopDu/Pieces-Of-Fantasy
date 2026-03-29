using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DT_NewDropTable", menuName = "PoF/DropTable")]
public class DropTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemData item;
        [Range(0f, 1f)] public float chance = 1f;
        [Min(1)] public int minQty = 1;
        [Min(1)] public int maxQty = 1;

        public bool IsValid()
        {
            if (item == null) return false;
            if (chance <= 0f) return false;
            if (minQty <= 0 || maxQty <= 0) return false;
            if (minQty > maxQty) return false;
            return true;
        }
    }

    [Serializable]
    public class WeightedPick
    {
        public ItemData item;
        [Min(0f)] public float weight = 1f;
        [Min(1)] public int minQty = 1;
        [Min(1)] public int maxQty = 1;

        public bool IsValid()
        {
            if (item == null) return false;
            if (weight <= 0f) return false;
            if (minQty <= 0 || maxQty <= 0) return false;
            if (minQty > maxQty) return false;
            return true;
        }
    }

    [Header("Guaranteed: pick exactly ONE from this list (weighted)")]
    public List<WeightedPick> guaranteedPickOne = new();

    [Header("Optional: Extra pick from the SAME guaranteed list")]
    [Range(0f, 1f)] public float extraPickChance = 0f; // 예: 0.30
    public bool extraPickNoDuplicate = true;           // 예: true면 첫 드랍과 중복 금지

    [Header("Optional rolls (independent chances)")]
    public List<Entry> entries = new();

    public List<(ItemData item, int qty)> Roll(System.Random rng = null)
    {
        rng ??= new System.Random();
        var results = new List<(ItemData item, int qty)>();

        // 1) 반드시 1개
        var firstPick = PickOneWeighted(guaranteedPickOne, rng, avoidItem: null);
        if (firstPick != null)
        {
            results.Add((firstPick.item, RollQty(firstPick.minQty, firstPick.maxQty, rng)));
        }

        // 2) 30% 확률로 1개 더 (same group)
        if (extraPickChance > 0f)
        {
            float r01 = (float)rng.NextDouble();
            if (r01 <= extraPickChance)
            {
                ItemData avoid = (extraPickNoDuplicate && firstPick != null) ? firstPick.item : null;
                var extraPick = PickOneWeighted(guaranteedPickOne, rng, avoidItem: avoid);

                if (extraPick != null)
                {
                    results.Add((extraPick.item, RollQty(extraPick.minQty, extraPick.maxQty, rng)));
                }
            }
        }

        // 3) 기존 확률 드랍(포션 등)
        foreach (var e in entries)
        {
            if (e == null || !e.IsValid()) continue;

            var r01 = (float)rng.NextDouble();
            if (r01 > e.chance) continue;

            results.Add((e.item, RollQty(e.minQty, e.maxQty, rng)));
        }

        return results;
    }

    static int RollQty(int min, int max, System.Random rng)
    {
        if (min == max) return min;
        return rng.Next(min, max + 1);
    }

    static WeightedPick PickOneWeighted(List<WeightedPick> list, System.Random rng, ItemData avoidItem)
    {
        if (list == null || list.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null || !p.IsValid()) continue;
            if (avoidItem != null && p.item == avoidItem) continue;
            total += p.weight;
        }

        if (total <= 0f) return null;

        float roll = (float)(rng.NextDouble() * total);
        float acc = 0f;

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null || !p.IsValid()) continue;
            if (avoidItem != null && p.item == avoidItem) continue;

            acc += p.weight;
            if (roll <= acc) return p;
        }

        // fallback
        for (int i = list.Count - 1; i >= 0; i--)
        {
            var p = list[i];
            if (p == null || !p.IsValid()) continue;
            if (avoidItem != null && p.item == avoidItem) continue;
            return p;
        }

        return null;
    }
}
