using UnityEngine;

public class InventoryController : MonoBehaviour
{
    [Header("UI Root")]
    [SerializeField] private CanvasGroup inventoryGroup;
    [SerializeField] private GameObject inventoryRoot;

    [Header("View")]
    [Tooltip("ItemSlot을 생성/관리하는 View")]
    [SerializeField] private InventoryView view;

    [Header("Auto-disable targets while open")]
    [Tooltip("비워두면 자동으로 PlayerControllerHumanoid를 찾아서 비활성화합니다.")]
    [SerializeField] private MonoBehaviour[] disableWhileOpen;

    [Header("Child Popups")]
    [Tooltip("인벤토리 위에서 뜨는 아이템 사용 팝업(우선 닫힘 대상). 비워두면 자식에서 자동 탐색합니다.")]
    [SerializeField] private ItemUsePopupController itemUsePopup;

    [SerializeField] private string _selectedItemId = null; // 마지막 선택 아이템 id
    [SerializeField] private int _selectedIndex = -1;        // (보조) 마지막 선택 인덱스

    [Header("Sort")]
    [SerializeField] private bool _sortEnabled = false;      // 정렬 ON/OFF
    [SerializeField] private SortSpinner sortSpinner;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        // inventoryRoot 미지정 시 CanvasGroup이 붙은 오브젝트를 루트로 사용
        if (inventoryRoot == null && inventoryGroup != null)
            inventoryRoot = inventoryGroup.gameObject;

        // ★ sort 상태 복원
        if (GameContext.I != null)
            _sortEnabled = GameContext.I.inventorySortEnabled;

        if (view != null) view.SetSortEnabled(_sortEnabled);
        if (sortSpinner != null) sortSpinner.SetActive(_sortEnabled);

        // 시작은 항상 닫힌 상태
        SetOpen(false, instant: true);
    }

    private void Start()
    {
        if (GameContext.I != null)
            _sortEnabled = GameContext.I.inventorySortEnabled;

        if (view != null) view.SetSortEnabled(_sortEnabled);
        if (sortSpinner != null) sortSpinner.SetActive(_sortEnabled);
    }

    /* =========================
     * Public API
     * ========================= */
    /// <summary>
    /// 인벤 토글 키(Esc/R 등)가 이 함수를 부르는 전제.
    /// 팝업이 떠 있으면 "팝업을 먼저 닫고" 이번 입력은 소비한다.
    /// </summary>
    public void Toggle()
    {
        if (itemUsePopup == null)
            itemUsePopup = GetComponentInChildren<ItemUsePopupController>(true);

        // 팝업이 떠 있으면: 팝업 먼저 닫고 이번 입력 소비
        if (IsPopupOpen())
        {
            ItemUsePopupController.ConsumeCloseHotkeyThisFrame();
            itemUsePopup.Close();
            return;
        }

        // 팝업이 방금 닫힌 프레임이면 인벤 토글 소비
        if (ItemUsePopupController.LastCloseByHotkeyFrame == Time.frameCount)
            return;

        SetOpen(!IsOpen);
    }

    // View에서 슬롯 클릭 시 호출하도록 공개 API 추가
    public void RememberSelection(ItemData item, int index)
    {
        _selectedItemId = (item != null) ? item.id : null;
        _selectedIndex = index;
    }

    // Sort 버튼에서 호출할 공개 API
    public void ToggleSort()
    {
        _sortEnabled = !_sortEnabled;
        view?.SetSortEnabled(_sortEnabled);
        view?.RefreshCurrentTabPreserveSelection(_selectedItemId, _selectedIndex);
    }


    public void Open() => SetOpen(true);

    public void Close()
    {
        // 인벤을 닫을 때 팝업이 켜져 있으면 같이 닫아준다 (재오픈 시 팝업 잔상 방지)
        if (IsPopupOpen())
            itemUsePopup.Close();

        SetOpen(false);
    }

    /* =========================
     * Core
     * ========================= */
    public void SetOpen(bool open, bool instant = false)
    {
        // (안전) 팝업 참조가 비어있으면 자식에서 자동 탐색
        if (itemUsePopup == null)
            itemUsePopup = GetComponentInChildren<ItemUsePopupController>(true);

        // 1) 팝업이 방금(이번 프레임) 핫키로 닫혔으면,
        //    이번 프레임의 인벤 close/toggle 요청은 무조건 소비
        if (!open && ItemUsePopupController.LastCloseByHotkeyFrame == Time.frameCount)
            return;

        // 2) "인벤 닫기" 요청이 들어왔는데 팝업이 떠 있으면:
        //    인벤은 닫지 말고 팝업만 닫고 이번 입력은 소비(=Esc 1번은 팝업만)
        if (!open && IsPopupOpen())
        {
            ItemUsePopupController.ConsumeCloseHotkeyThisFrame();
            itemUsePopup.Close();
            return;
        }

        // 열기 요청이면: 다른 UI가 열려있으면 거부
        if (open)
        {
            if (GameContext.I != null && !GameContext.I.TryEnterOverlay(UIOverlayKind.Inventory))
                return;
        }

        // 중복 호출 방지
        if (IsOpen == open)
            return;

        if (!instant)
        {
            if (open)
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);
            else
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);
        }

        IsOpen = open;

        if (!open)
        {
            GameContext.I?.ExitOverlay(UIOverlayKind.Inventory);
        }

        // 루트는 항상 활성 상태 유지 (CanvasGroup으로 제어)
        if (inventoryRoot != null)
            inventoryRoot.SetActive(true);

        // UI 표시/입력 제어
        if (inventoryGroup != null)
        {
            inventoryGroup.alpha = open ? 1f : 0f;
            inventoryGroup.interactable = open;
            inventoryGroup.blocksRaycasts = open;
        }

        // 인벤을 여는 순간, 슬롯이 아직 생성 안 됐으면 생성
        if (open)
        {
            if (GameContext.I != null)
                GameContext.I.OnInventoryChanged += HandleInventoryChanged; // ★ 변경

            // ★ View가 "어떤 아이템이 클릭됐는지" 알려주도록 구독
            if (view != null)
            {
                view.OnItemSelected -= HandleItemSelected;
                view.OnItemSelected += HandleItemSelected;
            }

            view?.SetOpenState(true);
            view?.Open();

            // ★ 처음 열 때도 마지막 선택 복원
            RestoreSelection();

            // 튜토리얼: 인벤토리 열기 완료
            TutorialManager.I?.CompleteInventoryTutorial();
        }
        else
        {
            if (GameContext.I != null)
                GameContext.I.OnInventoryChanged -= HandleInventoryChanged;

            if (view != null)
                view.OnItemSelected -= HandleItemSelected;

            view?.SetOpenState(false);
        }

        // 탐험 조작 잠금/해제
        ApplyDisableTargets(open);

        // 커서/카메라 룩 잠금 (인벤 열림 동안 카메라가 흔들리지 않게)
        ApplyCursorPolicy(open);

        // SSOT 플래그로 Look 입력 차단
        // (팝업은 Close()에서 인벤이 열려있는지 보고 플래그를 유지하도록 되어있음)
        GameContext.I?.SetUIBlockingLook(open);
    }

    private void HandleItemSelected(ItemData item, int index)
    {
        _selectedItemId = item != null ? item.id : null;
        _selectedIndex = index;
    }

    private void HandleInventoryChanged()
    {
        // 1) 뷰 갱신
        view.RefreshCurrentTab();

        // 2) 마지막 선택 복원
        RestoreSelection();
    }

    private void RestoreSelection()
    {
        if (view == null) return;

        // id 우선 복원
        if (!string.IsNullOrEmpty(_selectedItemId))
        {
            if (view.TrySelectByItemId(_selectedItemId))
                return;
        }

        // id가 없거나 못 찾으면 인덱스로 보조 복원
        if (_selectedIndex >= 0)
        {
            if (view.TrySelectByIndex(_selectedIndex))
                return;
        }

        // 그래도 안되면(아이템이 사라짐 등) 뷰 기본 정책으로
        view.SelectFirstIfAny();
    }

    private bool IsPopupOpen()
    {
        return itemUsePopup != null && itemUsePopup.gameObject.activeInHierarchy;
    }

    /* =========================
 * Exploration Lock
 * ========================= */
    private void ApplyDisableTargets(bool open)
    {
        // 1) Inspector로 지정된 대상이 있으면 그것을 우선 사용
        if (disableWhileOpen != null && disableWhileOpen.Length > 0)
        {
            for (int i = 0; i < disableWhileOpen.Length; i++)
            {
                if (disableWhileOpen[i] == null) continue;
                disableWhileOpen[i].enabled = !open;
            }
            return;
        }

        // 2) 없으면 런타임에 자동으로 플레이어 컨트롤러를 찾아서 처리
        var players = FindObjectsOfType<PlayerControllerHumanoid>(includeInactive: true);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            players[i].enabled = !open;
        }
    }

    private void ApplyCursorPolicy(bool open)
    {
        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnClickSort()
    {
        if (view == null) return;

        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        // 1) 정렬 토글 (SSOT는 컨트롤러)
        _sortEnabled = !_sortEnabled;

        // ★ SSOT 저장
        if (GameContext.I != null)
            GameContext.I.inventorySortEnabled = _sortEnabled;

        // 2) View에 반영
        view.SetSortEnabled(_sortEnabled);

        // 3) 리스트 리프레시 + 선택 복원
        view.RefreshCurrentTab();
        RestoreSelection();

        // 4) 스피너 ON/OFF
        if (sortSpinner != null)
            sortSpinner.SetActive(_sortEnabled);
    }
}
