using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplorationRewardToastController : MonoBehaviour
{
    [Header("Refs")]
    public Transform toastRoot;
    public RewardToastLine linePrefab;

    [Header("Timing")]
    public float pollInterval = 0.15f;
    public float showInterval = 0.08f;
    public float holdTime = 2.0f;
    public float fadeTime = 0.25f;

    [Header("Display Limit")]
    public int maxVisibleLines = 6;      // 화면에 보일 최대 줄 수
    public int maxProcessLines = 20;     // 한 번에 처리할 최대 보상 줄 수 (안전장치)

    private bool _isShowing;

    IEnumerator Start()
    {
        yield return null;

        if (toastRoot == null)
        {
            Debug.LogError("[RewardToast] toastRoot is NULL.");
            yield break;
        }

        if (linePrefab == null)
        {
            Debug.LogError("[RewardToast] linePrefab is NULL.");
            yield break;
        }

        while (GameContext.I == null)
            yield return null;

        while (true)
        {
            if (!_isShowing)
            {
                var rewards = GameContext.I.ConsumePendingRewards();
                if (rewards != null && rewards.Count > 0)
                    yield return StartCoroutine(CoShowRewardsSafe(rewards));
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }

    IEnumerator CoShowRewardsSafe(List<RewardLine> rewards)
    {
        _isShowing = true;

        int processed = 0;
        int skippedTotalQty = 0;

        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r.item == null || r.qty <= 0) continue;

            if (processed >= maxProcessLines)
            {
                skippedTotalQty += r.qty;
                continue;
            }

            // 화면 표시 제한 (while 금지)
            if (toastRoot.childCount >= maxVisibleLines)
            {
                var oldest = toastRoot.GetChild(0);
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }

            var line = Instantiate(linePrefab, toastRoot);
            line.Set(r.item, r.qty);

            AudioManager.I?.PlaySFX2D(SFXKey.Reward_Pickup, 1f, 0.06f);

            StartCoroutine(CoShowAndDestroy(line));

            processed++;
            yield return new WaitForSeconds(showInterval);
        }

        // 너무 많았던 경우 한 줄로 요약 (RewardToastLine에 SetSummary 필요)
        if (skippedTotalQty > 0)
        {
            if (toastRoot.childCount >= maxVisibleLines)
            {
                var oldest = toastRoot.GetChild(0);
                if (oldest != null)
                    Destroy(oldest.gameObject);
            }

            var summaryLine = Instantiate(linePrefab, toastRoot);
            summaryLine.SetSummary($"+{skippedTotalQty} more");

            StartCoroutine(CoShowAndDestroy(summaryLine));
        }

        _isShowing = false;
    }

    IEnumerator CoShowAndDestroy(RewardToastLine line)
    {
        if (line == null) yield break;

        yield return line.CoFadeInPop(0.18f, 0.96f);
        yield return new WaitForSeconds(holdTime);
        yield return line.CoFadeOut(fadeTime);

        if (line != null)
            Destroy(line.gameObject);
    }
}