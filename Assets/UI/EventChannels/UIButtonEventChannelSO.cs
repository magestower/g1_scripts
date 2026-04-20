using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ASH VEIL/Events/UI/Button Event Channel", fileName = "UIButtonEventChannel")]
public class UIButtonEventChannelSO : ScriptableObject
{
	// 버튼 클릭 이벤트(가장 많이 사용할 기본 이벤트)

	public event Action<string> OnButtonPressed;

	// 아이템 관련 이벤트 (인벤토리, 장착 등 확장용)
	public event Action<EquipmentSO> OnEquipRequested;

	// 이벤트 발생 메서드 (버튼에서 호출)
	public void RaiseButtonPressed(string buttonId)
	{
		OnButtonPressed?.Invoke(buttonId);
	}

	public void RaiseEquipRequested(EquipmentSO equipment)
	{
		OnEquipRequested?.Invoke(equipment);
	}


	// 구독 해제 안전장치 (메모리 누수 방지 - 모바일 중요!)
	private void OnDisable()
	{
		OnButtonPressed = null;
		OnEquipRequested = null;
	}

	// 에디터에서만 사용 (디버깅용)
#if UNITY_EDITOR
	private void OnEnable()
	{
		// 에디터에서 재컴파일 시 누수 방지
		OnDisable();
	}
#endif
}