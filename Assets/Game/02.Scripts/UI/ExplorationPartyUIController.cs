using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExplorationPartyUIController : MonoBehaviour
{
    [Header("Party Slots (3)")]
    public PartySlotUI[] slots;

    [Header("Secret Art Points (5)")]
    public Image[] pointImages;
    public Sprite pointOnSprite;
    public Sprite pointOffSprite;
    [Range(0f, 1f)] public float offAlpha = 0.25f;

    [Header("Game Over")]
    public string gameOverSceneName = "GameOver";

    [Header("Optional (Recommended)")]
    [Tooltip("있으면 슬롯 클릭 시 실제 필드 캐릭터 교대까지 함께 처리합니다.")]
    public PiecesOfFantasy.Exploration.ExplorationPartySwitcher partySwitcher; // ★ 선택

    void OnEnable()
    {
        Refresh();
    }

    void Start()
    {
        AudioManager.I?.PlayBGM(BGMKey.Exploration);
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        var ctx = GameContext.I;
        if (ctx == null)
        {
            ClearUI();
            return;
        }

        // active가 죽었으면 자동 보정, 전멸이면 게임 종료
        if (!ctx.EnsureActiveIsAlive())
        {
            LoadGameOver();
            return;
        }

        // 슬롯 표시
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            var cr = (ctx.party != null && i < ctx.party.Count)
                ? ctx.party[i]
                : null;

            bool isActive = (i == ctx.activePartyIndex);

            int idx = i;
            slots[i].Render(cr, isActive, onClickSwitch: () =>
            {
                // 1) Switcher가 있으면 실제 아바타 교대까지 수행
                if (partySwitcher != null)
                {
                    partySwitcher.TrySwitchTo(idx);
                }
                else
                {
                    // 2) 없으면 기존 방식(인덱스만 변경)
                    ctx.TrySetActiveIndex(idx);
                }
            });
        }

        // 포인트 표시
        int max = Mathf.Max(0, ctx.secretArtPointsMax);
        int cur = Mathf.Clamp(ctx.secretArtPoints, 0, max);

        for (int i = 0; i < pointImages.Length; i++)
        {
            if (!pointImages[i]) continue;

            bool withinMax = (i < max);

            // max 범위 밖이면 숨김(프로젝트에서 항상 5로 고정이면 이 줄은 취향)
            pointImages[i].gameObject.SetActive(withinMax);

            if (!withinMax) continue;

            bool on = (i < cur);

            if (pointOnSprite && pointOffSprite)
            {
                pointImages[i].sprite = on ? pointOnSprite : pointOffSprite;
                pointImages[i].color = Color.white;
            }
            else
            {
                var c = pointImages[i].color;
                c.a = on ? 1f : offAlpha;
                pointImages[i].color = c;
            }
        }
    }

    private void ClearUI()
    {
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].gameObject.SetActive(false);
            }
        }

        if (pointImages != null)
        {
            for (int i = 0; i < pointImages.Length; i++)
            {
                if (pointImages[i] != null)
                    pointImages[i].gameObject.SetActive(false);
            }
        }
    }

    void LoadGameOver()
    {
        if (!string.IsNullOrEmpty(gameOverSceneName) && Application.CanStreamedLevelBeLoaded(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
        else
            SceneManager.LoadScene("Title");
    }
}
