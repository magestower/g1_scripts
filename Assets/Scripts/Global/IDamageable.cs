using UnityEngine;

namespace G1
{
    /// <summary>
    /// 데미지를 받을 수 있는 오브젝트가 구현하는 인터페이스.
    /// 몬스터, 파괴 가능한 오브젝트 등 피격 처리가 필요한 모든 대상에 적용.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 데미지를 받아 체력을 감소시킨다.
        /// </summary>
        /// <param name="damage">적용할 데미지 양 (양수)</param>
        void TakeDamage(int damage);

        /// <summary>체력이 0 이하인지 여부</summary>
        bool IsDead { get; }
    }
}
