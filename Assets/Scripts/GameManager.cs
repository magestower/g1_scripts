using UnityEngine;


namespace G1
{
    // 게임의 진입점 - 씬 로드 전에 자동 생성됨
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            var go = new GameObject(nameof(GameManager));
            Instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }

        const int FrameRateDefault = 60;
        const int FrameRateDungeon = 30;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CreateGlobalObjects();
            SetFrameRate(FrameRateDefault);
        }

        void CreateGlobalObjects()
        {
            CreateFrameRateDisplay();
            CreateSkyboxRotator();
            CreateFogController();
            CreateSceneRestartButton();
        }

        void CreateFrameRateDisplay()
        {
            var go = new GameObject(nameof(FrameRateDisplay));
            go.AddComponent<FrameRateDisplay>();
            DontDestroyOnLoad(go);
        }

        void CreateSkyboxRotator()
        {
            var go = new GameObject(nameof(SkyboxRotator));
            go.AddComponent<SkyboxRotator>();
            DontDestroyOnLoad(go);
        }

        void CreateFogController()
        {
            var go = new GameObject(nameof(FogController));
            go.AddComponent<FogController>();
            DontDestroyOnLoad(go);
        }

        void CreateSceneRestartButton()
        {
            var go = new GameObject("SceneRestartButton");
            go.AddComponent<SceneRestartButton>();
            DontDestroyOnLoad(go);
        }

        // 사용 예시:
        // 던전 진입 시 → GameManager.Instance.SetFrameRate(30)
        // 던전 탈출 시 → GameManager.Instance.SetFrameRate(60)
        public void SetFrameRate(int fps)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }
    }
}
