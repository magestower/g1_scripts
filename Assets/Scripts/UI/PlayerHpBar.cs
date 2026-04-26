using UnityEngine;
using UnityEngine.UI;

namespace G1
{
    /// <summary>
    /// 플레이어 HP를 Screen Space UI 이미지 fillAmount로 표시하는 컴포넌트.
    /// PlayerController.OnHealthChanged 이벤트를 구독해 갱신된다.
    /// </summary>
    public class PlayerHpBar : MonoBehaviour
    {
        /// <summary>HP 게이지 fill 이미지 (Image Type: Filled)</summary>
        [SerializeField] private Image fillImage;

        private PlayerController player;

        /// <summary>씬에서 PlayerController를 찾아 이벤트를 구독하고 초기 체력을 반영한다.</summary>
        private void Start()
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[PlayerHpBar] PlayerController를 찾을 수 없습니다.", this);
                return;
            }
            player.OnHealthChanged += OnHealthChanged;
            // PlayerController.Start()에서 이미 이벤트를 발행했을 수 있으므로 구독 후 즉시 초기값 반영
            player.ForceHealthUIRefresh();
        }

        /// <summary>구독 해제로 메모리 누수를 방지한다.</summary>
        private void OnDestroy()
        {
            if (player != null)
                player.OnHealthChanged -= OnHealthChanged;
        }

        /// <summary>
        /// 체력 변경 시 fillAmount를 0~1로 갱신한다.
        /// </summary>
        /// <param name="current">현재 체력</param>
        /// <param name="max">최대 체력</param>
        private void OnHealthChanged(int current, int max)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }
}
