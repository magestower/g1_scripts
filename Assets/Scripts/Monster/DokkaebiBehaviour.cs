using UnityEngine;
using UnityEngine.AI;

namespace G1
{
    /// <summary>
    /// 도깨비 몬스터 AI 컨트롤러.
    /// Idle → Chase(감지) → Attack(근접) → Dead 상태 머신으로 동작한다.
    /// NavMeshAgent로 이동하며, MonsterManager가 배정한 슬롯 위치를 목적지로 사용한다.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class DokkaebiBehaviour : MonsterBase
    {
        // ─────────────────────────────────────────
        // Animator 파라미터 해시
        // ─────────────────────────────────────────
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        // ─────────────────────────────────────────
        // AI 상태 정의
        // ─────────────────────────────────────────

        /// <summary>도깨비의 행동 상태</summary>
        private enum EnemyState
        {
            /// <summary>플레이어 미감지, 정지</summary>
            Idle,
            /// <summary>플레이어 감지, 슬롯/플레이어 방향으로 이동 중</summary>
            Chase,
            /// <summary>공격 범위 내, 공격 중</summary>
            Attack,
            /// <summary>사망</summary>
            Dead
        }

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────
        [Header("AI 범위")]
        [SerializeField] private float detectRadius = 8f;
        [SerializeField] private float attackRadius = 1.8f;

        [Header("이동/공격")]
        [SerializeField] private float moveSpeed = 2.5f;
        /// <summary>초당 회전 각도(degree). NavMeshAgent 회전을 무시하고 직접 보간 회전에 사용된다.</summary>
        [SerializeField] private float rotationSpeed = 480f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private int attackDamage = 5;
        /// <summary>공격 애니메이션에서 데미지를 적용할 타이밍 (0~1 정규화 값)</summary>
        [SerializeField] private float hitTimingNormalized = 0.5f;

        /// <inheritdoc/>
        public override float HitTimingNormalized => hitTimingNormalized;

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        private EnemyState currentState = EnemyState.Idle;
        private Transform playerTransform;
        private IDamageable playerDamageable;
        private NavMeshAgent agent;
        private NavMeshObstacle obstacle;
        private float lastAttackTime = -999f;
        private bool atSlot;

        // 부동소수점 이동 연산 오차 허용 범위
        private const float DistanceTolerance = 0.10f;

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>
        /// NavMeshAgent와 플레이어 트랜스폼을 캐싱한다.
        /// NavMeshAgent 회전은 RotateToward()로 직접 제어하므로 angularSpeed를 0으로 설정한다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            agent = GetComponent<NavMeshAgent>();
            agent.speed = moveSpeed;
            agent.angularSpeed = 0f;   // 회전은 RotateToward()로 직접 처리
            agent.acceleration = 999f; // 즉각 가속으로 이동 반응성 확보
            agent.autoBraking = false;
            agent.stoppingDistance = 0f;

            obstacle = GetComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.enabled = false; // 초기에는 Agent가 이동을 담당

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerObj.TryGetComponent(out playerDamageable);
            }
        }

        /// <summary>매 프레임 수평 거리를 계산해 상태 머신을 갱신한다.</summary>
        private void Update()
        {
            if (IsDead) return;
            if (playerTransform == null) return;

            Vector3 flatDir = playerTransform.position - transform.position;
            flatDir.y = 0f;
            float flatDist = flatDir.magnitude;

            UpdateState(flatDist);
            ExecuteState(flatDir, flatDist);
            ApplySeparation();
        }

        // ─────────────────────────────────────────
        // 상태 머신
        // ─────────────────────────────────────────

        /// <summary>
        /// 수평 거리에 따라 현재 상태를 전이한다.
        /// ring 0 몬스터는 슬롯 도착 후에도 attackRadius 안까지 추가 접근한다.
        /// ring 1+ 몬스터는 슬롯 도착 시 그 자리에서 대기한다.
        /// </summary>
        /// <param name="flatDist">플레이어까지의 수평(XZ) 거리</param>
        private void UpdateState(float flatDist)
        {
            bool hasSlot = AssignedSlotRing >= 0;

            // ring 0: 슬롯 도착 후 attackRadius 밖이면 추가 접근 필요
            bool needsApproach = atSlot && AssignedSlotRing == 0
                && flatDist > attackRadius + DistanceTolerance;

            // Chase 유지 조건: 슬롯 미배정 / 슬롯 미도착 / ring 0 추가 접근 중
            bool slotReachable = !hasSlot || !atSlot || needsApproach;

            if (flatDist <= (attackRadius + DistanceTolerance) && !slotReachable)
            {
                // Obstacle 모드 해제 — ring 0은 obstacle이 꺼져있으므로 조건 무관하게 atSlot 리셋
                if (obstacle.enabled)
                    SetObstacleMode(false);
                atSlot = false;
                currentState = EnemyState.Attack;
            }
            else if (flatDist <= detectRadius)
                currentState = EnemyState.Chase;
            else
            {
                // Idle 전환 시 Obstacle 모드 해제 — ring 0 포함 항상 atSlot 리셋
                if (obstacle.enabled)
                    SetObstacleMode(false);
                atSlot = false;
                currentState = EnemyState.Idle;
            }
        }

        /// <summary>현재 상태에 맞는 행동을 실행한다.</summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        /// <param name="flatDist">플레이어까지의 수평(XZ) 거리</param>
        private void ExecuteState(Vector3 flatDir, float flatDist)
        {
            switch (currentState)
            {
                case EnemyState.Idle: HandleIdle(); break;
                case EnemyState.Chase: HandleChase(flatDir, flatDist); break;
                case EnemyState.Attack: HandleAttack(flatDir); break;
            }
        }

        // ─────────────────────────────────────────
        // 각 상태 처리
        // ─────────────────────────────────────────

        /// <summary>지정 방향을 향해 rotationSpeed로 보간 회전한다.</summary>
        /// <param name="dir">목표 방향 벡터 (Y=0 권장)</param>
        private void RotateToward(Vector3 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return;
            Quaternion target = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        /// <summary>Idle: Agent를 정지시키고 Speed를 0으로 유지한다.</summary>
        private void HandleIdle()
        {
            StopAgent();
            animator.SetFloat(SpeedHash, 0f);
        }

        /// <summary>
        /// Chase: NavMeshAgent로 슬롯 위치(또는 플레이어)를 향해 이동한다.
        /// ring 0 + atSlot 상태에서는 플레이어 방향으로 추가 접근한다.
        /// ring 1+ + atSlot 상태에서는 제자리 대기한다.
        /// </summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        /// <param name="flatDist">플레이어까지의 수평(XZ) 거리</param>
        private void HandleChase(Vector3 flatDir, float flatDist)
        {
            if (AssignedSlotRing >= 0)
            {
                Vector3 toSlot = AssignedSlotPos - transform.position;
                toSlot.y = 0f;
                float targetDist = toSlot.magnitude;

                // arriveThresh: NavMeshAgent 정지 오차를 고려해 고정값 사용
                // departThresh: 플레이어 이동으로 슬롯이 갱신될 때 진동 방지를 위해 넉넉하게 설정
                const float arriveThresh = 0.5f;
                const float departThresh = 1.2f;

                // 슬롯 도착 판정
                if (targetDist <= arriveThresh)
                {
                    // ring 0은 추가 접근이 필요하므로 Agent 모드 유지, ring 1+만 장애물 등록
                    // atSlot이 이미 true면 SetObstacleMode 중복 호출 방지
                    if (!atSlot)
                    {
                        atSlot = true;
                        if (AssignedSlotRing != 0)
                            SetObstacleMode(true);
                    }
                    // ring 0은 추가 접근이 남아있으므로 return하지 않고 아래 이동 로직으로 진행
                    if (AssignedSlotRing != 0)
                    {
                        RotateToward(flatDir);
                        animator.SetFloat(SpeedHash, 0f);
                        return;
                    }
                }

                // 이탈 판정 — 슬롯이 너무 멀어진 경우 atSlot 해제 후 새 슬롯 재배정 요청
                // return으로 이번 프레임 이동을 스킵해 Vector3.zero로의 이동 방지
                if (atSlot && targetDist > departThresh)
                {
                    atSlot = false;
                    if (AssignedSlotRing != 0)
                        SetObstacleMode(false); // ring 1+: 장애물 해제 후 Agent 재활성
                    MonsterManager.Instance?.RequestReassign(this);
                    return;
                }

                if (atSlot)
                {
                    if (AssignedSlotRing == 0 && flatDist > attackRadius + DistanceTolerance)
                    {
                        // ring 0: 플레이어 방향으로 추가 접근
                        MoveAgent(playerTransform.position);
                    }
                    else
                    {
                        // ring 1+: Obstacle 모드로 제자리 대기 (agent 비활성 상태이므로 StopAgent 불필요)
                        RotateToward(flatDir);
                        animator.SetFloat(SpeedHash, 0f);
                        return;
                    }
                }
                else
                {
                    // 슬롯을 향해 이동
                    MoveAgent(AssignedSlotPos);
                }
            }
            else
            {
                // 슬롯 미배정 — 플레이어 방향으로 직접 이동
                atSlot = false;
                MoveAgent(playerTransform.position);
            }

            // 이동 방향으로 회전 — velocity 기반, 없으면 플레이어 방향 (Obstacle 모드면 flatDir만 사용)
            if (agent.enabled)
            {
                Vector3 velocity = agent.velocity;
                velocity.y = 0f;
                RotateToward(velocity.sqrMagnitude > 0.01f ? velocity : flatDir);
                bool isMoving = agent.velocity.magnitude > 0.1f || agent.hasPath || agent.pathPending;
                animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
            }
            else
            {
                RotateToward(flatDir);
                animator.SetFloat(SpeedHash, 0f);
            }
        }

        /// <summary>
        /// Attack: Agent를 정지하고 플레이어 방향으로 회전하며 쿨다운마다 공격한다.
        /// </summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        private void HandleAttack(Vector3 flatDir)
        {
            StopAgent();
            animator.SetFloat(SpeedHash, 0f);
            RotateToward(flatDir);

            if (playerDamageable == null || playerDamageable.IsDead) return;

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.ResetTrigger(AttackHash); // 누적 트리거 초기화 후 재설정
                animator.SetTrigger(AttackHash);
            }
        }

        /// <summary>NavMeshAgent에 목적지를 설정하고 이동을 활성화한다.</summary>
        /// <param name="destination">목적지 월드 좌표</param>
        private void MoveAgent(Vector3 destination)
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        /// <summary>
        /// 이동 중일 때만 분리 벡터를 Warp로 적용해 몬스터 간 겹침을 완화한다.
        /// 정지 상태(atSlot 대기, Attack)에서는 Warp를 호출하지 않아 경로 리셋을 방지한다.
        /// </summary>
        private void ApplySeparation()
        {
            if (!agent.enabled) return;   // Obstacle 모드에서는 스킵
            if (!agent.isOnNavMesh) return;
            if (agent.isStopped) return;  // 정지 중에는 Warp 금지

            Vector3 sep = SeparationVec;
            sep.y = 0f;
            if (sep.sqrMagnitude < 0.001f) return;

            agent.Warp(transform.position + sep * Time.deltaTime);
        }

        /// <summary>NavMeshAgent 이동을 정지한다.</summary>
        private void StopAgent()
        {
            if (!agent.enabled) return;
            if (!agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        /// <summary>
        /// 정지 대기 시 NavMeshObstacle을 활성화해 다른 몬스터의 경로 계산에 장애물로 등록한다.
        /// NavMeshAgent와 NavMeshObstacle은 동시에 활성화할 수 없으므로 교대로 토글한다.
        /// </summary>
        /// <param name="isObstacle">true: Obstacle 활성 / Agent 비활성, false: Agent 활성 / Obstacle 비활성</param>
        private void SetObstacleMode(bool isObstacle)
        {
            if (isObstacle)
            {
                agent.enabled = false;
                obstacle.enabled = true;
            }
            else
            {
                obstacle.enabled = false;
                agent.enabled = true;
            }
        }

        // ─────────────────────────────────────────
        // 공격 히트 판정
        // ─────────────────────────────────────────

        /// <summary>
        /// 공격 애니메이션의 히트 타이밍에 호출된다.
        /// attackRadius 반경 내 플레이어에게 attackDamage를 적용한다.
        /// </summary>
        public override void OnAttackHit()
        {
            if (playerTransform == null) return;

            float flatDist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(playerTransform.position.x, 0f, playerTransform.position.z));

            if (flatDist > attackRadius + DistanceTolerance) return;

            playerDamageable?.TakeDamage(attackDamage);
        }

        // ─────────────────────────────────────────
        // 사망 처리 override
        // ─────────────────────────────────────────

        /// <summary>사망 시 Obstacle 모드를 해제하고 Agent를 정지한 뒤 AI 상태를 Dead로 전환한다.</summary>
        protected override void Die()
        {
            base.Die();
            currentState = EnemyState.Dead;
            SetObstacleMode(false); // Obstacle 모드 중 사망 시 Agent 복귀 후 정지
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
            }
        }

        /// <summary>풀에서 재사용될 때 AI 상태와 Agent를 초기화한다.</summary>
        public override void ResetState()
        {
            base.ResetState();
            currentState = EnemyState.Idle;
            lastAttackTime = -999f;
            atSlot = false;
            // Awake보다 ResetState가 먼저 호출될 수 있으므로 null이면 재캐싱
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();
            SetObstacleMode(false); // 풀 반납 후 재사용 시 Agent 모드로 복귀
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }
        }

#if UNITY_EDITOR
        // ─────────────────────────────────────────
        // 에디터 기즈모
        // ─────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
        }
#endif
    }
}
