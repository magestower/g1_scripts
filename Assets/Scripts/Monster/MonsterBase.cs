using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 모든 몬스터의 공통 기능을 담은 베이스 클래스.
    /// 체력 관리, 피격 처리, 사망 처리를 제공한다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(Collider))]
    public abstract class MonsterBase : MonoBehaviour, IDamageable
    {
        // ─────────────────────────────────────────
        // Animator 파라미터 해시 (GC 최적화)
        // ─────────────────────────────────────────
        protected static readonly int HitHash = Animator.StringToHash("Hit");
        protected static readonly int DeadHash = Animator.StringToHash("IsDead");

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────
        [Header("체력")]
        [SerializeField] protected int maxHealth = 30;

        [Header("사망 처리")]
        /// <summary>사망 애니메이션 재생 후 풀에 반납하기까지 대기 시간 (초)</summary>
        [SerializeField] private float releaseDelay = 3f;

        /// <summary>풀에서 꺼낼 때 MonsterPool이 설정하는 원본 프리팹 ID</summary>
        [System.NonSerialized] public EntityId PrefabID;

        /// <summary>MonsterManager가 배정한 포위 슬롯 월드 위치. 미배정 시 Vector3.zero.</summary>
        [System.NonSerialized] public Vector3 AssignedSlotPos;

        /// <summary>
        /// MonsterManager가 배정 시 계산한 실제 슬롯 반경 (playerRadius + colliderRadius + slotRadius).
        /// 도착/이탈 임계값 계산에 사용한다. 미배정 시 0.
        /// </summary>
        [System.NonSerialized] public float AssignedSlotRadius;

        /// <summary>MonsterManager가 계산한 이번 프레임 분리 벡터.</summary>
        [System.NonSerialized] public Vector3 SeparationVec;

        /// <summary>MonsterManager가 배정한 슬롯의 링 번호 (0-based). 미배정 시 -1.</summary>
        [System.NonSerialized] public int AssignedSlotRing = -1;

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────
        protected int currentHealth;
        protected Animator animator;
        protected Collider col;

        /// <summary>
        /// 데미지 팝업 스폰 기준 위치 (목 본 또는 높이 추정값).
        /// Awake에서 캐싱되며 TakeDamage에서 사용된다.
        /// </summary>
        private Transform neckBone;

        private HitFlasher hitFlasher;

        /// <summary>현재 체력이 0 이하인지 여부</summary>
        public bool IsDead { get; protected set; }

        /// <summary>
        /// 콜라이더 반경. CapsuleCollider면 radius, SphereCollider면 radius, 그 외엔 0을 반환한다.
        /// MonsterManager가 슬롯 위치 계산 시 몬스터별 반경으로 사용한다.
        /// </summary>
        public float ColliderRadius
        {
            get
            {
                if (col is CapsuleCollider capsule) return capsule.radius;
                if (col is SphereCollider sphere) return sphere.radius;
                return 0f;
            }
        }

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>컴포넌트 참조를 캐싱하고 초기 체력을 설정한다.</summary>
        protected virtual void Awake()
        {
            animator = GetComponent<Animator>();
            col = GetComponent<Collider>();
            currentHealth = maxHealth;
            neckBone = FindNeckBone(transform);
            hitFlasher = GetComponent<HitFlasher>();
        }

        /// <summary>
        /// 자식 오브젝트 중 이름에 "neck"이 포함된 Transform을 찾아 반환한다.
        /// 없으면 null을 반환한다.
        /// </summary>
        private static Transform FindNeckBone(Transform root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name.IndexOf("neck", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }
            return null;
        }

        /// <summary>
        /// 데미지 팝업 스폰 위치를 반환한다.
        /// 목 본이 있으면 그 위치, 없으면 콜라이더 높이의 80% 지점을 추정해 반환한다.
        /// </summary>
        private Vector3 GetPopupSpawnPos()
        {
            if (neckBone != null)
                return neckBone.position;

            // 목 본이 없을 때 — CapsuleCollider 높이 기준 80% 지점 추정
            float height = col is CapsuleCollider cap ? cap.height : 1.8f;
            return transform.position + Vector3.up * (height * 0.8f);
        }

        // ─────────────────────────────────────────
        // IDamageable 구현
        // ─────────────────────────────────────────

        /// <summary>
        /// 데미지를 받아 체력을 감소시킨다. 사망 시 Die()를 호출한다.
        /// </summary>
        /// <param name="damage">적용할 데미지 양</param>
        /// <param name="isCritical">크리티컬 여부. 팝업 색상/크기에 반영된다.</param>
        public virtual void TakeDamage(int damage, bool isCritical = false)
        {
            if (IsDead) return;

            currentHealth -= damage;
            if (currentHealth <= 0) currentHealth = 0;

            // 피격 애니메이션 재생
            animator.SetTrigger(HitHash);
            // 피격 플래시 이펙트 재생
            hitFlasher?.Flash();
            // 피격 히트스탑
            HitStop.Instance?.Trigger();
            // 피격 스파크 이펙트
            HitSparkPool.Instance?.Show(GetPopupSpawnPos());

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            // 피해량 수치를 화면에 표시
            DamagePopupPool.Instance?.Show(damage, GetPopupSpawnPos(), isCritical);

            if (currentHealth <= 0)
                Die();
        }

        // ─────────────────────────────────────────
        // 공격 히트 판정
        // ─────────────────────────────────────────

        /// <summary>
        /// 공격 애니메이션에서 데미지를 적용할 타이밍 (0~1 정규화 값).
        /// MonsterAttackBehaviour가 이 값을 기준으로 OnAttackHit()을 호출한다.
        /// </summary>
        public abstract float HitTimingNormalized { get; }

        /// <summary>
        /// 실제 데미지 적용 처리. MonsterAttackBehaviour에서 히트 타이밍에 호출된다.
        /// </summary>
        public abstract void OnAttackHit();

        // ─────────────────────────────────────────
        // 사망 처리
        // ─────────────────────────────────────────

        /// <summary>몬스터 사망 시 발행되는 이벤트. MonsterManager가 구독해 목록을 갱신한다.</summary>
        public static event System.Action<MonsterBase> OnMonsterDied;

        /// <summary>체력이 변경될 때 발행되는 인스턴스 이벤트. MonsterHpBar가 구독해 게이지를 갱신한다.</summary>
        public event System.Action<int, int> OnHealthChanged;

        /// <summary>
        /// 사망 처리: 플래그 설정, 사망 애니메이션 재생, 콜라이더 비활성화.
        /// OnMonsterDied 이벤트는 releaseDelay 후 풀 반납 직전에 발행해
        /// MonsterManager의 전멸 판정이 오브젝트 소멸 시점과 일치하도록 한다.
        /// 파생 클래스에서 override해 추가 처리(드롭 아이템 등)를 구현한다.
        /// </summary>
        protected virtual void Die()
        {
            IsDead = true;
            animator.SetBool(DeadHash, true);
            col.enabled = false;
            StartCoroutine(ReleaseAfterDelay());
        }

        /// <summary>
        /// releaseDelay 초 후 OnMonsterDied를 발행하고 MonsterPool에 반납한다.
        /// 이벤트를 반납 직전에 발행해 전멸 판정이 실제 소멸 시점과 일치하도록 보장한다.
        /// </summary>
        private IEnumerator ReleaseAfterDelay()
        {
            yield return new WaitForSeconds(releaseDelay);

            OnMonsterDied?.Invoke(this);

            if (MonsterPool.Instance != null)
                MonsterPool.Instance.Release(this);
            else
                Destroy(gameObject); // 풀 미사용 환경 폴백
        }

        /// <summary>현재 NavMeshAgent avoidancePriority 값. 미지원 클래스는 99 반환.</summary>
        public virtual int NavMeshPriority => 99;

        /// <summary>
        /// NavMeshAgent의 avoidancePriority를 설정한다.
        /// NavMeshAgent를 가진 파생 클래스에서 override해 실제 값을 적용한다.
        /// </summary>
        /// <param name="priority">0(최고) ~ 99(최저)</param>
        public virtual void SetAvoidancePriority(int priority) { }

        /// <summary>
        /// MonsterManager가 슬롯을 변경할 때 호출된다.
        /// 파생 클래스에서 override해 슬롯 변경에 따른 이동 재개 처리를 구현한다.
        /// </summary>
        public virtual void OnSlotChanged() { }

        /// <summary>
        /// 풀에서 꺼낼 때 호출된다. 체력·플래그·애니메이터·콜라이더를 초기 상태로 되돌린다.
        /// 파생 클래스에서 override해 추가 상태를 초기화한다.
        /// </summary>
        public virtual void ResetState()
        {
            // 이전 ReleaseAfterDelay 코루틴이 남아있을 경우 중복 실행 방지
            StopAllCoroutines();
            // Awake가 호출되기 전에 ResetState가 실행될 수 있으므로 null이면 재캐싱
            if (col == null) col = GetComponent<Collider>();
            if (animator == null) animator = GetComponent<Animator>();
            IsDead = false;
            currentHealth = maxHealth;
            col.enabled = true;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            AssignedSlotPos = Vector3.zero;
            AssignedSlotRadius = 0f;
            AssignedSlotRing = -1;
            SeparationVec = Vector3.zero;
            animator.SetBool(DeadHash, false);
            animator.ResetTrigger(HitHash);
        }
    }
}
