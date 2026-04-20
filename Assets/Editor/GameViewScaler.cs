// GameViewScaler.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class GameViewScaler
{
    static GameViewScaler()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SetGameViewScale();
        }
    }

    static void SetGameViewScale()
    {
        var assembly = typeof(EditorWindow).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(type);
        var areaField = type.GetField("m_ZoomArea",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        var area = areaField.GetValue(window);
        var scaleField = area.GetType().GetField("m_Scale",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        // 게임 시작 시 scale을 최솟값(0.1)으로 설정
        scaleField.SetValue(area, new Vector2(0.1f, 0.1f));
    }
}
#endif
