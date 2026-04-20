using UnityEngine;

namespace G1
{
    /// <summary>
    /// 의상 한 벌에 대한 데이터를 정의하는 ScriptableObject.
    /// Project 창 우클릭 → Create → G1 → Outfit Data 로 생성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "OutfitData_New", menuName = "G1/Outfit Data")]
    public class OutfitData : ScriptableObject
    {
        [Header("기본 정보")]

        /// <summary>인게임 표시 이름</summary>
        public string outfitName;

        /// <summary>UI 선택 화면에 표시할 썸네일 이미지</summary>
        public Sprite thumbnail;

        [Header("장착 설정")]

        /// <summary>장착할 의상 프리팹 (OutfitController 컴포넌트 필수 포함)</summary>
        public GameObject prefab;

        /// <summary>이 의상이 장착될 신체 슬롯</summary>
        public OutfitSlot slot;

        [Header("머터리얼 설정")]

        /// <summary>
        /// SkinnedMeshRenderer의 첫 번째 머터리얼을 교체할 머터리얼.
        /// null 이면 교체하지 않고 원본 유지.
        /// </summary>
        public Material bodyMaterial;

        [Header("신체 부위 숨김")]

        /// <summary>
        /// 이 의상을 장착했을 때 숨길 신체 부위 (Flags).
        /// 예) 상의 장착 시 Torso 체크 → 해당 부위 오브젝트 비활성화.
        /// </summary>
        public BodyPartFlags hideBodyParts;
    }
}
