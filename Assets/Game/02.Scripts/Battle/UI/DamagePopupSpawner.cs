using UnityEngine;

public class DamagePopupSpawner : MonoBehaviour
{
    public static DamagePopupSpawner I;

    [Header("Refs")]
    public GameObject popupPrefab;          // DamagePopup 프리팹
    public GameObject criticalFlashPrefab;  // CriticalFlash 프리팹
    public Canvas worldCanvas;              // Screen Space - Camera Canvas
    public Camera worldCamera;              // 메인 카메라

    void Awake()
    {
        I = this;

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    /// <summary>
    /// 데미지 숫자 팝업 생성
    /// </summary>
    public void SpawnPopup(int damage, Vector3 worldPos, bool isCritical = false)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[DamagePopupSpawner] popupPrefab이 비었습니다!", this);
            return;
        }
        if (worldCanvas == null)
        {
            Debug.LogError("[DamagePopupSpawner] worldCanvas가 설정되지 않았습니다!", this);
            return;
        }
        if (worldCamera == null)
        {
            Debug.LogError("[DamagePopupSpawner] worldCamera가 없습니다! Main Camera Tag 또는 필드 설정을 확인하세요.", this);
            return;
        }

        // 월드 → 스크린
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = worldCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            Debug.LogError("[DamagePopupSpawner] worldCanvas에 RectTransform이 없습니다.", worldCanvas);
            return;
        }

        // 스크린 → Canvas 로컬 좌표
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                worldCamera,
                out Vector2 localPos))
        {
            Debug.LogWarning("[DamagePopupSpawner] SpawnPopup: 좌표 변환 실패", this);
            return;
        }

        // 프리팹 인스턴스 생성
        GameObject go = Instantiate(popupPrefab, worldCanvas.transform);
        if (go == null)
        {
            Debug.LogError("[DamagePopupSpawner] Instantiate 결과가 null 입니다.", this);
            return;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("[DamagePopupSpawner] popupPrefab 루트에 RectTransform이 없습니다.", popupPrefab);
            return;
        }

        var popup = go.GetComponent<DamagePopup>();
        if (popup == null)
        {
            Debug.LogError("[DamagePopupSpawner] popupPrefab 루트에 DamagePopup 스크립트가 없습니다.", popupPrefab);
            return;
        }

        // 위치 세팅
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        // 팝업 세팅
        popup.Setup(damage, isCritical);
    }

    /// <summary>
    /// 크리티컬 전용 하얀 번쩍 이펙트
    /// </summary>
    public void SpawnCriticalFlash(Vector3 worldPos)
    {
        if (criticalFlashPrefab == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] criticalFlashPrefab이 설정되지 않아 크리 이펙트가 표시되지 않습니다.", this);
            return;
        }
        if (worldCanvas == null || worldCamera == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] SpawnCriticalFlash 실패: worldCanvas/worldCamera 가 비었습니다.", this);
            return;
        }

        // 월드 → 스크린
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
        RectTransform canvasRect = worldCanvas.transform as RectTransform;

        if (canvasRect == null)
        {
            Debug.LogError("[DamagePopupSpawner] worldCanvas에 RectTransform이 없습니다.", worldCanvas);
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                worldCamera,
                out Vector2 localPos))
        {
            Debug.LogWarning("[DamagePopupSpawner] SpawnCriticalFlash: 좌표 변환 실패", this);
            return;
        }

        GameObject go = Instantiate(criticalFlashPrefab, worldCanvas.transform);
        if (go == null)
        {
            Debug.LogError("[DamagePopupSpawner] criticalFlashPrefab Instantiate 실패", this);
            return;
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError("[DamagePopupSpawner] criticalFlashPrefab에 RectTransform이 없습니다.", criticalFlashPrefab);
            return;
        }

        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = localPos;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-10f, 10f)); // 살짝 랜덤 회전
    }
}
