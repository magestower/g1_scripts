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

        /// <summary>슬롯 배정 갱신 주기 (초)</summary>
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

        /// <summary>회수된 avoidancePriority 풀. 0~98 범위에서 순차 발급 후 사망 시 반납.</summary>
        private readonly Queue<int> priorityPool = new();

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

            // avoidancePriority 풀 초기화 (0~98, 99는 기본값으로 예약)
            for (int i = 0; i < 99; i++)
                priorityPool.Enqueue(i);
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

            // Priority 발급 — 풀이 고갈되면 기본값(99) 유지
            if (priorityPool.Count > 0)
                monster.SetAvoidancePriority(priorityPool.Dequeue());
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

            // Priority 반납
            if (monsterSlotMap.TryGetValue(monster, out _))
                priorityPool.Enqueue(monster.NavMeshPriority);

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

            // 전체 미배정 몬스터를 각도 순으로 수집 (GC 방지: 필드 버퍼 재사용)
            unassignedBuffer.Clear();
            for (int m = 0; m < activeMonsters.Count; m++)
            {
                MonsterBase monster = activeMonsters[m];
                if (monsterSlotMap.TryGetValue(monster, out int assigned) && assigned >= 0) continue;

                Vector3 toMonster = monster.transform.position - playerPos;
                float   angle     = toMonster.sqrMagnitude > 0.0001f
                    ? Mathf.Atan2(toMonster.z, toMonster.x)
                    : Random.Range(0f, 2f * Mathf.PI);
                if (angle < 0f) angle += 2f * Mathf.PI;
                unassignedBuffer.Add((monster, angle));
            }
            unassignedBuffer.Sort((a, b) => a.angle.CompareTo(b.angle));

            // ring 0에 배정할 몬스터: 각도 순 정렬된 버퍼에서 균등 간격으로 선발
            // 선발 수 = Min(미배정 몬스터 수, ring 0 빈 슬롯 수)
            int ring0Free = 0;
            for (int i = 0; i < totalSlots; i++)
                if (slotMetas[i].ring == 0 && slots[i] == null) ring0Free++;

            int pickCount = Mathf.Min(unassignedBuffer.Count, ring0Free);

            // 균등 간격으로 몬스터를 선발해 ring 0에 배정
            for (int p = 0; p < pickCount; p++)
            {
                // 버퍼 전체를 균등 분할해 대표 인덱스 선택
                int         idx          = Mathf.FloorToInt(p * unassignedBuffer.Count / (float)pickCount);
                MonsterBase monster      = unassignedBuffer[idx].monster;
                float       monsterAngle = unassignedBuffer[idx].angle;

                // 이미 배정된 몬스터면 스킵 (중복 선발 방지)
                if (monsterSlotMap.TryGetValue(monster, out int cur) && cur >= 0) continue;

                // ring 0 슬롯 중 각도 차이 최소 슬롯 배정
                int   bestSlot      = -1;
                float bestAngleDiff = float.MaxValue;
                for (int i = 0; i < totalSlots; i++)
                {
                    if (slots[i] != null || slotMetas[i].ring != 0) continue;
                    float slotAngle = slotMetas[i].indexInRing * (2f * Mathf.PI / slotMetas[i].slotsInRing);
                    float diff      = Mathf.Abs(Mathf.DeltaAngle(
                        monsterAngle * Mathf.Rad2Deg, slotAngle * Mathf.Rad2Deg));
                    if (diff < bestAngleDiff)
                    {
                        bestAngleDiff = diff;
                        bestSlot      = i;
                    }
                }
                if (bestSlot < 0) continue;
                slots[bestSlot]         = monster;
                monsterSlotMap[monster] = bestSlot;
            }

            // 나머지 미배정 몬스터를 ring 1+에 각도 차이 최소로 배정
            for (int m = 0; m < unassignedBuffer.Count; m++)
            {
                MonsterBase monster = unassignedBuffer[m].monster;
                if (monsterSlotMap.TryGetValue(monster, out int cur) && cur >= 0) continue;

                float monsterAngle  = unassignedBuffer[m].angle;
                int   bestSlot      = -1;
                int   bestRing      = int.MaxValue;
                float bestAngleDiff = float.MaxValue;

                for (int i = 0; i < totalSlots; i++)
                {
                    if (slots[i] != null || slotMetas[i].ring == 0) continue;
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
                if (bestSlot < 0) continue;
                slots[bestSlot]         = monster;
                monsterSlotMap[monster] = bestSlot;
            }

            // 배정된 모든 슬롯의 위치를 플레이어 이동에 따라 갱신 (신규 배정 포함)
            for (int i = 0; i < totalSlots; i++)
            {
                if (slots[i] == null) continue;
                slots[i].AssignedSlotPos    = CalcSlotPosition(playerPos, i, slots[i].ColliderRadius);
                slots[i].AssignedSlotRadius = CalcSlotRadius(i, slots[i].ColliderRadius);
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
        // private 메서드
        // ─────────────────────────────────────────

        /// <summary>몬스터 사망 시 목록과 슬롯에서 제거하고 전멸 여부를 확인한다.</summary>
        private void OnMonsterDied(MonsterBase monster)
        {
            Unregister(monster);

            if (activeMonsters.Count == 0)
                OnAllMonstersDied?.Invoke();
        }

#if UNITY_EDITOR
        // ─────────────────────────────────────────
        // 에디터 기즈모
        // ─────────────────────────────────────────

        /// <summary>링별 슬롯 위치를 Scene/Game View(Gizmos ON)에 표시한다. 빈 슬롯은 흰색, 점유 슬롯은 빨간색.</summary>
        private static readonly Color[] RingColors =
        {
            Color.cyan, Color.green, Color.yellow, Color.magenta, Color.white
        };

        private void OnDrawGizmos()
        {
            if (slots == null || playerTransform == null) return;

            Vector3 playerPos = playerTransform.position;

            for (int i = 0; i < totalSlots; i++)
            {
                SlotMeta meta   = slotMetas[i];
                Vector3  pos    = CalcSlotPosition(playerPos, i, avgMonsterRadius);
                Color    ring   = RingColors[meta.ring % RingColors.Length];
                bool     occupied = slots[i] != null;

                // 점유 슬롯은 빨간색, 빈 슬롯은 링 색상
                Gizmos.color = occupied ? Color.red : ring;
                Gizmos.DrawWireSphere(pos, 0.2f);

                // 플레이어 중심에서 슬롯까지 선
                Gizmos.color = new Color(ring.r, ring.g, ring.b, 0.3f);
                Gizmos.DrawLine(playerPos, pos);
            }
        }
#endif
    }
}
