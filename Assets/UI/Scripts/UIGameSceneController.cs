using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace G1
{
    /// <summary>
    /// 게임 씬 전용 UI 컨트롤러.
    /// 씬에 종속된 UI 패널을 참조하므로 DontDestroyOnLoad를 사용하지 않습니다.
    /// </summary>
    public class UIGameSceneController : MonoBehaviour
    {
    	public static UIGameSceneController Instance { get; private set; }

    	[Header("이벤트 채널")]
    	[SerializeField] private UIButtonEventChannelSO buttonEventChannel;

    	[Header("UI Panels - 불필요한 Overdraw 최소화")]
    	[SerializeField] private GameObject hudCanvas;
    	[SerializeField] private GameObject inventoryPanel;
    	[SerializeField] private GameObject statusPanel;
    	[SerializeField] private GameObject skillPanel;   // 나중에 확장

    	[Header("플레이어")]
    	[SerializeField] private PlayerController playerController; // 공격 등 플레이어 액션 호출 대상

    	[Header("의상 테스트")]
    	[SerializeField] private CharacterCostumeManager costumeManager;  // 의상 관리 대상 캐릭터
    	[SerializeField] private List<OutfitData> testOutfits = new();    // 테스트용 의상 데이터 목록 (여러 개 할당 가능)

    	[Header("장비 테스트")]
    	[SerializeField] private CharacterEquipmentManager equipmentManager; // 장비 관리 대상 캐릭터
    	[SerializeField] private EquipmentData testWeapon;                   // 테스트용 무기 데이터 (fryingpan)
    	[SerializeField] private EquipmentData testSubEquipment;             // 테스트용 보조장비 데이터 (pot_lid_shield)

    	/// <summary>X 버튼 토글 상태 — true: 장착됨(다음 누를 때 해제), false: 해제됨(다음 누를 때 장착)</summary>
    	private bool _outfitEquipped = false;

    	/// <summary>Y 버튼 토글 상태 — true: 장착됨(다음 누를 때 해제), false: 해제됨(다음 누를 때 장착)</summary>
    	private bool _equipmentEquipped = false;

    	private void Awake()
    	{
    		// 중복 인스턴스 방지 — 씬 종속 참조가 있으므로 DontDestroyOnLoad 사용 안 함
    		if (Instance != null)
    		{
    			Destroy(gameObject);
    			return;
    		}
    		Instance = this;

    		// 인스펙터 미할당 시 씬에서 자동 탐색
    		if (playerController == null)
    			playerController = FindAnyObjectByType<PlayerController>();
    		if (costumeManager == null)
    			costumeManager = FindAnyObjectByType<CharacterCostumeManager>();
    		if (equipmentManager == null)
    			equipmentManager = FindAnyObjectByType<CharacterEquipmentManager>();
    	}

    	private void OnEnable()
    	{
    		// 중복 오브젝트(Destroy 예정)가 이벤트를 구독하지 않도록 방어
    		if (Instance != this) return;

    		if (buttonEventChannel != null)
    		{
    			buttonEventChannel.OnButtonPressed += HandleButtonPressed;
    			buttonEventChannel.OnEquipRequested += HandleEquipRequested;
    		}
    	}

    	private void OnDisable()
    	{
    		if (buttonEventChannel != null)
    		{
    			buttonEventChannel.OnButtonPressed -= HandleButtonPressed;
    			buttonEventChannel.OnEquipRequested -= HandleEquipRequested;
    		}
    	}

    	private void OnDestroy()
    	{
    		// 이 인스턴스가 활성 싱글톤이었을 때만 해제
    		if (Instance == this)
    			Instance = null;
    	}

    	/// <summary>
    	/// Update: Windows 에디터에서 1 키를 눌렀을 때 공격을 실행합니다.
    	/// 모바일에서는 UI 버튼을 통해 HandleButtonPressed가 호출됩니다.
    	/// </summary>
    	private void Update()
    	{
#if UNITY_EDITOR_WIN
    		if (playerController == null) return;

    		// 숫자 키 1 → 공격 슬롯 0 실행
    		if (Keyboard.current.digit1Key.wasPressedThisFrame) playerController.OnAttackStart();
#endif
    	}

    	/// <summary>
    	/// 버튼 ID에 따라 UI 패널을 제어합니다.
    	/// </summary>
    	/// <param name="buttonId">버튼 식별 문자열</param>
    	private void HandleButtonPressed(string buttonId)
    	{
    		// 대소문자 혼용 방지 — 전달된 ID를 대문자로 정규화
    		switch (buttonId.ToUpperInvariant())
    		{
    			// 스킬 버튼 A, B, X, Y — 나중에 SkillManager로 위임
    			case "A":
    				// 슬롯 0 공격 실행 (인스펙터 attackSlots[0]에 할당된 AttackData 사용)
    				if (playerController != null)
    					playerController.OnAttackStart();
    				else
    					Debug.LogWarning("[UIGameSceneController] PlayerController를 찾을 수 없습니다.");
    				break;

    			case "B":
    				Debug.Log("B 버튼");
    				break;

    			case "X":
    				Debug.Log("X 버튼");
    				// 토글 방식 — 현재 장착 상태에 따라 장착/전체 해제를 번갈아 호출
    				if (_outfitEquipped)
    					TestUnequipAllOutfits();
    				else
    					TestEquipOutfit();
    				_outfitEquipped = !_outfitEquipped;
    				break;

    			case "Y":
    				Debug.Log("Y 버튼");
    				// 토글 방식 — 현재 장착 상태에 따라 fryingpan+pot_lid_shield 장착/전체 해제를 번갈아 호출
    				if (_equipmentEquipped)
    					TestUnequipAllEquipment();
    				else
    					TestEquipEquipment();
    				_equipmentEquipped = !_equipmentEquipped;
    				break;

    			case "OPENINVENTORY":
    				// 인벤토리 열 때 다른 패널 닫기 (Overdraw 절감)
    				TogglePanel(inventoryPanel, true);
    				TogglePanel(statusPanel, false);
    				break;

    			case "OPENSTATUS":
    				TogglePanel(statusPanel, true);
    				break;

    			case "CLOSEALLPANELS":
    				TogglePanel(inventoryPanel, false);
    				TogglePanel(statusPanel, false);
    				break;

    			case "SKILL_HEAL":
    				Debug.Log("치유 스킬 사용 요청 - 아직 실제 효과 없음");
    				break;

    			default:
    				Debug.LogWarning($"알 수 없는 버튼 ID: {buttonId}");
    				break;
    		}
    	}

    	/// <summary>
    	/// 장비 장착 요청을 처리합니다. equipment가 null이면 무시합니다.
    	/// </summary>
    	/// <param name="equipment">장착할 장비 데이터</param>
    	private void HandleEquipRequested(EquipmentSO equipment)
    	{
    		if (equipment == null)
    		{
    			Debug.LogWarning("[UIGameSceneController] HandleEquipRequested: equipment가 null입니다.");
    			return;
    		}

    		Debug.Log($"{equipment.itemName} 장착 요청");
    		// FindObjectOfType<CharacterEquipmentManager>().Equip(equipment);
    	}

    	/// <summary>
    	/// 테스트: testOutfits 목록의 모든 의상을 순서대로 캐릭터에 장착합니다.
    	/// 인스펙터에서 costumeManager와 testOutfits를 할당한 뒤 호출하세요.
    	/// </summary>
    	public void TestEquipOutfit()
    	{
    		if (costumeManager == null)
    		{
    			Debug.LogWarning("[UIGameSceneController] costumeManager가 할당되지 않았습니다.");
    			return;
    		}

    		if (testOutfits == null || testOutfits.Count == 0)
    		{
    			Debug.LogWarning("[UIGameSceneController] testOutfits가 비어 있습니다.");
    			return;
    		}

    		// 목록의 의상을 순서대로 장착 — null 항목은 건너뜀
    		foreach (var outfit in testOutfits)
    		{
    			if (outfit != null)
    				costumeManager.Equip(outfit);
    		}
    	}

    	/// <summary>
    	/// 테스트: 캐릭터의 모든 의상을 해제합니다.
    	/// </summary>
    	public void TestUnequipAllOutfits()
    	{
    		if (costumeManager == null)
    		{
    			Debug.LogWarning("[UIGameSceneController] costumeManager가 할당되지 않았습니다.");
    			return;
    		}

    		costumeManager.UnequipAll();
    	}

    	/// <summary>
    	/// 테스트: testWeapon과 testSubEquipment를 캐릭터에 장착합니다.
    	/// 인스펙터에서 equipmentManager, testWeapon, testSubEquipment를 할당한 뒤 호출하세요.
    	/// </summary>
    	public void TestEquipEquipment()
    	{
    		if (equipmentManager == null)
    		{
    			Debug.LogWarning("[UIGameSceneController] equipmentManager가 할당되지 않았습니다.");
    			return;
    		}

    		// 무기와 보조장비를 각각 장착 — 둘 중 하나만 할당되어 있어도 장착 진행
    		if (testWeapon != null)
    			equipmentManager.Equip(testWeapon);
    		else
    			Debug.LogWarning("[UIGameSceneController] testWeapon이 할당되지 않았습니다.");

    		if (testSubEquipment != null)
    			equipmentManager.Equip(testSubEquipment);
    		else
    			Debug.LogWarning("[UIGameSceneController] testSubEquipment가 할당되지 않았습니다.");
    	}

    	/// <summary>
    	/// 테스트: 캐릭터의 모든 장비를 해제합니다.
    	/// </summary>
    	public void TestUnequipAllEquipment()
    	{
    		if (equipmentManager == null)
    		{
    			Debug.LogWarning("[UIGameSceneController] equipmentManager가 할당되지 않았습니다.");
    			return;
    		}

    		equipmentManager.UnequipAll();
    	}

    	/// <summary>
    	/// 패널 활성화/비활성화 시 GraphicRaycaster도 함께 토글합니다.
    	/// 비활성 패널의 Raycaster를 끄면 불필요한 레이캐스트 연산을 방지합니다.
    	/// </summary>
    	/// <param name="panel">대상 패널 오브젝트</param>
    	/// <param name="active">활성화 여부</param>
    	public void TogglePanel(GameObject panel, bool active)
    	{
    		if (panel == null) return;

    		panel.SetActive(active);

    		// 비활성 패널의 Raycaster를 끄면 불필요한 레이캐스트 연산 방지
    		var raycaster = panel.GetComponent<UnityEngine.UI.GraphicRaycaster>();
    		if (raycaster != null) raycaster.enabled = active;
    	}
    }
}
