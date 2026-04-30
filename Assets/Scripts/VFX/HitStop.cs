using System.Collections;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 순간 Time.timeScale을 일시적으로 낮춰 타격감을 강화하는 싱글톤.
    /// MonsterBase.TakeDamage에서 HitStop.Instance.Trigger()를 호출해 사용한다.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        [Header("히트스탑 설정")]
        /// <summary>히트스탑 중 timeScale 값 (0에 가까울수록 강한 정지감)</summary>
        [SerializeField] private float slowScale = 0.05f;

        /// <summary>히트스탑 지속 시간 (실제 시간 기준, 초). 일반 피격 기본값. 강한 기술은 호출 시 직접 지정.</summary>
        [SerializeField] private float duration = 0.03f;

        private Coroutine stopCoroutine;

        /// <summary>싱글톤 인스턴스를 설정한다. 씬 전환 후에도 유지되도록 DontDestroyOnLoad를 적용한다.</summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            // DontDestroyOnLoad는 루트 오브젝트에만 적용 가능하므로 부모에서 분리 후 호출
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 히트스탑을 실행한다. 이미 실행 중이면 처음부터 다시 시작한다.
        /// MonsterBase.TakeDamage에서 피격 시 호출된다.
        /// </summary>
        /// <param name="overrideDuration">0 이상이면 Inspector 기본값 대신 이 값을 사용한다. 강한 기술에 활용.</param>
        public void Trigger(float overrideDuration = -1f)
        {
            if (stopCoroutine != null)
                StopCoroutine(stopCoroutine);
            float d = overrideDuration >= 0f ? overrideDuration : duration;
            stopCoroutine = StartCoroutine(StopRoutine(d));
        }

        /// <summary>
        /// timeScale을 slowScale로 낮추고 d초(실제 시간) 후 1로 복원한다.
        /// WaitForSecondsRealtime을 사용해 timeScale이 0이어도 대기가 진행된다.
        /// </summary>
        private IEnumerator StopRoutine(float d)
        {
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(d);
            Time.timeScale = 1f;
            stopCoroutine = null;
        }

        /// <summary>씬 전환 등으로 비활성화 시 진행 중인 히트스탑을 즉시 해제한다.</summary>
        private void OnDisable()
        {
            if (stopCoroutine != null)
            {
                StopCoroutine(stopCoroutine);
                stopCoroutine = null;
            }
            Time.timeScale = 1f;
        }

        /// <summary>오브젝트 파괴 시 timeScale이 낮은 채로 굳는 것을 방지한다.</summary>
        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
