using UnityEngine;

namespace G1
{
    /// <summary>
    /// 카메라 셰이크 기본 프리셋을 담는 ScriptableObject.
    /// Resources/CameraShakeConfig 경로에 에셋을 생성하면 MonsterBase가 자동으로 불러온다.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraShakeConfig", menuName = "G1/Camera Shake Config")]
    public class CameraShakeConfig : ScriptableObject
    {
        /// <summary>일반 피격 기본 셰이크</summary>
        public ShakePreset defaultShake = new ShakePreset { duration = 0.15f, magnitude = 0.08f, frequency = 20f };
        /// <summary>DamageType별 셰이크 오버라이드. 매칭 항목이 없으면 defaultShake를 사용한다.</summary>
        public DamageTypeShakeEntry[] shakeOverrides;
    }
}
