using UnityEngine;

namespace G1
{
    /// <summary>
    /// 모든 공격 Animator 스테이트에 공유하는 StateMachineBehaviour.
    /// PlayerController.CurrentAttackData를 읽어 이펙트 타이밍, 히트 판정, 데미지를 데이터 기반으로 처리합니다.
    /// </summary>
    public class AttackStateBehaviour : StateMachineBehaviour
    {
        private PlayerController player;
        private PlayerWeaponVFXController weaponVFX;

        private bool hasHit;          // 히트 판정 중복 호출 방지 플래그
        private bool hasPlayedEffect; // 슬래시 이펙트 + 휘두르기 사운드 중복 발동 방지 플래그

        /// <summary>
        /// 애니메이션 상태 진입 시 1회 호출됩니다.
        /// 컴포넌트를 캐싱하고 플래그를 초기화합니다.
        /// </summary>
        public override void OnStateEnter(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            // 컴포넌트 캐싱 (매번 GetComponent 방지)
            if (player == null)
                player = animator.GetComponent<PlayerController>();

            if (player == null)
            {
                Debug.LogWarning("[AttackStateBehaviour] PlayerController 컴포넌트를 찾을 수 없습니다.", animator);
                return;
            }

            // PlayerWeaponVFXController는 별도 오브젝트에 있으므로 씬 전체에서 탐색
            if (weaponVFX == null)
                weaponVFX = FindAnyObjectByType<PlayerWeaponVFXController>();

            hasHit = false;
            hasPlayedEffect = false;

            if (player.CurrentWeaponType != WeaponType.Unarmed)
                weaponVFX?.SetWeaponTrail(true);
        }

        /// <summary>
        /// 애니메이션 재생 중 매 프레임 호출됩니다.
        /// CurrentAttackData의 타이밍 설정에 따라 이펙트와 히트 판정을 처리합니다.
        /// </summary>
        public override void OnStateUpdate(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (player == null) return;

            AttackData data = player.CurrentAttackData;
            if (data == null) return;

            // normalizedTime을 0~1 범위로 정규화 (루프 애니메이션 대응)
            float progress = stateInfo.normalizedTime % 1f;

            // 지정된 진행률 시점에 슬래시 이펙트 + 휘두르기 사운드 발동 (1회만)
            if (!hasPlayedEffect && progress >= data.effectTriggerNormalized)
            {
                if (weaponVFX != null)
                    weaponVFX.PlayEffect(player.CurrentWeaponType);
                if (data.swingSound != null && SoundManager.Instance != null)
                    SoundManager.Instance.Play(data.swingSound, player.transform.position, pitchVariance: 0.05f);
                hasPlayedEffect = true;
            }

            // 지정된 진행률 시점에 히트 판정 (1회만), 실제 명중 시에만 타격 사운드 재생
            if (!hasHit && progress >= data.hitTimingNormalized)
            {
                bool landed = player.OnAttackHit(data.damage);
                if (landed && data.hitSound != null && SoundManager.Instance != null)
                    SoundManager.Instance.Play(data.hitSound, player.transform.position, pitchVariance: 0.1f);
                hasHit = true;
            }
        }

        /// <summary>
        /// 애니메이션 상태 종료 시 1회 호출됩니다.
        /// PlayerController에 공격 종료를 알립니다.
        /// </summary>
        public override void OnStateExit(
            Animator animator,
            AnimatorStateInfo stateInfo,
            int layerIndex)
        {
            if (player == null) return;

            weaponVFX?.SetWeaponTrail(false);
            player.OnAttackEnd();
        }
    }
}
