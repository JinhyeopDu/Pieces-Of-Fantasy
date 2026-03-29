using UnityEngine;
using UnityEngine.InputSystem;

public class ExplorationUIHotkeys : MonoBehaviour
{
    public bool ConsumedEscapeThisFrame { get; private set; }

    [SerializeField] private InventoryController inventory;
    [SerializeField] private InteractSensor interactSensor;
    [SerializeField] private PlayerControllerHumanoid player;
    [SerializeField] private CharacterScreenController characterUI;
    [SerializeField] public GatherListUIController gatherUI;
    [SerializeField] private SettingsPanelController settingsPanel;
    [SerializeField] private MiniMapController miniMapController;
    [SerializeField] private QuestPanelController questPanel;

    private void Awake()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<InventoryController>();

        if (gatherUI == null)
            gatherUI = FindFirstObjectByType<GatherListUIController>();

        if (interactSensor == null)
            interactSensor = FindFirstObjectByType<InteractSensor>();

        if (player == null)
            player = FindFirstObjectByType<PlayerControllerHumanoid>();

        if (characterUI == null)
            characterUI = FindFirstObjectByType<CharacterScreenController>();

        if (settingsPanel == null)
            settingsPanel = FindFirstObjectByType<SettingsPanelController>(FindObjectsInactive.Include);

        if (miniMapController == null)
            miniMapController = FindFirstObjectByType<MiniMapController>(FindObjectsInactive.Include);

        if (questPanel == null)
            questPanel = FindFirstObjectByType<QuestPanelController>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        ConsumedEscapeThisFrame = false;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool inventoryOpen = (inventory != null && inventory.IsOpen);
        bool characterOpen = (characterUI != null && characterUI.IsOpen);
        bool questOpen = (questPanel != null && questPanel.IsOpen);
        bool settingsOpen = (settingsPanel != null && settingsPanel.IsPanelOpen());
        bool worldMapOpen = (miniMapController != null && miniMapController.IsWorldMapOpen);

        // R : 인벤
        if (kb.rKey.wasPressedThisFrame)
        {
            if (inventory != null)
            {
                if (inventoryOpen)
                {
                    inventory.Toggle();
                }
                else if (!IsAnyOtherUIOpen(UIKind.Inventory))
                {
                    StopPlayerForUIOpen();
                    inventory.Toggle();
                }
            }
        }

        // C : 캐릭터 창
        if (kb.cKey.wasPressedThisFrame)
        {
            if (characterUI != null)
            {
                if (characterOpen)
                {
                    characterUI.Close();
                }
                else if (!IsAnyOtherUIOpen(UIKind.Character))
                {
                    StopPlayerForUIOpen();
                    characterUI.Open();
                }
            }
        }

        // Q : 퀘스트 창
        if (kb.qKey.wasPressedThisFrame)
        {
            if (questPanel != null)
            {
                if (questOpen)
                {
                    questPanel.Close();
                }
                else if (!IsAnyOtherUIOpen(UIKind.Quest))
                {
                    StopPlayerForUIOpen();
                    questPanel.Open();
                }
            }
        }

        // M : 월드맵
        if (kb.mKey.wasPressedThisFrame)
        {
            if (miniMapController != null)
            {
                if (worldMapOpen)
                {
                    miniMapController.ToggleWorldMap();
                }
                else if (!IsAnyOtherUIOpen(UIKind.WorldMap))
                {
                    StopPlayerForUIOpen();
                    miniMapController.ToggleWorldMap();
                }
            }
        }

        // Z : 설정창
        if (kb.zKey.wasPressedThisFrame)
        {
            if (settingsPanel != null)
            {
                if (settingsOpen)
                {
                    settingsPanel.Close();
                }
                else if (!IsAnyOtherUIOpen(UIKind.Settings))
                {
                    StopPlayerForUIOpen();
                    settingsPanel.Open();
                }
            }
        }

        // ESC : 우선 인벤 → 캐릭터 → 퀘스트 → 월드맵 → 설정창
        if (kb.escapeKey.wasPressedThisFrame)
        {
            if (inventoryOpen)
            {
                ConsumedEscapeThisFrame = true;
                inventory.Toggle();
                return;
            }

            if (characterOpen)
            {
                ConsumedEscapeThisFrame = true;
                characterUI.Close();
                return;
            }

            if (questOpen)
            {
                ConsumedEscapeThisFrame = true;
                questPanel.Close();
                return;
            }

            if (worldMapOpen)
            {
                ConsumedEscapeThisFrame = true;
                miniMapController.ToggleWorldMap();
                return;
            }

            if (settingsOpen)
            {
                ConsumedEscapeThisFrame = true;
                settingsPanel.Close();
                return;
            }
        }

        // F : 상호작용
        if (kb.fKey.wasPressedThisFrame)
        {
            if (inventoryOpen ||
                characterOpen ||
                questOpen ||
                worldMapOpen ||
                settingsOpen)
                return;

            gatherUI?.TryInteractSelected();
        }
    }

    // -------------------------------------------------
    // 버튼 연결용 공개 함수
    // -------------------------------------------------
    public void OnClickQuestButton()
    {
        bool questOpen = (questPanel != null && questPanel.IsOpen);

        if (questPanel == null) return;

        if (questOpen)
        {
            questPanel.Close();
        }
        else if (!IsAnyOtherUIOpen(UIKind.Quest))
        {
            StopPlayerForUIOpen();
            questPanel.Open();
        }
    }

    public void OnClickWorldMapButton()
    {
        bool worldMapOpen = (miniMapController != null && miniMapController.IsWorldMapOpen);

        if (miniMapController == null) return;

        if (worldMapOpen)
        {
            miniMapController.ToggleWorldMap();
        }
        else if (!IsAnyOtherUIOpen(UIKind.WorldMap))
        {
            StopPlayerForUIOpen();
            miniMapController.ToggleWorldMap();
        }
    }

    // -------------------------------------------------
    // 내부 헬퍼
    // -------------------------------------------------
    private enum UIKind
    {
        Inventory,
        Character,
        Quest,
        WorldMap,
        Settings
    }

    private bool IsAnyOtherUIOpen(UIKind self)
    {
        if (self != UIKind.Inventory && inventory != null && inventory.IsOpen)
            return true;

        if (self != UIKind.Character && characterUI != null && characterUI.IsOpen)
            return true;

        if (self != UIKind.Quest && questPanel != null && questPanel.IsOpen)
            return true;

        if (self != UIKind.WorldMap && miniMapController != null && miniMapController.IsWorldMapOpen)
            return true;

        if (self != UIKind.Settings && settingsPanel != null && settingsPanel.IsPanelOpen())
            return true;

        return false;
    }

    private void StopPlayerForUIOpen()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerControllerHumanoid>();

        if (player != null)
            player.ForceStopForUI();
    }
}