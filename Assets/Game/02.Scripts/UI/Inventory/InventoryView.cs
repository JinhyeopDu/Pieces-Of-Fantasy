using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private DetailPanelView detailPanel;

    [Header("Startup")]
    [SerializeField] private ItemCategory defaultCategory = ItemCategory.Material;

    private bool generatedOnce = false;
    private ItemSlotView currentSelectedSlot;
    private ItemCategory currentCategory;

    private bool _isOpen = false;
    private bool _dirty = false;

    // 슬롯 풀
    private readonly List<ItemSlotView> _slotPool = new();
    private Coroutine _selectCo;

    // 마지막 선택 유지용
    [SerializeField] private string _lastSelectedItemId = null;

    // 정렬 토글
    private bool _sortEnabled = false;

    // (선택) 외부(InventoryController)가 selection을 저장하고 싶으면 이벤트로 받을 수 있게
    public event Action<ItemData, int> OnItemSelected;

    public void SetSortEnabled(bool enabled)
    {
        _sortEnabled = enabled;
    }

    public void ToggleSortMode()
    {
        _sortEnabled = !_sortEnabled;
    }

    public ItemCategory CurrentCategoryOrDefault
    {
        get { return (int)currentCategory == 0 ? defaultCategory : currentCategory; }
    }

    public void Open()
    {
        EnsureGenerated();
        ApplyCategory(currentCategory == 0 ? defaultCategory : currentCategory);
    }

    public void EnsureGenerated()
    {
        if (generatedOnce) return;
        generatedOnce = true;

        // 초기 탭 설정
        currentCategory = defaultCategory;
    }

    public void SetOpenState(bool open)
    {
        _isOpen = open;

        if (_isOpen && _dirty)
        {
            _dirty = false;
            ApplyCategory(CurrentCategoryOrDefault);
        }
    }

    // ─────────────────────────────────────
    // 탭 버튼 OnClick 깨짐(Missing) 해결용 브릿지
    // 스크린샷의 <Missing InventoryView.Tab_Materials> 를 복구하기 위해 제공
    // 버튼 OnClick에 이 함수들을 연결해두면 됨
    // ─────────────────────────────────────
    public void Tab_Materials() => ApplyCategory(ItemCategory.Material);
    public void Tab_Consumable() => ApplyCategory(ItemCategory.Consumable);
    public void Tab_Equipment() => ApplyCategory(ItemCategory.Equipment);
    public void Tab_Quest() => ApplyCategory(ItemCategory.Quest);
    public void Tab_KeyItem() => ApplyCategory(ItemCategory.KeyItem);
    public void Tab_Etc() => ApplyCategory(ItemCategory.Etc);

    /// <summary>
    /// TabBar/버튼이 호출: 카테고리 필터 적용 + 슬롯 생성 + 첫 슬롯 자동 선택
    /// </summary>
    public void ApplyCategory(ItemCategory category)
    {
        if (!generatedOnce) EnsureGenerated();

        currentCategory = category;

        if (GameContext.I == null)
        {
            ClearSlots();
            detailPanel?.Clear();
            return;
        }

        // raw 가져오기
        List<ItemStack> raw = GameContext.I.GetItemsByCategory(category);

        // 표시용 리스트(정렬/그룹핑 적용)
        List<ItemStack> list = BuildDisplayList(raw);

        GenerateSlots(list);

        // 변경: 첫 선택 강제 대신 “복원 → 실패 시 첫 선택”
        if (!TryRestoreSelection())
        {
            if (list != null && list.Count > 0) RequestSelectFirstSlot();
            else detailPanel?.Clear();
        }
    }

    /// <summary>
    /// 인벤 데이터 변경 시(아이템 추가/소비) 현재 탭을 다시 그림
    /// </summary>
    public void RefreshCurrentTab()
    {
        if (GameContext.I == null)
        {
            ClearSlots();
            detailPanel?.Clear();
            return;
        }

        // 닫혀있으면 리빌드하지 말고 dirty만
        if (!_isOpen)
        {
            _dirty = true;
            return;
        }

        ApplyCategory(currentCategory);
    }

    private void GenerateSlots(List<ItemStack> items)
    {
        if (contentRoot == null || itemSlotPrefab == null) return;
        if (items == null) items = new List<ItemStack>();

        // 1) 풀 사이즈 확보
        while (_slotPool.Count < items.Count)
        {
            var go = Instantiate(itemSlotPrefab, contentRoot);
            var slot = go.GetComponent<ItemSlotView>();
            _slotPool.Add(slot);
        }

        // 2) 필요한 만큼 바인딩 + 활성화
        for (int i = 0; i < items.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot == null) continue;

            slot.gameObject.SetActive(true);
            slot.Bind(items[i], this);
            slot.SetSelected(false);
        }

        // 3) 남는 슬롯은 비활성화
        for (int i = items.Count; i < _slotPool.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot == null) continue;
            slot.gameObject.SetActive(false);
        }

        currentSelectedSlot = null;
    }

    private void ClearSlots()
    {
        if (_selectCo != null) { StopCoroutine(_selectCo); _selectCo = null; }

        for (int i = 0; i < _slotPool.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot == null) continue;
            slot.gameObject.SetActive(false);
        }
        currentSelectedSlot = null;
    }

    private void RequestSelectFirstSlot()
    {
        if (_selectCo != null) StopCoroutine(_selectCo);
        _selectCo = StartCoroutine(SelectFirstSlotNextFrame());
    }

    private IEnumerator SelectFirstSlotNextFrame()
    {
        yield return null;

        // 풀에서 active 첫 슬롯 찾기
        for (int i = 0; i < _slotPool.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot != null && slot.gameObject.activeSelf)
            {
                SelectSlot(slot);
                yield break;
            }
        }
    }

    public void SelectSlot(ItemSlotView slot)
    {
        if (slot == null) return;
        if (currentSelectedSlot == slot) return;

        if (currentSelectedSlot != null)
            currentSelectedSlot.SetSelected(false);

        currentSelectedSlot = slot;
        currentSelectedSlot.SetSelected(true);

        // 마지막 선택 저장
        var item = slot.BoundStack.item;
        _lastSelectedItemId = item != null ? item.id : null;

        // visible index 계산(활성 슬롯 기준 0..N-1)
        int visibleIndex = GetVisibleIndexOf(slot);

        // 컨트롤러에도 통지(복원 보조 인덱스까지 저장되게)
        OnItemSelected?.Invoke(item, visibleIndex);

        detailPanel?.Show(slot.BoundStack);
    }

    private int GetVisibleIndexOf(ItemSlotView target)
    {
        int visible = -1;
        for (int i = 0; i < _slotPool.Count; i++)
        {
            var s = _slotPool[i];
            if (s == null || !s.gameObject.activeSelf) continue;

            visible++;
            if (s == target) return visible;
        }
        return -1;
    }

    // InventoryController가 "정렬/리프레시 후 선택 유지"를 요청할 때 쓰기 좋음
    public void RefreshCurrentTabPreserveSelection(string preferItemId, int preferIndex = -1)
    {
        if (!string.IsNullOrEmpty(preferItemId))
            _lastSelectedItemId = preferItemId;

        RefreshCurrentTab();

        // RefreshCurrentTab 안에서 ApplyCategory를 타니까,
        // ApplyCategory가 끝난 뒤 TryRestoreSelection이 돌도록 구성해야 함.
    }

    // 외부에서 itemId로 선택 복원
    public bool TrySelectByItemId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        for (int i = 0; i < _slotPool.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot == null || !slot.gameObject.activeSelf) continue;

            var st = slot.BoundStack;
            if (st.item != null && st.item.id == itemId)
            {
                SelectSlot(slot);
                return true;
            }
        }
        return false;
    }

    public bool TrySelectByIndex(int index)
    {
        if (index < 0) return false;

        int visible = -1;
        for (int i = 0; i < _slotPool.Count; i++)
        {
            var slot = _slotPool[i];
            if (slot == null || !slot.gameObject.activeSelf) continue;

            visible++;
            if (visible == index)
            {
                SelectSlot(slot);
                return true;
            }
        }
        return false;
    }

    public void SelectFirstIfAny()
    {
        RequestSelectFirstSlot();
    }

    // ApplyCategory 끝에서 호출할 “선택 복원”
    private bool TryRestoreSelection()
    {
        if (!string.IsNullOrEmpty(_lastSelectedItemId))
            return TrySelectByItemId(_lastSelectedItemId);

        return false;
    }

    // 표시용 리스트 만들 때 “그룹핑 + 정렬”
    private List<ItemStack> BuildDisplayList(List<ItemStack> raw)
    {
        if (raw == null) raw = new List<ItemStack>();
        if (!_sortEnabled) return raw;

        // 1) 같은 아이템끼리 합치기(총량 합산)
        var grouped = raw
            .Where(s => s.item != null && s.count > 0)
            .GroupBy(s => s.item)
            .Select(g => new ItemStack { item = g.Key, count = g.Sum(x => x.count) })
            .ToList();

        // 2) 표시용 스택을 maxStack 기준으로 "쪼개기" (★ maxStack 보존)
        var result = new List<ItemStack>(grouped.Count);

        for (int i = 0; i < grouped.Count; i++)
        {
            var it = grouped[i].item;
            int total = grouped[i].count;

            int max = (it != null) ? Mathf.Max(1, it.maxStack) : 1;

            while (total > 0)
            {
                int take = Mathf.Min(max, total);
                result.Add(new ItemStack { item = it, count = take });
                total -= take;
            }
        }

        // 3) 정렬: IT_숫자 우선 → (표시명/이름) → id → (같은 아이템이면 큰 스택 우선)
        result.Sort((a, b) =>
        {
            int ao = GetItemOrderNumber(a.item);
            int bo = GetItemOrderNumber(b.item);
            if (ao != bo) return ao.CompareTo(bo);

            string an = GetItemName(a.item);
            string bn = GetItemName(b.item);
            int nameCmp = string.Compare(an, bn, StringComparison.Ordinal);
            if (nameCmp != 0) return nameCmp;

            // 같은 아이템이 여러 스택으로 쪼개졌으면 큰 스택이 위로 (선택 UX)
            if (a.item == b.item)
            {
                int countCmp = b.count.CompareTo(a.count);
                if (countCmp != 0) return countCmp;
            }

            string aid = a.item != null ? a.item.id : "";
            string bid = b.item != null ? b.item.id : "";
            int idCmp = string.Compare(aid, bid, StringComparison.Ordinal);
            if (idCmp != 0) return idCmp;

            return 0; // 모든 경로에서 return 보장
        });

        return result; // 반드시 result 반환
    }

    private static int GetItemOrderNumber(ItemData item)
    {
        if (item == null) return int.MaxValue;

        string s = !string.IsNullOrEmpty(item.id) ? item.id : item.name;
        if (string.IsNullOrEmpty(s)) return int.MaxValue;

        if (s.StartsWith("IT_"))
        {
            int i = 3; // "IT_" 다음
            int num = 0;
            int digits = 0;

            while (i < s.Length && char.IsDigit(s[i]))
            {
                num = (num * 10) + (s[i] - '0');
                i++;
                digits++;
            }

            if (digits > 0) return num;
        }

        return int.MaxValue;
    }

    private static string GetItemName(ItemData item)
    {
        if (item == null) return "";
        return string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;
    }

    private static int GetConsumablePriority(ItemData item)
    {
        if (item == null) return 9;

        // 효과 A/B 중 하나라도 해당되면 그 그룹으로 분류
        bool IsHeal(ConsumableEffectType t) =>
            t == ConsumableEffectType.HealHP ||
            t == ConsumableEffectType.RestoreSecretArt;

        bool IsRevive(ConsumableEffectType t) =>
            t == ConsumableEffectType.Revive;

        bool IsBuff(ConsumableEffectType t) =>
            t == ConsumableEffectType.BuffAttack ||
            t == ConsumableEffectType.BuffDefense ||
            t == ConsumableEffectType.BuffSpeed ||
            t == ConsumableEffectType.BuffMaxHP;

        var a = item.effectA != null ? item.effectA.type : ConsumableEffectType.None;
        var b = item.effectB != null ? item.effectB.type : ConsumableEffectType.None;

        if (IsHeal(a) || IsHeal(b)) return 0;   // 회복
        if (IsRevive(a) || IsRevive(b)) return 1; // 부활
        if (IsBuff(a) || IsBuff(b)) return 2;   // 버프

        return 9;
    }


}
