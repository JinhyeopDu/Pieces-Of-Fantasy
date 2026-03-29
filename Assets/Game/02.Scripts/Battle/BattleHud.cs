using UnityEngine;
using TMPro;
using System.Text;

public class BattleHud : MonoBehaviour
{
    [Header("Turn Info")]
    public TMP_Text turnText;          // 턴 표시 텍스트 (예: "Turn : Tribi")

    [Header("Skill Points UI")]
    public TMP_Text spText;            // (선택) "SP: 3/5" 텍스트
    public SkillPointPipUI spPipUI;    // (권장) 별 5개 UI

    [Header("Ally Cards")]
    public BattleUnitCardHUD[] allyCards; // 하단 카드 UI들

    [Header("Log UI")]
    public TMP_Text logText;           // 전투 로그 텍스트
    public int maxLogLines = 20;

    private StringBuilder logBuilder = new StringBuilder();

    private void OnEnable()
    {
        if (GameContext.I != null)
            GameContext.I.OnBattleSkillPointsChanged += OnSPChanged;
    }

    private void OnDisable()
    {
        if (GameContext.I != null)
            GameContext.I.OnBattleSkillPointsChanged -= OnSPChanged;
    }

    private void Start()
    {
        // 초기 표시(순서 이슈 대비)
        if (GameContext.I != null)
            OnSPChanged(GameContext.I.battleSkillPoints, GameContext.I.battleSkillPointsMax);
    }

    private void OnSPChanged(int cur, int max)
    {
        if (spText != null)
            spText.text = $"SP: {cur}/{max}";

        if (spPipUI != null)
            spPipUI.Set(cur, max);
    }

    /// <summary>
    /// SP 부족 시 별 UI를 잠깐 깜빡이게 한다.
    /// </summary>
    public void FlashSkillPoints()
    {
        if (spPipUI != null)
            spPipUI.Flash();
    }

    /// <summary>
    /// 턴이 바뀌거나 전투 시작 시 호출해서 전체 HUD를 갱신
    /// </summary>
    public void Render(BattleActorRuntime[] allies, BattleActorRuntime[] enemies, string whoseTurn)
    {
        if (turnText)
            turnText.text = $"Turn : {whoseTurn}";

        // 아군 카드 갱신
        for (int i = 0; i < allyCards.Length; i++)
        {
            if (i < allies.Length && allies[i] != null)
            {
                allyCards[i].Bind(allies[i], i);
            }
            else
            {
                allyCards[i].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// HP만 업데이트할 때 사용 (공격 후 등)
    /// </summary>
    public void RefreshHPBars(BattleActorRuntime[] allies, BattleActorRuntime[] enemies)
    {
        for (int i = 0; i < allyCards.Length && i < allies.Length; i++)
        {
            allyCards[i].UpdateHP();
        }
    }

    /// <summary>
    /// 로그 한 줄 추가
    /// </summary>
    public void AppendLog(string line)
    {
        if (string.IsNullOrEmpty(line)) return;

        logBuilder.AppendLine(line);

        // 최대 줄 수 제한
        var text = logBuilder.ToString();
        var lines = text.Split('\n');

        if (lines.Length > maxLogLines)
        {
            int removeCount = lines.Length - maxLogLines;
            var sb = new StringBuilder();
            for (int i = removeCount; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;
                sb.AppendLine(lines[i]);
            }
            logBuilder = sb;
        }

        if (logText)
            logText.text = logBuilder.ToString();
    }
}
