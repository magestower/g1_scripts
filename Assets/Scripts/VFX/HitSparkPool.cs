using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 스파크 이펙트를 관리하는 싱글톤 풀.
    /// MonsterBase.TakeDamage에서 Show()를 호출해 피격 위치에 이펙트를 재생한다.
    /// ParticleSystem의 재생 완료를 매 프레임 체크해 자동으로 풀에 반납한다.
    /// </summary>
    public class HitSparkPool : MonoBehaviour
    {
        public static HitSparkPool Instance { get; private set; }

        /// <summary>사용할 파티클 이펙트 프리팹 (ParticleSystem 컴포넌트 필수)</summary>
        [SerializeField] private GameObject sparkPrefab;

        /// <summary>씬 시작 시 미리 생성해둘 이펙트 오브젝트 수</summary>
        [SerializeField] private int prewarmSize = 8;

        private ParticleSystem[] pool;
        private int poolSize;

        /// <summary>싱글톤을 설정하고 풀을 미리 채운다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (sparkPrefab == null)
            {
                Debug.LogError("[HitSparkPool] sparkPrefab이 할당되지 않았습니다.", this);
                return;
            }

            pool = new ParticleSystem[prewarmSize];
            for (int i = 0; i < prewarmSize; i++)
                pool[i] = CreateInstance();
            poolSize = prewarmSize;
        }

        /// <summary>
        /// 피격 위치에 스파크 이펙트를 재생한다. MonsterBase.TakeDamage에서 호출된다.
        /// </summary>
        /// <param name="worldPos">피격 위치 (목 본 기준)</param>
        public void Show(Vector3 worldPos)
        {
            ParticleSystem ps = GetFromPool();
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>
        /// 재생이 끝난 파티클을 매 프레임 체크해 비활성화한다.
        /// </summary>
        private void Update()
        {
            if (pool == null) return;
            for (int i = 0; i < poolSize; i++)
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

        /// <summary>비활성화된 풀 슬롯에서 꺼내거나, 없으면 새 인스턴스를 생성해 반환한다.</summary>
        private ParticleSystem GetFromPool()
        {
            for (int i = 0; i < poolSize; i++)
                if (pool[i] != null && !pool[i].gameObject.activeSelf)
                    return pool[i];

            // 풀이 모자라면 동적 확장
            ParticleSystem newPs = CreateInstance();
            System.Array.Resize(ref pool, poolSize + 1);
            pool[poolSize] = newPs;
            poolSize++;
            return newPs;
        }

        /// <summary>새 파티클 인스턴스를 생성하고 비활성화 상태로 반환한다.</summary>
        private ParticleSystem CreateInstance()
        {
            GameObject go = Instantiate(sparkPrefab, transform);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                Debug.LogError("[HitSparkPool] sparkPrefab에 ParticleSystem 컴포넌트가 없습니다.", go);
            go.SetActive(false);
            return ps;
        }
    }
}
