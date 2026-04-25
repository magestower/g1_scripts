using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 스파크 이펙트를 관리하는 싱글톤 풀.
    /// AttackType/DamageType 조합에 따라 다른 파티클 프리팹을 재생한다.
    /// 매핑되지 않은 조합은 defaultSparkPrefab으로 폴백한다.
    /// </summary>
    public class HitSparkPool : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // 내부 타입
        // ─────────────────────────────────────────

        /// <summary>AttackType/DamageType 조합과 파티클 프리팹을 연결하는 매핑 항목</summary>
        [System.Serializable]
        private struct SparkEntry
        {
            public AttackType attackType;
            public DamageType damageType;
            /// <summary>해당 조합에 사용할 파티클 프리팹</summary>
            public GameObject prefab;
            /// <summary>씬 시작 시 미리 생성해둘 수</summary>
            public int prewarmSize;
        }

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        /// <summary>매핑되지 않은 조합에 사용할 기본 파티클 프리팹</summary>
        [SerializeField] private GameObject defaultSparkPrefab;
        /// <summary>기본 프리팹 prewarm 수</summary>
        [SerializeField] private int defaultPrewarmSize = 8;

        /// <summary>AttackType/DamageType 조합별 파티클 프리팹 매핑 목록</summary>
        [SerializeField] private SparkEntry[] sparkEntries;

        // ─────────────────────────────────────────
        // 싱글톤
        // ─────────────────────────────────────────

        public static HitSparkPool Instance { get; private set; }

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        /// <summary>프리팹별 파티클 풀 목록</summary>
        private readonly Dictionary<GameObject, List<ParticleSystem>> pools = new();

        /// <summary>조합 키 → 프리팹 빠른 조회용 딕셔너리</summary>
        private readonly Dictionary<(AttackType, DamageType), GameObject> prefabMap = new();

        // ─────────────────────────────────────────
        // Unity 이벤트
        // ─────────────────────────────────────────

        /// <summary>싱글톤 설정 및 풀 초기화</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 기본 풀 초기화
            if (defaultSparkPrefab != null)
            {
                var defaultPool = new List<ParticleSystem>();
                Prewarm(defaultSparkPrefab, defaultPool, defaultPrewarmSize);
                pools[defaultSparkPrefab] = defaultPool;
            }
            else
            {
                Debug.LogWarning("[HitSparkPool] defaultSparkPrefab이 할당되지 않았습니다.", this);
            }

            // 조합별 매핑 초기화
            if (sparkEntries == null) return;
            foreach (SparkEntry entry in sparkEntries)
            {
                if (entry.prefab == null) continue;

                var key = (entry.attackType, entry.damageType);
                prefabMap[key] = entry.prefab;

                if (!pools.ContainsKey(entry.prefab))
                {
                    var pool = new List<ParticleSystem>();
                    Prewarm(entry.prefab, pool, entry.prewarmSize);
                    pools[entry.prefab] = pool;
                }
            }
        }

        /// <summary>재생이 끝난 파티클을 매 프레임 체크해 비활성화한다.</summary>
        private void Update()
        {
            foreach (var pool in pools.Values)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    ParticleSystem ps = pool[i];
                    if (ps == null || !ps.gameObject.activeSelf) continue;
                    if (!ps.IsAlive())
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.gameObject.SetActive(false);
                    }
                }
            }
        }

        // ─────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────

        /// <summary>
        /// 피격 위치에 스파크 이펙트를 재생한다.
        /// AttackType/DamageType 조합에 맞는 프리팹을 사용하며, 매핑이 없으면 기본 프리팹으로 폴백한다.
        /// </summary>
        /// <param name="worldPos">피격 위치</param>
        /// <param name="attackType">공격 종류</param>
        /// <param name="damageType">데미지 유형</param>
        public void Show(Vector3 worldPos, AttackType attackType = AttackType.Physical, DamageType damageType = DamageType.Normal)
        {
            var key = (attackType, damageType);
            GameObject prefab = prefabMap.TryGetValue(key, out var mapped) ? mapped : defaultSparkPrefab;

            if (prefab == null) return;
            if (!pools.TryGetValue(prefab, out var pool))
            {
                Debug.LogWarning($"[HitSparkPool] prefab '{prefab.name}'에 대한 풀이 없습니다. Awake 초기화를 확인하세요.", this);
                return;
            }

            ParticleSystem ps = GetFromPool(prefab, pool);
            if (ps == null) return;
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>풀에서 비활성 파티클을 꺼내거나, 없으면 동적 생성한다.</summary>
        private ParticleSystem GetFromPool(GameObject prefab, List<ParticleSystem> pool)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                    return pool[i];

            ParticleSystem newPs = CreateInstance(prefab);
            if (newPs != null)
                pool.Add(newPs);
            return newPs;
        }

        /// <summary>프리팹을 prewarmCount만큼 미리 생성해 풀에 채운다.</summary>
        private void Prewarm(GameObject prefab, List<ParticleSystem> pool, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = CreateInstance(prefab);
                if (ps != null)
                    pool.Add(ps);
            }
        }

        /// <summary>새 파티클 인스턴스를 생성하고 비활성화 상태로 반환한다.</summary>
        private ParticleSystem CreateInstance(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                Debug.LogError($"[HitSparkPool] {prefab.name}에 ParticleSystem 컴포넌트가 없습니다.", go);
                Destroy(go);
                return null;
            }
            go.SetActive(false);
            return ps;
        }
    }
}
