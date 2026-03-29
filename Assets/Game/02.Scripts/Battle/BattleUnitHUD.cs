using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUnitHUD : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text hpText;
    public Image hpFill;

    private BattleActorRuntime boundActor;

    public void Bind(BattleActorRuntime actor)
    {
        boundActor = actor;

        if (actor == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        nameText.text = actor.data.displayName;
        UpdateHP();
    }

    public void UpdateHP()
    {
        if (boundActor == null) return;

        int hp = Mathf.Max(0, boundActor.hp);
        int max = Mathf.Max(1, boundActor.maxHp);
        hpText.text = $"{hp}";
        if (hpFill)
            hpFill.fillAmount = (float)hp / max;
    }
}
