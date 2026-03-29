using UnityEngine;
using UnityEngine.UI;

public class OpenCharacterScreenButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private CharacterScreenController characterScreen;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (characterScreen == null)
            characterScreen = FindObjectOfType<CharacterScreenController>(includeInactive: true);

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (characterScreen == null)
        {
            Debug.LogWarning("[OpenCharacterScreenButton] CharacterScreenController를 찾지 못했습니다.");
            return;
        }

        characterScreen.Toggle();
    }
}