using UnityEngine;

namespace G1
{
    /// <summary>
    /// 장비 아이템 데이터를 정의하는 ScriptableObject.
    /// Project 창 우클릭 → Create → ASH VEIL/Equipment/Equipment Data 로 생성합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "ASH VEIL/Equipment/Equipment Data", fileName = "NewEquipment")]
    public class EquipmentSO : ScriptableObject
    {
        [Header("기본 정보")]
        /// <summary>인게임 표시 이름</summary>
        public string itemName = "새 장비";

        /// <summary>아이템 설명 텍스트</summary>
        public string description;

        /// <summary>장착 슬롯 (주무기, 보조장비 등)</summary>
        public EquipmentSlot slot;

        [Header("능력치 보너스 - 추후에 데이터 테이블화")]
        /// <summary>근력 보너스</summary>
        public int strengthBonus;

        /// <summary>방어력 보너스</summary>
        public int defenseBonus;

        /// <summary>스태미나 보너스</summary>
        public int staminaBonus;

        /// <summary>최대 HP 추가량</summary>
        public int hpBonus;

        [Header("장착 시 바디 처리")]
        /// <summary>장착 시 인스턴스화할 Prefab (SkinnedMeshRenderer 포함)</summary>
        public GameObject clothingPrefab;

        /// <summary>장착 시 숨길 신체 부위 플래그</summary>
        public BodyPartFlags hideBodyParts;

        [Header("퍼포먼스 최적화 참고")]
        /// <summary>디자이너에서 확인용 (1500 tri 이하 권장)</summary>
        public int triangleCount;

        /// <summary>인벤토리 아이콘 이미지 (1024px 이하 ASTC 권장)</summary>
        public Texture2D icon;

        [Header("메타데이터 및 상점")]
        /// <summary>아이템 희귀도 등급</summary>
        public Rarity rarity = Rarity.Common;

        /// <summary>고대 주화 판매/구매 가격 (밸런스 미화)</summary>
        public int ancientCoinPrice;

        /// <summary>런타임에서 관리하는 캐시 데이터 (메모리 절약)</summary>
        [System.NonSerialized]
        public int currentDurability = 100;
    }
}
