using UnityEngine;

namespace G1
{
    /// <summary>
    /// 플레이어 기본 스탯을 정의하는 ScriptableObject.
    /// 인스펙터에서 수치를 조정하며, PlayerController가 런타임에 참조한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStat", menuName = "G1/PlayerStat")]
    public class PlayerStat : ScriptableObject
    {
        [Header("기본")]
        /// <summary>플레이어 레벨</summary>
        public int level = 1;

        [Header("체력")]
        /// <summary>최대 체력</summary>
        public int maxHealth = 100;

        [Header("공격")]
        /// <summary>기본 공격력 (AttackData.damage에 더해지는 보정치)</summary>
        public int attackPower = 10;
        /// <summary>크리티컬 발생 확률 (0~1)</summary>
        [Range(0f, 1f)] public float criticalChance = 0.2f;
        /// <summary>크리티컬 데미지 배율 (예: 2.0 = 200%)</summary>
        [Range(1f, 5f)] public float criticalMultiplier = 2f;

        [Header("방어")]
        /// <summary>받는 데미지를 고정값만큼 감소시키는 방어력</summary>
        public int defense = 0;
    }
}
