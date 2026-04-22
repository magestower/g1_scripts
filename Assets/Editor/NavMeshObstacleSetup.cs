using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace G1.Editor
{
    /// <summary>
    /// dokkaebi 프리팹의 NavMeshObstacle을 기본 비활성화 상태로 설정하는 유틸리티.
    /// NavMeshAgent와 동시 활성화 경고 방지를 위해 프리팹 저장 시 obstacle.enabled = false로 설정한다.
    /// </summary>
    public static class NavMeshObstacleSetup
    {
        [MenuItem("G1/Setup/NavMeshObstacle 비활성화 설정")]
        public static void DisableObstacleOnPrefab()
        {
            const string prefabPath = "Assets/Prefabs/Monster/dokkaebi.prefab";

            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            GameObject root = scope.prefabContentsRoot;

            NavMeshObstacle obstacle = root.GetComponent<NavMeshObstacle>();
            if (obstacle == null)
            {
                Debug.LogError("[NavMeshObstacleSetup] NavMeshObstacle 컴포넌트를 찾을 수 없습니다.");
                return;
            }

            obstacle.enabled = false;
            Debug.Log("[NavMeshObstacleSetup] dokkaebi 프리팹 NavMeshObstacle.enabled = false 설정 완료.");
        }
    }
}
