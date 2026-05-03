using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 이펙트를 관리하는 싱글톤 풀.
    /// EffectBase를 상속한 컴포넌트가 붙은 프리팹을 등록하면,
    /// Show(effectPos, HitEffectType 플래그 조합) 호출 시 해당 이펙트들을 동시 재생한다.
    /// </summary>
    public class HitSparkPool : MonoBehaviour
    {
        // ─────────────────────────────────────────
        // 내부 타입
        // ─────────────────────────────────────────

        /// <summary>이펙트 프리팹과 prewarm 수를 묶는 등록 항목</summary>
        [System.Serializable]
        private struct EffectEntry
        {
            /// <summary>EffectBase 컴포넌트가 붙어있는 프리팹</summary>
            public GameObject prefab;
            /// <summary>씬 시작 시 미리 생성해둘 수</summary>
            public int prewarmSize;
        }

        // ─────────────────────────────────────────
        // Inspector 설정값
        // ─────────────────────────────────────────

        /// <summary>등록할 이펙트 프리팹 목록. 각 프리팹에 EffectBase 컴포넌트가 있어야 한다.</summary>
        [SerializeField] private EffectEntry[] effectEntries;

        // ─────────────────────────────────────────
        // 싱글톤
        // ─────────────────────────────────────────

        public static HitSparkPool Instance { get; private set; }

        // ─────────────────────────────────────────
        // 런타임 상태
        // ─────────────────────────────────────────

        /// <summary>HitEffectType → 풀 (비활성 인스턴스 목록)</summary>
        private readonly Dictionary<HitEffectType, List<EffectBase>> pools = new();

        /// <summary>Show() 호출마다 Enum.GetValues 배열 할당을 피하기 위한 정적 캐시</summary>
        private static readonly HitEffectType[] allEffectTypes = (HitEffectType[])System.Enum.GetValues(typeof(HitEffectType));

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
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (effectEntries == null) return;
            foreach (EffectEntry entry in effectEntries)
            {
                if (entry.prefab == null) continue;

                EffectBase sample = entry.prefab.GetComponent<EffectBase>();
                if (sample == null)
                {
                    Debug.LogError($"[HitSparkPool] '{entry.prefab.name}'에 EffectBase 컴포넌트가 없습니다.", entry.prefab);
                    continue;
                }

                HitEffectType type = sample.EffectType;
                if (!pools.ContainsKey(type))
                    pools[type] = new List<EffectBase>();

                for (int i = 0; i < entry.prewarmSize; i++)
                {
                    EffectBase instance = CreateInstance(entry.prefab);
                    if (instance != null) pools[type].Add(instance);
                }
            }
        }

        /// <summary>씬 언로드 시 재생 중인 이펙트를 중단하고 파괴된 참조를 정리한다.</summary>
        private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene _)
        {
            foreach (var pool in pools.Values)
            {
                foreach (EffectBase effect in pool)
                    if (effect != null && effect.gameObject.activeSelf)
                        effect.Stop();
                pool.RemoveAll(e => e == null);
            }
        }

        /// <summary>오브젝트 파괴 시 싱글톤 참조 및 이벤트 구독 해제</summary>
        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
            if (Instance == this) Instance = null;
        }

        // ─────────────────────────────────────────
        // 공개 API
        // ─────────────────────────────────────────

        /// <summary>
        /// 플래그로 조합된 이펙트들을 worldPos에서 동시 재생한다.
        /// 등록되지 않은 플래그는 무시한다.
        /// </summary>
        /// <param name="worldPos">이펙트 중심 월드 좌표</param>
        /// <param name="effects">재생할 이펙트 조합 (비트 플래그)</param>
        public void Show(Vector3 worldPos, HitEffectType effects)
        {
            foreach (HitEffectType type in allEffectTypes)
            {
                if (type == HitEffectType.None) continue;
                if ((effects & type) == 0) continue;

                if (!pools.TryGetValue(type, out var pool))
                {
                    Debug.LogWarning($"[HitSparkPool] '{type}' 이펙트가 등록되지 않았습니다.", this);
                    continue;
                }

                EffectBase effect = GetFromPool(type, pool);
                effect?.Play(worldPos);
            }
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>풀에서 비활성 인스턴스를 꺼내거나, 없으면 동적 생성한다.</summary>
        private EffectBase GetFromPool(HitEffectType type, List<EffectBase> pool)
        {
            foreach (EffectBase e in pool)
                if (e != null && !e.gameObject.activeSelf)
                    return e;

            // 풀에 등록된 프리팹을 찾아 동적 생성
            if (effectEntries == null) return null;
            foreach (EffectEntry entry in effectEntries)
            {
                if (entry.prefab == null) continue;
                EffectBase sample = entry.prefab.GetComponent<EffectBase>();
                if (sample != null && sample.EffectType == type)
                {
                    EffectBase newInstance = CreateInstance(entry.prefab);
                    if (newInstance != null) pool.Add(newInstance);
                    return newInstance;
                }
            }
            return null;
        }

        /// <summary>프리팹 인스턴스를 생성하고 비활성화 상태로 반환한다.</summary>
        private EffectBase CreateInstance(GameObject prefab)
        {
            GameObject go = Instantiate(prefab, transform);
            EffectBase effect = go.GetComponent<EffectBase>();
            if (effect == null)
            {
                Debug.LogError($"[HitSparkPool] {prefab.name}에 EffectBase 컴포넌트가 없습니다.", go);
                Destroy(go);
                return null;
            }
            go.SetActive(false);
            return effect;
        }
    }
}
