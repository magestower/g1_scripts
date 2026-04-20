using UnityEngine;


namespace G1
{
    public class TrafficLightBlink : MonoBehaviour
    {
        public enum LightColor { Red, Orange, Green }

        Light targetLight;

        [Header("빛 설정")]
        public LightColor lightColor = LightColor.Red;
        public float maxIntensity = 3f;

        [Header("타이밍 설정")]
        public float blinkSpeed = 1f;   // 낮을수록 전체 주기가 길어짐
        public float onRatio = 0.2f;    // 켜져 있는 비율 (0.2 = 20%)

        static readonly Color[] colors = { Color.red, new Color(1f, 0.5f, 0f), Color.green };

        void Awake()
        {
            targetLight = GetComponent<Light>();
            targetLight.color = colors[(int)lightColor];
        }

        void Update()
        {
            float t = Mathf.Repeat(Time.time * blinkSpeed, 1f);
            targetLight.intensity = t < onRatio ? maxIntensity : 0f;
        }
    }
}
