using UnityEngine;
using UnityEngine.UI;

public class CriticalFlash : MonoBehaviour
{
    public Image image;

    [Header("Anim Settings")]
    public float lifeTime = 0.3f;       // 전체 지속 시간
    public float maxScale = 1.6f;       // 가장 크게 커지는 배율
    public float minScale = 0.8f;       // 시작 스케일

    private float _time;
    private RectTransform _rt;
    private Color _baseColor;

    void Awake()
    {
        if (_rt == null)
            _rt = GetComponent<RectTransform>();

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
            _baseColor = image.color;
    }

    void OnEnable()
    {
        _time = 0f;

        if (_rt == null)
            _rt = GetComponent<RectTransform>();

        if (image != null)
            image.color = _baseColor;

        // 시작 스케일 조금 작게
        if (_rt != null)
            _rt.localScale = Vector3.one * minScale;
    }

    void Update()
    {
        if (_rt == null || image == null)
            return;

        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / lifeTime);

        // 1) 스케일: 처음에 커졌다가 서서히 줄어드는 느낌 (또는 반대로 하고 싶으면 바꿔도 됨)
        float scale = Mathf.Lerp(minScale, maxScale, t);
        _rt.localScale = Vector3.one * scale;

        // 2) 알파: 처음엔 밝게 → 점점 사라짐
        Color c = _baseColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        image.color = c;

        // 3) 수명 끝나면 삭제
        if (_time >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
