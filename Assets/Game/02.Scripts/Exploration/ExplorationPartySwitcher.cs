using UnityEngine;
using UnityEngine.SceneManagement;

namespace PiecesOfFantasy.Exploration
{
    public class ExplorationPartySwitcher : MonoBehaviour
    {
        [Header("Spawn Point (Optional)")]
        [Tooltip("비워두면 스위처 위치에서 스폰합니다. (권장: SP_Default 같은 스폰 포인트를 만들어 연결)")]
        [SerializeField] private Transform spawnPoint;

        [Header("Ground Snap (Recommended)")]
        [Tooltip("지면 스냅에 사용할 레이어 마스크. Ground 레이어만 지정 권장. 비워두면 Everything(~0)")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("Raycast 시작 높이(스폰 위치 위에서 아래로 쏨)")]
        [SerializeField] private float groundRayStartHeight = 10f;

        [Tooltip("Raycast 최대 거리")]
        [SerializeField] private float groundRayDistance = 50f;

        [Tooltip("지면에 박히는 것 방지용 Y 오프셋")]
        [SerializeField] private float groundSnapYOffset = 0.05f;

        [Header("Camera Follow")]
        [SerializeField] private ExplorationCameraFollow cameraFollow;

        [Header("Options")]
        [SerializeField] private bool carryOverTransform = true;

        [Header("UI (Optional)")]
        [SerializeField] private GatherListUIController gatherListUI;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private PlayerControllerHumanoid currentAvatar;
        private Transform currentRoot; // Instantiate된 프리팹 루트

        private bool _autoSavedAfterInitialSpawn = false;

        private void Awake()
        {
            if (cameraFollow == null)
                cameraFollow = FindFirstObjectByType<ExplorationCameraFollow>();

            if (gatherListUI == null)
                gatherListUI = FindFirstObjectByType<GatherListUIController>();
        }

        private void Start()
        {
            var ctx = GameContext.I;
            int startIndex = 0;

            if (ctx != null)
            {
                if (!ctx.EnsureActiveIsAlive())
                    startIndex = 0;
                else
                    startIndex = ctx.activePartyIndex;
            }

            SpawnActiveAvatar(startIndex, force: true);
        }

        private void Update()
        {
            // 전투 씬에서는 교대 금지
            if (SceneManager.GetActiveScene().name == "Battle")
                return;

            // UI가 열려있으면 교대 입력 금지
            if (GameContext.I != null && GameContext.I.IsUIBlockingLook)
                return;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TrySwitchTo(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TrySwitchTo(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TrySwitchTo(2);
        }

        public bool TrySwitchTo(int newIndex)
        {
            // UI가 열려있으면 교대 금지(최종 방어선)
            if (GameContext.I != null && GameContext.I.IsUIBlockingLook)
                return false;

            var ctx = GameContext.I;
            if (ctx == null || ctx.party == null || ctx.party.Count == 0) return false;

            if (newIndex < 0 || newIndex >= ctx.party.Count) return false;

            int oldIndex = ctx.activePartyIndex;
            if (newIndex == oldIndex) return false;

            // 파티 기준 교대 가능 여부 검사(HP<=0 차단)
            if (!ctx.TrySetActiveIndex(newIndex))
            {
                if (debugLog) Debug.LogWarning($"[ExplorationPartySwitcher] Switch denied: idx={newIndex} (hp<=0 또는 데이터 문제)");
                return false;
            }

            SpawnActiveAvatar(newIndex, force: false);
            return true;
        }

        private void SpawnActiveAvatar(int activeIndex, bool force)
        {
            var ctx = GameContext.I;
            if (ctx == null || ctx.party == null || ctx.party.Count == 0)
                return;

            activeIndex = Mathf.Clamp(activeIndex, 0, ctx.party.Count - 1);

            var cr = ctx.party[activeIndex];
            if (cr == null || cr.data == null) return;

            var prefab = cr.data.explorationPrefab;
            if (prefab == null) return;

            // 1) 기준 위치 결정
            Vector3 desiredPos;
            Quaternion desiredRot;

            if (carryOverTransform && currentAvatar != null)
            {
                // 교대 시: 기존 캐릭터 위치 그대로 사용
                desiredPos = currentAvatar.transform.position;
                desiredRot = currentAvatar.transform.rotation;
            }
            else
            {
                // 최초 스폰 시 우선순위:
                // 1) Continue 로드 좌표
                // 2) Battle 복귀 좌표
                // 3) 기본 spawnPoint
                if (ctx != null && ctx.hasPendingLoadedWorldPosition)
                {
                    desiredPos = ctx.pendingLoadedWorldPosition;
                    desiredRot = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

                    desiredPos = SnapToGround(desiredPos);

                    // 1회성 소비
                    ctx.hasPendingLoadedWorldPosition = false;

                    if (debugLog)
                        Debug.Log($"[ExplorationPartySwitcher] Using LOADED position: {desiredPos}");
                }
                else if (ctx != null && ctx.hasReturnPoint)
                {
                    desiredPos = ctx.returnPlayerPos;
                    desiredRot = ctx.returnPlayerRot;

                    desiredPos = SnapToGround(desiredPos);

                    // 1회성 소비
                    ctx.ClearReturnPoint();

                    if (debugLog)
                        Debug.Log($"[ExplorationPartySwitcher] Using RETURN position: {desiredPos}");
                }
                else
                {
                    desiredPos = (spawnPoint != null) ? spawnPoint.position : transform.position;
                    desiredRot = (spawnPoint != null) ? spawnPoint.rotation : transform.rotation;

                    desiredPos = SnapToGround(desiredPos);

                    if (debugLog)
                        Debug.Log($"[ExplorationPartySwitcher] Using DEFAULT spawn position: {desiredPos}");
                }
            }

            // 2) 기존 캐릭터 제거
            if (currentAvatar != null)
                GameContext.I?.ClearExplorationPlayer(currentAvatar.transform);

            if (currentRoot != null)
                Destroy(currentRoot.gameObject);

            currentAvatar = null;
            currentRoot = null;

            // 3) 새 캐릭터 생성
            var root = Instantiate(prefab, desiredPos, desiredRot);
            currentRoot = root.transform;

            currentAvatar = root.GetComponentInChildren<PlayerControllerHumanoid>(true);
            if (currentAvatar == null)
            {
                if (cameraFollow != null)
                    cameraFollow.SetTarget(currentRoot);

                if (gatherListUI == null) gatherListUI = FindFirstObjectByType<GatherListUIController>();
                gatherListUI?.RegisterPlayer(null); // 숨김

                TutorialPlayerGuideController.I?.ClearTargets();

                return;
            }

            // 4) CharacterController 안전 처리
            var cc = currentAvatar.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 5) 컨트롤러 기준 위치 보정 (한 번만)
            Vector3 delta = desiredPos - currentAvatar.transform.position;
            currentRoot.position += delta;
            currentRoot.rotation = desiredRot;

            if (cc != null) cc.enabled = true;

            // 6) 카메라 타겟
            if (cameraFollow != null)
                cameraFollow.SetTarget(currentAvatar.transform);

            // 미니맵/월드맵 타겟 갱신
            MiniMapController.I?.SetTarget(currentAvatar.transform);

            // 런타임 스폰된 플레이어를 UI에 주입
            if (gatherListUI == null) gatherListUI = FindFirstObjectByType<GatherListUIController>();
            gatherListUI?.RegisterPlayer(currentAvatar);

            // 저장용 실제 탐험 플레이어 등록
            GameContext.I?.RegisterExplorationPlayer(currentAvatar.transform);

            // 튜토리얼 플레이어 가이드가 현재 켜져 있다면,
            // 새로 교체된 플레이어를 즉시 반영
            TutorialPlayerGuideController.I?.SetPlayer(currentAvatar.transform);

            if (debugLog)
                Debug.Log($"[ExplorationPartySwitcher] Switched to {cr.data.name} at {currentAvatar.transform.position}");

            // 7) 최초 탐험 스폰 후 자동 저장 1회
            if (force && !_autoSavedAfterInitialSpawn)
            {
                _autoSavedAfterInitialSpawn = true;
                AutoSaveManager.I?.TryAutoSave("ExplorationEnter");
            }
        }

        // ─────────────────────────────────────────────
        // Ground Snap Helper
        // ─────────────────────────────────────────────
        private Vector3 SnapToGround(Vector3 pos)
        {
            Vector3 rayStart = pos + Vector3.up * groundRayStartHeight;

            if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out var hit,
                groundRayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
            {
                pos.y = hit.point.y + groundSnapYOffset;
            }

            return pos;
        }
    }
}
