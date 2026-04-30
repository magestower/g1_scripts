using System;
using UnityEngine;
using G1;

[CreateAssetMenu(menuName = "ASH VEIL/Events/UI/Button Event Channel", fileName = "UIButtonEventChannel")]
public class UIButtonEventChannelSO : ScriptableObject
{
	// 버튼 클릭 이벤트(문자열 기반 범용 기본 이벤트)
	public event Action<string> OnButtonPressed;

	// 장비 장착 요청 이벤트 (인벤토리, 상점 등 확장)
	public event Action<EquipmentSO> OnEquipRequested;

	/// <summary>버튼 클릭 이벤트를 발생시킵니다.</summary>
	/// <param name="buttonId">버튼 식별자</param>
	public void RaiseButtonPressed(string buttonId)
	{
		OnButtonPressed?.Invoke(buttonId);
	}

	/// <summary>장비 장착 요청 이벤트를 발생시킵니다.</summary>
	/// <param name="equipment">장착할 장비 데이터</param>
	public void RaiseEquipRequested(EquipmentSO equipment)
	{
		OnEquipRequested?.Invoke(equipment);
	}

	// 이벤트 누수 방지 (메모리 해제 용도 - 항상 중요!)
	private void OnDisable()
	{
		OnButtonPressed = null;
		OnEquipRequested = null;
	}

#if UNITY_EDITOR
	// 에디터에서의 초기화 (선택적)
	private void OnEnable()
	{
		// 에디터에서 재컴파일 후 이벤트 초기화
		OnDisable();
	}
#endif
}
