using UnityEngine;


namespace G1
{
    public class SkyboxRotator : MonoBehaviour
    {
        [Tooltip("Rotation speed in degrees per second")]
        public float rotationSpeed = 1f;

        private float currentAngle = 0f;

        void Update()
        {
            if (RenderSettings.skybox == null) return;

            currentAngle += rotationSpeed * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            RenderSettings.skybox.SetFloat("_Rotation", currentAngle);
        }

        void OnDisable()
        {
            if (RenderSettings.skybox != null)
                RenderSettings.skybox.SetFloat("_Rotation", 0f);
        }
    }
}
