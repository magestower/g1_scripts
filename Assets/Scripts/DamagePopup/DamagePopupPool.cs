using System.Collections.Generic;
using UnityEngine;

namespace G1
{
    /// <summary>
    /// DamagePopup 오브젝트를 관리하는 싱글톤 풀.
    /// MonsterBase.TakeDamage에서 Show()를 직접 호출해 피격 위치에 팝업을 표시한다.
    /// MonsterPool과 동일한 싱글톤 + Stack 패턴을 따른다.
    /// </summary>
    public class DamagePopupPool : MonoBehaviour
    {
        public static DamagePopupPool Instance { get; private set; }

        /// <summary>팝업으로 사용할 DamagePopup 프리팹</summary>
        [SerializeField] private DamagePopup popupPrefab;

        /// <summary>씬 시작 시 미리 생성해둘 팝업 오브젝트 수</summary>
        [SerializeField] private int prewarmSize = 10;

        private readonly Stack<DamagePopup> pool = new();

        /// <summary>싱글톤을 설정하고 풀을 미리 채운다. 씬 전환 후에도 유지되도록 DontDestroyOnLoad를 적용한다.</summary>
        private void Awake()
        {
            // 중복 인스턴스 방지
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 풀 미리 채우기
            for (int i = 0; i < prewarmSize; i++)
                pool.Push(CreateInstance());
        }

        /// <summary>
        /// 피격 위치에 데미지 팝업을 표시한다. MonsterBase.TakeDamage에서 호출된다.
        /// </summary>
        /// <param name="damage">표시할 데미지 수치</param>
        /// <param name="worldPos">피격된 몬스터의 월드 위치</param>
        /// <param name="isCritical">크리티컬 여부. 팝업 색상/크기에 반영된다.</param>
        public void Show(int damage, Vector3 worldPos, bool isCritical = false)
        {
            DamagePopup popup = pool.Count > 0 ? pool.Pop() : CreateInstance();
            // SetActive 전에 위치를 먼저 잡아 1프레임 위치 오차 방지
            popup.transform.position = worldPos;
            popup.gameObject.SetActive(true);
            popup.Play(damage, worldPos, isCritical, () => Release(popup));
        }

        /// <summary>
        /// 팝업 애니메이션이 완료된 후 풀에 반납한다. DamagePopup의 onComplete 콜백으로 호출된다.
        /// </summary>
        /// <param name="popup">반납할 팝업 오브젝트</param>
        private void Release(DamagePopup popup)
        {
            popup.gameObject.SetActive(false);
            pool.Push(popup);
        }

        /// <summary>
        /// 새 DamagePopup 인스턴스를 생성한다.
        /// Awake 강제 실행을 위해 SetActive(true) 후 SetActive(false) 순서로 초기화한다.
        /// </summary>
        private DamagePopup CreateInstance()
        {
            DamagePopup inst = Instantiate(popupPrefab, transform);

            // Awake가 SetActive(true) 시점에 실행되도록 강제
            inst.gameObject.SetActive(true);
            inst.gameObject.SetActive(false);

            return inst;
        }
    }
}
