using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI text;

    [Header("Anim Settings")]
    public float lifeTime = 0.8f;   // 전체 지속 시간
    public float moveSpeed = 60f;    // 위로 이동 속도
    public float fadeStartTime = 0.4f;   // 이 시점부터 서서히 투명해짐

    [Header("Critical Settings")]
    public float critLifeTimeMul = 1.1f;    // 크리티컬이면 수명 약간 증가
    public float critMoveSpeedMul = 1.1f;    // 크리티컬 이동 속도 보정
    public float critScaleMul = 1.3f;    // 크리티컬 전체 스케일 배수
    public float critRotAngle = 12f;     // 회전 각도(±)
    public float critRotSpeed = 15f;     // 회전 속도

    private float _time;
    private RectTransform _rt;
    private CanvasGroup _canvasGroup;
    private float _startScale = 1.0f;
    private float _popScale = 1.2f;

    private bool _isCritical;
    private float _baseLifeTime;
    private float _baseMoveSpeed;

    public void Setup(int damage, bool isCritical = false)
    {
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>();

        if (_rt == null)
            _rt = GetComponent<RectTransform>();

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (text == null)
        {
            Debug.LogError("[DamagePopup] TextMeshProUGUI를 찾지 못했습니다.", this);
            return;
        }

        _isCritical = isCritical;
        _time = 0f;
        _canvasGroup.alpha = 1f;

        // 기본 수치 저장
        _baseLifeTime = lifeTime;
        _baseMoveSpeed = moveSpeed;

        // 크리티컬이면 파라미터 살짝 강화
        if (_isCritical)
        {
            lifeTime = _baseLifeTime * critLifeTimeMul;
            moveSpeed = _baseMoveSpeed * critMoveSpeedMul;
        }
        else
        {
            lifeTime = _baseLifeTime;
            moveSpeed = _baseMoveSpeed;
        }

        // 텍스트/색
        text.text = damage.ToString();

        if (_isCritical)
        {
            text.fontSize = 60f;
            // 황금색 + 살짝 흰색 쪽으로
            text.color = new Color(1.0f, 0.93f, 0.35f);
        }
        else
        {
            text.fontSize = 40f;
            text.color = Color.red;
        }

        var c = text.color;
        c.a = 1f;
        text.color = c;

        // 초기 스케일
        float startScale = _startScale * (_isCritical ? critScaleMul : 1f);
        _rt.localScale = Vector3.one * startScale;

        // 초기 회전 리셋
        _rt.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (_rt == null || _canvasGroup == null)
            return;

        _time += Time.deltaTime;
        float tNorm = Mathf.Clamp01(_time / lifeTime);

        // 1) 위로 이동
        _rt.anchoredPosition += Vector2.up * (moveSpeed * Time.deltaTime);

        // 2) 팝업 스케일 애니메이션
        float baseStart = _startScale * (_isCritical ? critScaleMul : 1f);
        float basePop = _popScale * (_isCritical ? critScaleMul : 1f);

        if (tNorm < 0.3f)
        {
            float k = tNorm / 0.3f;
            float scale = Mathf.Lerp(baseStart, basePop, k);
            _rt.localScale = Vector3.one * scale;
        }
        else
        {
            float k = (tNorm - 0.3f) / 0.7f;
            float scale = Mathf.Lerp(basePop, baseStart, k);
            _rt.localScale = Vector3.one * scale;
        }

        // 3) 크리티컬이면 살짝 좌우로 흔들리는 회전
        if (_isCritical)
        {
            float rot = Mathf.Sin(_time * critRotSpeed) * critRotAngle;
            _rt.localRotation = Quaternion.Euler(0f, 0f, rot);
        }

        // 4) 페이드 아웃
        if (_time > fadeStartTime)
        {
            float fadeT = (_time - fadeStartTime) / (lifeTime - fadeStartTime);
            _canvasGroup.alpha = Mathf.Clamp01(1f - fadeT);
        }

        // 5) 수명 끝나면 삭제
        if (_time >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
