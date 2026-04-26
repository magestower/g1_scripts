using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace G1
{
    /// <summary>
    /// PlayerProfile ScriptableObject의 데이터를 UI에 표시하는 컴포넌트.
    /// 초상화 이미지와 플레이어 이름을 Start 시점에 반영한다.
    /// </summary>
    public class PlayerProfileUI : MonoBehaviour
    {
        /// <summary>표시할 플레이어 프로필 데이터</summary>
        [SerializeField] private PlayerProfile profile;

        /// <summary>표시할 플레이어 스탯 데이터 (레벨 참조용)</summary>
        [SerializeField] private PlayerStat stat;

        /// <summary>초상화를 표시할 Image 컴포넌트</summary>
        [SerializeField] private Image portraitImage;

        /// <summary>플레이어 이름을 표시할 TextMeshProUGUI 컴포넌트 (선택)</summary>
        [SerializeField] private TextMeshProUGUI nameLabel;

        /// <summary>플레이어 레벨을 표시할 TextMeshProUGUI 컴포넌트 (선택)</summary>
        [SerializeField] private TextMeshProUGUI levelLabel;

        /// <summary>프로필 데이터를 UI에 반영한다.</summary>
        private void Start()
        {
            if (profile == null)
            {
                Debug.LogWarning("[PlayerProfileUI] profile이 연결되지 않았습니다.", this);
                return;
            }

            if (portraitImage != null)
                portraitImage.sprite = profile.portrait;

            if (nameLabel != null)
                nameLabel.text = profile.playerName;

            if (levelLabel != null && stat != null)
                levelLabel.text = $"Lv.{stat.level}";
        }
    }
}
