using UnityEngine;


namespace G1
{
    public class BoneRemapper : MonoBehaviour
    {
    	public GameObject targetRig; // 바디(메인 리그) 오브젝트

    	// 바디의 본 구조에 맞게 의상 모델의 본을 재매핑하는 함수
    	// ContextMenu 속성으로 에디터에서 우클릭 메뉴로 실행할 수 있도록 설정
    	// 실행 시 의상 모델의 SkinnedMeshRenderer의 bones와 rootBone을 바디 모델의 SkinnedMeshRenderer와 동일하게 설정
    	// 이렇게 하면 의상 모델이 바디 모델의 본 구조를 따라 움직이게 됩니다.
    	// Remap() 실행 후 root 제거.
    	[ContextMenu("Remap Bones")]
    	public void Remap()
    	{
    		SkinnedMeshRenderer clothingRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    		SkinnedMeshRenderer targetRenderer = targetRig.GetComponentInChildren<SkinnedMeshRenderer>();

    		if (clothingRenderer != null && targetRenderer != null)
    		{
    			clothingRenderer.bones = targetRenderer.bones;
    			clothingRenderer.rootBone = targetRenderer.rootBone;
    			Debug.Log("본 매핑 완료!");
    		}
    	}
    }
}
