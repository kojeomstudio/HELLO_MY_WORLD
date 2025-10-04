using UnityEngine;
using Minecraft.Core;
using SharedProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Drives Unity lighting and ambient colours from server-sourced Minecraft day and night ticks.
    /// </summary>
    public class WorldTimeController : MonoBehaviour
    {
        private const float DayTicks = 24000f;

        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private Light sunLight;

        [Header("Lighting")]
        [SerializeField] private Gradient ambientColorGradient;
        [SerializeField] private AnimationCurve sunIntensityCurve;
        [SerializeField] private float sunRotationOffset = -90f;
        [SerializeField] private float smoothingSpeed = 0.25f;

        private float _targetSunAngle;
        private float _currentSunAngle;
        private bool _hasSnapshot;

        private void Awake()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            EnsureDefaults();
        }

        private void OnEnable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.TimeUpdated += OnTimeUpdated;

            if (gameClient.TryGetLastTimeSnapshot(out var snapshot) && snapshot != null)
            {
                ApplyInstant(snapshot.DayTime);
            }
        }

        private void OnDisable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.TimeUpdated -= OnTimeUpdated;
        }

        private void Update()
        {
            if (!_hasSnapshot)
            {
                return;
            }

            var angleStep = smoothingSpeed * Time.deltaTime * 360f;
            _currentSunAngle = Mathf.MoveTowardsAngle(_currentSunAngle, _targetSunAngle, angleStep);
            ApplyLighting(_currentSunAngle);
        }

        private void OnTimeUpdated(TimeUpdateMessage message)
        {
            if (message == null)
            {
                _hasSnapshot = false;
                return;
            }

            SetTarget(message.DayTime);
        }

        private void ApplyInstant(long dayTime)
        {
            _targetSunAngle = DayTimeToAngle(dayTime);
            _currentSunAngle = _targetSunAngle;
            _hasSnapshot = true;
            ApplyLighting(_currentSunAngle);
        }

        private void SetTarget(long dayTime)
        {
            _targetSunAngle = DayTimeToAngle(dayTime);
            if (!_hasSnapshot)
            {
                _currentSunAngle = _targetSunAngle;
                _hasSnapshot = true;
                ApplyLighting(_currentSunAngle);
            }
        }

        private static float DayTimeToAngle(long dayTime)
        {
            var ticks = Mathf.Repeat(dayTime, DayTicks);
            var fraction = ticks / DayTicks;
            return fraction * 360f;
        }

        private void ApplyLighting(float sunAngle)
        {
            var fraction = Mathf.Repeat(sunAngle / 360f, 1f);

            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle + sunRotationOffset, 0f, 0f);

                if (sunIntensityCurve != null && sunIntensityCurve.length > 0)
                {
                    sunLight.intensity = sunIntensityCurve.Evaluate(fraction);
                }
            }

            if (ambientColorGradient != null)
            {
                RenderSettings.ambientLight = ambientColorGradient.Evaluate(fraction);
            }
        }

        private void EnsureDefaults()
        {
            if (ambientColorGradient == null)
            {
                ambientColorGradient = new Gradient
                {
                    colorKeys = new[]
                    {
                        new GradientColorKey(new Color(0.06f, 0.07f, 0.15f), 0f),
                        new GradientColorKey(new Color(0.9f, 0.65f, 0.45f), 0.23f),
                        new GradientColorKey(new Color(1f, 0.98f, 0.92f), 0.5f),
                        new GradientColorKey(new Color(0.9f, 0.55f, 0.35f), 0.77f),
                        new GradientColorKey(new Color(0.06f, 0.07f, 0.15f), 1f)
                    },
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 1f)
                    }
                };
            }

            if (sunIntensityCurve == null || sunIntensityCurve.length == 0)
            {
                sunIntensityCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.24f, 1f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(0.76f, 1f),
                    new Keyframe(1f, 0f)
                );
            }
        }
    }
}
