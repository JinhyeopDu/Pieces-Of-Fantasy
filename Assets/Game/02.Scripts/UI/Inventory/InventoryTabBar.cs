using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryTabBar : MonoBehaviour
{
    [System.Serializable]
    public class TabEntry
    {
        public ItemCategory category;
        public Button button;
        public GameObject selectedBg; // Tab 오브젝트 내부 SelectedBG
    }

    [Header("Target")]
    [SerializeField] private InventoryView inventoryView;

    [Header("Tabs")]
    [SerializeField] private List<TabEntry> tabs = new();

    [Header("Default")]
    [SerializeField] private ItemCategory defaultCategory = ItemCategory.Material;

    private ItemCategory current;

    private void Awake()
    {
        // 버튼 리스너 연결
        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i; // 클로저 캡처 방지
            if (tabs[idx].button != null)
            {
                tabs[idx].button.onClick.AddListener(() =>
                {
                    Select(tabs[idx].category);
                });
            }
        }

        // 시작 탭 권한은 TabBar가 가진다
        Select(defaultCategory);
    }

    public void Select(ItemCategory category)
    {
        current = category;

        // 1) BG ON/OFF
        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = (tabs[i].category == category);
            if (tabs[i].selectedBg != null)
                tabs[i].selectedBg.SetActive(on);
        }

        // 2) 실제 필터 적용
        if (inventoryView != null)
            inventoryView.ApplyCategory(category);
    }
}
