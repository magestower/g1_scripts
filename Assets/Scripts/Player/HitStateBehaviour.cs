using UnityEngine;

namespace G1
{
    /// <summary>
    /// root|Hit_Head_remap_Slot 1 애니메이션 상태에 부착하는 StateMachineBehaviour.
    /// 피격 애니메이션 종료 시 isHit 파라미터를 false로 초기화합니다.
    /// </summary>
    public class HitStateBehaviour : StateMachineBehaviour
    {
        /// <summary>isHit 파라미터 해시 — 매 프레임 StringToHash 비용 제거</summary>
        private static readonly int IsHit = Animator.StringToHash("isHit");

        /// <summary>
        /// 피격 애니메이션 종료 시 호출됩니다.
        /// isHit을 false로 설정하여 Idle/Run 상태로 복귀할 수 있도록 합니다.
        /// </summary>
        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            animator.SetBool(IsHit, false);
        }
    }
}
