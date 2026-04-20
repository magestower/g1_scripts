using UnityEditor;
using UnityEngine;
using System.Reflection;

public static class GameViewMaximizer
{
    // 단축키: F11
    [MenuItem("Tools/Toggle Game View Maximize _F11")]
    static void ToggleMaximize()
    {
        EditorWindow gameView = GetGameView();
        if (gameView != null)
        {
            gameView.maximized = !gameView.maximized;
        }
    }

    static EditorWindow GetGameView()
    {
        Assembly assembly = typeof(EditorWindow).Assembly;
        System.Type type = assembly.GetType("UnityEditor.GameView");
        return EditorWindow.GetWindow(type);
    }
}
