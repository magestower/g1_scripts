using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace G1
{
    /// <summary>
    /// 도깨비 몬스터 AI 컨트롤러.
    /// Idle → Chase(감지) → SlotWait(ring 1+ 대기) → Attack(근접) → Dead 상태 머신으로 동작한다.
    /// 정지 상태(SlotWait/Attack)에서는 NavMeshObstacle(carving)을 활성화해 다른 몬스터가 우회하도록 하고,
    /// 이동 재개 시 1프레임 대기 후 NavMeshAgent를 활성화해 carving 해제 후 스냅 현상을 방지한다.
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
            /// <summary>배정된 슬롯에 도착해 대기 중 (ring 1+)</summary>
            SlotWait,
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
        private MonsterHpBar hpBar;
        private float lastAttackTime = NeverAttacked;
        private bool atSlot;
        /// <summary>현재 obstacle(정지) 모드인지 여부. 중복 전환 방지에 사용한다.</summary>
        private bool isObstacleMode = false;

        // 부동소수점 이동 연산 오차 허용 범위
        private const float DistanceTolerance = 0.10f;
        // 슬롯 이탈 판정 임계값 — 플레이어 이동으로 슬롯이 이 거리 이상 멀어지면 재배정
        private const float DepartThresh = 1.2f;
        // 공격 쿨다운 초기값 — 게임 시작 즉시 공격 가능하도록 충분히 작은 값
        private const float NeverAttacked = -999f;

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>
        /// NavMeshAgent/NavMeshObstacle과 플레이어 트랜스폼을 캐싱한다.
        /// NavMeshAgent 회전은 RotateToward()로 직접 제어하므로 angularSpeed를 0으로 설정한다.
        /// 초기 상태는 Agent 활성 / Obstacle 비활성으로 시작한다.
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
            obstacle.enabled = false; // 초기에는 Agent 모드

            hpBar = GetComponentInChildren<MonsterHpBar>(includeInactive: true);

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
        }

        // ─────────────────────────────────────────
        // 상태 머신
        // ─────────────────────────────────────────

        /// <summary>
        /// 수평 거리에 따라 현재 상태를 전이한다.
        /// 정지(SlotWait/Attack) 진입 시 Obstacle 모드로 전환해 다른 몬스터가 carving으로 우회하고,
        /// 이동(Chase/Idle) 복귀 시 1프레임 대기 후 Agent를 재활성화해 스냅 현상을 방지한다.
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

            EnemyState prev = currentState;

            if (flatDist <= attackRadius + DistanceTolerance && !slotReachable)
            {
                currentState = EnemyState.Attack;
            }
            else if (flatDist > detectRadius)
            {
                atSlot = false;
                currentState = EnemyState.Idle;
            }
            else if (atSlot && AssignedSlotRing > 0)
            {
                currentState = EnemyState.SlotWait;
            }
            else
            {
                if (currentState == EnemyState.Attack)
                    atSlot = false; // Attack 복귀 시 슬롯 재접근
                currentState = EnemyState.Chase;
            }

            if (hpBar != null)
            {
                hpBar.SetVisible(
                    AssignedSlotRing == 0 &&
                    atSlot == true &&
                    flatDist <= attackRadius + DistanceTolerance
                );
            }

            // 상태 전환 시 Agent ↔ Obstacle 모드 전환
            if (prev != currentState)
            {
                bool shouldWait = currentState == EnemyState.SlotWait
                               || currentState == EnemyState.Attack;
                if (shouldWait && !isObstacleMode)
                    EnterObstacleMode();
                else if (!shouldWait && isObstacleMode)
                    StartCoroutine(ExitObstacleModeNextFrame());
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
                case EnemyState.SlotWait: HandleSlotWait(flatDir); break;
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
        /// Chase: NavMeshAgent로 슬롯(또는 플레이어) 방향으로 이동한다.
        /// ring 0이 슬롯 도착 후에는 플레이어 방향으로 추가 접근한다.
        /// </summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        /// <param name="flatDist">플레이어까지의 수평(XZ) 거리</param>
        private void HandleChase(Vector3 flatDir, float flatDist)
        {
            // agent 비활성 구간(ExitObstacleModeNextFrame 대기 중)에는 이동/판정 전부 스킵
            if (!agent.enabled)
            {
                animator.SetFloat(SpeedHash, 0f);
                return;
            }

            if (AssignedSlotRing >= 0)
            {
                Vector3 toSlot = AssignedSlotPos - transform.position;
                toSlot.y = 0f;
                float targetDist = toSlot.magnitude;

                // 슬롯 도착 판정
                if (targetDist <= DistanceTolerance && !atSlot)
                    atSlot = true;

                if (atSlot && AssignedSlotRing == 0 && flatDist > attackRadius + DistanceTolerance)
                {
                    // ring 0: 슬롯 도착 후 플레이어에서 attackRadius만큼 떨어진 위치까지 접근
                    Vector3 toMonster = (transform.position - playerTransform.position);
                    toMonster.y = 0f;
                    Vector3 target = playerTransform.position + toMonster.normalized * attackRadius;
                    MoveAgent(target);
                }
                else if (!atSlot)
                {
                    MoveAgent(AssignedSlotPos);
                }
            }
            else
            {
                // 슬롯 미배정 — 플레이어 방향으로 직접 이동
                MoveAgent(playerTransform.position);
            }

            Vector3 vel = agent.velocity;
            vel.y = 0f;
            RotateToward(vel.sqrMagnitude > 0.01f ? vel : flatDir);
            bool isMoving = vel.magnitude > 0.1f || agent.hasPath || agent.pathPending;
            animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
        }

        /// <summary>
        /// SlotWait: ring 1+ 몬스터가 슬롯에 도착해 대기한다.
        /// 슬롯이 너무 멀어지면 Chase로 재배정한다.
        /// </summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        private void HandleSlotWait(Vector3 flatDir)
        {
            Vector3 toSlot = AssignedSlotPos - transform.position;
            toSlot.y = 0f;

            // 슬롯 이탈 판정 — 플레이어 이동으로 슬롯 위치가 크게 갱신된 경우
            if (toSlot.magnitude > DepartThresh)
            {
                if (MonsterManager.Instance != null)
                    MonsterManager.Instance.RequestReassign(this); // 내부에서 OnSlotChanged() → atSlot = false
                return;
            }

            RotateToward(flatDir);
            animator.SetFloat(SpeedHash, 0f);
        }

        /// <summary>
        /// Attack: 플레이어 방향으로 회전하며 쿨다운마다 공격한다.
        /// </summary>
        /// <param name="flatDir">플레이어 방향 수평 벡터 (Y=0)</param>
        private void HandleAttack(Vector3 flatDir)
        {
            animator.SetFloat(SpeedHash, 0f);
            RotateToward(flatDir);

            if (playerDamageable == null || playerDamageable.IsDead) return;

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                lastAttackTime = Time.time;
                animator.ResetTrigger(AttackHash);
                animator.SetTrigger(AttackHash);
            }
        }

        /// <summary>
        /// 슬롯이 변경됐을 때 호출된다. atSlot을 리셋해 새 슬롯으로 Chase를 재개한다.
        /// ring 0 배정 시 HP 바를 표시하고, 그 외에는 숨긴다.
        /// </summary>
        public override void OnSlotChanged()
        {
            atSlot = false;
            // 슬롯 재배정 직후는 아직 슬롯에 도달하지 않은 이동 중 상태이므로 HP바 숨김
            // HP바 활성화는 UpdateState에서 atSlot 도달 후 거리 조건 충족 시 처리
            // if (hpBar != null) hpBar.SetVisible(false);
        }

        /// <summary>정지 모드: Agent를 비활성화하고 Obstacle(carving)을 활성화한다.</summary>
        private void EnterObstacleMode()
        {
            isObstacleMode = true;
            agent.enabled = false;
            obstacle.enabled = true;
        }

        /// <summary>
        /// 이동 재개 모드: Obstacle을 비활성화하고 2프레임 대기 후 Agent를 활성화한다.
        /// NavMeshObstacle carving은 비동기로 해제되므로 충분히 대기해야 Agent 활성화 시 스냅이 없다.
        /// </summary>
        private IEnumerator ExitObstacleModeNextFrame()
        {
            obstacle.enabled = false;
            yield return null;
            yield return null; // carving 해제 2프레임 대기
            if (IsDead) yield break;
            agent.enabled = true;
            isObstacleMode = false;
        }

        /// <inheritdoc/>
        public override int NavMeshPriority => agent != null && agent.enabled ? agent.avoidancePriority : 99;

        /// <summary>NavMeshAgent의 회피 우선순위를 설정한다.</summary>
        /// <param name="priority">0(최고) ~ 99(최저)</param>
        public override void SetAvoidancePriority(int priority)
        {
            if (agent != null)
                agent.avoidancePriority = priority;
        }

        /// <summary>NavMeshAgent에 목적지를 설정하고 이동을 활성화한다.</summary>
        /// <param name="destination">목적지 월드 좌표</param>
        private void MoveAgent(Vector3 destination)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        /// <summary>NavMeshAgent 이동을 정지한다.</summary>
        private void StopAgent()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
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

            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.magnitude > attackRadius + DistanceTolerance) return;

            playerDamageable?.TakeDamage(attackDamage);
        }

        // ─────────────────────────────────────────
        // 사망 처리 override
        // ─────────────────────────────────────────

        /// <summary>사망 시 Agent를 정지하고 AI 상태를 Dead로 전환한다. HP 바를 즉시 숨긴다.</summary>
        protected override void Die()
        {
            base.Die();
            currentState = EnemyState.Dead;
            StopAgent();
            // 코루틴 없이 즉시 Obstacle로 전환 (사망이므로 스냅 문제 없음)
            if (!isObstacleMode) EnterObstacleMode();
            if (hpBar != null) hpBar.SetVisible(false);
        }

        /// <summary>풀에서 재사용될 때 AI 상태와 Agent/Obstacle을 초기화한다.</summary>
        public override void ResetState()
        {
            base.ResetState();
            currentState = EnemyState.Idle;
            lastAttackTime = NeverAttacked;
            atSlot = false;
            isObstacleMode = false;
            if (hpBar != null) hpBar.SetVisible(false);
            if (obstacle == null) obstacle = GetComponent<NavMeshObstacle>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            // Obstacle 비활성 → Agent 활성 상태로 복원
            if (obstacle != null) obstacle.enabled = false;
            if (agent != null) agent.enabled = true;
            StopAgent();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();
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

            // 배정된 슬롯 위치 (파란색 구)
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(AssignedSlotPos, 0.15f);

            // 슬롯까지 선
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, AssignedSlotPos);

            // 실제 ColliderRadius 표시
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, ColliderRadius);
        }
#endif
    }
}
