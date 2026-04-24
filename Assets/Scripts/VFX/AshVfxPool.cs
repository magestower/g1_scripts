using UnityEngine;

namespace G1
{
    /// <summary>
    /// 몬스터 사망 시 재(Ash) 파티클 이펙트를 관리하는 싱글톤 풀.
    /// MonsterBase.Die()에서 Show()를 호출해 사망 위치에 이펙트를 1회 재생한다.
    /// </summary>
    public class AshVfxPool : MonoBehaviour
    {
        public static AshVfxPool Instance { get; private set; }

        /// <summary>vfx_ash 파티클 프리팹 (ParticleSystem 컴포넌트 필수)</summary>
        [SerializeField] private GameObject ashPrefab;

        /// <summary>씬 시작 시 미리 생성해둘 이펙트 오브젝트 수</summary>
        [SerializeField] private int prewarmSize = 4;

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

            if (ashPrefab == null)
            {
                Debug.LogError("[AshVfxPool] ashPrefab이 할당되지 않았습니다.", this);
                return;
            }

            pool = new ParticleSystem[prewarmSize];
            for (int i = 0; i < prewarmSize; i++)
                pool[i] = CreateInstance();
            poolSize = prewarmSize;
        }

        /// <summary>
        /// 지정 위치에 재 파티클을 1회 재생한다. MonsterBase.Die()에서 호출된다.
        /// </summary>
        /// <param name="worldPos">재생할 월드 위치 (몬스터 발 위치 기준)</param>
        public void Show(Vector3 worldPos)
        {
            // ashPrefab 미할당으로 풀 초기화가 실패한 경우 무시
            if (pool == null) return;
            ParticleSystem ps = GetFromPool();
            if (ps == null) return;
            ps.transform.position = worldPos;
            ps.gameObject.SetActive(true);
            ps.Play();
        }

        // ─────────────────────────────────────────
        // 내부 처리
        // ─────────────────────────────────────────

        /// <summary>재생이 끝난 파티클을 매 프레임 체크해 비활성화한다.</summary>
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

        /// <summary>비활성화된 풀 슬롯에서 꺼내거나, 없으면 동적으로 새 인스턴스를 생성해 반환한다.</summary>
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
            GameObject go = Instantiate(ashPrefab, transform);
            ParticleSystem ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
                Debug.LogError("[AshVfxPool] ashPrefab에 ParticleSystem 컴포넌트가 없습니다.", go);
            go.SetActive(false);
            return ps;
        }
    }
}
