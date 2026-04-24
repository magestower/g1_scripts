using UnityEngine;


namespace G1
{
    /// <summary>
    /// CharacterEquipmentManager의 장착/해제 이벤트를 구독하여
    /// Animator의 IsEquipped 파라미터를 자동으로 동기화하는 브릿지 컴포넌트.
    /// CharacterEquipmentManager와 같은 오브젝트에 부착합니다.
    /// </summary>
    [RequireComponent(typeof(CharacterEquipmentManager))]
    public class EquipmentAnimationBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        /// <summary>Animator 파라미터 해시 — string 비교 대신 int 해시 사용 (성능)</summary>
        private static readonly int IsEquippedHash = Animator.StringToHash("isEquipped");

        private CharacterEquipmentManager _manager;

        /// <summary>
        /// Awake: CharacterEquipmentManager 참조 획득, Animator 인스펙터 미할당 시 자식에서 자동 탐색.
        /// </summary>
        private void Awake()
        {
            _manager = GetComponent<CharacterEquipmentManager>();

            // 인스펙터 미할당 시 자식 오브젝트에서 Animator 자동 탐색
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null)
                Debug.LogError("[EquipmentAnimationBridge] Animator를 찾을 수 없습니다. 인스펙터에서 직접 할당해주세요.", this);
        }

        private void OnEnable()
        {
            _manager.OnEquipped   += HandleEquipped;
            _manager.OnUnequipped += HandleUnequipped;
        }

        private void OnDisable()
        {
            _manager.OnEquipped   -= HandleEquipped;
            _manager.OnUnequipped -= HandleUnequipped;
        }

        /// <summary>파괴 시 구독을 명시적으로 해제한다. OnDisable보다 늦게 호출되므로 파괴 순서 의존성을 제거한다.</summary>
        private void OnDestroy()
        {
            if (_manager == null) return;
            _manager.OnEquipped   -= HandleEquipped;
            _manager.OnUnequipped -= HandleUnequipped;
        }

        /// <summary>
        /// 장비 장착 이벤트 처리.
        /// Weapon 슬롯 장착 시 Animator의 IsEquipped를 true로 설정합니다.
        /// </summary>
        /// <param name="slot">장착된 슬롯</param>
        /// <param name="data">장착된 장비 데이터</param>
        private void HandleEquipped(EquipmentSlot slot, EquipmentData data)
        {
            if (animator == null) return;

            // Weapon 슬롯 장착 시 IsEquipped = true
            if (slot == EquipmentSlot.Weapon)
                animator.SetBool(IsEquippedHash, true);
        }

        /// <summary>
        /// 장비 해제 이벤트 처리.
        /// Weapon 슬롯 해제 시 Animator의 IsEquipped를 false로 설정합니다.
        /// </summary>
        /// <param name="slot">해제된 슬롯</param>
        /// <param name="data">해제된 장비 데이터</param>
        private void HandleUnequipped(EquipmentSlot slot, EquipmentData data)
        {
            if (animator == null) return;

            // Weapon 슬롯 해제 시 IsEquipped = false
            if (slot == EquipmentSlot.Weapon)
                animator.SetBool(IsEquippedHash, false);
        }
    }
}
