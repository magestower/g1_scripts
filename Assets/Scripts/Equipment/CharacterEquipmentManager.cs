using System;
using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 캐릭터의 무기/보조장비 장착/해제를 런타임에서 관리하는 컴포넌트.
    /// EquipmentSlot 단위로 장비를 관리하며, 같은 슬롯에 새 장비 장착 시 기존 장비를 자동 제거합니다.
    /// maru 캐릭터 루트 오브젝트에 CharacterCostumeManager와 함께 부착합니다.
    /// </summary>
    public class CharacterEquipmentManager : MonoBehaviour
    {
        // ──────────────────────────────────────────────────────────────
        //  인스펙터 설정
        // ──────────────────────────────────────────────────────────────

        [Header("손 본 직접 할당 (권장)")]

        /// <summary>오른손 본 — 무기(Weapon) 슬롯 장착 위치</summary>
        [SerializeField] private Transform weaponBone;

        /// <summary>왼손 본 — 보조장비(SubEquipment) 슬롯 장착 위치</summary>
        [SerializeField] private Transform subEquipmentBone;

        [Header("자동 탐색 폴백 — 인스펙터 미할당 시 사용")]

        /// <summary>인스펙터 미할당 시 FindDeep으로 탐색할 오른손 본 이름</summary>
        [SerializeField] private string weaponBoneName = "weapon";

        /// <summary>인스펙터 미할당 시 FindDeep으로 탐색할 왼손 본 이름</summary>
        [SerializeField] private string subEquipmentBoneName = "sub_equipment";

        // ──────────────────────────────────────────────────────────────
        //  이벤트
        // ──────────────────────────────────────────────────────────────

        /// <summary>장비 장착 완료 시 발생합니다. 파라미터: (슬롯, 장착된 데이터)</summary>
        public event Action<EquipmentSlot, EquipmentData> OnEquipped;

        /// <summary>장비 해제 완료 시 발생합니다. 파라미터: (슬롯, 해제된 데이터)</summary>
        public event Action<EquipmentSlot, EquipmentData> OnUnequipped;

        // ──────────────────────────────────────────────────────────────
        //  내부 상태
        // ──────────────────────────────────────────────────────────────

        /// <summary>슬롯별 장착된 장비 인스턴스</summary>
        private readonly Dictionary<EquipmentSlot, GameObject> _equippedInstances
            = new Dictionary<EquipmentSlot, GameObject>();

        /// <summary>슬롯별 장착된 EquipmentData — 이벤트 및 외부 조회에 사용</summary>
        private readonly Dictionary<EquipmentSlot, EquipmentData> _equippedData
            = new Dictionary<EquipmentSlot, EquipmentData>();

        // ──────────────────────────────────────────────────────────────
        //  Unity 생명주기
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 인스펙터에서 본이 할당되지 않은 경우 FindDeep으로 자동 탐색합니다.
        /// CharacterCostumeManager의 AutoFindBodyParts() 패턴을 동일하게 적용합니다.
        /// </summary>
        private void Awake()
        {
            if (weaponBone == null)
                weaponBone = transform.FindDeep(weaponBoneName);
            if (subEquipmentBone == null)
                subEquipmentBone = transform.FindDeep(subEquipmentBoneName);

            if (weaponBone == null)
                Debug.LogError($"[CharacterEquipmentManager] 오른손 본 '{weaponBoneName}'을 찾을 수 없습니다. 인스펙터에서 직접 할당해주세요.");
            if (subEquipmentBone == null)
                Debug.LogError($"[CharacterEquipmentManager] 왼손 본 '{subEquipmentBoneName}'을 찾을 수 없습니다. 인스펙터에서 직접 할당해주세요.");
        }

        /// <summary>오브젝트 파괴 시 장착된 장비 인스턴스를 정리합니다.</summary>
        private void OnDestroy()
        {
            UnequipAll();
        }

        // ──────────────────────────────────────────────────────────────
        //  공개 API
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정한 EquipmentData를 해당 슬롯에 장착합니다.
        /// 같은 슬롯에 기존 장비가 있으면 자동으로 제거 후 교체합니다.
        /// </summary>
        /// <param name="data">장착할 장비 데이터 (EquipmentData ScriptableObject)</param>
        public void Equip(EquipmentData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[CharacterEquipmentManager] EquipmentData가 null입니다.");
                return;
            }

            if (data.prefab == null)
            {
                Debug.LogWarning($"[CharacterEquipmentManager] '{data.equipmentName}'의 prefab이 할당되지 않았습니다.");
                return;
            }

            // 대상 손 본 결정
            Transform bone = data.slot == EquipmentSlot.Weapon ? weaponBone : subEquipmentBone;
            if (bone == null)
            {
                Debug.LogError($"[CharacterEquipmentManager] 슬롯 '{data.slot}'에 대한 손 본이 없습니다. Awake 오류를 확인해주세요.");
                return;
            }

            // 같은 슬롯에 기존 장비가 있으면 제거
            Unequip(data.slot);

            // 장비 프리팹을 손 본의 자식으로 생성
            GameObject instance = Instantiate(data.prefab, bone);
            instance.name = $"Equipment_{data.slot}_{data.equipmentName}";

            // 손 본 기준 Local Transform 보정 적용
            instance.transform.localPosition = data.positionOffset;
            instance.transform.localRotation = Quaternion.Euler(data.rotationOffset);
            instance.transform.localScale    = data.scaleOverride;

            // 슬롯 등록
            _equippedInstances[data.slot] = instance;
            _equippedData[data.slot]      = data;

            OnEquipped?.Invoke(data.slot, data);

            Debug.Log($"[CharacterEquipmentManager] '{data.equipmentName}' 장착 완료 (슬롯: {data.slot}).");
        }

        /// <summary>
        /// 지정한 슬롯의 장비를 해제하고 오브젝트를 제거합니다.
        /// 해당 슬롯에 장비가 없으면 아무것도 하지 않습니다.
        /// </summary>
        /// <param name="slot">해제할 장비 슬롯</param>
        public void Unequip(EquipmentSlot slot)
        {
            if (!_equippedInstances.TryGetValue(slot, out GameObject existing))
                return;

            // 해제 이벤트는 Destroy 전에 발생 — 구독자가 인스턴스를 참조할 수 있도록
            EquipmentData data = _equippedData.TryGetValue(slot, out EquipmentData d) ? d : null;
            OnUnequipped?.Invoke(slot, data);

            if (existing != null)
            {
                Destroy(existing);
                Debug.Log($"[CharacterEquipmentManager] 슬롯 '{slot}' 장비 제거 완료.");
            }

            _equippedInstances.Remove(slot);
            _equippedData.Remove(slot);
        }

        /// <summary>현재 장착된 모든 장비를 해제하고 오브젝트를 제거합니다.</summary>
        public void UnequipAll()
        {
            // 순회 중 딕셔너리 수정을 피하기 위해 키 목록 복사
            EquipmentSlot[] slots = new EquipmentSlot[_equippedInstances.Count];
            _equippedInstances.Keys.CopyTo(slots, 0);

            foreach (EquipmentSlot slot in slots)
                Unequip(slot);

            Debug.Log("[CharacterEquipmentManager] 모든 장비 해제 완료.");
        }

        /// <summary>
        /// 특정 슬롯에 장비가 장착되어 있는지 확인합니다.
        /// </summary>
        /// <param name="slot">확인할 슬롯</param>
        /// <returns>장착되어 있으면 true</returns>
        public bool IsEquipped(EquipmentSlot slot)
        {
            return _equippedInstances.ContainsKey(slot) && _equippedInstances[slot] != null;
        }

        /// <summary>
        /// 특정 슬롯에 장착된 장비 인스턴스(GameObject)를 반환합니다.
        /// 장착되어 있지 않으면 null 반환.
        /// </summary>
        /// <param name="slot">조회할 슬롯</param>
        /// <returns>장착된 GameObject, 없으면 null</returns>
        public GameObject GetEquipped(EquipmentSlot slot)
        {
            return _equippedInstances.TryGetValue(slot, out GameObject obj) ? obj : null;
        }

        /// <summary>
        /// 특정 슬롯에 장착된 EquipmentData를 반환합니다.
        /// 데미지 계산, UI 능력치 표시 등 메타데이터 접근에 사용합니다.
        /// 장착되어 있지 않으면 null 반환.
        /// </summary>
        /// <param name="slot">조회할 슬롯</param>
        /// <returns>장착된 EquipmentData, 없으면 null</returns>
        public EquipmentData GetEquippedData(EquipmentSlot slot)
        {
            return _equippedData.TryGetValue(slot, out EquipmentData data) ? data : null;
        }
    }
}
