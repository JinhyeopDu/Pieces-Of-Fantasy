using UnityEngine;

public class SortSpinner : MonoBehaviour
{
    [SerializeField] private RectTransform spinnerRect;
    [SerializeField] private float degreesPerSecond = 180f;

    private bool _active;

    void Awake()
    {
        if (spinnerRect == null) spinnerRect = GetComponent<RectTransform>();
        //SetActive(false);
    }


    void Update()
    {
        if (spinnerRect == null) return;

        float d = degreesPerSecond * Time.unscaledDeltaTime;
        spinnerRect.Rotate(0f, 0f, -d);
    }

    public void SetActive(bool on)
    {
        // “활성 상태” 자체가 동작 여부가 되게 통일
        if (gameObject.activeSelf != on)
            gameObject.SetActive(on);
    }
}