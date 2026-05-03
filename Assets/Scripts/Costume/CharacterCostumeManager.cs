using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// 캐릭터의 의상 장착/해제를 런타임에서 관리하는 핵심 컴포넌트.
    /// 슬롯(OutfitSlot) 단위로 의상을 관리하며, 같은 슬롯에 새 의상 장착 시 기존 의상을 자동 제거합니다.
    /// maru_03 등 캐릭터 루트 오브젝트에 부착합니다.
    /// </summary>
    public class CharacterCostumeManager : MonoBehaviour
    {
        [Header("캐릭터 설정")]

        /// <summary>모든 의상에 자동으로 추가할 아웃라인 머터리얼 (Gritline_outline)</summary>
        public Material outlineMaterial;

        /// <summary>
        /// 본 리매핑 기준 SkinnedMeshRenderer — 게임 시작 시 referenceMeshName으로 자동 탐색합니다.
        /// </summary>
        [HideInInspector]
        private SkinnedMeshRenderer referenceRenderer;

        [Header("기준 메시 이름 (코드 기본값, 인스펙터에서 수정 가능)")]
        [SerializeField] private string referenceMeshName = "maru"; // 자동 탐색할 body 메시 오브젝트 이름

        /// <summary>기본 머리 메시 — 장비 장착 시 숨겨집니다.</summary>
        [Header("신체 부위 오브젝트 (인스펙터에서 직접 할당 권장)")]
        [SerializeField] private GameObject baseMesh_Head;
        [SerializeField] private GameObject baseMesh_Torso;
        [SerializeField] private GameObject baseMesh_Legs;
        [SerializeField] private GameObject baseMesh_Foots;
        [SerializeField] private GameObject baseMesh_Hip;
        [SerializeField] private GameObject baseMesh_Hands;

        [Header("자동 검색 폴백 (이름 검색) - 디버깅용")]
        [SerializeField] private string fallbackTorsoName = "base_torso";
        [SerializeField] private string fallbackHeadName = "base_head";
        [SerializeField] private string fallbackLegsName = "base_legs";
        [SerializeField] private string fallbackFootsName = "base_foots";
        [SerializeField] private string fallbackHipName = "base_hip";
        [SerializeField] private string fallbackHandsName = "base_hands";

        /// <summary>슬롯별로 현재 장착된 의상 인스턴스를 보관하는 딕셔너리</summary>
        private readonly Dictionary<OutfitSlot, GameObject> _equippedOutfits
            = new Dictionary<OutfitSlot, GameObject>();

        /// <summary>슬롯별로 현재 장착된 OutfitData를 보관 — 신체 부위 숨김 계산에 사용</summary>
        private readonly Dictionary<OutfitSlot, OutfitData> _equippedData
            = new Dictionary<OutfitSlot, OutfitData>();


        private void Awake()
        {
            AutoFindBodyParts();
        }

		/// <summary>
		/// 시작 시점에 신체 부위 GameObject가 인스펙터에서 할당되지 않았을 때, 자동으로 이름으로 검색하여 할당을 시도합니다.
		/// </summary>
		private void AutoFindBodyParts()
        {
            // 0. referenceRenderer — referenceMeshName 오브젝트에서 SkinnedMeshRenderer 자동 탐색
            if (referenceRenderer == null)
            {
                Transform found = transform.FindDeep(referenceMeshName);
                if (found != null)
                    referenceRenderer = found.GetComponent<SkinnedMeshRenderer>();
            }
            if (referenceRenderer == null)
                Debug.LogError($"[CharacterCostumeManager] referenceRenderer를 찾을 수 없습니다! '{referenceMeshName}' 오브젝트에 SkinnedMeshRenderer가 있는지 확인해주세요.");

            // 1. 인스펙터 할당 확인 후 없으면 이름으로 자동 검색
            if (baseMesh_Torso == null)
                baseMesh_Torso = transform.FindDeep(fallbackTorsoName)?.gameObject;
            if (baseMesh_Head == null)
                baseMesh_Head = transform.FindDeep(fallbackHeadName)?.gameObject;
            if (baseMesh_Legs == null)
                baseMesh_Legs = transform.FindDeep(fallbackLegsName)?.gameObject;
            if (baseMesh_Foots == null)
                baseMesh_Foots = transform.FindDeep(fallbackFootsName)?.gameObject;
            if (baseMesh_Hip == null)
                baseMesh_Hip = transform.FindDeep(fallbackHipName)?.gameObject;
            if (baseMesh_Hands == null)
                baseMesh_Hands = transform.FindDeep(fallbackHandsName)?.gameObject;

            // 2. 그래도 null이면 강력 경고
            if (baseMesh_Torso == null)
                Debug.LogError($"[CharacterCostumeManager] baseMesh_Torso를 찾을 수 없습니다! 인스펙터에서 직접 할당해주세요.");
            if (baseMesh_Head == null)
                Debug.LogError($"[CharacterCostumeManager] baseMesh_Head를 찾을 수 없습니다! 인스펙터에서 직접 할당해주세요.");
            if (baseMesh_Legs == null)
                Debug.LogError($"[CharacterCostumeManager] baseMesh_Legs를 찾을 수 없습니다! 인스펙터에서 직접 할당해주세요.");
            if (baseMesh_Foots == null)
                Debug.LogError($"[CharacterCostumeManager] baseMesh_Foots를 찾을 수 없습니다! 인스펙터에서 직접 할당해주세요.");
        }

        // ──────────────────────────────────────────────────────────────
        //  공개 API
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 지정한 OutfitData를 해당 슬롯에 장착합니다.
        /// 같은 슬롯에 기존 의상이 있으면 자동으로 제거 후 교체합니다.
        /// </summary>
        /// <param name="data">장착할 의상 데이터 (OutfitData ScriptableObject)</param>
        public void Equip(OutfitData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[CharacterCostumeManager] OutfitData가 null입니다.");
                return;
            }

            if (data.prefab == null)
            {
                Debug.LogWarning($"[CharacterCostumeManager] '{data.outfitName}' 의상의 prefab이 할당되지 않았습니다.");
                return;
            }

            if (referenceRenderer == null)
            {
                Debug.LogWarning("[CharacterCostumeManager] referenceRenderer가 할당되지 않았습니다.");
                return;
            }

            // 같은 슬롯에 기존 의상이 있으면 제거
            Unequip(data.slot);

            // 의상 프리팹 인스턴스 생성 (캐릭터 자식으로)
            GameObject instance = Instantiate(data.prefab, transform);
            instance.name = $"Outfit_{data.slot}_{data.outfitName}";

            // OutfitController를 통해 본 리매핑 + 머터리얼 초기화
            OutfitController controller = instance.GetComponent<OutfitController>();
            if (controller == null)
            {
                // OutfitController가 없으면 자동 추가
                controller = instance.AddComponent<OutfitController>();
                Debug.LogWarning($"[CharacterCostumeManager] '{data.outfitName}' 프리팹에 OutfitController가 없어 자동 추가했습니다.");
            }

            controller.Initialize(referenceRenderer, outlineMaterial, data.bodyMaterial);

            // 슬롯 등록
            _equippedOutfits[data.slot] = instance;
            _equippedData[data.slot] = data;

            // 신체 부위 가시성 갱신
            ApplyBodyPartVisibility();

            Debug.Log($"[CharacterCostumeManager] '{data.outfitName}' 장착 완료 (슬롯: {data.slot}).");
        }

        /// <summary>
        /// 지정한 슬롯의 의상을 해제하고 오브젝트를 제거합니다.
        /// 해당 슬롯에 의상이 없으면 아무것도 하지 않습니다.
        /// </summary>
        /// <param name="slot">해제할 의상 슬롯</param>
        public void Unequip(OutfitSlot slot)
        {
            if (!_equippedOutfits.TryGetValue(slot, out GameObject existing))
                return;

            if (existing != null)
            {
                Destroy(existing);
                Debug.Log($"[CharacterCostumeManager] 슬롯 '{slot}' 의상 제거 완료.");
            }

            _equippedOutfits.Remove(slot);
            _equippedData.Remove(slot);

            // 신체 부위 가시성 갱신
            ApplyBodyPartVisibility();
        }

        /// <summary>
        /// 현재 장착된 모든 의상을 해제하고 오브젝트를 제거합니다.
        /// </summary>
        public void UnequipAll()
        {
            // 순회 중 딕셔너리 수정을 피하기 위해 키 목록 복사
            OutfitSlot[] slots = new OutfitSlot[_equippedOutfits.Count];
            _equippedOutfits.Keys.CopyTo(slots, 0);

            foreach (OutfitSlot slot in slots)
                Unequip(slot);

            _equippedData.Clear();
            Debug.Log("[CharacterCostumeManager] 모든 의상 해제 완료.");
        }

        /// <summary>
        /// 특정 슬롯에 현재 의상이 장착되어 있는지 확인합니다.
        /// </summary>
        /// <param name="slot">확인할 슬롯</param>
        /// <returns>장착되어 있으면 true</returns>
        public bool IsEquipped(OutfitSlot slot)
        {
            return _equippedOutfits.ContainsKey(slot) && _equippedOutfits[slot] != null;
        }

        /// <summary>
        /// 특정 슬롯에 장착된 의상 인스턴스를 반환합니다.
        /// 장착되어 있지 않으면 null 반환.
        /// </summary>
        /// <param name="slot">조회할 슬롯</param>
        /// <returns>장착된 GameObject, 없으면 null</returns>
        public GameObject GetEquipped(OutfitSlot slot)
        {
            return _equippedOutfits.TryGetValue(slot, out GameObject obj) ? obj : null;
        }

        // ──────────────────────────────────────────────────────────────
        //  내부 유틸리티
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 장착된 모든 의상의 hideBodyParts 플래그를 OR 합산하여
        /// 각 신체 부위 GameObject 의 활성화 여부를 갱신합니다.
        /// 어떤 의상도 해당 부위를 가리지 않으면 다시 활성화됩니다.
        /// </summary>
        private void ApplyBodyPartVisibility()
        {
            // 장착된 의상 전체의 숨김 플래그 합산
            BodyPartFlags hidden = BodyPartFlags.None;
            foreach (OutfitData data in _equippedData.Values)
            {
                if (data != null)
                    hidden |= data.hideBodyParts;
            }

            // 플래그에 따라 각 신체 부위 ON/OFF
            if (baseMesh_Head != null)  baseMesh_Head.SetActive(!hidden.HasFlag(BodyPartFlags.Head));
            if (baseMesh_Torso != null) baseMesh_Torso.SetActive(!hidden.HasFlag(BodyPartFlags.Torso));
            if (baseMesh_Legs != null)  baseMesh_Legs.SetActive(!hidden.HasFlag(BodyPartFlags.Legs));
            if (baseMesh_Foots != null) baseMesh_Foots.SetActive(!hidden.HasFlag(BodyPartFlags.Shoes));
            if (baseMesh_Hip != null)   baseMesh_Hip.SetActive(!hidden.HasFlag(BodyPartFlags.Hip));
            if (baseMesh_Hands != null) baseMesh_Hands.SetActive(!hidden.HasFlag(BodyPartFlags.Hands));
        }		

        // ──────────────────────────────────────────────────────────────
        //  Unity 생명주기
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 오브젝트 파괴 시 장착된 의상 인스턴스 정리.
        /// </summary>
        private void OnDestroy()
        {
            UnequipAll();
        }
    }
}
