using UnityEngine;

public class DragonBreathEventRelay : MonoBehaviour
{
    [Header("Refs")]
    public Transform breathAnchor;          // HeadBone 아래 BreathAnchor
    public GameObject breathPrefab;         // PF_DragonBreath
    public bool autoDestroyOnOff = false;   // 끌 때 파괴할지(초보는 false 추천)

    GameObject _instance;

    // Animation Event에서 호출할 함수 (이름 정확히!)
    public void Anim_BreathOn()
    {
        if (breathAnchor == null || breathPrefab == null) return;

        if (_instance == null)
        {
            _instance = Instantiate(breathPrefab, breathAnchor);
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
        }

        _instance.SetActive(true);
    }

    // Animation Event에서 호출할 함수 (이름 정확히!)
    public void Anim_BreathOff()
    {
        if (_instance == null) return;

        if (autoDestroyOnOff)
        {
            Destroy(_instance);
            _instance = null;
        }
        else
        {
            _instance.SetActive(false);
        }
    }
}