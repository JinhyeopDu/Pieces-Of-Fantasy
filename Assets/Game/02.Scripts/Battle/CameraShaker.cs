using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    private static CameraShaker _instance;
    public static CameraShaker Instance => _instance;

    [Header("Target Camera (optional)")]
    public Camera targetCamera;      // 비우면 Camera.main을 사용

    private Transform camTransform;
    private Vector3 originalLocalPos;
    private float originalFov;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            camTransform = targetCamera.transform;
            originalLocalPos = camTransform.localPosition;
            originalFov = targetCamera.fieldOfView;
        }
        else
        {
            Debug.LogWarning("[CameraShaker] 타겟 카메라를 찾지 못했습니다.");
        }
    }

    /// <summary>
    /// duration 동안 magnitude 강도로 카메라를 흔듭니다.
    /// optionalZoom > 0이면 살짝 줌 인 후 원복합니다.
    /// </summary>
    public static void Shake(float duration, float magnitude, float optionalZoom = 0f)
    {
        if (Instance == null || Instance.camTransform == null || Instance.targetCamera == null)
        {
            Debug.LogWarning("[CameraShaker] 인스턴스 또는 카메라가 없습니다.");
            return;
        }

        Instance.StopAllCoroutines();
        Instance.StartCoroutine(Instance.ShakeRoutine(duration, magnitude, optionalZoom));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude, float optionalZoom)
    {
        float elapsed = 0f;

        // 줌 인 목표 FOV
        float zoomTargetFov = originalFov - optionalZoom;
        if (zoomTargetFov < 10f) zoomTargetFov = 10f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // 위치 랜덤 오프셋
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            camTransform.localPosition = originalLocalPos + new Vector3(x, y, 0f);

            // 살짝 줌 인/아웃 (곡선 느낌)
            if (optionalZoom > 0f)
            {
                float t = elapsed / duration;
                // 초반에 줌 인, 후반에 되돌리기
                float curve = 1f - Mathf.Abs(2f * t - 1f); // 0→1→0
                float fov = Mathf.Lerp(originalFov, zoomTargetFov, curve);
                targetCamera.fieldOfView = fov;
            }

            yield return null;
        }

        // 원래 위치/시야각으로 복원
        camTransform.localPosition = originalLocalPos;
        targetCamera.fieldOfView = originalFov;
    }

    public void Shake(float intensity, float duration)
    {
        // 기존 네 구현을 여기서 호출하도록 연결
        // 예: StartCoroutine(ShakeCo(intensity, duration));
    }
}
