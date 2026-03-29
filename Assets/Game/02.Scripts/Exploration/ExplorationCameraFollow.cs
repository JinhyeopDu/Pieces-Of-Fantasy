using UnityEngine;

namespace PiecesOfFantasy.Exploration
{
    public class ExplorationCameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;          // 따라갈 대상 (플레이어)
        [SerializeField] private float targetHeight = 1.6f; // 플레이어 머리 높이 근처

        [Header("Distance")]
        [SerializeField] private float distance = 8f;       // 기본 거리
        [SerializeField] private float minDistance = 3f;    // 최소 줌 인 거리
        [SerializeField] private float maxDistance = 12f;   // 최대 줌 아웃 거리
        [SerializeField] private float zoomSpeed = 5f;      // 마우스 휠 줌 속도

        [Header("Rotation (Mouse Orbit)")]
        [SerializeField] private float yaw = 0f;            // 좌우 각도 (Y축 회전)
        [SerializeField] private float pitch = 20f;         // 상하 각도 (X축 회전)
        [SerializeField] private float minPitch = -10f;     // 아래로 보는 제한
        [SerializeField] private float maxPitch = 60f;      // 위로 보는 제한
        [SerializeField] private float mouseSensitivityX = 5f;
        [SerializeField] private float mouseSensitivityY = 3f;
        [SerializeField] private bool invertY = false;      // 마우스 Y 반전 여부

        [Header("Smoothing")]
        [SerializeField] private float collisionLerpSpeed = 20f; // 충돌 시 거리 보간 속도

        [Header("Collision (Camera Clipping)")]
        [SerializeField] private LayerMask collisionMask;   // 지형/벽 등 카메라가 막혀야 할 레이어
        [SerializeField] private float collisionRadius = 0.2f;  // SphereCast 반경
        [SerializeField] private float collisionBuffer = 0.2f;  // 벽에서 살짝 띄우는 거리

        private float _currentDistance; // 실제 적용 중인 거리 (충돌 보정용)

        private void Start()
        {
            // 1) target이 비어 있으면 Tag로 Player 자동 찾기
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                }
                else
                {
                    Debug.LogWarning("[ExplorationCameraFollow] 'Player' 태그를 가진 오브젝트를 찾지 못했습니다.");
                }
            }

            // 2) 현재 카메라 회전값을 기준으로 yaw/pitch 초기화
            Vector3 euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;

            _currentDistance = distance;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            bool uiBlock = (GameContext.I != null && GameContext.I.IsUIBlockingLook);
            bool altHold = Input.GetKey(KeyCode.LeftAlt); // Alt 홀드도 막고 싶으면 유지

            if (uiBlock || altHold)
            {
                UpdateCamera(); // 따라가기/충돌만 유지
                return;
            }

            HandleInput();
            UpdateCamera();
        }


        /// <summary>
        /// 마우스 입력 처리 (회전 + 줌)
        /// </summary>
        private void HandleInput()
        {
            // --- 마우스 회전 ---
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * mouseSensitivityX;

            float vertical = invertY ? mouseY : -mouseY;
            pitch += vertical * mouseSensitivityY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // --- 마우스 휠 줌 ---
            // 채집 리스트가 떠 있으면(휠로 선택 이동) 카메라 줌은 무시
            bool blockZoom = (GameContext.I != null && GameContext.I.IsGatherListOpen);
            if (!blockZoom)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    distance -= scroll * zoomSpeed;
                    distance = Mathf.Clamp(distance, minDistance, maxDistance);
                }
            }
        }

        /// <summary>
        /// 카메라 위치/회전 계산 + 충돌 처리
        /// </summary>
        private void UpdateCamera()
        {
            // 1) 기준점: 플레이어 위치 + 머리 높이
            Vector3 focusPoint = target.position + Vector3.up * targetHeight;

            // 2) yaw/pitch로 회전 쿼터니언 생성
            //    ★ roll(Z축 회전)은 항상 0으로 고정
            Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

            // 3) 카메라가 서 있어야 할 방향 (뒤쪽)
            Vector3 idealDir = targetRot * new Vector3(0f, 0f, -1f);

            // 4) 충돌 없을 때의 이상적인 카메라 위치
            float desiredDistance = distance;
            Vector3 idealCamPos = focusPoint + idealDir * distance;

            // 5) SphereCast로 플레이어~카메라 사이에 장애물이 있는지 체크
            Ray ray = new Ray(focusPoint, idealDir);
            RaycastHit hit;
            if (Physics.SphereCast(
                    ray,
                    collisionRadius,
                    out hit,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                // 벽 바로 앞까지 카메라를 당겨오기
                desiredDistance = Mathf.Clamp(hit.distance - collisionBuffer, minDistance, maxDistance);
            }

            // 6) 거리만 부드럽게 보간 (충돌 대응)
            _currentDistance = Mathf.Lerp(_currentDistance, desiredDistance, collisionLerpSpeed * Time.deltaTime);

            // 7) 최종 카메라 위치
            Vector3 finalCamPos = focusPoint + idealDir * _currentDistance;

            // 8) ★ 위치는 바로 세팅 (Lerp 안 씀 → 플레이어와의 거리 유지, 어지럼증 감소)
            transform.position = finalCamPos;

            // 9) ★ 회전도 우리가 계산한 값으로 바로 세팅 (roll = 0 고정)
            transform.rotation = targetRot;

            // 혹시라도 부동소수점 오차가 걱정되면 아래처럼 한번 더 보정 가능:
            // var e = transform.rotation.eulerAngles;
            // transform.rotation = Quaternion.Euler(e.x, e.y, 0f);
        }

        // 필요 시 외부에서 타겟을 바꾸고 싶을 때 사용
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
