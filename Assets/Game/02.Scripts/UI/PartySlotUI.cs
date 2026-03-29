using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartySlotUI : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Portrait(Mask) 안쪽의 실제 Portrait Image")]
    public Image portraitImage;

    [Tooltip("HPBarBG/HPBarFill의 Fill Image (Type=Filled 권장)")]
    public Image hpFillImage;

    [Tooltip("Slot의 Name TMP_Text")]
    public TMP_Text nameText;

    [Tooltip("SecretArtIcon(Mask) 오브젝트(통째로 On/Off)")]
    public GameObject secretArtIconRoot;

    [Tooltip("SecretArtIcon(Mask) 안쪽의 실제 Icon Image (없으면 알파 제어 불가)")]
    public Image secretArtIconImage; // ★ 추가: 캐릭터별 아이콘 스프라이트를 여기로 넣음

    [Header("Secret Art Icon State")]
    [Range(0f, 1f)] public float secretArtReadyAlpha = 1f;
    [Range(0f, 1f)] public float secretArtNotReadyAlpha = 0.35f;

    [Header("Optional")]
    public Button clickButton; // 슬롯 클릭 교대(있으면)

    [Header("Tint Colors")]
    public Color aliveColor = Color.white;
    public Color deadTintColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    [Tooltip("HP바 Fill도 같이 어둡게 할지")]
    public bool tintHpFillToo = true;

    [Tooltip("이름 텍스트도 같이 흐리게 할지")]
    public bool tintNameToo = true;

    // 내부적으로 저장해두면, 나중에 색상 커스터마이징 할 때 안전
    private Color _nameAliveColorCached;
    private bool _nameColorCached;

    public void Render(CharacterRuntime cr, bool isActive, System.Action onClickSwitch = null)
    {
        if (cr == null || cr.data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 1) 이름
        if (nameText)
        {
            nameText.text = cr.data.displayName;

            if (!_nameColorCached)
            {
                _nameAliveColorCached = nameText.color;
                _nameColorCached = true;
            }
        }

        // 2) 초상
        if (portraitImage)
            portraitImage.sprite = cr.data.portrait;

        // 3) HP 바
        float ratio = (cr.maxHp > 0) ? Mathf.Clamp01((float)cr.hp / cr.maxHp) : 0f;
        if (hpFillImage)
            hpFillImage.fillAmount = ratio;

        // 4) 비술 아이콘(캐릭터 고유 아이콘 표시)
        ApplySecretArtIcon(cr);

        // 5) 전투불능이면 어둡게, 회복(HP>0)이면 원래로 자동 복구
        bool dead = cr.hp <= 0;
        ApplyTint(dead);

        // 6) 교대 불가 처리(죽은 캐릭터는 클릭 불가)
        if (clickButton)
        {
            clickButton.interactable = !dead;

            clickButton.onClick.RemoveAllListeners();
            if (!dead && onClickSwitch != null)
                clickButton.onClick.AddListener(() => onClickSwitch());
        }
    }

    private void ApplySecretArtIcon(CharacterRuntime cr)
    {
        if (!secretArtIconRoot) return;

        // 아이콘이 없으면 통째로 숨김
        Sprite icon = (cr.data != null) ? cr.data.secretArtIcon : null;
        bool hasIcon = icon != null;

        secretArtIconRoot.SetActive(hasIcon);
        if (!hasIcon) return;

        if (secretArtIconImage)
        {
            secretArtIconImage.sprite = icon;

            // 준비 상태에 따라 알파로 표현(Ready=진하게, NotReady=흐리게)
            float a = cr.secretArtReady ? secretArtReadyAlpha : secretArtNotReadyAlpha;

            Color c = secretArtIconImage.color;
            c.a = a;
            secretArtIconImage.color = c;
        }
        else
        {
            // Image 참조가 없다면 최소 동작(Ready일 때만 보이게)로 폴백
            // (가능하면 secretArtIconImage를 연결하는 걸 권장)
            secretArtIconRoot.SetActive(cr.secretArtReady);
        }
    }

    private void ApplyTint(bool dead)
    {
        // Portrait / HPFill은 “aliveColor <-> deadTintColor”로 복귀
        Color c = dead ? deadTintColor : aliveColor;

        if (portraitImage)
            portraitImage.color = c;

        if (tintHpFillToo && hpFillImage)
            hpFillImage.color = c;

        // Name 텍스트는 “원래 색”으로 복귀하도록 캐싱한 색 사용
        if (tintNameToo && nameText)
        {
            if (!_nameColorCached)
            {
                _nameAliveColorCached = nameText.color;
                _nameColorCached = true;
            }

            nameText.color = dead
                ? new Color(_nameAliveColorCached.r * 0.7f, _nameAliveColorCached.g * 0.7f, _nameAliveColorCached.b * 0.7f, _nameAliveColorCached.a)
                : _nameAliveColorCached;
        }
    }
}
