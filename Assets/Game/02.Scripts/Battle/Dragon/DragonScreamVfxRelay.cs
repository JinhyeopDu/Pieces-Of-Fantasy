using System.Collections;
using UnityEngine;

public class DragonScreamVfxRelay : MonoBehaviour
{
    [Header("Scream VFX")]
    public GameObject screamVfxPrefab;
    public Transform screamVfxAnchor;
    public bool followAnchor = true;

    [Header("Failsafe")]
    [Tooltip("OFF 이벤트가 누락될 경우 이 시간 뒤 강제 제거")]
    public float failSafeDestroyDelay = 2.0f;

    private GameObject _spawned;
    private ParticleSystem[] _ps;
    private Coroutine _failSafeCo;

    public void OnScreamVfxOn()
    {
        Debug.Log("[ScreamFX] ON called");

        if (screamVfxPrefab == null || screamVfxAnchor == null)
            return;

        // 혹시 이전 이펙트가 남아있다면 먼저 정리
        ForceCleanupImmediate();

        _spawned = Instantiate(
            screamVfxPrefab,
            screamVfxAnchor.position,
            screamVfxAnchor.rotation
        );

        if (followAnchor)
            _spawned.transform.SetParent(screamVfxAnchor, worldPositionStays: true);

        _ps = _spawned.GetComponentsInChildren<ParticleSystem>(true);

        if (_ps != null)
        {
            for (int i = 0; i < _ps.Length; i++)
            {
                if (_ps[i] != null)
                    _ps[i].Play(true);
            }
        }

        // OFF 이벤트 누락 대비
        if (_failSafeCo != null)
            StopCoroutine(_failSafeCo);

        _failSafeCo = StartCoroutine(CoFailSafeCleanup());
    }

    public void OnScreamVfxOff()
    {
        Debug.Log("[ScreamFX] OFF called");
        CleanupWithStop();
    }

    private IEnumerator CoFailSafeCleanup()
    {
        yield return new WaitForSeconds(failSafeDestroyDelay);

        if (_spawned != null)
        {
            Debug.LogWarning("[ScreamFX] Failsafe cleanup triggered");
            CleanupWithStop();
        }
    }

    private void CleanupWithStop()
    {
        if (_failSafeCo != null)
        {
            StopCoroutine(_failSafeCo);
            _failSafeCo = null;
        }

        if (_spawned == null)
            return;

        if (_ps != null)
        {
            for (int i = 0; i < _ps.Length; i++)
            {
                if (_ps[i] != null)
                    _ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        Destroy(_spawned, 0.02f);
        _spawned = null;
        _ps = null;
    }

    private void ForceCleanupImmediate()
    {
        if (_failSafeCo != null)
        {
            StopCoroutine(_failSafeCo);
            _failSafeCo = null;
        }

        if (_spawned != null)
        {
            Destroy(_spawned);
            _spawned = null;
        }

        _ps = null;
    }

    private void OnDisable()
    {
        ForceCleanupImmediate();
    }

    private void OnDestroy()
    {
        ForceCleanupImmediate();
    }
}