using System;
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
			/// <summary>Awake에서 인스턴스화할 슬래시 파티클 프리팹</summary>
			public GameObject slashPrefab;
			/// <summary>Awake에서 slashPrefab을 인스턴스화한 런타임 인스턴스</summary>
			[NonSerialized] public ParticleSystem slashParticle;
			/// <summary>파티클 프리팹의 원본 startSize — Awake에서 캐싱, effectSize 비율 계산 기준</summary>
			[NonSerialized] public float originalStartSize;
		}

		[Header("VFX 수치 프로필 (ScriptableObject)")]
		/// <summary>무기 타입별 크기/위치/회전 수치 에셋 — 코드 변경과 무관하게 수치가 보존됩니다.</summary>
		[SerializeField] private WeaponVFXProfile vfxProfile;

		[Header("무기 타입별 파티클 참조")]
		/// <summary>WeaponType마다 재생할 ParticleSystem을 인스펙터에서 할당합니다.</summary>
		[SerializeField] private WeaponParticleEntry[] weaponParticles;

		[Header("무기 Trail")]
		/// <summary>공격 애니메이션 중 활성화할 Tiny.Trail 컴포넌트. weapon 오브젝트에서 직접 할당한다.</summary>
		[SerializeField] private Tiny.Trail weaponTrail;

		/// <summary>무기 Trail을 켜거나 끈다. AttackStateBehaviour에서 호출한다.</summary>
		public void SetWeaponTrail(bool active)
		{
			if (weaponTrail == null) return;
			// Clear는 enabled 상태에서만 동작하므로 비활성화 전에 먼저 호출
			if (!active) weaponTrail.Clear();
			weaponTrail.enabled = active;
		}


		/// <summary>
		/// Awake: 인스펙터 미할당 시 스켈레톤 본 이름으로 rightHandBone을 자동 탐색합니다.
		/// </summary>
		private void Awake()
		{
			// Trail은 공격 시작 전까지 항상 비활성 상태로 시작
			if (weaponTrail != null)
			{
				weaponTrail.Clear();
				weaponTrail.enabled = false;
			}

			// 프리팹 인스턴스화 및 원본 startSize 캐싱
			if (weaponParticles != null)
				for (int i = 0; i < weaponParticles.Length; i++)
				{
					if (weaponParticles[i].slashPrefab == null) continue;

					GameObject instance = Instantiate(weaponParticles[i].slashPrefab, transform);
					weaponParticles[i].slashParticle = instance.GetComponent<ParticleSystem>();

					if (weaponParticles[i].slashParticle == null)
					{
						Debug.LogWarning($"[PlayerWeaponVFXController] {weaponParticles[i].slashPrefab.name}에 ParticleSystem이 없습니다.");
						continue;
					}
					// 활성 상태로 생성 후 즉시 정지 — SetActive 토글 없이 Stop/Play로만 제어
					weaponParticles[i].slashParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

					var startSize = weaponParticles[i].slashParticle.main.startSize;
					float size = startSize.mode switch
					{
						ParticleSystemCurveMode.Constant    => startSize.constant,
						ParticleSystemCurveMode.TwoConstants => (startSize.constantMin + startSize.constantMax) * 0.5f,
						ParticleSystemCurveMode.Curve        => startSize.curve.Evaluate(0f),
						ParticleSystemCurveMode.TwoCurves    => (startSize.curveMin.Evaluate(0f) + startSize.curveMax.Evaluate(0f)) * 0.5f,
						_                                    => 0f,
					};
					if (size <= 0f)
						Debug.LogWarning($"[PlayerWeaponVFXController] {weaponParticles[i].slashParticle.name}의 originalStartSize가 0입니다. ParticleSystem startSize 설정을 확인하세요.");
					weaponParticles[i].originalStartSize = size;
				}
		}

		/// <summary>
		/// 파티클 시스템을 지정 위치/회전으로 이동하고 프로필 수치를 적용하여 재생합니다.
		/// </summary>
		/// <param name="particle">재생할 ParticleSystem</param>
		/// <param name="profileEntry">수치를 가져올 프로필 엔트리</param>
		/// <param name="position">이펙트를 생성할 월드 좌표</param>
		private void PlaySlashParticle(ParticleSystem particle, float originalStartSize, in WeaponVFXProfile.Entry profileEntry, Vector3 position)
		{
			// 캐릭터 Y축 회전 기준으로 슬래시 회전값 적용
			Quaternion finalRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * Quaternion.Euler(profileEntry.slashRotation);
			particle.transform.SetPositionAndRotation(position, finalRotation);

			// effectSize를 원본 startSize 대비 비율로 적용 (1.0 = 원본 크기)
			var main = particle.main;
			main.startSize  = originalStartSize * profileEntry.effectSize;
			main.startDelay = profileEntry.playDelay;

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
			// weaponType에 대응하는 파티클과 원본 크기를 함께 조회
			ParticleSystem particle = null;
			float originalStartSize = 1f;
			if (weaponParticles != null)
				for (int i = 0; i < weaponParticles.Length; i++)
					if (weaponParticles[i].weaponType == weaponType)
					{
						particle = weaponParticles[i].slashParticle;
						originalStartSize = weaponParticles[i].originalStartSize;
						break;
					}
			if (particle == null) return;

			if (vfxProfile == null || !vfxProfile.TryGetEntry(weaponType, out WeaponVFXProfile.Entry profileEntry))
			{
				Debug.LogWarning($"[PlayerWeaponVFXController] WeaponVFXProfile에 {weaponType} 항목이 없습니다.");
				return;
			}

			// 캐릭터 루트 기준 offset 적용
			Vector3 effectPos = transform.position
			                  + transform.right   * profileEntry.positionOffset.x
			                  + transform.up      * profileEntry.positionOffset.y
			                  + transform.forward * profileEntry.positionOffset.z;

			PlaySlashParticle(particle, originalStartSize, profileEntry, effectPos);
		}
	}
}
