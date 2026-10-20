using UnityEngine;

namespace MinhaVila.Unity
{
    public sealed class DynamicLighting : MonoBehaviour
    {
        private Light _sun;
        private Camera _camera;
        private int _lastHour = -1;

        public void Initialize(Camera camera)
        {
            _camera = camera;
            GameObject sunObject = new GameObject("Sol e luz da vila");
            sunObject.transform.SetParent(transform, false);
            _sun = sunObject.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.intensity = 1.1f;
            _sun.shadows = LightShadows.Soft;
            _sun.shadowStrength = 0.72f;
            _sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
            QualitySettings.shadowDistance = 45;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0035f;
            UpdateTime(8);
        }

        public void UpdateTime(int hour)
        {
            int normalizedHour = Mathf.Clamp(hour, 0, 23);
            if (_lastHour == normalizedHour) return;
            _lastHour = normalizedHour;

            float daylight = Mathf.Clamp01(Mathf.Sin((normalizedHour - 5f) / 14f * Mathf.PI));
            float warm = Mathf.Clamp01(1 - Mathf.Abs(normalizedHour - 17f) / 4f);
            _sun.intensity = Mathf.Lerp(0.16f, 1.18f, daylight);
            _sun.color = Color.Lerp(new Color(0.46f, 0.57f, 0.79f), new Color(1f, 0.88f, 0.69f), warm * 0.55f + daylight * 0.45f);
            _sun.transform.rotation = Quaternion.Euler(18 + daylight * 52, -30 + normalizedHour * 2.4f, 0);

            Color nightAmbient = new Color(0.14f, 0.19f, 0.29f);
            Color dayAmbient = new Color(0.67f, 0.66f, 0.55f);
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, daylight);
            Color sky = Color.Lerp(new Color(0.04f, 0.08f, 0.16f), new Color(0.52f, 0.70f, 0.83f), daylight);
            if (warm > 0.5f) sky = Color.Lerp(sky, new Color(0.79f, 0.48f, 0.32f), (warm - 0.5f) * 0.45f);
            RenderSettings.fogColor = sky;
            if (_camera != null) _camera.backgroundColor = sky;
        }
    }
}
