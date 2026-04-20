using UnityEngine;

namespace G1
{
    /// <summary>
    /// 공격 종류별 데이터를 정의하는 ScriptableObject.
    /// 인스펙터에서 공격 타이밍, 데미지, 이펙트 조건 등을 설정합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "AttackData", menuName = "G1/AttackData")]
    public class AttackData : ScriptableObject
    {
        [Header("Animator 연동")]
        /// <summary>이 공격을 발동할 Animator Trigger 파라미터명</summary>
        public string animatorTrigger;

        [Header("타이밍 (0~1, 애니메이션 진행률)")]
        /// <summary>슬래시 이펙트를 발동할 애니메이션 진행률</summary>
        [Range(0f, 1f)] public float effectTriggerNormalized = 0.3f;

        /// <summary>히트 판정을 발생시킬 애니메이션 진행률</summary>
        [Range(0f, 1f)] public float hitTimingNormalized = 0.5f;

        [Header("데미지")]
        /// <summary>이 공격의 기본 데미지</summary>
        public int damage = 10;

        [Header("이펙트 조건")]
        /// <summary>true이면 isEquipped(장비 장착) 상태일 때만 슬래시 이펙트 발동</summary>
        public bool requiresWeapon = true;
    }
}
