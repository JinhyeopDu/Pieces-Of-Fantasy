using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character Screen (HSR-ish)
/// - Party button list
/// - Preview (RenderTexture)
/// - Stats + EXP Bar
/// - Materials selection
///   - LevelUp: "캐릭터별 광석 1종" 선택 → EXP/레벨/스탯 프리뷰
///   - Promotion: 승급 재료 다종 (공통 테이블) → 승급 가능 시 스탯 프리뷰
/// - Action button policy
///   - 선택 재료가 1개 이상이면 버튼 활성(승급모드도 동일)
///   - 레벨업 모드: 필요 EXP 도달 시 "LEVEL UP", 아니면 "EXP 적용"
///   - 승급 모드: need 모두 충족 시 "승급", 아니면 "재료 부족"
/// - AutoFill
///   - 레벨업: "승급 전까지" 가능한 만큼 레벨업이 일어나도록 EXP 재료 최대 채움(보유량 한도)
///   - 승급: need 충족하도록 자동 채움(보유량 한도)
/// </summary>
public class CharacterScreenController : MonoBehaviour
{
    private const string GREEN = "#00FF66";

    [Header("UI Root")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private GameObject root;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;

    [Header("Party List")]
    [SerializeField] private Transform partyListRoot;
    [SerializeField] private PartyButtonView partyButtonPrefab;

    [Header("Preview")]
    [SerializeField] private CharacterPreviewRig previewRig;

    [Header("Skill Mini Panel")]
    [SerializeField] private CharacterSkillMiniPanelController skillMiniPanelController;

    [Header("Stats UI (TMP)")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText; // (우측 패널 상단 레벨 표시)
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text atkText;
    [SerializeField] private TMP_Text defText;
    [SerializeField] private TMP_Text spdText;

    [Header("EXP Bar (View)")]
    [SerializeField] private ExpBarView expBarView;

    [Header("Action Button")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;
    [SerializeField] private Color actionNormalColor = Color.black;
    [SerializeField] private Color actionInsufficientColor = Color.red;

    [Header("Action Labels")]
    [SerializeField] private string actionLabelLevelUp = "LEVEL UP";
    [SerializeField] private string actionLabelApplyExp = "EXP 적용";
    [SerializeField] private string actionLabelPromote = "승급";
    [SerializeField] private string actionLabelNeedSelect = "재료 선택";
    [SerializeField] private string actionLabelInsufficient = "재료 부족";

    [Header("Materials UI")]
    [SerializeField] private Transform materialsRoot;
    [SerializeField] private MaterialSlotView materialSlotPrefab;

    [Header("Auto Fill Button")]
    [SerializeField] private Button autoFillButton;

    [Header("Level Up Policy (Temporary)")]
    [Tooltip("광석 1개당 획득 경험치")]
    [SerializeField] private int expPerOre = 100;

    [Header("Ore Items (Assign in Inspector)")]
    [SerializeField] private ItemData oreEmerald;   // Tribi
    [SerializeField] private ItemData oreDiamonds;  // Kisora
    [SerializeField] private ItemData oreRuby;      // StellarWitch

    // -----------------------------
    // 승급 재료(공통)
    // -----------------------------
    [Header("Promotion Materials (Common) - Assign in Inspector")]
    [SerializeField] private ItemData herbGreen;
    [SerializeField] private ItemData herbRed;
    [SerializeField] private ItemData herbBlue;
    [SerializeField] private ItemData herbYellow;

    [SerializeField] private ItemData spikeNormal;
    [SerializeField] private ItemData spikeSharp;

    [SerializeField] private ItemData shellBlue;
    [SerializeField] private ItemData shellOrange;

    [SerializeField] private ItemData stoneShard;
    [SerializeField] private ItemData golemCoreShard;

    [Header("Auto-disable targets while open")]
    [SerializeField] private MonoBehaviour[] disableWhileOpen;

    // CharacterScreenController 내부에 추가
    private const int FinalLevel = 50;

    public bool IsOpen { get; private set; }

    // -----------------------
    // Party selection
    // -----------------------
    private readonly List<PartyButtonView> _partyButtons = new();
    private int _selectedIndex = 0;

    // -----------------------
    // Materials selection
    // -----------------------
    [System.Serializable]
    public class MaterialRequirement
    {
        public ItemData item;
        public int need;      // 승급 모드에서 사용
        public int have;
        public int selected;  // 레벨업 모드: 선택 수량 / 승급 모드: 선택 수량(need 채우기)
        public MaterialSlotView view;
    }

    private readonly List<MaterialRequirement> _materials = new();
    private int _focusedMaterialIndex = -1;

    private enum ScreenMode
    {
        LevelUp = 0,
        Promotion = 10,
    }

    private ScreenMode _mode = ScreenMode.LevelUp;

    //   캐릭터 바꿔도 이전 캐릭터 선택값이 섞이지 않게 캐시
    // key: CharacterRuntime 참조(또는 data)
    private readonly Dictionary<CharacterRuntime, Dictionary<ItemData, int>> _selectedCache = new();

    // -----------------------
    // Unity
    // -----------------------
    private void Awake()
    {
        if (root == null && group != null) root = group.gameObject;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (actionButton != null)
        {
            actionButton.onClick.RemoveListener(OnClickAction);
            actionButton.onClick.AddListener(OnClickAction);
        }

        if (autoFillButton != null)
        {
            autoFillButton.onClick.RemoveListener(OnClickAutoFill);
            autoFillButton.onClick.AddListener(OnClickAutoFill);
        }

        SetOpen(false, instant: true);
    }

    public void Toggle() => SetOpen(!IsOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool open, bool instant = false)
    {
        if (IsOpen == open)
            return;

        // 1) 열기 요청 시: Exclusive UI Lock 확인
        if (open)
        {
            if (GameContext.I != null)
            {
                bool entered = GameContext.I.TryEnterOverlay(UIOverlayKind.CharacterScreen);
                if (!entered)
                    return;
            }
        }

        IsOpen = open;

        if (!instant)
        {
            if (open)
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Open);
            else
                AudioManager.I?.PlaySFX2D(SFXKey.UI_Close);
        }

        // 2) Root 활성화
        if (root != null)
            root.SetActive(true);

        // 3) CanvasGroup 처리
        if (group != null)
        {
            group.alpha = open ? 1f : 0f;
            group.interactable = open;
            group.blocksRaycasts = open;
        }

        // 4) 플레이어 입력/카메라 락
        ApplyDisableTargets(open);
        ApplyCursorPolicy(open);
        GameContext.I?.SetUIBlockingLook(open);

        // 5) 열릴 때 초기화
        if (open)
        {
            BuildPartyButtons();
            SelectFirstCharacter();
            RefreshSelectedCharacterUI(rebuildMaterials: true);
            UpdatePartyButtonSelectionVisual();

            // 튜토리얼: 캐릭터 메뉴 열기 완료
            TutorialManager.I?.CompleteCharacterOpenTutorial();
        }
        else
        {
            _focusedMaterialIndex = -1;
            GameContext.I?.ExitOverlay(UIOverlayKind.CharacterScreen);

            if (root != null)
                root.SetActive(false);
        }
    }

    // =========================
    // Party Buttons
    // =========================
    private void BuildPartyButtons()
    {
        if (partyListRoot == null || partyButtonPrefab == null) return;
        if (GameContext.I == null || GameContext.I.party == null) return;

        for (int i = 0; i < _partyButtons.Count; i++)
            if (_partyButtons[i] != null) Destroy(_partyButtons[i].gameObject);
        _partyButtons.Clear();

        var party = GameContext.I.party;
        for (int i = 0; i < party.Count; i++)
        {
            int idx = i;
            var cr = party[i];
            if (cr == null || cr.data == null) continue;

            var view = Instantiate(partyButtonPrefab, partyListRoot);
            _partyButtons.Add(view);

            view.Set(cr.data.portrait);

            if (view.button != null)
            {
                view.button.onClick.RemoveAllListeners();
                view.button.onClick.AddListener(() =>
                {
                    AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);
                    SelectCharacter(idx);
                });
            }
        }
    }

    private void UpdatePartyButtonSelectionVisual()
    {
        for (int i = 0; i < _partyButtons.Count; i++)
        {
            if (_partyButtons[i] == null) continue;
            _partyButtons[i].SetSelected(i == _selectedIndex);
        }
    }

    private void SelectFirstCharacter()
    {
        _selectedIndex = 0;
        if (GameContext.I == null || GameContext.I.party == null || GameContext.I.party.Count == 0)
            return;

        for (int i = 0; i < GameContext.I.party.Count; i++)
        {
            var c = GameContext.I.party[i];
            if (c != null && c.data != null)
            {
                _selectedIndex = i;
                break;
            }
        }
    }

    private void SelectCharacter(int index)
    {
        //   캐릭터 바꾸기 전, 현재 선택값을 캐시에 저장
        CacheSelectionForCurrentCharacter();

        _selectedIndex = index;

        //   캐릭터 바꾸면 재료를 그 캐릭터 기준으로 리빌드
        RefreshSelectedCharacterUI(rebuildMaterials: true);
        UpdatePartyButtonSelectionVisual();
    }

    private CharacterRuntime GetSelected()
    {
        if (GameContext.I == null || GameContext.I.party == null) return null;
        if (GameContext.I.party.Count == 0) return null;

        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, GameContext.I.party.Count - 1);
        return GameContext.I.party[_selectedIndex];
    }

    // =========================
    // Refresh UI
    // =========================
    private void RefreshSelectedCharacterUI(bool rebuildMaterials)
    {
        var cr = GetSelected();
        if (cr == null || cr.data == null)
        {
            skillMiniPanelController?.Clear();
            return;
        }

        // 0) 스탯 재계산
        cr.RecalculateStats(keepHpRatio: true);

        // 기본 UI는 최고 레벨 여부와 상관없이 먼저 갱신
        if (nameText != null) nameText.text = cr.data.displayName;

        if (previewRig != null)
            previewRig.ShowCharacter(cr.data);

        skillMiniPanelController?.Bind(cr.data);

        // 최고 레벨이면: UI를 “최고 레벨 모드”로 강제 전환 후 종료
        if (IsMaxLevel(cr))
        {
            if (levelText != null) levelText.text = $"Lv. {cr.level}";

            int curLvl = Mathf.Max(1, cr.level);
            int curP = Mathf.Clamp(cr.promotionStage, 0, LevelingPolicy.MaxPromotionStage);

            GetStatsFinalForUI(cr, curLvl, curP, out int curHp, out int curAtk, out int curDef, out int curSpd);

            SetStatTextPreview(hpText, "HP", curHp, 0, showPreview: false);
            SetStatTextPreview(atkText, "ATK", curAtk, 0, showPreview: false);
            SetStatTextPreview(defText, "DEF", curDef, 0, showPreview: false);
            SetStatTextPreview(spdText, "SPD", curSpd, 0, showPreview: false);

            ApplyMaxLevelUI(cr);
            return;
        }

        // 1) 모드 결정
        _mode = LevelingPolicy.IsPromotionRequired(cr.level, cr.promotionStage)
            ? ScreenMode.Promotion
            : ScreenMode.LevelUp;

        // 3) 재료/UI 리빌드
        if (rebuildMaterials)
        {
            if (materialsRoot != null) materialsRoot.gameObject.SetActive(true);
            if (autoFillButton != null) autoFillButton.interactable = true;

            BuildMaterials(cr);
            BuildMaterialsUI();
            RestoreSelectionFromCache(cr);
            RefreshMaterialsUI(cr);
        }

        // 4) 프리뷰(레벨/exp/스탯) + 버튼
        RefreshPreviewAndBars(cr);
    }

    // =========================
    // Stats preview helpers
    // =========================
    private void SetStatTextPreview(TMP_Text t, string label, int value, int delta, bool showPreview)
    {
        if (t == null) return;

        if (!showPreview || delta <= 0)
        {
            t.text = $"{label}: {value}";
            return;
        }

        t.text = $"{label}: {value} <color={GREEN}>(+{delta})</color>";
    }

    private void GetStatsFinalForUI(
        CharacterRuntime cr,
        int level,
        int promotionStage,
        out int hpFinal,
        out int atkFinal,
        out int defFinal,
        out int spdFinal
    )
    {
        var cd = cr.data;

        int lvl = Mathf.Max(1, level);
        int p = Mathf.Clamp(promotionStage, 0, LevelingPolicy.MaxPromotionStage);

        // 베이스 + 레벨 성장
        int hpBase = cd.baseHP + cd.hpPerLevel * (lvl - 1);
        int atkBase = cd.baseATK + cd.atkPerLevel * (lvl - 1);
        int defBase = cd.baseDEF + cd.defPerLevel * (lvl - 1);
        int spdBase = cd.baseSPD + cd.spdPerLevel * (lvl - 1);

        // 승급 누적 퍼센트
        float hpMul = 1f, atkMul = 1f, defMul = 1f, spdMul = 1f;
        for (int i = 0; i < p; i++)
        {
            hpMul *= (1f + cd.promoHpPercent);
            atkMul *= (1f + cd.promoAtkPercent);
            defMul *= (1f + cd.promoDefPercent);
            spdMul *= (1f + cd.promoSpdPercent);
        }

        int hpAfterPromo = Mathf.RoundToInt(hpBase * hpMul);
        int atkAfterPromo = Mathf.RoundToInt(atkBase * atkMul);
        int defAfterPromo = Mathf.RoundToInt(defBase * defMul);
        int spdAfterPromo = Mathf.RoundToInt(spdBase * spdMul);

        // perm/temp 반영 (HP는 CharacterRuntime에 tempHpAdd가 없으므로 maxHp를 신뢰)
        //   "예상치"이므로 maxHp 대신 계산값 기준으로 통일.
        hpFinal = hpAfterPromo; // ← HP는 성장치 프리뷰용 계산값
        atkFinal = atkAfterPromo + cr.permAtkAdd + cr.tempAtkAdd;
        defFinal = defAfterPromo + cr.permDefAdd + cr.tempDefAdd;
        spdFinal = spdAfterPromo + cr.permSpdAdd + cr.tempSpdAdd;
    }

    // =========================
    // Preview/ExpBar/Button unified refresh
    // =========================
    private void RefreshPreviewAndBars(CharacterRuntime cr)
    {
        if (cr == null || cr.data == null) return;

        // 최고 레벨이면: exp/프리뷰 계산 스킵 + 최고 레벨 UI
        if (IsMaxLevel(cr))
        {
            if (levelText != null) levelText.text = $"Lv. {cr.level}";
            ApplyMaxLevelUI(cr);
            return;
        }

        // ---------
        // 레벨업 프리뷰 시뮬
        // ---------
        int addExpTotal = (_mode == ScreenMode.LevelUp) ? GetSelectedTotalExp() : 0;

        int simLvl = cr.level;
        int simExp = cr.exp;

        if (_mode == ScreenMode.LevelUp && addExpTotal > 0)
        {
            LevelingPolicy.ApplyExpAndLevelUp(ref simLvl, ref simExp, addExpTotal, cr.promotionStage);
        }

        int deltaLv = Mathf.Max(0, simLvl - cr.level);

        // ---------
        // 승급 프리뷰 시뮬
        // ---------
        bool canPromote = (_mode == ScreenMode.Promotion) && CanPerformPromotion();

        // 현재 스탯
        int curLvl = Mathf.Max(1, cr.level);
        int curP = Mathf.Clamp(cr.promotionStage, 0, LevelingPolicy.MaxPromotionStage);

        GetStatsFinalForUI(cr, curLvl, curP, out int curHp, out int curAtk, out int curDef, out int curSpd);

        // 다음 스탯(레벨업/승급 중 하나)
        int nextLvl = curLvl;
        int nextP = curP;

        if (_mode == ScreenMode.LevelUp && deltaLv > 0)
            nextLvl = simLvl;
        else if (_mode == ScreenMode.Promotion && canPromote)
            nextP = Mathf.Clamp(curP + 1, 0, LevelingPolicy.MaxPromotionStage);

        GetStatsFinalForUI(cr, nextLvl, nextP, out int nextHp, out int nextAtk, out int nextDef, out int nextSpd);

        int dHp = nextHp - curHp;
        int dAtk = nextAtk - curAtk;
        int dDef = nextDef - curDef;
        int dSpd = nextSpd - curSpd;

        bool showStatPreview =
            (_mode == ScreenMode.LevelUp && deltaLv > 0 && addExpTotal > 0)
            || (_mode == ScreenMode.Promotion && canPromote);

        // 레벨 텍스트: CAP 제거, (+n)만
        if (levelText != null)
        {
            if (_mode == ScreenMode.LevelUp && deltaLv > 0)
                levelText.text = $"Lv. {cr.level} <color={GREEN}>(+{deltaLv})</color>";
            else
                levelText.text = $"Lv. {cr.level}";
        }

        SetStatTextPreview(hpText, "HP", curHp, dHp, showStatPreview);
        SetStatTextPreview(atkText, "ATK", curAtk, dAtk, showStatPreview);
        SetStatTextPreview(defText, "DEF", curDef, dDef, showStatPreview);
        SetStatTextPreview(spdText, "SPD", curSpd, dSpd, showStatPreview);

        // ExpBar
        if (expBarView != null)
        {
            if (_mode == ScreenMode.LevelUp)
            {
                int needExp = LevelingPolicy.GetNeedExpForNextLevel(cr.level, cr.promotionStage);
                int curExp = Mathf.Clamp(cr.exp, 0, needExp);
                int remain = Mathf.Max(0, needExp - curExp);

                int addForFill = Mathf.Clamp(addExpTotal, 0, remain);

                expBarView.Set(
                    cr.level,
                    curExp,
                    addForFill,
                    needExp,
                    isCap: false,
                    previewLevelDelta: deltaLv,
                    addExpTotalForText: addExpTotal,
                    isMaxLevel: false
                );
            }
            else
            {
                expBarView.Set(
                    cr.level,
                    0, 0, 1,
                    isCap: true,
                    previewLevelDelta: 0,
                    addExpTotalForText: 0,
                    isMaxLevel: false
                );
            }
        }

        RefreshActionButton(cr);
    }

    // =========================
    // Materials build by mode
    // =========================
    private void BuildMaterials(CharacterRuntime cr)
    {
        _materials.Clear();
        _focusedMaterialIndex = -1;

        if (cr == null || cr.data == null) return;

        if (_mode == ScreenMode.LevelUp)
        {
            ItemData ore = GetOreFor(cr.data);
            if (ore == null) return;

            _materials.Add(new MaterialRequirement
            {
                item = ore,
                need = 0,
                have = CountItem(ore),
                selected = 0,
                view = null
            });
        }
        else
        {
            //   승급 테이블(모든 캐릭터 공통)
            // stage: 0->1(레벨10), 1->2(레벨20), 2->3(레벨30), 3->4(레벨40)
            // (여기서는 stage 값만으로 필요 재료를 결정)
            AddPromotionSetForStage(cr.promotionStage);
        }
    }

    private void AddPromotionSetForStage(int stage)
    {
        stage = Mathf.Clamp(stage, 0, LevelingPolicy.MaxPromotionStage);

        // stage 0: 1차 승급(레벨 10): 허브 4종 10개씩
        if (stage == 0)
        {
            AddPromotionMat(herbGreen, 10);
            AddPromotionMat(herbRed, 10);
            AddPromotionMat(herbBlue, 10);
            AddPromotionMat(herbYellow, 10);
            return;
        }

        // stage 1: 2차 승급(레벨 20): 가시 2종
        if (stage == 1)
        {
            AddPromotionMat(spikeNormal, 10);
            AddPromotionMat(spikeSharp, 5);
            return;
        }

        // stage 2: 3차 승급(레벨 30): 껍질 조각 2종
        if (stage == 2)
        {
            AddPromotionMat(shellBlue, 10);
            AddPromotionMat(shellOrange, 5);
            return;
        }

        // stage 3: 마지막 승급(레벨 40): 일반 돌 조각 8, 골렘의 핵 조각 3
        if (stage >= 3)
        {
            AddPromotionMat(stoneShard, 8);
            AddPromotionMat(golemCoreShard, 3);
            return;
        }
    }

    private void AddPromotionMat(ItemData item, int need)
    {
        if (item == null) return;
        if (need <= 0) return;

        _materials.Add(new MaterialRequirement
        {
            item = item,
            need = need,
            have = CountItem(item),
            selected = 0,
            view = null
        });
    }

    // =========================
    // Materials UI: spawn slots
    // =========================
    private void BuildMaterialsUI()
    {
        if (materialsRoot == null || materialSlotPrefab == null) return;

        for (int i = materialsRoot.childCount - 1; i >= 0; i--)
            Destroy(materialsRoot.GetChild(i).gameObject);

        for (int i = 0; i < _materials.Count; i++)
        {
            var slot = Instantiate(materialSlotPrefab, materialsRoot);

            var m = _materials[i];
            m.view = slot;
            _materials[i] = m;

            slot.OnClick -= HandleMaterialSlotClick;
            slot.OnClick += HandleMaterialSlotClick;

            slot.OnClickMinus -= HandleMaterialSlotMinus;
            slot.OnClickMinus += HandleMaterialSlotMinus;
        }
    }

    private void HandleMaterialSlotClick(MaterialSlotView view)
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        int idx = FindMaterialIndexByView(view);
        if (idx < 0) return;

        _focusedMaterialIndex = idx;
        ChangeSelected(idx, +1);

        var cr = GetSelected();
        if (cr != null)
        {
            RefreshMaterialsUI(cr);
            RefreshPreviewAndBars(cr);
        }
    }

    private void HandleMaterialSlotMinus(MaterialSlotView view)
    {
        int idx = FindMaterialIndexByView(view);
        if (idx < 0) return;

        _focusedMaterialIndex = idx;
        ChangeSelected(idx, -1);

        var cr = GetSelected();
        if (cr != null)
        {
            RefreshMaterialsUI(cr);
            RefreshPreviewAndBars(cr);
        }
    }

    private int FindMaterialIndexByView(MaterialSlotView v)
    {
        if (v == null) return -1;
        for (int i = 0; i < _materials.Count; i++)
        {
            if (_materials[i].view == v) return i;
        }
        return -1;
    }//_materials

    private void RefreshMaterialsUI(CharacterRuntime cr)
    {
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            m.have = CountItem(m.item);

            // 모드에 따라 클램프 정책 적용
            m.selected = Mathf.Clamp(m.selected, 0, GetMaxSelectableByAllConstraints(cr, i, m.have));
            _materials[i] = m;

            ApplyMaterialSlotVisual(cr, i);
        }
    }

    private void ApplyMaterialSlotVisual(CharacterRuntime cr, int index)
    {
        var m = _materials[index];
        var v = m.view;
        if (v == null) return;

        v.SetIcon(m.item != null ? m.item.icon : null);

        // 표시 정책:
        // - LevelUp: selected/have
        // - Promotion: have/need
        if (v.countText != null)
        {
            if (_mode == ScreenMode.Promotion && m.need > 0)
                v.countText.text = $"{m.have}/{m.need}";
            else
                v.countText.text = $"{m.selected}/{m.have}";
        }

        // SelectedFrame 정책 변경
        // - LevelUp: selected > 0
        // - Promotion: have >= need (need 충족/초과면 ON)
        bool selectedOn;
        if (_mode == ScreenMode.Promotion)
        {
            selectedOn = (m.need > 0) && (m.have >= m.need);
        }
        else
        {
            selectedOn = (m.selected > 0);
        }

        v.SetSelectedFrame(selectedOn);

        // Minus 정책 (요청 반영: 승급은 항상 OFF)
        if (_mode == ScreenMode.Promotion)
        {
            v.SetMinusVisible(false);
            return;
        }

        // LevelUp: 포커스 기반
        bool isFocused = (index == _focusedMaterialIndex);
        bool canShowMinus = (m.selected > 0);

        // AutoFill로 채웠을 때 포커스가 없으면 첫 "선택된 슬롯"을 포커스로 간주
        if (_focusedMaterialIndex < 0 && canShowMinus)
            isFocused = true;

        v.SetMinusVisible(isFocused && canShowMinus);

    }

    // =========================
    // EXP selection constraints
    // =========================
    private void ChangeSelected(int index, int delta)
    {
        if (index < 0 || index >= _materials.Count) return;

        var cr = GetSelected();
        if (cr == null) return;

        var m = _materials[index];

        int have = CountItem(m.item);
        int maxSelectable = GetMaxSelectableByAllConstraints(cr, index, have);

        m.selected = Mathf.Clamp(m.selected + delta, 0, maxSelectable);
        m.have = have;

        _materials[index] = m;

        CacheSelectionFor(cr, m.item, m.selected);
    }

    private int GetMaxSelectableByAllConstraints(CharacterRuntime cr, int materialIndex, int have)
    {
        int maxByHave = Mathf.Max(0, have);

        if (_mode == ScreenMode.LevelUp)
        {
            if (!CanSelectExpMaterial(cr))
                return 0;

            //   "승급 전까지 최대 레벨업"을 위해서는
            //    '현재 레벨 needExp' 기준이 아니라,
            //    '승급 전까지 필요한 총 EXP' 기준으로도 제한해야 함.
            // 하지만: 실제 ApplyExpAndLevelUp가 캡에서 멈추므로,
            // 선택 제한은 UX만 담당 → 너무 큰 값도 허용 가능.
            // 네 요구사항 중 "과도한 경험치 재료 사용 방지"는
            // "현재 레벨 바 기준"이 아니라 "승급 전까지 가능한 만큼"이므로
            // 여기서는 보유량 한도만 두고,
            // AutoFill에서 승급 전까지 필요한 만큼만 채우게 한다.
            return maxByHave;
        }

        // Promotion 모드: need까지만 (have도 같이 적용)
        int need = Mathf.Max(0, _materials[materialIndex].need);
        return Mathf.Min(maxByHave, need);
    }

    // =========================
    // Action button state + click
    // =========================
    private void RefreshActionButton(CharacterRuntime cr)
    {
        if (cr == null) return;

        if (_mode == ScreenMode.Promotion)
        {
            // 승급 모드: 선택 개념 없음 → 재료 충족 여부로만 판단
            bool canPromote = CanPerformPromotion();

            if (actionButton != null)
                actionButton.interactable = canPromote;   // 충족이면만 클릭 가능 (원하면 항상 true로 해도 됨)

            if (actionButtonText != null)
            {
                actionButtonText.text = canPromote ? actionLabelPromote : actionLabelInsufficient;
                actionButtonText.color = canPromote ? actionNormalColor : actionInsufficientColor;
            }
            return;
        }

        // ===== LevelUp 모드는 기존 로직 유지 =====
        bool hasAnySelection = GetAnySelectedCount() > 0;

        if (actionButton != null)
            actionButton.interactable = hasAnySelection;

        if (actionButtonText != null)
        {
            if (!hasAnySelection)
            {
                actionButtonText.text = actionLabelNeedSelect;
                actionButtonText.color = actionInsufficientColor;
                return;
            }

            int needExp = LevelingPolicy.GetNeedExpForNextLevel(cr.level, cr.promotionStage);
            int curExp = Mathf.Clamp(cr.exp, 0, needExp);
            int addExp = GetSelectedTotalExp();

            bool willLevelUp = (curExp + addExp) >= needExp;

            actionButtonText.text = willLevelUp ? actionLabelLevelUp : actionLabelApplyExp;
            actionButtonText.color = actionNormalColor;
        }
    }

    private int GetAnySelectedCount()
    {
        int sum = 0;
        for (int i = 0; i < _materials.Count; i++)
            sum += Mathf.Max(0, _materials[i].selected);
        return sum;
    }

    private bool CanPerformPromotion()
    {
        // 승급: 보유(have) >= 필요(need)
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.need <= 0) continue;

            // RefreshMaterialsUI에서 m.have를 최신화하고 있으니 그 값 사용
            if (m.have < m.need) return false;
        }
        return true;
    }

    private void OnClickAction()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        var cr = GetSelected();
        if (cr == null || cr.data == null) return;

        //if (GetAnySelectedCount() <= 0)
        //{
        //    RefreshPreviewAndBars(cr);
        //    return;
        //}

        if (_mode == ScreenMode.Promotion)
        {
            if (!CanPerformPromotion())
            {
                RefreshPreviewAndBars(cr);
                return;
            }

            DoPromotion(cr);

            cr.RecalculateStats(keepHpRatio: true);

            // 승급 후: 리빌드(다음 단계 재료로 전환)
            RefreshSelectedCharacterUI(rebuildMaterials: true);
            return;
        }

        // ===== LevelUp Mode =====
        if (!LevelingPolicy.CanGainExp(cr.level, cr.promotionStage))
        {
            RefreshSelectedCharacterUI(rebuildMaterials: true);
            return;
        }

        int addExpTotal = GetSelectedTotalExp();
        if (addExpTotal <= 0)
        {
            RefreshPreviewAndBars(cr);
            return;
        }

        // 1) 재료 소모(선택된 개수만)
        GameContext.I?.BeginInventoryBatch();
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.item == null) continue;
            if (m.selected <= 0) continue;
            GameContext.I?.RemoveItem(m.item, m.selected);
        }
        GameContext.I?.EndInventoryBatch();

        // 2) EXP 반영 + 자동 레벨업(승급 전까지 가능)
        int beforeLevel = cr.level;
        int beforeHp = cr.hp;
        int beforeMaxHp = Mathf.Max(1, cr.maxHp);
        bool wasAlive = beforeHp > 0;

        int lvl = cr.level;
        int exp = cr.exp;
        LevelingPolicy.ApplyExpAndLevelUp(ref lvl, ref exp, addExpTotal, cr.promotionStage);
        cr.level = lvl;
        cr.exp = exp;

        if (cr.level > beforeLevel)
        {
            TutorialManager.I?.CompleteLevelUpTutorial();
        }

        // 3) 레벨업 후 스탯 재계산
        cr.RecalculateStats(keepHpRatio: false);

        int gainedMaxHp = Mathf.Max(0, cr.maxHp - beforeMaxHp);

        if (wasAlive)
        {
            // 살아있는 캐릭터만 증가한 최대 HP만큼 회복
            cr.hp = Mathf.Clamp(beforeHp + gainedMaxHp, 0, cr.maxHp);
        }
        else
        {
            // 죽은 캐릭터는 절대 부활하지 않음
            cr.hp = 0;
        }

        // 퀘스트 자동 완료 갱신
        QuestManager.I?.RefreshAutoCompleteConditions();

        // 4) 선택 초기화 + 캐시도 초기화(소모했으니)
        ClearCurrentSelectionCache(cr);

        // 5) UI 갱신(모드 바뀔 수 있으니 리빌드)
        RefreshSelectedCharacterUI(rebuildMaterials: true);
    }

    private void DoPromotion(CharacterRuntime cr)
    {
        // 1) 재료 소모(need만큼)
        GameContext.I?.BeginInventoryBatch();
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.item == null) continue;
            if (m.need <= 0) continue;
            GameContext.I?.RemoveItem(m.item, m.need);
        }
        GameContext.I?.EndInventoryBatch();

        int beforeHp = cr.hp;
        int beforeMaxHp = Mathf.Max(1, cr.maxHp);
        bool wasAlive = beforeHp > 0;

        // 2) 승급 반영
        cr.promotionStage = Mathf.Clamp(cr.promotionStage + 1, 0, LevelingPolicy.MaxPromotionStage);

        // 승급 시 exp는 0으로 초기화(정책)
        cr.exp = 0;

        cr.RecalculateStats(keepHpRatio: false);

        int gainedMaxHp = Mathf.Max(0, cr.maxHp - beforeMaxHp);

        if (wasAlive)
        {
            // 살아있는 캐릭터만 증가한 최대 HP만큼 회복
            cr.hp = Mathf.Clamp(beforeHp + gainedMaxHp, 0, cr.maxHp);
        }
        else
        {
            // 죽은 캐릭터는 절대 부활하지 않음
            cr.hp = 0;
        }

        // 퀘스트 자동 완료 갱신
        QuestManager.I?.RefreshAutoCompleteConditions();

        // 선택 캐시 초기화
        ClearCurrentSelectionCache(cr);
    }

    // =========================
    // AutoFill
    // =========================
    private void OnClickAutoFill()
    {
        AudioManager.I?.PlaySFX2D(SFXKey.UI_Click);

        var cr = GetSelected();
        if (cr == null || cr.data == null) return;

        if (_materials.Count == 0)
        {
            RefreshSelectedCharacterUI(rebuildMaterials: true);
            cr = GetSelected();
            if (cr == null) return;
        }

        if (_mode == ScreenMode.LevelUp)
        {
            AutoFillLevelUp(cr);
        }
        else
        {
            AutoFillPromotion(cr);
        }

        RefreshMaterialsUI(cr);
        RefreshPreviewAndBars(cr);
    }

    private void AutoFillLevelUp(CharacterRuntime cr)
    {
        if (!CanSelectExpMaterial(cr)) return;
        if (_materials.Count <= 0) return;

        var m = _materials[0];
        if (m.item == null) return;

        int have = CountItem(m.item);
        if (have <= 0) return;

        //   승급 전까지 가능한 만큼 레벨이 오르도록
        // 목표: ApplyExpAndLevelUp로 시뮬해보고
        // "다음 승급 필요 상태(= PromotionRequired)"가 될 때까지 EXP를 넣는다.
        int per = Mathf.Max(1, expPerOre);

        int lvl = cr.level;
        int exp = cr.exp;

        // 안전 루프: 재료 수량 한도 내에서만
        int use = 0;
        int remainItem = have;

        // 너무 긴 루프 방지(보유량이 수천개일 수 있으니)
        // 보유량만큼 반복은 괜찮지만, 그래도 cap을 둔다.
        int guard = Mathf.Min(have, 9999);

        while (remainItem > 0 && guard-- > 0)
        {
            int testLvl = lvl;
            int testExp = exp;

            // 1개 추가 시뮬
            LevelingPolicy.ApplyExpAndLevelUp(ref testLvl, ref testExp, per, cr.promotionStage);

            // 레벨/exp 변화가 없으면 더 넣어봤자 의미 없음(캡)
            if (testLvl == lvl && testExp == exp)
                break;

            // 반영
            lvl = testLvl;
            exp = testExp;
            use++;
            remainItem--;

            //   승급 필요 상태가 되면 여기서 멈춤
            if (LevelingPolicy.IsPromotionRequired(lvl, cr.promotionStage))
                break;
        }

        // 적용
        m.selected = Mathf.Clamp(use, 0, have);
        m.have = have;

        _materials[0] = m;
        _focusedMaterialIndex = 0; //   AutoFill 후 minus 버튼 보이게

        CacheSelectionFor(cr, m.item, m.selected);
    }

    private void AutoFillPromotion(CharacterRuntime cr)
    {
        // 각 슬롯: need 채우기(보유량 한도)
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            int have = CountItem(m.item);
            int need = Mathf.Max(0, m.need);

            m.have = have;
            m.selected = Mathf.Min(have, need);
            _materials[i] = m;

            CacheSelectionFor(cr, m.item, m.selected);
        }

        //   AutoFill 후 minus 버튼 보이게
        _focusedMaterialIndex = FindFirstSelectableIndex();
    }

    private int FindFirstSelectableIndex()
    {
        for (int i = 0; i < _materials.Count; i++)
            if (_materials[i].selected > 0) return i;
        return -1;
    }

    // =========================
    // Item mapping + inventory helpers
    // =========================
    private ItemData GetOreFor(CharacterData cd)
    {
        if (cd == null) return null;

        switch (cd.secretArtType)
        {
            case SecretArtType.HealParty: return oreEmerald;
            case SecretArtType.DefBuffParty: return oreDiamonds;
            case SecretArtType.GainBattleSP: return oreRuby;
        }
        return oreEmerald;
    }

    private int CountItem(ItemData item)
    {
        if (item == null) return 0;
        if (GameContext.I == null || GameContext.I.inventory == null || GameContext.I.inventory.items == null) return 0;

        int total = 0;
        var list = GameContext.I.inventory.items;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].item == item)
                total += list[i].count;
        }
        return total;
    }

    public bool CanSelectExpMaterial(CharacterRuntime c)
    {
        if (c == null) return false;

        // 레벨캡이면 exp 재료 못 씀
        if (!LevelingPolicy.CanGainExp(c.level, c.promotionStage))
            return false;

        int need = LevelingPolicy.GetNeedExpForNextLevel(c.level, c.promotionStage);

        // exp가 need 이상이면(= 꽉 참) 선택 불가
        return c.exp < need;
    }

    private int GetExpPerItem(ItemData item)
    {
        return Mathf.Max(1, expPerOre);
    }

    private int GetSelectedTotalExp()
    {
        if (_mode != ScreenMode.LevelUp) return 0;

        int sum = 0;
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.item == null) continue;
            sum += m.selected * GetExpPerItem(m.item);
        }
        return sum;
    }

    // =========================
    // 캐릭터별 선택 캐시
    // =========================
    private void CacheSelectionForCurrentCharacter()
    {
        var cr = GetSelected();
        if (cr == null) return;

        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.item == null) continue;
            CacheSelectionFor(cr, m.item, m.selected);
        }
    }

    private void CacheSelectionFor(CharacterRuntime cr, ItemData item, int selected)
    {
        if (cr == null || item == null) return;

        if (!_selectedCache.TryGetValue(cr, out var map) || map == null)
        {
            map = new Dictionary<ItemData, int>();
            _selectedCache[cr] = map;
        }

        map[item] = Mathf.Max(0, selected);
    }

    private void RestoreSelectionFromCache(CharacterRuntime cr)
    {
        if (cr == null) return;
        if (!_selectedCache.TryGetValue(cr, out var map) || map == null) return;

        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            if (m.item == null) continue;

            if (map.TryGetValue(m.item, out int sel))
                m.selected = Mathf.Max(0, sel);

            _materials[i] = m;
        }
    }

    private void ClearCurrentSelectionCache(CharacterRuntime cr)
    {
        if (cr == null) return;

        if (_selectedCache.TryGetValue(cr, out var map) && map != null)
            map.Clear();

        // 리스트 쪽도 0으로
        for (int i = 0; i < _materials.Count; i++)
        {
            var m = _materials[i];
            m.selected = 0;
            _materials[i] = m;
        }

        _focusedMaterialIndex = -1;
    }

    // =========================
    // Exploration lock
    // =========================
    private void ApplyDisableTargets(bool open)
    {
        if (disableWhileOpen != null && disableWhileOpen.Length > 0)
        {
            for (int i = 0; i < disableWhileOpen.Length; i++)
            {
                if (disableWhileOpen[i] == null) continue;
                disableWhileOpen[i].enabled = !open;
            }
            return;
        }

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

    private bool IsMaxLevel(CharacterRuntime cr)
    {
        if (cr == null) return false;
        return cr.level >= FinalLevel;
    }

    private void ApplyMaxLevelUI(CharacterRuntime cr)
    {
        // 1) 재료/오토 숨김
        if (materialsRoot != null) materialsRoot.gameObject.SetActive(false);
        if (autoFillButton != null) autoFillButton.interactable = false;

        // 2) 액션 버튼 잠금 + 텍스트
        if (actionButton != null) actionButton.interactable = false;
        if (actionButtonText != null)
        {
            actionButtonText.text = "최고 레벨";
            actionButtonText.color = actionNormalColor;
        }

        // 3) ExpBarView -> 최고 레벨 표기
        if (expBarView != null)
        {
            // 아래 ExpBarView 수정안에서 isMaxLevel을 지원한다고 가정
            expBarView.Set(
                level: cr.level,
                curExp: 0,
                addExp: 0,
                needExp: 1,
                isCap: false,
                previewLevelDelta: 0,
                addExpTotalForText: 0,
                isMaxLevel: true
            );
        }
    }
}