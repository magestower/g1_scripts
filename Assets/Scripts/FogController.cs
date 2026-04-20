using UnityEngine;


namespace G1
{
    public class FogController : MonoBehaviour
    {
        [Header("색상 보정")]
        [Range(0f, 1f)]
        public float brightnessMultiplier = 0.8f; // 너무 밝으면 낮추기

        void Start()
        {
            RenderSettings.fog = true;

            SetFogColorFromSkybox();
        }

        void SetFogColorFromSkybox()
        {
            Material skybox = RenderSettings.skybox;
            if (skybox == null) return;

            Color avgColor = GetSkyboxAverageColor(skybox);

            // 밝기 보정
            avgColor *= brightnessMultiplier;
            avgColor.a = 1f;

            RenderSettings.fogColor = avgColor;
        }

        Color GetSkyboxAverageColor(Material skybox)
        {
            // 텍스처에서 평균 색상 추출
            Texture2D tex = null;

            if (skybox.HasProperty("_MainTex"))
                tex = skybox.GetTexture("_MainTex") as Texture2D;
            else if (skybox.HasProperty("_FrontTex"))
                tex = skybox.GetTexture("_FrontTex") as Texture2D;

            if (tex != null)
                return AverageColorFromTexture(tex);

            // 기본 회색
            return Color.gray;
        }

        Color AverageColorFromTexture(Texture2D tex)
        {
            // 샘플링 간격 (클수록 빠르지만 부정확)
            int step = Mathf.Max(1, tex.width / 16);

            Color sum = Color.black;
            int count = 0;

            try
            {
                for (int x = 0; x < tex.width; x += step)
                {
                    for (int y = 0; y < tex.height; y += step)
                    {
                        sum += tex.GetPixel(x, y);
                        count++;
                    }
                }
            }
            catch
            {
                // 텍스처 Read/Write 비활성화 시 기본값 반환
                Debug.LogWarning("스카이박스 텍스처 Read/Write를 활성화해주세요.");
                return Color.gray;
            }

            return count > 0 ? sum / count : Color.gray;
        }

        // 런타임 중 스카이박스 교체 시 호출
        public void UpdateFogColor()
        {
            SetFogColorFromSkybox();
        }

        /// <summary>
        /// Fog 활성화/비활성화 토글
        /// </summary>
        public void ToggleFog()
        {
            RenderSettings.fog = !RenderSettings.fog;
        }

        /// <summary>
        /// 화면 우측 상단 Restart 버튼 바로 아래에 Fog On/Off 버튼 렌더링
        /// Restart 버튼과 동일한 크기 및 X 위치 사용
        /// </summary>
        private void OnGUI()
        {
            // SceneRestartButton과 동일한 크기/여백 기준
            const float buttonWidth  = 180f;
            const float buttonHeight = 70f;
            const float margin       = 10f;

            float x = Screen.width - buttonWidth - margin;   // Restart 버튼과 동일한 X
            float y = margin + buttonHeight + margin;         // Restart 버튼 아래

            string label = RenderSettings.fog ? "Fog ON" : "Fog OFF";
            if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), label))
                ToggleFog();
        }

        private void OnDisable()
        {
            RenderSettings.fog = false;
        }
    }
}
