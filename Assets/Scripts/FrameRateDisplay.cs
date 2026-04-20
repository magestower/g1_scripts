using UnityEngine;


namespace G1
{
    // 화면 왼쪽 하단에 프레임(fps)과 처리 시간(ms) 표시
    public class FrameRateDisplay : MonoBehaviour
    {
        float deltaTime = 0f;

        void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        void OnGUI()
        {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new();
            Rect rect = new(10, h - 40, w, 30);
            style.alignment = TextAnchor.LowerLeft;
            style.fontSize = h * 2 / 80;
            style.normal.textColor = Color.white;

            float ms = deltaTime * 1000f;
            float fps = 1f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", ms, fps);
            GUI.Label(rect, text, style);
        }
    }
}
