using TMPro;
using UnityEngine;

public class CharacterSkillInfoPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillTagText;
    [SerializeField] private TMP_Text skillDescriptionText;

    private const string COLOR_ATTACK = "#FF7A7A";      // ´õ ¹àÀº »¡°­
    private const string COLOR_SUPPORT = "#7CFFB2";     // ¹àÀº ¹ÎÆ® ±×¸°
    private const string COLOR_AOE = "#FFC266";         // ¹àÀº ¿À·»Áö
    private const string COLOR_SECRET_ART = "#D8A8FF";  // ¹àÀº º¸¶ó (¿äÃ» ¹Ý¿µ)

    public void Show(string skillName, string tagText, string description)
    {
        gameObject.SetActive(true);

        if (skillNameText != null) skillNameText.text = skillName ?? "";
        if (skillTagText != null) skillTagText.text = FormatTagText(tagText ?? "");
        if (skillDescriptionText != null) skillDescriptionText.text = description ?? "";
    }

    public void Clear()
    {
        if (skillNameText != null) skillNameText.text = "";
        if (skillTagText != null) skillTagText.text = "";
        if (skillDescriptionText != null) skillDescriptionText.text = "";

        gameObject.SetActive(false);
    }

    private string FormatTagText(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        string result = raw;

        result = result.Replace("[°ø°Ý]", $"<color={COLOR_ATTACK}><b>[°ø°Ý]</b></color>");
        result = result.Replace("[¼­Æ÷Æ®]", $"<color={COLOR_SUPPORT}><b>[¼­Æ÷Æ®]</b></color>");
        result = result.Replace("[±¤¿ª]", $"<color={COLOR_AOE}><b>[±¤¿ª]</b></color>");
        result = result.Replace("[ºñ¼ú]", $"<color={COLOR_SECRET_ART}><b>[ºñ¼ú]</b></color>");

        return result;
    }
}