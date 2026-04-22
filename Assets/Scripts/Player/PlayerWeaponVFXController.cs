using UnityEngine;

namespace G1
{
	public class PlayerWeaponVFXController : MonoBehaviour
	{
		/// <summary>
		/// WeaponType별 파티클 참조 엔트리.
		/// 씬/프리팹 오브젝트 참조는 ScriptableObject에 저장할 수 없으므로 여기서 관리합니다.
		/// 수치(크기, 위치, 회전)는 WeaponVFXProfile 에셋에서 관리합니다.
		/// </summary>
		[System.Serializable]
		private struct WeaponParticleEntry
		{
			/// <summary>이 엔트리가 대응하는 무기 타입</summary>
			public WeaponType weaponType;
			/// <summary>해당 무기 타입에서 재생할 슬래시 파티클</summary>
			public ParticleSystem slashParticle;
		}

		[Header("VFX 수치 프로필 (ScriptableObject)")]
		/// <summary>무기 타입별 크기/위치/회전 수치 에셋 — 코드 변경과 무관하게 수치가 보존됩니다.</summary>
		[SerializeField] private WeaponVFXProfile vfxProfile;

		[Header("무기 타입별 파티클 참조")]
		/// <summary>WeaponType마다 재생할 ParticleSystem을 인스펙터에서 할당합니다.</summary>
		[SerializeField] private WeaponParticleEntry[] weaponParticles;

		[Header("오른손 기준 위치")]
		[SerializeField] private Transform rightHandBone;             // 오른손 본 Transform (직접 할당)
		[SerializeField] private string rightHandBoneName = "hand.r"; // 자동 탐색 시 사용할 본 이름

		[SerializeField] private Transform characterTransform;        // 캐릭터 Transform (방향 기준)

		/// <summary>
		/// Awake: 인스펙터 미할당 시 스켈레톤 본 이름으로 rightHandBone을 자동 탐색합니다.
		/// </summary>
		private void Awake()
		{
			if (rightHandBone == null)
				rightHandBone = FindChildByName(rightHandBoneName);

			characterTransform = transform;
		}

		/// <summary>
		/// 모든 자식 오브젝트를 순회하여 대소문자 구분 없이 이름이 일치하는 Transform을 반환합니다.
		/// 자기 자신은 제외하며, 찾지 못하면 경고 로그를 출력하고 null을 반환합니다.
		/// </summary>
		/// <param name="targetName">탐색할 오브젝트 이름</param>
		/// <returns>찾은 Transform, 없으면 null</returns>
		private Transform FindChildByName(string targetName)
		{
			foreach (Transform child in GetComponentsInChildren<Transform>(includeInactive: true))
			{
				if (child == transform) continue;

				if (string.Equals(child.name, targetName, System.StringComparison.OrdinalIgnoreCase))
					return child;
			}

			Debug.LogWarning($"[PlayerWeaponVFXController] 자식 오브젝트 '{targetName}'을 찾을 수 없습니다.", this);
			return null;
		}

		/// <summary>
		/// weaponParticles 배열에서 weaponType에 대응하는 ParticleSystem을 반환합니다.
		/// 등록되지 않은 타입이면 null을 반환합니다.
		/// </summary>
		/// <param name="weaponType">조회할 무기 타입</param>
		/// <returns>대응하는 ParticleSystem, 없으면 null</returns>
		private ParticleSystem GetParticle(WeaponType weaponType)
		{
			if (weaponParticles == null) return null;

			foreach (var e in weaponParticles)
			{
				if (e.weaponType == weaponType)
					return e.slashParticle;
			}
			return null;
		}

		/// <summary>
		/// 파티클 시스템을 지정 위치/회전으로 이동하고 프로필 수치를 적용하여 재생합니다.
		/// </summary>
		/// <param name="particle">재생할 ParticleSystem</param>
		/// <param name="profileEntry">수치를 가져올 프로필 엔트리</param>
		/// <param name="position">이펙트를 생성할 월드 좌표</param>
		private void PlaySlashParticle(ParticleSystem particle, in WeaponVFXProfile.Entry profileEntry, Vector3 position)
		{
			// 캐릭터 Y축 회전 기준으로 슬래시 회전값 적용
			float characterYaw = characterTransform != null ? characterTransform.eulerAngles.y : 0f;
			Quaternion finalRotation = Quaternion.Euler(0f, characterYaw, 0f) * Quaternion.Euler(profileEntry.slashRotation);
			particle.transform.SetPositionAndRotation(position, finalRotation);

			// 프로필 에셋에서 읽어온 수치 적용
			var main = particle.main;
			main.startSize  = profileEntry.effectSize;
			main.startDelay = profileEntry.playDelay;

			// 이미 재생 중인 경우 초기화 후 재생
			particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			particle.Play();
		}

		/// <summary>
		/// 무기 타입에 대응하는 슬래시 이펙트를 발동합니다.
		/// weaponParticles 또는 vfxProfile에 해당 타입이 미등록이면 재생되지 않습니다.
		/// AnimatorStateBehaviour 등에서 특정 애니메이션 시점에 직접 호출합니다.
		/// </summary>
		/// <param name="weaponType">현재 장착 무기 타입</param>
		public void PlayEffect(WeaponType weaponType)
		{
			if (rightHandBone == null) return;

			ParticleSystem particle = GetParticle(weaponType);
			if (particle == null) return;

			if (vfxProfile == null || !vfxProfile.TryGetEntry(weaponType, out WeaponVFXProfile.Entry profileEntry))
			{
				Debug.LogWarning($"[PlayerWeaponVFXController] WeaponVFXProfile에 {weaponType} 항목이 없습니다.");
				return;
			}

			// 프로필 수치 기반 위치 보정 계산 (characterTransform null 시 rightHandBone 위치 그대로 사용)
			Vector3 effectPos = rightHandBone.position;
			if (characterTransform != null)
			{
				effectPos += characterTransform.right   * profileEntry.positionOffset.x
				           + characterTransform.up      * profileEntry.positionOffset.y
				           + characterTransform.forward * profileEntry.positionOffset.z;
			}

			PlaySlashParticle(particle, profileEntry, effectPos);
		}
	}
}
