using UnityEngine;


namespace G1
{
    public class BlobShadowFollower : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("마루 캐릭터의 Transform")]
        public Transform characterTransform;

        [Tooltip("그림자가 붙을 바닥 레이어 (Ground 레이어로 설정하세요)")]
        public LayerMask groundLayer = ~0;   // 기본값: 모든 레이어

        [Tooltip("바닥에서 살짝 띄우는 높이 (Z-Fighting 방지)")]
        public float heightOffset = 0.02f;

        [Tooltip("최대 Raycast 거리")]
        public float maxDistance = 10f;

        [Tooltip("그림자 투명도 (0~1)")]
        [Range(0f, 1f)]
        public float shadowAlpha = 0.6f;

        private Renderer shadowRenderer;
        private MaterialPropertyBlock propBlock;   // Draw Call 최적화용

        void Start()
        {
            shadowRenderer = GetComponentInChildren<Renderer>();

            if (shadowRenderer == null)
            {
                Debug.LogWarning("[BlobShadowFollower] 자식 Renderer를 찾을 수 없습니다.", this);
                return;
            }

            propBlock = new MaterialPropertyBlock();
            shadowRenderer.GetPropertyBlock(propBlock);

            if (characterTransform == null)
                characterTransform = transform.parent;   // 자동으로 부모(마루) 찾기
        }

        void LateUpdate()   // 카메라 이동 후에 실행되도록 LateUpdate 사용
        {
            if (characterTransform == null || shadowRenderer == null)
                return;

            // 캐릭터 발 위치에서 아래로 Raycast
            Vector3 rayStart = characterTransform.position + Vector3.up * 0.8f;  // 머리 높이에서 시작
            Ray ray = new Ray(rayStart, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundLayer))
            {
                // 그림자 위치 = 히트 지점 + 약간 위
                transform.position = hit.point + Vector3.up * heightOffset;

                // 캐릭터 회전에 따라 그림자도 회전 (Y축만)
                transform.rotation = Quaternion.Euler(90f, characterTransform.eulerAngles.y, 0f);

                // 투명도 조절 (거리 멀어질수록 희미하게 - 선택사항)
                float distanceFactor = Mathf.Clamp01(hit.distance / maxDistance);
                float currentAlpha = Mathf.Lerp(shadowAlpha, 0.1f, distanceFactor);

                // MaterialPropertyBlock으로 색상 변경 (Draw Call 절감)
                propBlock.SetColor("_BaseColor", new Color(0f, 0f, 0f, currentAlpha));
                shadowRenderer.SetPropertyBlock(propBlock);

                shadowRenderer.enabled = true;
            }
            else
            {
                // 바닥이 없으면 (공중 점프 등) 그림자 숨김
                shadowRenderer.enabled = false;
            }
        }
    }
}
