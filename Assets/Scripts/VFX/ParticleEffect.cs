using UnityEngine;

namespace G1
{
    /// <summary>
    /// ParticleSystem 기반 피격 이펙트.
    /// EffectBase를 상속하며, Play() 호출 시 해당 위치에서 파티클을 재생한다.
    /// Inspector의 Effect Type을 Spark로 설정해야 HitSparkPool이 올바르게 탐색한다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleEffect : EffectBase
    {
        private ParticleSystem ps;

        /// <summary>
        /// ParticleSystem 컴포넌트 캐싱 및 Stop Action 설정.
        /// Stop Action을 Callback으로 설정해야 OnParticleSystemStopped가 호출된다.
        /// </summary>
        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }

        /// <summary>파티클이 완전히 종료될 때 Unity가 자동 호출한다. Update 폴링 없이 종료 처리.</summary>
        private void OnParticleSystemStopped()
        {
            // Stop()에서 ps.Stop() 호출 시에도 트리거되므로 활성 상태일 때만 처리
            if (gameObject.activeSelf)
                base.Stop();
        }

        /// <summary>지정한 월드 위치에서 파티클을 재생한다.</summary>
        /// <param name="worldPos">이펙트 중심 월드 좌표</param>
        public override void Play(Vector3 worldPos)
        {
            if (ps == null) return;

            transform.position = worldPos;
            gameObject.SetActive(true);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        /// <summary>파티클을 즉시 정지하고 비활성화한다. 정지 후 OnParticleSystemStopped가 이어서 호출된다.</summary>
        public override void Stop()
        {
            ps?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
