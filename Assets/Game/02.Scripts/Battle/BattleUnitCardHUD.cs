using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUnitCardHUD : MonoBehaviour
{
    [Header("Portrait")]
    public Image portraitImage;   // PortraitImage (캐릭터 초상)
    public Image portraitBG;      // 선택사항 (배경)

    [Header("HP")]
    public TMP_Text hpNumberText; // HPNumText (현재 HP 숫자)
    public Image hpFill;          // HPFill (HP 바)

    [Header("Order")]
    public TMP_Text orderText;    // OrderText (행동 순서 번호)

    private BattleActorRuntime actor;

    /// <summary>
    /// 이 카드에 어떤 캐릭터를 바인딩할지 결정
    /// </summary>
    public void Bind(BattleActorRuntime runtime, int orderIndex)
    {
        actor = runtime;

        if (runtime == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // 순서 번호 (0 → 1, 1 → 2...)
        if (orderText)
            orderText.text = (orderIndex + 1).ToString();

        // 초상 이미지 (CharacterData에 portrait가 설정되어 있다면)
        if (portraitImage && runtime.data != null && runtime.data.portrait != null)
            portraitImage.sprite = runtime.data.portrait;

        UpdateHP();
    }

    /// <summary>
    /// HP 표시/게이지 업데이트
    /// </summary>
    public void UpdateHP()
    {
        if (actor == null) return;

        int hp = Mathf.Max(0, actor.hp);
        int max = Mathf.Max(1, actor.maxHp);

        if (hpNumberText)
            hpNumberText.text = hp.ToString();   // "6428" 처럼 현재 HP만 표시

        if (hpFill)
            hpFill.fillAmount = (float)hp / max;
    }
}
