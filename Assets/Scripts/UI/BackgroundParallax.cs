using System;
using UnityEngine;
using UnityEngine.UI;



namespace G1
{
    /// <summary>
    /// 캐릭터의 월드 이동량을 기반으로 RawImage 배경을 반대 방향으로 이동시켜
    /// 패럴랙스 스크롤링 효과를 연출합니다.
    /// 배경이 경계에 도달하면 OnBoundaryChanged 이벤트를 발행합니다.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class BackgroundParallax : MonoBehaviour
    {
        /// <summary>
        /// 배경 경계 도달 상태 변경 이벤트.
        /// Vector2 각 성분 의미:
        ///   +1 : 해당 축의 양방향 경계 도달 (배경이 +maxX/+maxY 에 위치)
        ///   -1 : 해당 축의 음방향 경계 도달 (배경이 -maxX/-maxY 에 위치)
        ///    0 : 경계 미도달 (자유 이동 가능)
        /// PlayerController 는 이 값을 보고 반대 방향 캐릭터 입력을 차단합니다.
        /// </summary>
        public static event Action<Vector2> OnBoundaryChanged;

        [Header("추적할 캐릭터 Transform")]
        [SerializeField] private Transform character;

        [Header("패럴랙스 이동 속도 (클수록 배경이 빠르게 이동)")]
        [SerializeField] private float parallaxSpeed = 50f;

        // 이전 프레임 경계 상태 (중복 이벤트 방지)
        private Vector2 lastBoundary = Vector2.zero;

        private RectTransform rectTransform;
        private Vector3 lastCharacterPosition;

        // 경계 판정 허용 오차 (픽셀)
        private const float BoundaryEpsilon = 0.5f;

        // 배경 이미지가 바뀌지 않으므로 Start에서 한 번만 계산해 캐싱
        private float maxX;
        private float maxY;

        /// <summary>
        /// Awake: RectTransform 캐싱 및 character 미할당 시 씬에서 PlayerController 자동 탐색
        /// </summary>
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // Inspector에서 할당되지 않은 경우 씬에서 PlayerController를 자동으로 탐색
            if (character == null)
            {
                PlayerController pc = FindAnyObjectByType<PlayerController>();
                if (pc != null)
                    character = pc.transform;
                else
                    Debug.LogWarning("[BackgroundParallax] 씬에서 PlayerController를 찾을 수 없습니다.");
            }
        }

        /// <summary>
        /// Start: 캐릭터 초기 위치 기록 및 이동 가능 범위(maxX/maxY) 사전 계산
        /// </summary>
        private void Start()
        {
            // character가 null이어도 lastCharacterPosition을 현재 위치로 초기화해
            // 이후 character가 설정될 때 delta 오차로 배경이 순간 이동하는 것을 방지
            lastCharacterPosition = character != null ? character.position : Vector3.zero;

            CacheMoveBounds();

            rectTransform.anchoredPosition = new Vector2(0f, maxY);
        }

        /// <summary>ㄴ
        /// 배경 크기와 부모 크기를 기반으로 maxX / maxY 를 계산합니다.
        /// 배경 이미지가 교체될 때만 재호출하면 됩니다.
        /// </summary>
        private void CacheMoveBounds()
        {
            RectTransform parentRect = transform.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 bgSize     = rectTransform.sizeDelta;
            Vector2 parentSize = parentRect.rect.size;

            maxX = Mathf.Max(0f, (bgSize.x - parentSize.x) * 0.5f);
            maxY = Mathf.Max(0f, (bgSize.y - parentSize.y) * 0.5f);
        }

        /// <summary>
        /// LateUpdate: 캐릭터 이동 후 배경 위치 갱신 및 경계 이벤트 발행
        /// </summary>
        private void LateUpdate()
        {
            if (character == null) return;

            // 이번 프레임 캐릭터 이동량 계산 (XZ 평면 기준)
            Vector3 delta = character.position - lastCharacterPosition;
            lastCharacterPosition = character.position;

            // 캐릭터 이동 반대 방향으로 배경 이동
            Vector2 bgMove = new Vector2(-delta.x, -delta.z) * parallaxSpeed;

    		// y 좌표 고정 (캐릭터의 수직 이동은 배경에 영향을 주지 않음)
    		// 상호연동 LINK: D:\workspace\code\G1\Assets\Scripts\Player\PlayerController.cs#L282
    		bgMove.y = 0;

    		rectTransform.anchoredPosition += bgMove;

            // 경계 클램프 및 이벤트 발행
            ClampAndNotify();
        }

        /// <summary>
        /// 배경 위치를 클램프하고, 경계 도달 상태가 바뀌면 OnBoundaryChanged 이벤트를 발행합니다.
        /// </summary>
        private void ClampAndNotify()
        {
            // 클램프 적용 (maxX / maxY 는 Start 에서 사전 계산된 캐싱 값 사용)
            Vector2 pos = rectTransform.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
            pos.y = Mathf.Clamp(pos.y, -maxY, maxY);
            rectTransform.anchoredPosition = pos;

            // 각 축의 경계 도달 여부 판정
            // +1 : 양방향 경계 (배경이 +max 에 위치 → 캐릭터 음방향 입력 차단)
            // -1 : 음방향 경계 (배경이 -max 에 위치 → 캐릭터 양방향 입력 차단)
            //  0 : 경계 미도달
            float signX = 0f;
            if      (maxX > 0f && Mathf.Abs(pos.x - maxX)  < BoundaryEpsilon) signX =  1f;
            else if (maxX > 0f && Mathf.Abs(pos.x + maxX)  < BoundaryEpsilon) signX = -1f;

            float signY = 0f;
            if      (maxY > 0f && Mathf.Abs(pos.y - maxY)  < BoundaryEpsilon) signY =  1f;
            else if (maxY > 0f && Mathf.Abs(pos.y + maxY)  < BoundaryEpsilon) signY = -1f;

            Vector2 newBoundary = new Vector2(signX, signY);

            // 상태가 바뀐 경우에만 이벤트 발행 (매 프레임 발행 방지)
            if (newBoundary != lastBoundary)
            {
                lastBoundary = newBoundary;
                OnBoundaryChanged?.Invoke(newBoundary);
            }
        }
    }
}
