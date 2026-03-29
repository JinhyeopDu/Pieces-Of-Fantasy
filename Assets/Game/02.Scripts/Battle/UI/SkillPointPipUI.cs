using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillPointPipUI : MonoBehaviour
{
    [Header("Pip Images (Left → Right)")]
    public Image[] pips;

    [Header("Sprites")]
    public Sprite filledSprite;   // GUI_24 (밝은 별)
    public Sprite emptySprite;    // GUI_25 (어두운 별)

    [Header("Flash (SP 부족 연출)")]
    [Tooltip("깜빡임 반복 횟수(2면 2번 반짝)")]
    public int flashLoops = 2;

    [Tooltip("반주기(초). 0.08~0.12 추천")]
    public float flashHalfPeriod = 0.10f;

    [Tooltip("깜빡일 때 비어있는 별을 강조 색으로 잠깐 바꿉니다.")]
    public Color flashColor = new Color(1f, 0.45f, 0.45f, 1f);

    [Tooltip("max보다 큰 pip은 숨김 처리")]
    public bool hideAboveMax = true;

    private Coroutine flashCo;

    public void Set(int current, int max)
    {
        if (pips == null || pips.Length == 0) return;

        current = Mathf.Max(0, current);
        max = Mathf.Max(0, max);

        int displayMax = Mathf.Min(max, pips.Length);
        current = Mathf.Clamp(current, 0, displayMax);

        for (int i = 0; i < pips.Length; i++)
        {
            var img = pips[i];
            if (img == null) continue;

            bool inRange = i < displayMax;

            if (hideAboveMax)
                img.gameObject.SetActive(inRange);

            if (!inRange) continue;

            img.sprite = (i < current) ? filledSprite : emptySprite;
            img.color = Color.white;
            img.transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// SP 부족 같은 상황에서 별 UI를 잠깐 깜빡임
    /// </summary>
    public void Flash()
    {
        if (!gameObject.activeInHierarchy) return;

        if (flashCo != null)
        {
            StopCoroutine(flashCo);
            flashCo = null;
        }
        flashCo = StartCoroutine(FlashCo());
    }

    private IEnumerator FlashCo()
    {
        if (pips == null || pips.Length == 0) yield break;

        // 원본 상태 백업
        Color[] originalColors = new Color[pips.Length];
        Vector3[] originalScales = new Vector3[pips.Length];

        for (int i = 0; i < pips.Length; i++)
        {
            var img = pips[i];
            if (img == null) continue;
            originalColors[i] = img.color;
            originalScales[i] = img.transform.localScale;
        }

        for (int loop = 0; loop < flashLoops; loop++)
        {
            ApplyFlashVisual(on: true);
            yield return new WaitForSeconds(flashHalfPeriod);

            ApplyFlashVisual(on: false);
            yield return new WaitForSeconds(flashHalfPeriod);
        }

        // 최종 원복
        for (int i = 0; i < pips.Length; i++)
        {
            var img = pips[i];
            if (img == null) continue;
            img.color = originalColors[i];
            img.transform.localScale = originalScales[i];
        }

        flashCo = null;
    }

    private void ApplyFlashVisual(bool on)
    {
        for (int i = 0; i < pips.Length; i++)
        {
            var img = pips[i];
            if (img == null) continue;
            if (!img.gameObject.activeSelf) continue;

            // 비어있는 별만 깜빡이게(직관적)
            bool isEmpty = (img.sprite == emptySprite);
            if (!isEmpty) continue;

            if (on)
            {
                img.color = flashColor;
                img.transform.localScale = Vector3.one * 1.06f;
            }
            else
            {
                img.color = Color.white;
                img.transform.localScale = Vector3.one;
            }
        }
    }
}
