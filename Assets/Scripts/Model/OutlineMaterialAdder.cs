using UnityEngine;



namespace G1
{
    /// <summary>
    /// 하위 오브젝트의 모든 SkinnedMeshRenderer에 Gritline_outline 머터리얼을 추가하는 유틸리티 컴포넌트.
    /// 인스펙터의 컨텍스트 메뉴에서 "Add Outline Material" 항목을 클릭하여 실행합니다.
    /// </summary>
    public class OutlineMaterialAdder : MonoBehaviour
    {
        /// <summary>추가할 Gritline_outline 머터리얼 레퍼런스</summary>
        public Material outlineMaterial;

        /// <summary>첫 번째 슬롯을 교체할 대상 머터리얼 레퍼런스</summary>
        public Material replaceMaterial;

        /// <summary>
        /// 자신 및 하위 오브젝트의 모든 SkinnedMeshRenderer를 순회하며
        /// outlineMaterial이 없는 경우 머터리얼 배열 끝에 추가합니다.
        /// </summary>
        [ContextMenu("Add Outline Material")]
        public void AddOutlineMaterial()
        {
            // 머터리얼이 할당되지 않은 경우 중단
            if (outlineMaterial == null)
            {
                Debug.LogWarning("[OutlineMaterialAdder] Outline Material이 할당되지 않았습니다.");
                return;
            }

            // 자신과 모든 하위 오브젝트의 SkinnedMeshRenderer 수집
            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning("[OutlineMaterialAdder] SkinnedMeshRenderer를 찾을 수 없습니다.");
                return;
            }

            int addedCount = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // 이미 outlineMaterial이 포함되어 있는지 확인
                bool alreadyHas = false;
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == outlineMaterial)
                    {
                        alreadyHas = true;
                        break;
                    }
                }

                // 없는 경우에만 추가
                if (!alreadyHas)
                {
                    Material[] newMaterials = new Material[renderer.sharedMaterials.Length + 1];
                    renderer.sharedMaterials.CopyTo(newMaterials, 0);
                    newMaterials[newMaterials.Length - 1] = outlineMaterial;
                    renderer.sharedMaterials = newMaterials;
                    addedCount++;
                    Debug.Log($"[OutlineMaterialAdder] '{renderer.gameObject.name}'에 아웃라인 머터리얼 추가 완료.");
                }
                else
                {
                    Debug.Log($"[OutlineMaterialAdder] '{renderer.gameObject.name}'에 이미 아웃라인 머터리얼이 있습니다. 건너뜀.");
                }
            }

            Debug.Log($"[OutlineMaterialAdder] 총 {addedCount}개의 SkinnedMeshRenderer에 아웃라인 머터리얼을 추가했습니다.");
        }

        /// <summary>
        /// 자신 및 하위 오브젝트의 모든 SkinnedMeshRenderer의
        /// 첫 번째 머터리얼을 replaceMaterial로 교체합니다.
        /// </summary>
        [ContextMenu("Replace First Material")]
        public void ReplaceFirstMaterial()
        {
            // 교체할 머터리얼이 할당되지 않은 경우 중단
            if (replaceMaterial == null)
            {
                Debug.LogWarning("[OutlineMaterialAdder] Replace Material이 할당되지 않았습니다.");
                return;
            }

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                Debug.LogWarning("[OutlineMaterialAdder] SkinnedMeshRenderer를 찾을 수 없습니다.");
                return;
            }

            int replacedCount = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // 머터리얼 슬롯이 하나도 없는 경우 건너뜀
                if (renderer.sharedMaterials.Length == 0)
                {
                    Debug.LogWarning($"[OutlineMaterialAdder] '{renderer.gameObject.name}'에 머터리얼 슬롯이 없습니다. 건너뜀.");
                    continue;
                }

                Material[] mats = renderer.sharedMaterials;
                Material prev = mats[0];
                mats[0] = replaceMaterial;
                renderer.sharedMaterials = mats;
                replacedCount++;
                Debug.Log($"[OutlineMaterialAdder] '{renderer.gameObject.name}' 첫 번째 머터리얼 교체: '{prev?.name}' → '{replaceMaterial.name}'");
            }

            Debug.Log($"[OutlineMaterialAdder] 총 {replacedCount}개의 SkinnedMeshRenderer 첫 번째 머터리얼을 교체했습니다.");
        }

        /// <summary>
        /// 자신 및 하위 오브젝트의 모든 SkinnedMeshRenderer에서
        /// outlineMaterial을 제거합니다.
        /// </summary>
        [ContextMenu("Remove Outline Material")]
        public void RemoveOutlineMaterial()
        {
            // 머터리얼이 할당되지 않은 경우 중단
            if (outlineMaterial == null)
            {
                Debug.LogWarning("[OutlineMaterialAdder] Outline Material이 할당되지 않았습니다.");
                return;
            }

            SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            int removedCount = 0;

            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                // outlineMaterial을 제외한 배열 재구성
                System.Collections.Generic.List<Material> filtered = new System.Collections.Generic.List<Material>();
                bool found = false;

                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == outlineMaterial)
                    {
                        found = true; // 제거 대상, 리스트에 추가 안 함
                    }
                    else
                    {
                        filtered.Add(mat);
                    }
                }

                if (found)
                {
                    renderer.sharedMaterials = filtered.ToArray();
                    removedCount++;
                    Debug.Log($"[OutlineMaterialAdder] '{renderer.gameObject.name}'에서 아웃라인 머터리얼 제거 완료.");
                }
            }

            Debug.Log($"[OutlineMaterialAdder] 총 {removedCount}개의 SkinnedMeshRenderer에서 아웃라인 머터리얼을 제거했습니다.");
        }
    }
}
