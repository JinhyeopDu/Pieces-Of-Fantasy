using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExpBarView : MonoBehaviour
{
    [Header("Fills")]
    public Image baseFill;     // 현재 exp
    public Image previewFill;  // 추가분(누적 후)

    [Header("Texts")]
    public TMP_Text levelText; // "Lv. 20" / "Lv. 1 (+3)"
    public TMP_Text expText;   // "5000(+7000)/16580" 또는 "최고 레벨"

    // 파라미터 이름을 addExp로 유지 (named argument 호환)
    public void Set(
        int level,
        int curExp,
        int addExp,
        int needExp,
        bool isCap = false,
        int previewLevelDelta = 0,
        int addExpTotalForText = 0,
        bool isMaxLevel = false
    )
    {
        // 최고 레벨
        if (isMaxLevel)
        {
            if (baseFill != null) baseFill.fillAmount = 1f;
            if (previewFill != null) previewFill.fillAmount = 1f;

            if (levelText != null)
                levelText.text = $"Lv. {level}";

            if (expText != null)
                expText.text = "최고 레벨";

            return;
        }

        if (needExp <= 0) needExp = 1;

        int clampedCur = Mathf.Clamp(curExp, 0, needExp);
        int clampedAdd = Mathf.Clamp(addExp, 0, needExp - clampedCur);

        int after = clampedCur + clampedAdd;

        float base01 = clampedCur / (float)needExp;
        float after01 = after / (float)needExp;

        if (baseFill != null) baseFill.fillAmount = base01;
        if (previewFill != null) previewFill.fillAmount = after01;

        if (levelText != null)
        {
            if (previewLevelDelta > 0)
                levelText.text = $"Lv. {level} <color=#00FF66>(+{previewLevelDelta})</color>";
            else
                levelText.text = $"Lv. {level}";
        }

        if (expText != null)
        {
            if (isCap)
            {
                expText.text = "승급 필요";
            }
            else
            {
                // 텍스트는 "총 추가 예정 경험치"를 초록색으로
                if (addExpTotalForText > 0)
                    expText.text = $"{clampedCur}<color=#00FF66>(+{addExpTotalForText})</color>/{needExp}";
                else
                    expText.text = $"{clampedCur}/{needExp}";
            }
        }
    }
}