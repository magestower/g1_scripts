using UnityEngine;
using UnityEngine.UI;

namespace G1
{
    /// <summary>
    /// 몬스터 머리 위에 표시되는 World Space HP 게이지.
    /// MonsterBase의 OnHealthChanged 이벤트를 구독해 체력 변화를 반영한다.
    /// ring 0 배정 시 표시, 그 외에는 숨긴다.
    /// </summary>
    public class MonsterHpBar : MonoBehaviour
    {
        /// <summary>HP 게이지 Fill Image (Image Type: Filled)</summary>
        [SerializeField] private Image fillImage;

        /// <summary>HP 바 루트 오브젝트. SetActive로 표시/숨김 처리한다.</summary>
        [SerializeField] private GameObject barRoot;

        private MonsterBase monster;
        private Camera mainCamera;

        /// <summary>MonsterBase 참조를 캐싱하고 이벤트를 구독한다.</summary>
        private void Awake()
        {
            mainCamera = Camera.main;

            // 초기에는 숨김
            if (barRoot != null)
                barRoot.SetActive(false);

            Subscribe(GetComponentInParent<MonsterBase>());
        }

        /// <summary>이벤트 구독을 해제한다.</summary>
        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// monster를 교체하며 이벤트 구독을 갱신한다.
        /// 풀 재사용 등으로 부모가 바뀔 때 외부에서 호출할 수 있다.
        /// </summary>
        /// <param name="newMonster">새로 구독할 MonsterBase. null이면 구독만 해제한다.</param>
        public void Subscribe(MonsterBase newMonster)
        {
            Unsubscribe();
            monster = newMonster;
            if (monster != null)
                monster.OnHealthChanged += UpdateBar;
        }

        /// <summary>현재 구독 중인 이벤트를 해제한다.</summary>
        private void Unsubscribe()
        {
            if (monster != null)
            {
                monster.OnHealthChanged -= UpdateBar;
                monster = null;
            }
        }

        /// <summary>매 프레임 카메라를 향해 빌보드 회전한다.</summary>
        private void LateUpdate()
        {
            if (mainCamera == null) return;
            transform.rotation = mainCamera.transform.rotation;
        }

        /// <summary>
        /// HP 비율에 따라 Fill Image를 갱신한다.
        /// </summary>
        /// <param name="current">현재 체력</param>
        /// <param name="max">최대 체력</param>
        private void UpdateBar(int current, int max)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = max > 0 ? (float)current / max : 0f;
        }

        /// <summary>
        /// HP 바 표시 여부를 설정한다. ring 0 배정 시 true, 그 외 false.
        /// </summary>
        /// <param name="visible">표시 여부</param>
        public void SetVisible(bool visible)
        {
            if (barRoot != null)
                barRoot.SetActive(visible);
        }
    }
}
