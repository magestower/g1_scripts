using UnityEngine;
using UnityEngine.UI;


namespace G1
{
    [RequireComponent(typeof(Button))]
    public class UIButtonHandler : MonoBehaviour
    {
    	[SerializeField] private UIButtonEventChannelSO buttonEventChannel;
    	[SerializeField] private string buttonId = "DefaultButton";   // "OpenInventory", "Skill_Heal" 등

    	private Button button;

    	private void Awake()
    	{
    		button = GetComponent<Button>();
    		button.onClick.AddListener(OnButtonClicked);
    	}

    	private void OnButtonClicked()
    	{
    		if (buttonEventChannel != null)
    		{
    			buttonEventChannel.RaiseButtonPressed(buttonId);

    			// 모바일 클릭 피드백 (가볍게)
    			// LeanTween이나 DOTween으로 Scale 애니메이션 추가 가능
    		}
    		else
    		{
    			Debug.LogError("UIButtonEventChannelSO가 할당되지 않았습니다!");
    		}
    	}

    	private void OnDestroy()
    	{
    		if (button != null)
    			button.onClick.RemoveListener(OnButtonClicked);
    	}
    }
}
