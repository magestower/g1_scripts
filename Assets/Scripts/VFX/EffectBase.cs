using UnityEngine;

namespace G1
{
    /// <summary>
    /// 피격 이펙트의 기반 클래스.
    /// HitEffectType 플래그 값을 선언하고 Play()를 구현한다.
    /// HitSparkPool이 이 컴포넌트를 탐색해 해당 플래그에 맞는 이펙트를 재생한다.
    /// </summary>
    public abstract class EffectBase : MonoBehaviour
    {
        /// <summary>이 컴포넌트가 담당하는 이펙트 종류</summary>
        [SerializeField] private HitEffectType effectType;

        /// <summary>이 컴포넌트가 담당하는 이펙트 종류</summary>
        public HitEffectType EffectType => effectType;

        /// <summary>
        /// 지정한 월드 위치에서 이펙트를 재생한다.
        /// </summary>
        /// <param name="worldPos">이펙트 중심 월드 좌표</param>
        public abstract void Play(Vector3 worldPos);

        /// <summary>
        /// 이펙트를 중단하고 비활성화한다. IsFinished가 true일 때 HitSparkPool이 호출한다.
        /// 기본 구현은 gameObject.SetActive(false). 추가 정리가 필요한 경우 오버라이드한다.
        /// </summary>
        public virtual void Stop()
        {
            gameObject.SetActive(false);
        }
    }
}
