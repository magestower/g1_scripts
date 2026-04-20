using UnityEngine;
using UnityEngine.SceneManagement;


namespace G1
{
    /// <summary>
    /// 화면 우측 상단에 현재 씬 재시작 버튼을 표시합니다.
    /// </summary>
    public class SceneRestartButton : MonoBehaviour
    {
        [SerializeField] private float buttonWidth  = 180f;  // 버튼 너비
        [SerializeField] private float buttonHeight = 70f;   // 버튼 높이
        [SerializeField] private float margin       = 10f;  // 화면 가장자리 여백

        /// <summary>
        /// 화면 우측 상단에 재시작 버튼을 렌더링합니다.
        /// </summary>
        private void OnGUI()
        {
            // 우측 상단 기준으로 버튼 위치 계산
            float x = Screen.width - buttonWidth - margin;
            if (GUI.Button(new Rect(x, margin, buttonWidth, buttonHeight), "Restart"))
                RestartScene();
        }

        /// <summary>
        /// 현재 활성화된 씬을 재로드합니다.
        /// </summary>
        private void RestartScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
