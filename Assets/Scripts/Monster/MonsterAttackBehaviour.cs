using UnityEngine;

namespace G1
{
    /// <summary>
    /// 모든 몬스터의 공격 애니메이션 스테이트에 공유하는 StateMachineBehaviour.
    /// MonsterBase.HitTimingNormalized를 기준으로 OnAttackHit()을 호출해 데미지를 적용한다.
    /// </summary>
    public class MonsterAttackBehaviour : StateMachineBehaviour
    {
        private MonsterBase monster;
        private bool hasHit;

        /// <summary>공격 애니메이션 진입 시 컴포넌트를 캐싱하고 히트 플래그를 초기화한다.</summary>
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (monster == null)
                monster = animator.GetComponent<MonsterBase>();

            if (monster == null)
            {
                Debug.LogWarning("[MonsterAttackBehaviour] MonsterBase 컴포넌트를 찾을 수 없습니다.", animator);
                return;
            }

            hasHit = false;
        }

        /// <summary>
        /// 매 프레임 애니메이션 진행률을 확인해 HitTimingNormalized 시점에 OnAttackHit()을 1회 호출한다.
        /// </summary>
        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (monster == null || hasHit || monster.IsDead) return;

            float progress = stateInfo.normalizedTime % 1f;
            if (progress >= monster.HitTimingNormalized)
            {
                monster.OnAttackHit();
                hasHit = true;
            }
        }
    }
}
