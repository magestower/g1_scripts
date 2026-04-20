using UnityEngine;

namespace G1
{
    /// <summary>
    /// 무기/보조장비 한 개의 데이터를 정의하는 ScriptableObject.
    /// Project 창 우클릭 → Create → G1 → Equipment Data 로 생성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentData_New", menuName = "G1/Equipment Data")]
    public class EquipmentData : ScriptableObject
    {
        [Header("기본 정보")]

        /// <summary>인게임 표시 이름</summary>
        public string equipmentName;

        /// <summary>
        /// 무기 종류 — PlayerController의 weaponAttackData 배열 인덱스와 대응됩니다.
        /// Unarmed는 장비 에셋에서 사용하지 않으며, 무기 미장착 시 자동으로 적용됩니다.
        /// </summary>
        public WeaponType weaponType;

        /// <summary>UI 선택 화면에 표시할 썸네일 이미지</summary>
        public Sprite thumbnail;

        [Header("장착 설정")]

        /// <summary>장착할 장비 프리팹</summary>
        public GameObject prefab;

        /// <summary>장착 슬롯 — Weapon: 오른손, SubEquipment: 왼손</summary>
        public EquipmentSlot slot;

        [Header("손 위치 보정 (Local)")]

        /// <summary>
        /// 손 본 기준 위치 오프셋.
        /// DCC 툴 좌표계 차이로 인한 위치 틀어짐을 프리팹 수정 없이 보정합니다.
        /// </summary>
        public Vector3 positionOffset;

        /// <summary>
        /// 손 본 기준 회전 오프셋 (오일러각).
        /// DCC 툴 좌표계 차이로 인한 각도 틀어짐을 프리팹 수정 없이 보정합니다.
        /// </summary>
        public Vector3 rotationOffset;

        /// <summary>
        /// 스케일 덮어쓰기. 기본값 (1, 1, 1).
        /// </summary>
        public Vector3 scaleOverride = Vector3.one;
    }
}
