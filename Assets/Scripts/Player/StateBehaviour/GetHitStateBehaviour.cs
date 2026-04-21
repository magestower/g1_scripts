using UnityEngine;

namespace G1
{
    /// <summary>
    /// GetHit Animator 스테이트에 부착하는 StateMachineBehaviour.
    /// Any State → GetHit 점프 시 OnStateExit가 호출되지 않는 문제를 보완한다.
    /// 공격 중 피격으로 진입한 경우 PlayerController에 공격 종료를 강제로 알린다.
    /// </summary>
    public class GetHitStateBehaviour : StateMachineBehaviour
    {
        private PlayerController player;

        /// <summary>
        /// GetHit 스테이트 진입 시 호출됩니다.
        /// 공격 중이었다면 OnAttackEnd()를 호출해 Attacking 상태 고착을 방지합니다.
        /// </summary>
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (player == null)
                player = animator.GetComponent<PlayerController>();

            if (player == null) return;

            // 공격 중 피격으로 진입한 경우 공격 종료 처리
            if (player.IsAttacking)
                player.OnAttackEnd();
        }
    }
}
