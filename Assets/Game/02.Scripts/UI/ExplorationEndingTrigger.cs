using UnityEngine;

public class ExplorationEndingTrigger : MonoBehaviour
{
    [SerializeField] private EndingPanelController endingPanel;
    [SerializeField] private string finalBossSpawnId = "드래곤01";

    private void Awake()
    {
        if (endingPanel == null)
            endingPanel = FindFirstObjectByType<EndingPanelController>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        var g = GameContext.I;
        if (g == null)
        {
            Debug.LogWarning("[ExplorationEndingTrigger] GameContext is null.");
            return;
        }

        if (endingPanel == null)
        {
            Debug.LogWarning("[ExplorationEndingTrigger] endingPanel is null.");
            return;
        }

        Debug.Log($"[ExplorationEndingTrigger] endingPanel={endingPanel.name}, scene={endingPanel.gameObject.scene.name}, activeSelf={endingPanel.gameObject.activeSelf}, activeInHierarchy={endingPanel.gameObject.activeInHierarchy}");

        // 드래곤 처치만으로는 엔딩 패널을 띄우지 않는다.
        // 마지막 퀘스트 보상 수령 시점에 QuestManager/QuestPanelController에서 처리한다.
        if (g.IsUniqueDefeated(finalBossSpawnId))
        {
            Debug.Log("[ExplorationEndingTrigger] Final boss already defeated. Waiting for final quest reward claim.");
        }
    }
}