using UnityEngine;

namespace G1
{
    /// <summary>
    /// neck.x 본의 회전을 Animator와 독립적으로 제어한다.
    /// LateUpdate에서 Animator 업데이트 이후 회전을 덮어씌워 캐릭터가 좌우로 돌아보는 2차 표현 레이어를 구현한다.
    /// 플레이어 사망 시 OnPlayerDead 이벤트를 수신해 자동으로 비활성화된다.
    /// </summary>
    public class NeckController : MonoBehaviour
    {
        [Header("플레이어 컨트롤러")]
        /// <summary>Idle 상태 여부 판단용. 비어 있으면 Awake에서 자동 탐색한다.</summary>
        [SerializeField] private PlayerController playerController;

        [Header("목 본 설정")]
        /// <summary>제어할 neck.x 본 Transform. 비어 있으면 neckBoneName으로 자동 탐색한다.</summary>
        [SerializeField] private Transform neckBone;
        /// <summary>자동 탐색 시 사용할 본 이름 (대소문자 무관)</summary>
        [SerializeField] private string neckBoneName = "neck.x";

        [Header("회전 초기화할 자식 본 (예: head.x)")]
        /// <summary>
        /// LateUpdate마다 localRotation을 identity로 초기화할 자식 본 이름 목록.
        /// Animator가 자식 본에 회전을 적용해 원하는 결과가 나오지 않을 때 사용한다.
        /// </summary>
        [SerializeField] private string[] resetChildBoneNames = { "head.x" };

        [Header("X축 고정 각도 (전후 기울기)")]
        /// <summary>neck.x 본의 X축 회전각 (도 단위). 양수: 앞으로 기울기, 음수: 뒤로 기울기.</summary>
        [SerializeField] private float pitchAngle = 0f;

        [Header("Y축 좌우 회전 설정")]
        /// <summary>바라볼 대상 Transform. null이면 유휴 좌우 돌아보기 동작을 수행한다.</summary>
        [SerializeField] private Transform lookTarget;
        /// <summary>좌우 최대 회전 각도 (도 단위)</summary>
        [SerializeField] private float yawClampAngle = 60f;
        /// <summary>Y축 회전 보간 속도. 값이 클수록 빠르게 목표 각도에 도달한다.</summary>
        [SerializeField] private float rotationSpeed = 5f;

        [Header("유휴 좌우 돌아보기 (타겟 없을 때)")]
        /// <summary>유휴 좌우 돌아보기 활성화 여부</summary>
        [SerializeField] private bool enableIdleLook = true;
        /// <summary>유휴 상태에서 좌우로 돌아보는 최대 각도 (도 단위)</summary>
        [SerializeField] private float idleLookAngle = 40f;
        /// <summary>한쪽 방향을 바라본 후 반대 방향으로 전환하기까지의 대기 시간 (초)</summary>
        [SerializeField] private float idleLookInterval = 2f;

        private Transform[] resetChildBones;
        private float currentYaw = 0f;
        private float targetYaw = 0f;
        private float idleTimer = 0f;
        private float idleLookSign = 1f;

        // OnEnable/OnDisable에서 구독 해제를 위해 람다를 필드로 캐싱
        private System.Action<Transform> onAttackTargetChanged;

        // ─────────────────────────────────────────
        // 프로퍼티
        // ─────────────────────────────────────────

        /// <summary>
        /// 바라볼 대상 Transform. null 설정 시 idleTimer를 초기화해 유휴 돌아보기를 처음부터 시작한다.
        /// </summary>
        public Transform LookTarget
        {
            get => lookTarget;
            set
            {
                lookTarget = value;
                if (value == null) idleTimer = 0f;
            }
        }

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>인스펙터 미할당 시 컴포넌트를 자동 탐색하고 neck.x 본과 자식 본을 캐싱한다.</summary>
        private void Awake()
        {
            // OnEnable보다 먼저 람다를 생성해야 null 구독을 방지할 수 있음
            // (Awake → OnEnable 순서가 보장되지만, 람다 생성을 Awake에서만 하므로 OnEnable에서 항상 유효)
            onAttackTargetChanged = t => LookTarget = t;
            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (neckBone == null)
                neckBone = transform.FindDeep(neckBoneName);

            if (neckBone == null)
            {
                Debug.LogError(
                    $"[NeckController] 목 본 '{neckBoneName}'을 찾을 수 없습니다. " +
                    "인스펙터에서 직접 할당해주세요.", this);
                return;
            }

            CacheResetChildBones();
        }

        /// <summary>공격 타겟 변경 및 플레이어 사망 이벤트 구독</summary>
        private void OnEnable()
        {
            PlayerController.OnAttackTargetChanged += onAttackTargetChanged;
            PlayerController.OnPlayerDead += OnPlayerDead;
        }

        /// <summary>공격 타겟 변경 및 플레이어 사망 이벤트 구독 해제</summary>
        private void OnDisable()
        {
            PlayerController.OnAttackTargetChanged -= onAttackTargetChanged;
            PlayerController.OnPlayerDead -= OnPlayerDead;
        }

        /// <summary>
        /// Animator가 본 위치를 갱신한 이후 neck.x 본의 회전을 덮어쓴다.
        /// 글로벌 Y축 기준으로 yaw를 적용하므로 척추가 기울어져도 좌우 회전이 자연스럽다.
        /// </summary>
        private void LateUpdate()
        {
            if (neckBone == null) return;

            // Idle 상태가 아니면 정면(0도)으로 서서히 되돌리고 종료
            if (playerController != null && !playerController.IsIdle)
            {
                targetYaw = 0f;
                currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);
                neckBone.rotation = Quaternion.AngleAxis(transform.eulerAngles.y + currentYaw, Vector3.up)
                                  * Quaternion.Euler(pitchAngle, 0f, 0f);
                ResetChildBoneRotations();
                return;
            }

            // 1. 목표 yaw 결정
            if (lookTarget != null)
            {
                // 타겟 방향을 캐릭터 루트 로컬 공간으로 변환 (척추 회전 영향 배제)
                Vector3 localDir = transform.InverseTransformDirection(lookTarget.position - neckBone.position);
                localDir.y = 0f; // 수평 방향만 사용 (상하 각도는 pitchAngle로 별도 제어)

                if (localDir.sqrMagnitude > 0.001f)
                    targetYaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
            }
            else if (enableIdleLook)
            {
                // 타겟 없음: 좌우를 번갈아 돌아보는 유휴 동작
                idleTimer += Time.deltaTime;
                if (idleTimer >= idleLookInterval)
                {
                    idleTimer = 0f;
                    idleLookSign = -idleLookSign;
                }
                targetYaw = idleLookAngle * idleLookSign;
            }
            else
            {
                targetYaw = 0f;
            }

            // 2. 좌우 최대 각도 제한
            targetYaw = Mathf.Clamp(targetYaw, -yawClampAngle, yawClampAngle);

            // 3. 부드러운 보간
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);

            // 4. 루트 Y축 회전 기준 절대값으로 적용 — 누적 곱 방지
            neckBone.rotation = Quaternion.AngleAxis(transform.eulerAngles.y + currentYaw, Vector3.up)
                              * Quaternion.Euler(pitchAngle, 0f, 0f);

            // 5. 자식 본 회전 초기화 (Animator가 자식 본에 적용한 회전 제거)
            ResetChildBoneRotations();
        }

        // ─────────────────────────────────────────
        // private 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// resetChildBoneNames를 순회해 탐색에 성공한 본만 캐싱한다.
        /// 찾지 못한 이름은 경고 로그를 출력한다.
        /// </summary>
        private void CacheResetChildBones()
        {
            if (resetChildBoneNames == null || resetChildBoneNames.Length == 0)
            {
                resetChildBones = System.Array.Empty<Transform>();
                return;
            }

            var found = new System.Collections.Generic.List<Transform>();
            for (int i = 0; i < resetChildBoneNames.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(resetChildBoneNames[i])) continue;

                Transform bone = transform.FindDeep(resetChildBoneNames[i]);
                if (bone != null)
                    found.Add(bone);
                else
                    Debug.LogWarning(
                        $"[NeckController] 초기화 대상 자식 본 '{resetChildBoneNames[i]}'을 찾을 수 없습니다.", this);
            }

            resetChildBones = found.ToArray();
        }

        /// <summary>캐싱된 자식 본들의 localRotation을 identity로 초기화한다.</summary>
        private void ResetChildBoneRotations()
        {
            for (int i = 0; i < resetChildBones.Length; i++)
                resetChildBones[i].localRotation = Quaternion.identity;
        }

        /// <summary>플레이어 사망 시 이 컴포넌트를 비활성화한다.</summary>
        private void OnPlayerDead() => enabled = false;

        // ─────────────────────────────────────────
        // public 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 목의 Y축(좌우) 목표 각도를 직접 설정한다.
        /// lookTarget이 할당된 경우 LateUpdate에서 이 값은 무시된다.
        /// </summary>
        /// <param name="angle">목표 각도 (도 단위, 양수=오른쪽, 음수=왼쪽)</param>
        public void SetYAngle(float angle) => targetYaw = angle;

        /// <summary>
        /// lookTarget을 해제한다.
        /// enableIdleLook이 true이면 유휴 좌우 돌아보기로 전환되고, false이면 정면으로 되돌아간다.
        /// </summary>
        public void ClearLookTarget()
        {
            LookTarget = null;
            targetYaw = 0f;
        }
    }
}
