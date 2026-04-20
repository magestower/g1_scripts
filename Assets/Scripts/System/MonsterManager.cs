using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 씬 내 모든 몬스터의 등록/해제, 생존 상태, 포위 슬롯 배정, 분리 벡터 계산을 관리하는 싱글톤.
    /// 슬롯은 플레이어 주변 다중 원형(링)으로 배치되며, 안쪽 링부터 채워진다.
    /// MonsterBase.OnMonsterDied 이벤트를 구독해 사망 시 목록과 슬롯에서 자동 제거한다.
    /// </summary>
    public class MonsterManager : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // 싱글톤
        // ─────────────────────────────────────────
        public static MonsterManager Instance { get; private set; }

        // ─────────────────────────────────────────
        // 이벤트
        // ─────────────────────────────────────────

        /// <summary>모든 몬스터가 전멸했을 때 발행된다.</summary>
        public static event System.Action OnAllMonstersDied;

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        [Header("포위 슬롯 - 다중 링")]
        /// <summary>1링의 기본 간격(오프셋). 실제 1링 반경 = playerRadius + monsterRadius + baseRingOffset.</summary>
        [SerializeField] private float baseRingOffset = 1.5f;

        /// <summary>링 간 추가 간격. n링 반경 = 1링 반경 + (n-1) * ringSpacing.</summary>
        [SerializeField] private float ringSpacing = 1.2f;

        /// <summary>슬롯 간 최소 간격(호 길이). 각 링의 슬롯 수 = 원주 / minSlotSpacing.</summary>
        [SerializeField] private float minSlotSpacing = 1.2f;

        /// <summary>
        /// 슬롯 수 계산에 사용할 몬스터 평균 콜라이더 반경.
        /// 실제 링 반경(playerRadius + avgMonsterRadius + baseRingOffset + ...)으로 슬롯 수를 계산한다.
        /// </summary>
        [SerializeField] private float avgMonsterRadius = 0.3f;

        /// <summary>생성할 최대 링 수.</summary>
        [SerializeField] private int maxRings = 5;

        [Header("분리 벡터")]
        /// <summary>주변 몬스터를 밀어낼 반경</summary>
        [SerializeField] private float separationRadius = 1.2f;

        /// <summary>분리력 세기</summary>
        [SerializeField] private float separationStrength = 2f;

        /// <summary>슬롯·분리 벡터 갱신 주기 (초)</summary>
        [SerializeField] private float updateInterval = 0.1f;

        // ─────────────────────────────────────────
        // 슬롯 메타데이터
        // ─────────────────────────────────────────

        /// <summary>슬롯 하나의 메타 정보 (링 번호, 각도 인덱스, 해당 링의 슬롯 수)</summary>
        private struct SlotMeta
        {
            public int   ring;       // 0-based 링 번호
            public int   indexInRing;
            public int   slotsInRing;
        }

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────
        private readonly List<MonsterBase> activeMonsters = new();

        /// <summary>슬롯 인덱스 → 점유 몬스터 (null이면 빈 슬롯)</summary>
        private MonsterBase[] slots;

        /// <summary>슬롯 인덱스 → 슬롯 메타 정보</summary>
        private SlotMeta[] slotMetas;

        /// <summary>총 슬롯 수 (링별 슬롯 수 합산)</summary>
        private int totalSlots;

        /// <summary>몬스터 → 점유 슬롯 인덱스 (-1이면 미배정)</summary>
        private readonly Dictionary<MonsterBase, int> monsterSlotMap = new();

        private Transform playerTransform;
        /// <summary>플레이어 CharacterController 반경. 슬롯 위치 계산 시 기준 반경으로 사용한다.</summary>
        private float playerRadius;
        private float timer;

        /// <summary>AssignSlots에서 매 틱 재사용하는 미배정 몬스터 버퍼. GC 할당 방지.</summary>
        private readonly List<(MonsterBase monster, float angle)> unassignedBuffer = new();

        /// <summary>현재 살아있는 몬스터 수</summary>
        public int AliveCount => activeMonsters.Count;

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>싱글톤 인스턴스를 설정하고 사망 이벤트를 초기화한다. 슬롯 레이아웃은 Start()에서 구축한다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            MonsterBase.OnMonsterDied += OnMonsterDied;
        }

        /// <summary>인스펙터 값 변경 시 유효성을 검증한다.</summary>
        private void OnValidate()
        {
            if (baseRingOffset < 0.1f) baseRingOffset = 0.1f;
            if (ringSpacing    < 0.1f) ringSpacing    = 0.1f;
            if (minSlotSpacing < 0.1f) minSlotSpacing = 0.1f;
            if (maxRings       < 1)    maxRings        = 1;
        }

        /// <summary>
        /// 플레이어 반경을 캐싱한 뒤 슬롯 레이아웃을 구축한다.
        /// playerRadius가 확정된 후 BuildSlotLayout()을 호출해야 링별 슬롯 수가 정확히 계산된다.
        /// </summary>
        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerRadius = playerObj.TryGetComponent<CharacterController>(out var cc) ? cc.radius : 0f;
            }
            BuildSlotLayout();
        }

        /// <summary>사망 이벤트 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            MonsterBase.OnMonsterDied -= OnMonsterDied;
        }

        /// <summary>updateInterval 주기로 슬롯 배정과 분리 벡터를 갱신한다.</summary>
        private void Update()
        {
            if (playerTransform == null || activeMonsters.Count == 0 || slots == null) return;

            timer += Time.deltaTime;
            if (timer < updateInterval) return;
            timer = 0f;

            AssignSlots();
            CalcSeparations();
        }

        // ─────────────────────────────────────────
        // public 메서드
        // ─────────────────────────────────────────

        /// <summary>
        /// 몬스터를 활성 목록에 등록한다. 슬롯 배정은 다음 AssignSlots() 호출 시 수행된다.
        /// </summary>
        /// <param name="monster">등록할 몬스터</param>
        public void Register(MonsterBase monster)
        {
            if (monsterSlotMap.ContainsKey(monster)) return;
            activeMonsters.Add(monster);
            monsterSlotMap[monster] = -1;
        }

        /// <summary>
        /// 몬스터의 슬롯을 해제하고 미배정 상태로 되돌린다.
        /// 다음 AssignSlots() 호출 시 새 슬롯으로 재배정된다.
        /// </summary>
        /// <param name="monster">슬롯을 재배정할 몬스터</param>
        public void RequestReassign(MonsterBase monster)
        {
            ReleaseSlot(monster);
            monsterSlotMap[monster]    = -1;
            monster.AssignedSlotPos    = Vector3.zero;
            monster.AssignedSlotRadius = 0f;
            monster.AssignedSlotRing   = -1;
        }

        /// <summary>
        /// 몬스터를 활성 목록과 슬롯에서 제거한다.
        /// </summary>
        /// <param name="monster">제거할 몬스터</param>
        public void Unregister(MonsterBase monster)
        {
            activeMonsters.Remove(monster);
            ReleaseSlot(monster);
            monsterSlotMap.Remove(monster);
        }

        // ─────────────────────────────────────────
        // 슬롯 레이아웃 구축
        // ─────────────────────────────────────────

        /// <summary>
        /// 링별 슬롯 수를 계산해 slotMetas 및 slots 배열을 초기화한다.
        /// 각 링의 슬롯 수 = Mathf.Max(1, Mathf.FloorToInt(2π * ringRadius / minSlotSpacing)).
        /// Start()에서 playerRadius 캐싱 후 호출되므로 실제 반경 기준으로 계산된다.
        /// </summary>
        private void BuildSlotLayout()
        {
            var metas = new List<SlotMeta>();

            for (int ring = 0; ring < maxRings; ring++)
            {
                // playerRadius + avgMonsterRadius를 포함한 실제 링 반경으로 슬롯 수를 계산
                float ringRadius = playerRadius + avgMonsterRadius + baseRingOffset + ring * ringSpacing;
                int count = Mathf.Max(1, Mathf.FloorToInt(2f * Mathf.PI * ringRadius / minSlotSpacing));

                for (int j = 0; j < count; j++)
                {
                    metas.Add(new SlotMeta
                    {
                        ring        = ring,
                        indexInRing = j,
                        slotsInRing = count,
                    });
                }
            }

            totalSlots = metas.Count;
            slots      = new MonsterBase[totalSlots];
            slotMetas  = metas.ToArray();
        }

        // ─────────────────────────────────────────
        // 슬롯 배정
        // ─────────────────────────────────────────

        /// <summary>
        /// 슬롯 미배정 몬스터에게 빈 슬롯을 배정한다.
        /// 몬스터를 플레이어 기준 각도 순으로 정렬한 뒤, 각도 허용 범위 안에서 ring이 낮은 슬롯을 우선 배정한다.
        /// 이미 배정된 몬스터의 슬롯 위치도 플레이어 이동에 따라 매 틱 갱신한다.
        /// </summary>
        private void AssignSlots()
        {
            Vector3 playerPos = playerTransform.position;

            // 미배정 몬스터 목록을 각도 순으로 정렬해 배정 순서 편향 제거 (GC 방지: 필드 버퍼 재사용)
            unassignedBuffer.Clear();
            for (int m = 0; m < activeMonsters.Count; m++)
            {
                MonsterBase monster = activeMonsters[m];
                if (monsterSlotMap.TryGetValue(monster, out int assigned) && assigned >= 0) continue;

                Vector3 toMonster    = monster.transform.position - playerPos;
                float   monsterAngle = toMonster.sqrMagnitude > 0.0001f
                    ? Mathf.Atan2(toMonster.z, toMonster.x)
                    : Random.Range(0f, 2f * Mathf.PI);
                if (monsterAngle < 0f) monsterAngle += 2f * Mathf.PI;
                unassignedBuffer.Add((monster, monsterAngle));
            }
            unassignedBuffer.Sort((a, b) => a.angle.CompareTo(b.angle));

            // ring 0 슬롯 간격의 절반을 angleTolerance로 사용 (슬롯 간격 > tolerance 보장)
            int   ring0SlotCount   = 0;
            for (int i = 0; i < totalSlots; i++)
                if (slotMetas[i].ring == 0) { ring0SlotCount = slotMetas[i].slotsInRing; break; }
            float angleTolerance = ring0SlotCount > 0
                ? (360f / ring0SlotCount * 0.6f)  // 슬롯 간격의 60% — 인접 슬롯까지 허용
                : 30f;

            for (int m = 0; m < unassignedBuffer.Count; m++)
            {
                MonsterBase monster      = unassignedBuffer[m].monster;
                float       monsterAngle = unassignedBuffer[m].angle;

                int   bestSlot      = -1;
                int   bestRing      = int.MaxValue;
                float bestAngleDiff = float.MaxValue;

                for (int i = 0; i < totalSlots; i++)
                {
                    if (slots[i] != null) continue;

                    SlotMeta meta      = slotMetas[i];
                    float    slotAngle = meta.indexInRing * (2f * Mathf.PI / meta.slotsInRing);
                    float    diff      = Mathf.Abs(Mathf.DeltaAngle(
                        monsterAngle * Mathf.Rad2Deg, slotAngle * Mathf.Rad2Deg));

                    if (diff > angleTolerance) continue;

                    bool betterRing     = meta.ring < bestRing;
                    bool sameRingBetter = meta.ring == bestRing && diff < bestAngleDiff;
                    if (betterRing || sameRingBetter)
                    {
                        bestRing      = meta.ring;
                        bestAngleDiff = diff;
                        bestSlot      = i;
                    }
                }

                // 허용 범위 내 슬롯이 없으면 각도 무시하고 가장 가까운 빈 슬롯 배정
                if (bestSlot < 0)
                {
                    for (int i = 0; i < totalSlots; i++)
                    {
                        if (slots[i] != null) continue;
                        SlotMeta meta      = slotMetas[i];
                        float    slotAngle = meta.indexInRing * (2f * Mathf.PI / meta.slotsInRing);
                        float    diff      = Mathf.Abs(Mathf.DeltaAngle(
                            monsterAngle * Mathf.Rad2Deg, slotAngle * Mathf.Rad2Deg));
                        bool betterRing     = meta.ring < bestRing;
                        bool sameRingBetter = meta.ring == bestRing && diff < bestAngleDiff;
                        if (betterRing || sameRingBetter)
                        {
                            bestRing      = meta.ring;
                            bestAngleDiff = diff;
                            bestSlot      = i;
                        }
                    }
                }

                if (bestSlot < 0) continue;

                slots[bestSlot]         = monster;
                monsterSlotMap[monster] = bestSlot;
            }

            // 배정된 모든 슬롯의 위치를 플레이어 이동에 따라 갱신 (신규 배정 포함)
            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] == null) continue;
                float r = CalcSlotRadius(i, slots[i].ColliderRadius);
                slots[i].AssignedSlotPos    = CalcSlotPosition(playerPos, i, slots[i].ColliderRadius);
                slots[i].AssignedSlotRadius = r;
                slots[i].AssignedSlotRing   = slotMetas[i].ring;
            }
        }

        /// <summary>
        /// 슬롯 인덱스와 몬스터 반경으로 해당 슬롯의 실제 접촉 반경을 반환한다.
        /// 실제 반경 = playerRadius + monsterRadius + baseRingOffset + ring * ringSpacing.
        /// </summary>
        /// <param name="slotIndex">슬롯 인덱스</param>
        /// <param name="monsterRadius">몬스터 콜라이더 반경</param>
        private float CalcSlotRadius(int slotIndex, float monsterRadius)
        {
            int ring = slotMetas[slotIndex].ring;
            return playerRadius + monsterRadius + baseRingOffset + ring * ringSpacing;
        }

        /// <summary>
        /// 슬롯 인덱스와 몬스터 반경으로 슬롯의 월드 위치를 반환한다.
        /// 각도는 링 내 인덱스를 슬롯 수로 나눠 균등 배분한다.
        /// </summary>
        /// <param name="center">플레이어 월드 위치</param>
        /// <param name="slotIndex">슬롯 인덱스</param>
        /// <param name="monsterRadius">몬스터 콜라이더 반경</param>
        private Vector3 CalcSlotPosition(Vector3 center, int slotIndex, float monsterRadius)
        {
            SlotMeta meta     = slotMetas[slotIndex];
            float    radius   = CalcSlotRadius(slotIndex, monsterRadius);
            float    slotAngle = meta.indexInRing * (2f * Mathf.PI / meta.slotsInRing);
            return center + new Vector3(Mathf.Cos(slotAngle) * radius, 0f, Mathf.Sin(slotAngle) * radius);
        }

        /// <summary>몬스터가 점유한 슬롯을 해제한다.</summary>
        private void ReleaseSlot(MonsterBase monster)
        {
            if (slots == null) return;
            if (!monsterSlotMap.TryGetValue(monster, out int idx) || idx < 0) return;
            if (slots[idx] == monster)
                slots[idx] = null;
        }

        // ─────────────────────────────────────────
        // 분리 벡터 계산
        // ─────────────────────────────────────────

        /// <summary>
        /// 모든 몬스터 쌍을 순회해 separationRadius 이내 몬스터끼리 분리 벡터를 계산한다.
        /// Physics 쿼리 없이 위치 비교만으로 처리한다.
        /// </summary>
        private void CalcSeparations()
        {
            for (int i = 0; i < activeMonsters.Count; i++)
                activeMonsters[i].SeparationVec = Vector3.zero;

            float radiusSq = separationRadius * separationRadius;

            for (int i = 0; i < activeMonsters.Count; i++)
            {
                for (int j = i + 1; j < activeMonsters.Count; j++)
                {
                    Vector3 diff = activeMonsters[i].transform.position - activeMonsters[j].transform.position;
                    diff.y = 0f;

                    float distSq = diff.sqrMagnitude;
                    if (distSq >= radiusSq || distSq < 0.0001f) continue;

                    float   dist  = Mathf.Sqrt(distSq);
                    // 가까울수록 강하게 밀어냄
                    Vector3 force = diff.normalized * (separationStrength * (1f - dist / separationRadius));

                    activeMonsters[i].SeparationVec += force;
                    activeMonsters[j].SeparationVec -= force;
                }
            }
        }

        // ─────────────────────────────────────────
        // private 메서드
        // ─────────────────────────────────────────

        /// <summary>몬스터 사망 시 목록과 슬롯에서 제거하고 전멸 여부를 확인한다.</summary>
        private void OnMonsterDied(MonsterBase monster)
        {
            Unregister(monster);

            if (activeMonsters.Count == 0)
                OnAllMonstersDied?.Invoke();
        }
    }
}
