using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class CharacterPreviewRig : MonoBehaviour
{
    [Header("Rig")]
    public Transform previewAnchor;
    public Camera previewCamera;

    [Header("Optional")]
    public string previewLayerName = "Preview";

    private GameObject _currentInstance;

    public void ShowCharacter(CharacterData data)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        Clear();

        if (previewAnchor == null)
        {
            Debug.LogError("[CharacterPreviewRig] previewAnchor가 비어있습니다.");
            return;
        }

        if (data.explorationPrefab == null)
        {
            Debug.LogWarning($"[CharacterPreviewRig] {data.displayName} explorationPrefab이 없습니다.");
            return;
        }

        _currentInstance = Instantiate(data.explorationPrefab, previewAnchor);

        var t = _currentInstance.transform;
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;

        // ─────────────────────────────────────────────
        // 핵심 1) 프리뷰 클론은 절대 Player 태그를 가지면 안 된다
        // ─────────────────────────────────────────────
        SetTagRecursively(_currentInstance, "Untagged");

        // ─────────────────────────────────────────────
        // 핵심 2) 레이어를 Preview로 통일
        // ─────────────────────────────────────────────
        if (!string.IsNullOrEmpty(previewLayerName))
        {
            int layer = LayerMask.NameToLayer(previewLayerName);
            if (layer >= 0)
                SetLayerRecursively(_currentInstance, layer);
            else
                Debug.LogWarning($"[CharacterPreviewRig] Layer '{previewLayerName}' not found.");
        }

        // ─────────────────────────────────────────────
        // 핵심 3) 프리뷰에서는 게임플레이 컴포넌트 전부 끄기
        // ─────────────────────────────────────────────
        DisableGameplayComponentsForPreview(_currentInstance);

        // 애니메이터는 켜도 됨(프리뷰 idle/pose용)
        var anim = _currentInstance.GetComponentInChildren<Animator>(true);
        if (anim != null)
            anim.enabled = true;
    }

    /// <summary>
    /// 프리뷰 렌더링만 필요한데, explorationPrefab에는 실제 플레이용 컴포넌트가 붙어있어서
    /// Awake/Start/Update에서 위치 이동, NavMesh warp, 입력 처리 등이 돌 수 있음.
    /// 프리뷰에서는 이런 것들을 꺼서 "검은 덩어리/클리핑/월드로 튀기"를 방지.
    /// </summary>
    private void DisableGameplayComponentsForPreview(GameObject root)
    {
        if (root == null) return;

        // 0) 프리뷰에서는 비술 FX는 항상 OFF
        DisableSecretArtFxForPreview(root);

        // 1) 플레이어 이동 스크립트 비활성화
        var playerControllers = root.GetComponentsInChildren<PlayerControllerHumanoid>(true);
        for (int i = 0; i < playerControllers.Length; i++)
            playerControllers[i].enabled = false;

        // 2) 입력 비활성화
        var playerInputs = root.GetComponentsInChildren<PlayerInput>(true);
        for (int i = 0; i < playerInputs.Length; i++)
            playerInputs[i].enabled = false;

        // 3) NavMeshAgent 비활성화
        var agents = root.GetComponentsInChildren<NavMeshAgent>(true);
        for (int i = 0; i < agents.Length; i++)
            agents[i].enabled = false;

        // 4) CharacterController 비활성화
        var ccs = root.GetComponentsInChildren<CharacterController>(true);
        for (int i = 0; i < ccs.Length; i++)
            ccs[i].enabled = false;

        // 5) Rigidbody 물리 제거
        var rbs = root.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rbs.Length; i++)
        {
            var rb = rbs[i];

            if (!rb.isKinematic)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
            rb.Sleep();
        }

        // 6) 오디오 비활성화
        var audios = root.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audios.Length; i++)
            audios[i].enabled = false;
    }

    /// <summary>
    /// 프리뷰 클론에서 비술 FX를 강제로 끈다.
    /// </summary>
    private void DisableSecretArtFxForPreview(GameObject root)
    {
        if (root == null) return;

        var pcs = root.GetComponentsInChildren<PlayerControllerHumanoid>(true);
        for (int i = 0; i < pcs.Length; i++)
        {
            var pc = pcs[i];
            if (pc == null) continue;

            if (pc.secretArtFxRoot != null)
            {
                pc.secretArtFxRoot.SetActive(false);

                var ps = pc.secretArtFxRoot.GetComponentsInChildren<ParticleSystem>(true);
                for (int p = 0; p < ps.Length; p++)
                {
                    ps[p].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps[p].Clear(true);
                }
            }
        }
    }

    public void Clear()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        var tr = obj.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i).gameObject;
            SetLayerRecursively(child, layer);
        }
    }

    private void SetTagRecursively(GameObject obj, string tagName)
    {
        if (obj == null) return;

        obj.tag = tagName;

        var tr = obj.transform;
        for (int i = 0; i < tr.childCount; i++)
        {
            var child = tr.GetChild(i).gameObject;
            SetTagRecursively(child, tagName);
        }
    }
}