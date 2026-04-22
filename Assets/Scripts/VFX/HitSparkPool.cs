using System.Collections.Generic;
using CartoonFX;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 스파크 이펙트(CFXR)를 관리하는 싱글톤 풀.
    /// MonsterBase.TakeDamage에서 Show()를 호출해 피격 위치에 이펙트를 재생한다.
    /// CFXR_Effect의 ClearBehavior.Disable을 활용해 재생 완료 시 자동으로 비활성화되며,
    /// HitSparkDisableNotifier가 OnDisable을 감지해 풀에 반납한다.
    /// </summary>
    public class HitSparkPool : MonoBehaviour
    {
        public static HitSparkPool Instance { get; private set; }

        /// <summary>사용할 CFXR 히트 이펙트 프리팹</summary>
        [SerializeField] private GameObject sparkPrefab;

        /// <summary>씬 시작 시 미리 생성해둘 이펙트 오브젝트 수</summary>
        [SerializeField] private int prewarmSize = 8;

        private readonly Stack<CFXR_Effect> pool = new();

        /// <summary>싱글톤을 설정하고 풀을 미리 채운다. 씬 전환 후에도 유지되도록 DontDestroyOnLoad를 적용한다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            for (int i = 0; i < prewarmSize; i++)
                pool.Push(CreateInstance());
        }

        /// <summary>
        /// 피격 위치에 스파크 이펙트를 재생한다. MonsterBase.TakeDamage에서 호출된다.
        /// </summary>
        /// <param name="worldPos">피격 위치 (목 본 기준)</param>
        public void Show(Vector3 worldPos)
        {
            CFXR_Effect effect = pool.Count > 0 ? pool.Pop() : CreateInstance();
            effect.transform.position = worldPos;
            effect.gameObject.SetActive(true);
        }

        /// <summary>
        /// HitSparkDisableNotifier에서 OnDisable 감지 시 호출된다.
        /// </summary>
        /// <param name="effect">반납할 이펙트 오브젝트</param>
        internal void Release(CFXR_Effect effect)
        {
            pool.Push(effect);
        }

        /// <summary>
        /// 새 이펙트 인스턴스를 생성한다.
        /// ClearBehavior를 Disable로 설정하고, OnDisable 감지용 컴포넌트를 추가한다.
        /// </summary>
        private CFXR_Effect CreateInstance()
        {
            GameObject go = Instantiate(sparkPrefab, transform);
            CFXR_Effect effect = go.GetComponent<CFXR_Effect>();

            // 재생 완료 시 Destroy 대신 Disable로 전환해 풀 재사용 가능하게 설정
            effect.clearBehavior = CFXR_Effect.ClearBehavior.Disable;

            // OnDisable 감지 후 풀 반납을 위한 노티파이어 추가
            var notifier = go.AddComponent<HitSparkDisableNotifier>();
            notifier.Init(effect, this);

            go.SetActive(false);
            return effect;
        }
    }

    /// <summary>
    /// CFXR_Effect가 재생 완료 후 SetActive(false)될 때 HitSparkPool에 반납 신호를 보내는 컴포넌트.
    /// HitSparkPool.CreateInstance에서 동적으로 추가된다.
    /// </summary>
    internal class HitSparkDisableNotifier : MonoBehaviour
    {
        private CFXR_Effect effect;
        private HitSparkPool pool;

        /// <summary>감지 대상 이펙트와 반납할 풀을 설정한다.</summary>
        public void Init(CFXR_Effect effect, HitSparkPool pool)
        {
            this.effect = effect;
            this.pool   = pool;
        }

        /// <summary>오브젝트 비활성화 시 풀에 반납한다. prewarm 초기화 시 호출을 막기 위해 pool null 체크.</summary>
        private void OnDisable()
        {
            if (pool != null && effect != null)
                pool.Release(effect);
        }
    }
}
