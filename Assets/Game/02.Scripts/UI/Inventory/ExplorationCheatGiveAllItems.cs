using System.Collections.Generic;
using UnityEngine;

public class ExplorationCheatGiveAllItems : MonoBehaviour
{
    [Header("Key")]
    [SerializeField] private KeyCode giveKey = KeyCode.F9;

    [Header("Give Policy")]
    [SerializeField] private int giveCountPerItem = 30;

    [Header("Items to give (assign in Inspector)")]
    [SerializeField] private List<ItemData> allItems = new();

    [Header("Guard")]
    [SerializeField] private bool explorationOnly = true;
    [SerializeField] private string explorationSceneName = "Exploration";

    private void Update()
    {
        if (explorationOnly)
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (active != explorationSceneName) return;
        }

        if (!Input.GetKeyDown(giveKey)) return;

        GiveAll();
    }

    private void GiveAll()
    {
        if (GameContext.I == null)
        {
            Debug.LogWarning("[Cheat] GameContext.I is null");
            return;
        }

        if (allItems == null || allItems.Count == 0)
        {
            Debug.LogWarning("[Cheat] allItems is empty");
            return;
        }

        int added = 0;

        GameContext.I.BeginInventoryBatch();
        for (int i = 0; i < allItems.Count; i++)
        {
            var item = allItems[i];
            if (item == null) continue;

            GameContext.I.AddItem(item, giveCountPerItem);
            added++;
        }
        GameContext.I.EndInventoryBatch();

        Debug.Log($"[Cheat] Gave {giveCountPerItem} x {added} items (key={giveKey})");
    }
}