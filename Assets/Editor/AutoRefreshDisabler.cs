// Editor 폴더 안에 AutoRefreshDisabler.cs 생성
#if UNITY_EDITOR
using UnityEditor;

namespace G1
{
    [InitializeOnLoad]
    public static class AutoRefreshDisabler
    {
        static AutoRefreshDisabler()
        {
            // 포커스 복귀 시 자동 새로고침 끄기 (수동 Ctrl+R로 제어)
            AssetDatabase.DisallowAutoRefresh();
        }
    }
}

#endif