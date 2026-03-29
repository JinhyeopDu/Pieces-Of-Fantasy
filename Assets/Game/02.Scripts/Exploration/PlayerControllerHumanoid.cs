using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerHumanoid : MonoBehaviour
{
    [Header("Identity (Required)")]
    [Tooltip("이 프리팹이 어떤 캐릭터인지 지정한다. 런타임 캐릭터 매칭과 Secret Art 동기화에 사용된다.")]
    public CharacterData selfCharacterData;

    [Header("References")]
    [Tooltip("비주얼 모델 루트. Animator 자동 탐색의 기준으로도 사용될 수 있다.")]
    public Transform modelRoot;

    [Tooltip("이 캐릭터의 Animator. 비어 있으면 modelRoot 기준으로 자동 탐색한다.")]
    public Animator animator;

    [Header("Secret Art FX")]
    [Tooltip("캐릭터 프리팹 안의 SecretArtFx 루트 오브젝트. Secret Art 준비 상태와 동기화된다.")]
    public GameObject secretArtFxRoot;

    [Header("Movement")]
    public float moveSpeed = 4f;
    public float sprintSpeed = 6.5f;
    public float rotationSpeed = 720f;

    [Header("Footstep SFX (Loop)")]
    public bool enableFootstepSfx = true;
    public AudioSource footstepLoopSource;
    public float footstepMinSpeed = 0.15f;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
    public float footstepFadeOutTime = 0.08f;

    private Coroutine _footstepFadeCo;

    [Header("Footstep Walk/Run Tuning")]
    public float walkFootstepPitch = 1.0f;
    public float runFootstepPitch = 1.2f;
    public float walkFootstepVolume = 0.75f;
    public float runFootstepVolume = 0.95f;

    [Header("Gravity")]
    public float gravity = -20f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = ~0;

    [Tooltip("현재 타겟이 있으나 Encounter/EnemyPack이 전부 비었을 때도 전투 예약으로 간주할지 여부(권장: false)")]
    public bool allowEmptyReserve = false;

    private CharacterController cc;
    private Vector2 moveInput;
    private bool sprintHeld;
    private Vector3 velocity;
    private Transform cam;

    static readonly int HashSpeed = Animator.StringToHash("Speed");
    static readonly int HashGrounded = Animator.StringToHash("Grounded");
    static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");

    [Header("Cursor Policy")]
    [Tooltip("탐험 씬 시작 시 기본적으로 커서를 잠글지 여부")]
    public bool lockCursorByDefault = true;

    [Tooltip("이 키를 누르고 있는 동안 커서를 임시 표시한다.")]
    public KeyCode cursorHoldKey = KeyCode.LeftAlt;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        cam = Camera.main ? Camera.main.transform : null;

        if (!animator && modelRoot)
            animator = modelRoot.GetComponentInChildren<Animator>();

        if (footstepLoopSource == null)
            footstepLoopSource = GetComponent<AudioSource>();

        if (footstepLoopSource != null)
        {
            footstepLoopSource.playOnAwake = false;
            footstepLoopSource.loop = true;
            footstepLoopSource.spatialBlend = 0f;
            footstepLoopSource.volume = footstepVolume;
        }
    }

    /// <summary>
    /// 오브젝트가 활성화될 때 Secret Art FX 상태를 즉시 동기화하고
    /// 현재 커서 정책을 강제로 다시 적용한다.
    /// </summary>
    void OnEnable()
    {
        // ★ 중요: FX는 "활성 캐릭터"가 아니라 "나 자신"의 런타임을 기준으로 동기화
        SyncSecretArtFxWithMyRuntime();
        ApplyCursorPolicy(force: true);
    }

    void OnDisable()
    {
        StopFootstepLoopImmediate();
    }

    /// <summary>
    /// 저장된 월드 위치가 있으면 해당 위치로 플레이어를 복원한다.
    /// Continue 진입 직후 Exploration 씬 복귀에 사용된다.
    /// </summary>
    private void Start()
    {
        if (GameContext.I != null && GameContext.I.hasPendingLoadedWorldPosition)
        {
            transform.position = GameContext.I.pendingLoadedWorldPosition;
            GameContext.I.hasPendingLoadedWorldPosition = false;
        }
    }

    /// <summary>
    /// 두 CharacterData가 같은 캐릭터를 가리키는지 판단한다.
    /// 동일 에셋 참조면 true,
    /// 참조가 달라도 id가 같으면 같은 캐릭터로 본다.
    /// </summary>
    private bool SameCharacter(CharacterData a, CharacterData b)
    {
        if (a == null || b == null) return false;

        // 1) 같은 에셋 참조면 true
        if (a == b) return true;

        // 2) id가 둘 다 있으면 id로 비교
        if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id))
            return a.id == b.id;

        // 3) id가 비어있으면 안전하게 false
        return false;
    }

    // PlayerInput(UnityEvents) 바인딩
    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    public void OnSprint(InputAction.CallbackContext ctx) => sprintHeld = ctx.ReadValueAsButton();

    /// <summary>
    /// 탐험 씬 기준의 매 프레임 처리.
    /// 커서 정책 적용, UI 차단 상태 확인, 이동 처리, Secret Art FX 복구를 수행한다.
    /// </summary>
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "Battle")
            return;

        ApplyCursorPolicy();

        // UI가 열려 있으면 이동/달리기/발소리 상태를 강제로 정지
        if (GameContext.I != null && GameContext.I.IsUIBlockingLook)
        {
            ForceStopForUI();
            return;
        }

        HandleMovement();

        // 상태(런타임)와 FX(오브젝트 active)가 어긋나면 자동 복구
        AutoRepairSecretArtFxMismatch();
    }

    /// <summary>
    /// 런타임 상태(secretArtReady)와 실제 FX 활성 상태가 어긋났을 때
    /// 자동으로 다시 맞춰주는 안전 복구 함수.
    /// </summary>
    private void AutoRepairSecretArtFxMismatch()
    {
        // Battle 제외는 Update에서 이미 처리
        var g = GameContext.I;
        if (g == null) return;

        var my = FindMyRuntime();
        if (my == null) return;

        if (secretArtFxRoot == null) return;

        bool shouldOn = my.secretArtReady;
        bool isOn = secretArtFxRoot.activeSelf;

        // 상태는 ON인데 FX가 OFF면 즉시 복구
        if (shouldOn && !isOn)
            ApplySecretArtFx(true);

        // (원하면) 상태는 OFF인데 FX가 ON이면 끄기까지 같이 보장
        if (!shouldOn && isOn)
            ApplySecretArtFx(false);
    }

    // E = 비술 준비(현재 "활성 캐릭터"만 발동)
    /// <summary>
    /// 탐험 씬에서 Secret Art 입력을 처리한다.
    /// 현재 활성 캐릭터 기준으로 비술 준비 상태를 켜고,
    /// 성공 시 Secret Art 포인트를 1 소모한다.
    /// </summary>
    public void OnSecretArt(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (SceneManager.GetActiveScene().name == "Battle") return;

        // ★ UI(인벤/캐릭터창 등)가 열려 Look을 막는 동안은 비술 입력 무시
        var g = GameContext.I;
        if (g != null && g.IsUIBlockingLook) return;

        if (g == null) return;

        var cr = g.GetActiveCharacter();
        if (cr == null || cr.data == null) return;

        if (g.secretArtPoints <= 0)
        {
            return;
        }

        if (cr.secretArtReady)
        {
            return;
        }

        cr.secretArtReady = true;
        g.secretArtPoints -= 1;

        // 활성 캐릭터의 컨트롤러에서 E를 처리하고 있으므로, 조건 없이 FX ON 보장
        ApplySecretArtFx(true);

        // (선택) 안전망: 혹시라도 바로 꺼지면 다음 프레임에 한번 더 동기화
        StartCoroutine(CoSyncSecretArtFxNextFrame());

        // 튜토리얼: 키소라(DefBuffParty) 비술을 실제 성공 발동했을 때 완료
        if (cr.data != null && cr.data.secretArtType == SecretArtType.DefBuffParty)
        {
            TutorialManager.I?.CompleteSecretArtTutorial();
        }

    }

    /// <summary>
    /// Secret Art 준비 직후 다음 프레임에 FX 상태를 한 번 더 동기화한다.
    /// 즉시 꺼지는 예외 상황을 방지하기 위한 안전망이다.
    /// </summary>
    private IEnumerator CoSyncSecretArtFxNextFrame()
    {
        yield return null;
        SyncSecretArtFxWithMyRuntime();
    }

    // ─────────────────────────────────────────
    // Movement
    // ─────────────────────────────────────────
    /// <summary>
    /// 탐험 씬 이동 처리.
    /// 카메라 기준 방향 이동, 회전, 중력, 애니메이션, 발소리 등을 갱신한다.
    /// </summary>
    void HandleMovement()
    {
        // =========================
        // 즉시 안정화 가드(핵심)
        // =========================
        if (cc == null) return;
        if (!cc.enabled) return;                 // CharacterController가 꺼져있으면 Move 금지
        if (!gameObject.activeInHierarchy) return; // 오브젝트 비활성이면 Move 금지

        // (선택) UI가 열려있으면 이동 입력/중력도 멈추고 싶다면:
        // if (GameContext.I != null && GameContext.I.IsUIBlockingLook) return;

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);

        // 이동 튜토리얼은 TutorialManager가 CharacterController.velocity로 판정
        // 달리기 튜토리얼은 여기서 실제 Shift + 이동 입력을 감지
        if (sprintHeld && input.sqrMagnitude > 0.01f)
        {
            TutorialManager.I?.CompleteSprintTutorial();
        }

        //// 튜토리얼: 실제 이동 입력 감지
        //if (input.sqrMagnitude > 0.01f)
        //{
        //    TutorialManager.I?.CompleteMoveTutorial();
        //}

        Vector3 dir;

        if (cam != null)
        {
            Vector3 camF = cam.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = cam.right; camR.y = 0f; camR.Normalize();
            dir = camF * input.z + camR * input.x;
        }
        else dir = input;

        Vector3 planar = dir;
        planar.y = 0f;

        if (planar.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(planar, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime);
        }

        bool grounded =
            cc.isGrounded ||
            Physics.SphereCast(
                transform.position + Vector3.up * 0.05f,
                cc.radius * 0.95f,
                Vector3.down,
                out _,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);

        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        float speed = sprintHeld ? sprintSpeed : moveSpeed;

        Vector3 motion =
            (planar.sqrMagnitude > 0.0001f ? planar.normalized * speed : Vector3.zero)
            + Vector3.up * velocity.y;

        cc.Move(motion * Time.deltaTime);

        if (animator)
        {
            Vector3 v = cc.velocity;
            v.y = 0f;
            animator.SetFloat(HashSpeed, v.magnitude);
            animator.SetBool(HashGrounded, grounded);
            animator.SetBool(HashIsSprinting, sprintHeld);
        }

        UpdateFootstepLoop(grounded, sprintHeld);
    }

    public void ForceStopForUI()
    {
        moveInput = Vector2.zero;
        sprintHeld = false;

        // 중력은 유지하되, 수평 이동은 끊기
        velocity.x = 0f;
        velocity.z = 0f;

        if (animator != null)
        {
            animator.SetFloat(HashSpeed, 0f);
            animator.SetBool(HashIsSprinting, false);
        }

        StopFootstepLoopImmediate();
    }

    void UpdateFootstepLoop(bool grounded, bool sprinting)
    {
        if (!enableFootstepSfx)
        {
            StopFootstepLoopImmediate();
            return;
        }

        if (footstepLoopSource == null)
            return;

        bool uiBlocked = (GameContext.I != null && GameContext.I.IsUIBlockingLook);
        if (uiBlocked)
        {
            StopFootstepLoopImmediate();
            return;
        }

        if (!grounded)
        {
            StopFootstepLoopSmooth();
            return;
        }

        Vector3 v = cc.velocity;
        v.y = 0f;

        float planarSpeed = v.magnitude;
        bool shouldPlay = planarSpeed >= footstepMinSpeed;

        if (shouldPlay)
        {
            if (_footstepFadeCo != null)
            {
                StopCoroutine(_footstepFadeCo);
                _footstepFadeCo = null;
            }

            footstepLoopSource.pitch = sprinting ? runFootstepPitch : walkFootstepPitch;
            footstepLoopSource.volume = sprinting ? runFootstepVolume : walkFootstepVolume;

            if (!footstepLoopSource.isPlaying)
                footstepLoopSource.Play();
        }
        else
        {
            StopFootstepLoopSmooth();
        }
    }

    void StopFootstepLoopImmediate()
    {
        if (footstepLoopSource == null) return;

        if (_footstepFadeCo != null)
        {
            StopCoroutine(_footstepFadeCo);
            _footstepFadeCo = null;
        }

        if (footstepLoopSource.isPlaying)
            footstepLoopSource.Stop();
    }

    void StopFootstepLoopSmooth()
    {
        if (footstepLoopSource == null) return;
        if (!footstepLoopSource.isPlaying) return;
        if (_footstepFadeCo != null) return;

        _footstepFadeCo = StartCoroutine(CoFadeOutFootstep());
    }

    IEnumerator CoFadeOutFootstep()
    {
        if (footstepLoopSource == null)
        {
            _footstepFadeCo = null;
            yield break;
        }

        float startVol = footstepLoopSource.volume;
        float t = 0f;
        float dur = Mathf.Max(0.01f, footstepFadeOutTime);

        while (t < dur)
        {
            t += Time.deltaTime;

            if (footstepLoopSource == null)
            {
                _footstepFadeCo = null;
                yield break;
            }

            footstepLoopSource.volume = Mathf.Lerp(startVol, 0f, t / dur);
            yield return null;
        }

        if (footstepLoopSource != null)
        {
            footstepLoopSource.Stop();
            footstepLoopSource.volume = walkFootstepVolume;
            footstepLoopSource.pitch = walkFootstepPitch;
        }

        _footstepFadeCo = null;
    }

    // ─────────────────────────────────────────
    // Secret Art FX
    // ─────────────────────────────────────────
    private void ApplySecretArtFx(bool on)
    {
        if (secretArtFxRoot == null)
        {
            Debug.LogWarning($"[SecretArtFX] secretArtFxRoot is NULL on {gameObject.name}");
            return;
        }

        secretArtFxRoot.SetActive(on);

        if (!on) return;

        // ★ 핵심: 가끔 안 보이는 케이스 방지용 “강제 리셋 재생”
        var ps = secretArtFxRoot.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < ps.Length; i++)
        {
            ps[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps[i].Clear(true);
            ps[i].Play(true);
        }

        // (선택) Renderer가 꺼져있으면 보일 수가 없으니 같이 켬
        var rends = secretArtFxRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
            rends[i].enabled = true;
    }

    private CharacterRuntime FindMyRuntime()
    {
        var g = GameContext.I;
        if (g == null || g.party == null) return null;

        if (selfCharacterData == null) return null;

        for (int i = 0; i < g.party.Count; i++)
        {
            var cr = g.party[i];

            // ★ 변경: 참조 비교(==) 대신 ID 기반 비교
            if (cr != null && SameCharacter(cr.data, selfCharacterData))
                return cr;
        }

        return null;
    }

    /// <summary>
    /// 현재 이 캐릭터의 런타임 상태를 기준으로
    /// Secret Art FX 오브젝트의 활성 상태를 동기화한다.
    /// </summary>
    private void SyncSecretArtFxWithMyRuntime()
    {
        // Battle에서 생성된 비주얼(필드 컨트롤러가 붙어있는 프리팹)이
        // OnEnable로 FX를 켜버리는 일을 막기 위해:
        // - Battle 씬이면 무조건 OFF.
        if (SceneManager.GetActiveScene().name == "Battle")
        {
            ApplySecretArtFx(false);
            return;
        }

        var my = FindMyRuntime();
        if (my == null)
        {
            ApplySecretArtFx(false);
            return;
        }

        ApplySecretArtFx(my.secretArtReady);
    }

    void ApplyCursorPolicy(bool force = false)
    {
        if (SceneManager.GetActiveScene().name == "Battle")
            return;

        // 인벤 등 UI가 Look을 막는 상태면: 무조건 커서 풀고 보이게
        bool uiBlock = (GameContext.I != null && GameContext.I.IsUIBlockingLook);

        // Alt 홀드 동안도 커서 풀기
        bool altHold = Input.GetKey(cursorHoldKey);

        bool wantFreeCursor = uiBlock || altHold;

        if (!lockCursorByDefault && !wantFreeCursor && !force)
            return; // 기본 잠금 정책 안 쓰면 패스

        if (wantFreeCursor)
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
