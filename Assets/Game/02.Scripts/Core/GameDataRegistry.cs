using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataRegistry", menuName = "PoF/Game Data Registry")]
public class GameDataRegistry : ScriptableObject
{
    [Header("Master Data")]
    public List<CharacterData> characters = new();
    public List<ItemData> items = new();
    public List<QuestData> quests = new();

    private Dictionary<string, CharacterData> _characterMap;
    private Dictionary<string, ItemData> _itemMap;
    private Dictionary<string, QuestData> _questMap;

    public void BuildMaps()
    {
        _characterMap = new Dictionary<string, CharacterData>();
        _itemMap = new Dictionary<string, ItemData>();
        _questMap = new Dictionary<string, QuestData>();

        for (int i = 0; i < characters.Count; i++)
        {
            var c = characters[i];
            if (c == null || string.IsNullOrEmpty(c.id)) continue;
            _characterMap[c.id] = c;
        }

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null || string.IsNullOrEmpty(item.id)) continue;
            _itemMap[item.id] = item;
        }

        for (int i = 0; i < quests.Count; i++)
        {
            var q = quests[i];
            if (q == null)
            {
                Debug.LogWarning($"[GameDataRegistry] quests[{i}] is null");
                continue;
            }

            string key = q.questId != null ? q.questId.Trim() : string.Empty;

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning($"[GameDataRegistry] quests[{i}] asset={q.name} has empty questId");
                continue;
            }

            if (_questMap.ContainsKey(key))
            {
                Debug.LogWarning($"[GameDataRegistry] Duplicate questId detected: key='{key}', asset='{q.name}'");
            }

            _questMap[key] = q;
        }

    }

    public CharacterData GetCharacter(string id)
    {
        if (_characterMap == null) BuildMaps();
        if (string.IsNullOrEmpty(id)) return null;

        _characterMap.TryGetValue(id, out var result);
        return result;
    }

    public ItemData GetItem(string id)
    {
        if (_itemMap == null) BuildMaps();
        if (string.IsNullOrEmpty(id)) return null;

        _itemMap.TryGetValue(id, out var result);
        return result;
    }

    public QuestData GetQuest(string id)
    {
        if (_questMap == null) BuildMaps();
        if (string.IsNullOrEmpty(id)) return null;

        string key = id.Trim();
        _questMap.TryGetValue(key, out var result);

        if (result == null)
        {
            string knownKeys = string.Join(", ", _questMap.Keys);
            Debug.LogWarning(
                $"[GameDataRegistry] GetQuest failed | requested='{key}' | registry='{name}' | knownKeys=[{knownKeys}]"
            );
        }
        else
        {
           // Debug.Log($"[GameDataRegistry] GetQuest success | requested='{key}' | asset='{result.name}'");
        }

        return result;
    }
}