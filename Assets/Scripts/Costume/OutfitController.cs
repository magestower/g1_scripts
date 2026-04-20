using UnityEngine;

namespace G1
{
    /// <summary>
    /// 의상 프리팹 루트에 부착하는 컴포넌트.
    /// CharacterCostumeManager가 Initialize()를 호출하면
    /// 본 리매핑, 머터리얼 교체, 아웃라인 추가를 자동으로 처리합니다.
    /// </summary>
    public class OutfitController : MonoBehaviour
    {
        /// <summary>
        /// 의상 초기화 진입점.
        /// CharacterCostumeManager.Equip() 에서 인스턴스 생성 직후 호출합니다.
        /// </summary>
        /// <param name="referenceRenderer">캐릭터 기준 SkinnedMeshRenderer (뼈대 원본)</param>
        /// <param name="outlineMaterial">추가할 아웃라인 머터리얼 (null 이면 추가 안 함)</param>
        /// <param name="bodyMaterial">첫 번째 슬롯을 교체할 머터리얼 (null 이면 유지)</param>
        public void Initialize(
            SkinnedMeshRenderer referenceRenderer,
            Material outlineMaterial,
            Material bodyMaterial = null)
        {
            if (referenceRenderer == null)
            {
                Debug.LogWarning($"[OutfitController] '{gameObject.name}': referenceRenderer가 null입니다.");
                return;
            }

            // 자신과 하위의 모든 SkinnedMeshRenderer에 대해 순서대로 처리
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning($"[OutfitController] '{gameObject.name}': SkinnedMeshRenderer를 찾을 수 없습니다.");
                return;
            }

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // 1단계: 본 리매핑 — 의상 뼈대를 캐릭터 뼈대로 교체
                RemapBones(renderer, referenceRenderer);

                // 2단계: 첫 번째 머터리얼 교체 (bodyMaterial이 지정된 경우에만)
                if (bodyMaterial != null)
                    ReplaceFirstMaterial(renderer, bodyMaterial);

                // 3단계: 아웃라인 머터리얼 추가 (outlineMaterial이 지정된 경우에만)
                if (outlineMaterial != null)
                    AddOutlineMaterial(renderer, outlineMaterial);
            }

            Debug.Log($"[OutfitController] '{gameObject.name}' 초기화 완료 (렌더러 {renderers.Length}개 처리).");
        }

        /// <summary>
        /// 의상 SkinnedMeshRenderer의 bones와 rootBone을
        /// 캐릭터 기준 SkinnedMeshRenderer와 동일하게 설정합니다.
        /// </summary>
        /// <param name="outfitRenderer">의상의 SkinnedMeshRenderer</param>
        /// <param name="referenceRenderer">캐릭터 기준 SkinnedMeshRenderer</param>
        private void RemapBones(SkinnedMeshRenderer outfitRenderer, SkinnedMeshRenderer referenceRenderer)
        {
            outfitRenderer.bones    = referenceRenderer.bones;
            outfitRenderer.rootBone = referenceRenderer.rootBone;
            Debug.Log($"[OutfitController] '{outfitRenderer.gameObject.name}' 본 리매핑 완료.");
        }

        /// <summary>
        /// SkinnedMeshRenderer의 첫 번째 머터리얼 슬롯을 지정한 머터리얼로 교체합니다.
        /// </summary>
        /// <param name="renderer">대상 SkinnedMeshRenderer</param>
        /// <param name="newMaterial">교체할 머터리얼</param>
        private void ReplaceFirstMaterial(SkinnedMeshRenderer renderer, Material newMaterial)
        {
            if (renderer.sharedMaterials.Length == 0)
            {
                Debug.LogWarning($"[OutfitController] '{renderer.gameObject.name}': 머터리얼 슬롯이 없습니다.");
                return;
            }

            Material[] mats = renderer.sharedMaterials;
            Material prev   = mats[0];
            mats[0]         = newMaterial;
            renderer.sharedMaterials = mats;

            Debug.Log($"[OutfitController] '{renderer.gameObject.name}' 첫 번째 머터리얼 교체: '{prev?.name}' → '{newMaterial.name}'");
        }

        /// <summary>
        /// SkinnedMeshRenderer에 아웃라인 머터리얼이 없으면 배열 끝에 추가합니다.
        /// </summary>
        /// <param name="renderer">대상 SkinnedMeshRenderer</param>
        /// <param name="outlineMaterial">추가할 아웃라인 머터리얼</param>
        private void AddOutlineMaterial(SkinnedMeshRenderer renderer, Material outlineMaterial)
        {
            // 이미 포함되어 있으면 건너뜀
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat == outlineMaterial)
                {
                    Debug.Log($"[OutfitController] '{renderer.gameObject.name}': 아웃라인 머터리얼 이미 존재. 건너뜀.");
                    return;
                }
            }

            // 배열 끝에 아웃라인 머터리얼 추가
            Material[] newMats = new Material[renderer.sharedMaterials.Length + 1];
            renderer.sharedMaterials.CopyTo(newMats, 0);
            newMats[newMats.Length - 1] = outlineMaterial;
            renderer.sharedMaterials    = newMats;

            Debug.Log($"[OutfitController] '{renderer.gameObject.name}' 아웃라인 머터리얼 추가 완료.");
        }
    }
}
