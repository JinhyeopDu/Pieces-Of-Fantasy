using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemUsePopupController : MonoBehaviour
{
    [Header("UI - Party")]
    public Transform partyListRoot;
    public PartyUseCardView partyCardPrefab;

    [Header("UI - Header")]
    public Image itemIcon;
    public TMP_Text itemNameText;
    public TMP_Text ownedCountText;

    [Header("UI - Buttons")]
    public Button btnConfirm;
    public Button btnClose;

    [Header("UI - Effect Desc")]
    public TMP_Text effectDescText;

    [Header("UI - Secret Points")]
    public GameObject secretPointRoot;   // 비술 아이템일 때만 켬
    public Image[] secretPointIcons;     // 5개 고정(0~4)
    public float secretBlinkSpeed = 6f;  // 깜박임 속도

    [Header("UI - Fail Message")]
    public TMP_Text failMessageText;
    public float failMessageDuration = 1.2f;
    [Header("UI - Fail Banner")]
    public FailBannerAnimator failBanner;

    private Coroutine _failCo;

    private Coroutine _secretBlinkCo;

    private ItemData currentItem;
    private int currentTargetIndex;

    private readonly List<PartyUseCardView> _cards = new();

    private InventoryController _cachedInventory;

    // Hotkey consume flag: InventoryController can check this to avoid toggling in the same frame.
    public static int LastCloseByHotkeyFrame { get; private set; } = -1;

    public static void ConsumeCloseHotkeyThisFrame()
    {
        LastCloseByHotkeyFrame = Time.frameCount;
    }

    void Awake()
    {
        // 버튼을 Inspector에서 OnClick 연결 안 해도 되게 스크립트에서 연결
        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveListener(OnClickConfirm);
            btnConfirm.onClick.AddListener(OnClickConfirm);
        }

        if (btnClose != null)
        {
            btnClose.onClick.RemoveListener(OnClickClose);
            btnClose.onClick.AddListener(OnClickClose);
        }

    }

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        if (IsCloseHotkeyPressedThisFrame())
        {
            ConsumeCloseHotkeyThisFrame();
            OnClickClose();
        }
    }

    private static bool IsCloseHotkeyPressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return false;
        // Esc + R 둘 다 팝업 닫기 우선권
        return Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.R);
#endif
    }

    public void Open(ItemData item)
    {
        if (item == null) return;

        if (_failCo != null)
        {
            StopCoroutine(_failCo);
            _failCo = null;
        }

        if (failMessageText != null)
            failMessageText.gameObject.SetActive(false);

        failBanner?.HideImmediate();

        StopSecretBlink();
        if (secretPointRoot != null)
            secretPointRoot.SetActive(false);

        currentItem = item;
        currentTargetIndex = FindFirstInteractableTargetIndex(item);

        gameObject.SetActive(true);
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);

        RefreshHeader();
        Refresh();

        GameContext.I?.SetUIBlockingLook(true);
    }

    private int FindFirstInteractableTargetIndex(ItemData item)
    {
        if (GameContext.I == null || GameContext.I.party == null)
            return 0;

        if (item == null || item.targetPolicy != ItemTargetPolicy.SingleAlly)
            return 0;

        for (int i = 0; i < GameContext.I.party.Count; i++)
        {
            if (IsInteractableTarget(i, item))
                return i;
        }

        return 0;
    }

    public void Close()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);

        if (_failCo != null)
        {
            StopCoroutine(_failCo);
            _failCo = null;
        }

        if (failMessageText != null)
            failMessageText.gameObject.SetActive(false);

        failBanner?.HideImmediate();

        gameObject.SetActive(false);
        currentItem = null;

        if (_secretBlinkCo != null) { StopCoroutine(_secretBlinkCo); _secretBlinkCo = null; }
        if (secretPointRoot != null) secretPointRoot.SetActive(false);

        bool keepBlock = false;
        var inv = GetInventory();
        if (inv != null) keepBlock = inv.IsOpen;

        GameContext.I?.SetUIBlockingLook(keepBlock);
    }

    void RefreshHeader()
    {
        if (currentItem == null) return;

        if (itemIcon != null)
        {
            itemIcon.sprite = currentItem.icon;
            itemIcon.enabled = (itemIcon.sprite != null);
            itemIcon.preserveAspect = true;
        }

        if (itemNameText != null)
            itemNameText.text = string.IsNullOrEmpty(currentItem.displayName) ? currentItem.name : currentItem.displayName;

        // 인벤에서 보유 수량 계산
        int owned = 0;
        if (GameContext.I != null && GameContext.I.inventory != null && GameContext.I.inventory.items != null)
        {
            var list = GameContext.I.inventory.items;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].item == currentItem)
                    owned += list[i].count;
            }
        }

        if (ownedCountText != null)
            ownedCountText.text = $"보유 : {owned}";
    }

    void Refresh()
    {
        if (GameContext.I == null || currentItem == null)
            return;

        var party = GameContext.I.party;
        if (party == null) return;

        // currentTargetIndex 안전 보정
        currentTargetIndex = Mathf.Clamp(currentTargetIndex, 0, Mathf.Max(0, party.Count - 1));

        var preview = GameContext.I.PreviewUseItem(currentItem, currentTargetIndex);

        EnsureCardCount(party.Count);

        // 1) 효과 설명
        if (effectDescText != null)
            effectDescText.text = BuildEffectDesc(currentItem, preview);

        // 2) 비술 여부
        bool isSecret = IsSecretRestoreItem(currentItem);

        // 3) 비술 UI
        ApplySecretPointUI(isSecret, preview);

        // 4) 아이템 타입 판정
        bool isSingleTarget = (currentItem.targetPolicy == ItemTargetPolicy.SingleAlly);
        bool isAllParty = (currentItem.targetPolicy == ItemTargetPolicy.AllParty);

        bool isBuffItem = IsBuffItem(currentItem);
        bool isSingleHealItem = IsSingleHealItem(currentItem);

        // 5) 선택 가능 여부
        // - 비술은 카드 선택 없음
        // - SingleAlly(단일)인 경우만 타겟 선택 가능 (힐/버프/부활 등)
        bool selectable = (!isSecret) && isSingleTarget;

        // 6) 전체 아이템은 “전원 선택 프레임 ON” (힐이든 버프든)
        bool highlightAll = (!isSecret) && isAllParty;

        for (int i = 0; i < party.Count; i++)
        {
            var c = party[i];
            if (c == null) continue;

            // ─────────────────────────────
            // A) “이미 적용됨” 판정 (버프 아이템일 때만 의미)
            // ─────────────────────────────
            bool alreadyApplied = false;
            if (isBuffItem && isSingleTarget)
                alreadyApplied = IsAlreadyAppliedForCharacter(i, currentItem);

            // ─────────────────────────────
            // B) 클릭 가능(Interactable) 판정
            // ─────────────────────────────
            bool interactable = false;

            if (selectable)
            {
                // Single 힐: HP가 max면 비활성
                // Single 버프: alreadyApplied면 비활성
                // Single 부활: 죽은 캐릭터만(네 IsInteractableTarget에 이미 반영돼있음)
                interactable = IsInteractableTarget(i, currentItem);
            }
            else
            {
                // AllParty / Secret 은 “카드 클릭 자체는 없음”
                interactable = false;
            }

            // ─────────────────────────────
            // C) 오버레이를 보여줄지 정책
            // ─────────────────────────────
            // - AllParty(전체 적용)는 어둡게 가리지 않는 게 UX적으로 더 낫다(네 의견 100% 동의)
            // - Single 힐에서만 “못 고르는 애”를 오버레이로 보여주면 직관적
            // - Single 버프는 alreadyApplied인 애만 오버레이/배지로 표시
            bool showDisabledOverlay = false;

            if (!highlightAll && selectable)
            {
                // 단일 선택 모드일 때만, 비활성 타겟을 오버레이로 표시
                // (Single 힐: maxHP인 캐릭터 / Single 버프: alreadyApplied 캐릭터)
                showDisabledOverlay = !interactable;
            }

            // ─────────────────────────────
            // D) SelectedFrame 정책
            // ─────────────────────────────
            bool isSelected =
                highlightAll ? true :
                (selectable && i == currentTargetIndex);

            // ─────────────────────────────
            // E) Bind
            // ─────────────────────────────
            // 주의: 여기 Bind는 “11개 인자” 버전으로 호출(마지막 bool = showDisabledOverlay)
            _cards[i].Bind(
                i,
                c.data.displayName,
                c.data.portrait,
                preview.hpBefore[i],
                preview.hpAfter[i],
                c.maxHp,
                isSelected,
                (selectable && interactable) ? OnSelectTarget : null,
                interactable,
                alreadyApplied,
                showDisabledOverlay
            );

            // 비술이면 HP 프리뷰 숨김
            _cards[i].SetHpPreviewVisible(!isSecret);
        }
    }

    private bool IsAlreadyAppliedForCharacter(int index, ItemData item)
    {
        if (GameContext.I == null || item == null) return false;
        if (string.IsNullOrEmpty(item.id)) return false;

        var party = GameContext.I.party;
        if (party == null || index < 0 || index >= party.Count) return false;

        var c = party[index];
        if (c == null) return false;

        // 공격/방어 둘 다 있는 빵이면, 어느 쪽이든 이미 기록되어 있으면 "이미 적용"으로 취급하는 게 UX상 자연스러움
        bool atk = (c.tempAtkSources != null && c.tempAtkSources.Contains(item.id));
        bool def = (c.tempDefSources != null && c.tempDefSources.Contains(item.id));
        bool spd = (c.tempSpdSources != null && c.tempSpdSources.Contains(item.id));
        bool mhp = (c.tempMaxHpSources != null && c.tempMaxHpSources.Contains(item.id));

        return atk || def || spd || mhp;
    }

    private bool IsSecretRestoreItem(ItemData item)
    {
        if (item == null) return false;
        return item.effectA.type == ConsumableEffectType.RestoreSecretArt
            || item.effectB.type == ConsumableEffectType.RestoreSecretArt;
    }

    private string BuildEffectDesc(ItemData item, GameContext.ItemUsePreview preview)
    {
        if (item != null && !string.IsNullOrEmpty(item.description))
            return item.description;


        if (preview.secretArtDelta > 0)
            return $"사용 후 즉시 아군의 비술 포인트를 {preview.secretArtDelta}pt 회복한다";

        int maxDelta = 0;
        if (preview.hpDelta != null)
        {
            for (int i = 0; i < preview.hpDelta.Length; i++)
                if (preview.hpDelta[i] > maxDelta) maxDelta = preview.hpDelta[i];
        }

        if (maxDelta > 0)
        {
            return preview.needsTargetSelect
                ? "사용 후 즉시 선택한 아군의 HP를 회복한다"
                : "사용 후 즉시 아군 전체의 HP를 회복한다";
        }

        return "사용 시 효과가 발동한다";
    }

    // ================================================
    // [추가] 비술 포인트 UI + 깜박임 함수 추가
    // ================================================
    private void ApplySecretPointUI(bool active, GameContext.ItemUsePreview preview)
    {
        if (secretPointRoot == null) return;

        if (!active)
        {
            secretPointRoot.SetActive(false);
            StopSecretBlink();
            return;
        }

        if (secretPointIcons == null || secretPointIcons.Length == 0)
        {
            Debug.LogWarning("[ItemUsePopup] secretPointIcons not assigned.");
            secretPointRoot.SetActive(false);
            StopSecretBlink();
            return;
        }

        secretPointRoot.SetActive(true);

        int max = GameContext.I != null ? GameContext.I.secretArtPointsMax : 5;
        int before = Mathf.Clamp(preview.secretArtBefore, 0, max);
        int after = Mathf.Clamp(preview.secretArtAfter, 0, max);

        DrawSecretIconsStatic(before, after);

        StopSecretBlink();
        if (after > before)
            _secretBlinkCo = StartCoroutine(BlinkSecret(before, after));
    }
    private void StopSecretBlink()
    {
        if (_secretBlinkCo != null) { StopCoroutine(_secretBlinkCo); _secretBlinkCo = null; }
    }

    private void DrawSecretIconsStatic(int before, int after)
    {
        for (int i = 0; i < secretPointIcons.Length; i++)
        {
            var img = secretPointIcons[i];
            if (img == null) continue;

            // after까지는 보이게(최대면 전부 보임), 나머지는 희미하게
            float a = (i < after) ? 1f : 0.2f;
            var c = img.color; c.a = a; img.color = c;
        }
    }

    private System.Collections.IEnumerator BlinkSecret(int before, int after)
    {
        while (true)
        {
            float a = Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.unscaledTime * secretBlinkSpeed) + 1f) * 0.5f);

            for (int i = 0; i < secretPointIcons.Length; i++)
            {
                var img = secretPointIcons[i];
                if (img == null) continue;

                bool fixedOn = (i < before);
                bool blinking = (i >= before && i < after);

                // ON/OFF 표시 정책(이미지처럼 "회복분만 깜박임", 나머진 어둡게)
                float alpha = fixedOn ? 1f : (blinking ? a : 0.2f);

                var c = img.color;
                c.a = alpha;
                img.color = c;
            }

            yield return null;
        }
    }
    // ================================================
    // [추가] 비술 포인트 UI + 깜박임 함수 추가
    // ================================================



    void OnSelectTarget(int index)
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);
        currentTargetIndex = index;
        Refresh();
    }

    void EnsureCardCount(int count)
    {
        while (_cards.Count < count)
        {
            var card = Instantiate(partyCardPrefab, partyListRoot);
            _cards.Add(card);
        }
    }

    // 버튼 이벤트
    public void OnClickConfirm()
    {
        Debug.Log("[Popup] Confirm clicked");

        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        if (GameContext.I == null || currentItem == null) return;

        var fail = GameContext.I.CheckCanUseItem(currentItem, currentTargetIndex);

        if (fail != ItemUseFailReason.None)
        {
            ShowFailMessage(fail);
            return;
        }

        bool used = GameContext.I.TryUseItem(currentItem, currentTargetIndex);

        if (used)
        {
            AudioManager.I?.PlaySFX2D(SFXKey.Item_Use, 1f, 0.05f);

            RefreshHeader(); // 보유 수량 갱신
            Close();
        }
        else
        {
            Debug.Log("[ItemUsePopup] Use failed.");
        }
    }

    private void ShowFailMessage(ItemUseFailReason reason)
    {
        string msg = reason switch
        {
            ItemUseFailReason.NoHpTarget => "회복할 캐릭터가 없습니다.",
            ItemUseFailReason.NoDeadTarget => "사망한 플레이어가 없습니다.",
            ItemUseFailReason.SecretArtFull => "비술 포인트가 이미 최대입니다.",
            ItemUseFailReason.AlreadyBuffed => "이미 적용된 효과입니다.",
            _ => "사용할 수 없습니다."
        };

        Debug.Log($"[Popup] ShowFailMessage called. failBanner={(failBanner != null)}");

        if (failBanner != null)
        {
            failBanner.Play(msg);
        }
        else
        {
            Debug.LogWarning("[ItemUsePopup] failBanner is null. Fallback to failMessageText.");

            if (failMessageText == null) return;

            if (_failCo != null)
                StopCoroutine(_failCo);

            _failCo = StartCoroutine(FailMessageRoutine(msg));
        }
    }

    private IEnumerator FailMessageRoutine(string msg)
    {
        if (failMessageText == null) yield break;

        failMessageText.text = msg;
        failMessageText.gameObject.SetActive(true);

        yield return new WaitForSecondsRealtime(failMessageDuration);

        failMessageText.gameObject.SetActive(false);
    }

    private InventoryController GetInventory()
    {
        if (_cachedInventory == null || _cachedInventory.Equals(null))
            _cachedInventory = FindObjectOfType<InventoryController>(true);

        return _cachedInventory;
    }

    private bool IsBuffItem(ItemData item)
    {
        if (item == null) return false;

        bool IsBuffType(ConsumableEffectType t) =>
            t == ConsumableEffectType.BuffAttack ||
            t == ConsumableEffectType.BuffDefense ||
            t == ConsumableEffectType.BuffSpeed ||
            t == ConsumableEffectType.BuffMaxHP;

        return IsBuffType(item.effectA.type) || IsBuffType(item.effectB.type);
    }

    private bool IsSingleHealItem(ItemData item)
    {
        if (item == null) return false;
        if (item.targetPolicy != ItemTargetPolicy.SingleAlly) return false;

        return item.effectA.type == ConsumableEffectType.HealHP ||
               item.effectB.type == ConsumableEffectType.HealHP;
    }

    private bool IsInteractableTarget(int index, ItemData item)
    {
        if (GameContext.I == null || item == null) return false;
        var party = GameContext.I.party;
        if (party == null || index < 0 || index >= party.Count) return false;

        var c = party[index];
        if (c == null) return false;

        // 비술 아이템은 타겟 선택 없음 (카드는 그냥 표시용)
        if (IsSecretRestoreItem(item)) return false;

        // 단일 힐: HP가 살아있고, maxHp 미만일 때만 가능
        if (IsSingleHealItem(item))
            return (c.hp > 0 && c.hp < c.maxHp);

        // 단일 부활(원하면 추가)
        if (item.targetPolicy == ItemTargetPolicy.SingleAlly &&
            (item.effectA.type == ConsumableEffectType.Revive || item.effectB.type == ConsumableEffectType.Revive))
            return (c.hp <= 0);

        // 단일 버프: 이미 적용된 캐릭터는 불가
        if (item.targetPolicy == ItemTargetPolicy.SingleAlly && IsBuffItem(item))
            return !IsAlreadyAppliedForCharacter(index, item);

        // 그 외 단일 아이템은 일단 가능 처리(정책 있으면 여기 확장)
        if (item.targetPolicy == ItemTargetPolicy.SingleAlly)
            return true;

        return false;
    }

    public void OnClickClose()
    {
        Close();
    }
}