using System.Collections;
using UnityEngine;

public class ExplorationReturnApplier : MonoBehaviour
{
    [Tooltip("플레이어를 찾을 태그. (권장: Player)")]
    public string playerTag = "Player";

    [Header("Find Policy")]
    [Tooltip("플레이어 생성이 늦는 경우를 대비해 최대 대기 시간(초)")]
    public float maxWaitSeconds = 2.0f;

    [Tooltip("몇 프레임마다 다시 찾을지(1=매 프레임)")]
    public int pollEveryFrames = 1;

    IEnumerator Start()
    {
        yield return null;
        yield break;
    }
}
