using System;
using System.Collections;
using Minecraft.Core;
using SharedProtocol;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM && !UNITY_DISABLE_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Minecraft.UI
{
    /// <summary>
    /// Applies tactile feedback (hit pause, screen shake, controller rumble) when the local player receives high-damage combat events.
    /// </summary>
    [AddComponentMenu("Minecraft/UI/Combat Hit Feedback Effects")]
    public sealed class CombatHitFeedbackEffects : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private Camera targetCamera;

        [Header("Damage Thresholds")]
        [SerializeField] private float highDamageThreshold = 6f;
        [SerializeField] private float criticalAmplitudeMultiplier = 1.4f;
        [SerializeField] private float criticalDurationMultiplier = 1.2f;

        [Header("Hit Pause")]
        [SerializeField] private float hitPauseDuration = 0.08f;
        [SerializeField] private float hitPauseTimescale = 0.15f;

        [Header("Screen Shake")]
        [SerializeField] private float screenShakeDuration = 0.25f;
        [SerializeField] private float screenShakeAmplitude = 0.12f;

        [Header("Controller Rumble")]
        [SerializeField] private bool enableRumble = true;
        [SerializeField] private float rumbleLowFrequency = 0.55f;
        [SerializeField] private float rumbleHighFrequency = 0.85f;
        [SerializeField] private float rumbleDuration = 0.22f;

        private Transform _cameraTransform;
        private Vector3 _cameraLocalOrigin;
        private bool _hasCameraOrigin;

        private Coroutine _hitPauseRoutine;
        private Coroutine _screenShakeRoutine;
        private Coroutine _rumbleRoutine;

        private float _savedTimeScale = 1f;
        private float _savedFixedDeltaTime = 0.02f;
        private bool _hitPauseActive;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void OnEnable()
        {
            EnsureDependencies();

            if (gameClient != null)
            {
                gameClient.CombatEventReceived += HandleCombatEvent;
            }
        }

        private void OnDisable()
        {
            if (gameClient != null)
            {
                gameClient.CombatEventReceived -= HandleCombatEvent;
            }

            if (_hitPauseRoutine != null)
            {
                StopCoroutine(_hitPauseRoutine);
                _hitPauseRoutine = null;
            }

            RestoreTimescale();

            if (_screenShakeRoutine != null)
            {
                StopCoroutine(_screenShakeRoutine);
                _screenShakeRoutine = null;
            }

            ResetCameraOffset();

#if ENABLE_INPUT_SYSTEM && !UNITY_DISABLE_INPUTSYSTEM
            if (_rumbleRoutine != null)
            {
                StopCoroutine(_rumbleRoutine);
                _rumbleRoutine = null;

                if (Gamepad.current != null)
                {
                    Gamepad.current.SetMotorSpeeds(0f, 0f);
                }
            }
#endif
        }

        private void HandleCombatEvent(CombatEventMessage message)
        {
            if (message == null)
            {
                return;
            }

            EnsureDependencies();

            if (!IsLocalPlayerTarget(message))
            {
                return;
            }

            var damage = Mathf.Max(0f, message.FinalDamage);
            if (damage < highDamageThreshold && !message.IsCritical)
            {
                return;
            }

            TriggerHitPause();
            TriggerScreenShake(damage, message.IsCritical);
            TriggerRumble(message.IsCritical);
        }

        private bool IsLocalPlayerTarget(CombatEventMessage message)
        {
            if (gameClient == null || gameClient.PlayerState == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(message.TargetName))
            {
                return false;
            }

            return string.Equals(
                gameClient.PlayerState.Username,
                message.TargetName,
                StringComparison.OrdinalIgnoreCase);
        }

        private void TriggerHitPause()
        {
            if (hitPauseDuration <= 0f || hitPauseTimescale <= 0f)
            {
                return;
            }

            if (_hitPauseRoutine != null)
            {
                StopCoroutine(_hitPauseRoutine);
                _hitPauseRoutine = null;
                RestoreTimescale();
            }

            _hitPauseRoutine = StartCoroutine(HitPauseRoutine(hitPauseDuration, hitPauseTimescale));
        }

        private IEnumerator HitPauseRoutine(float duration, float pauseScale)
        {
            _hitPauseActive = true;
            _savedTimeScale = Time.timeScale;
            _savedFixedDeltaTime = Time.fixedDeltaTime;

            var clampedScale = Mathf.Clamp(pauseScale, 0.01f, 1f);
            Time.timeScale = clampedScale;
            Time.fixedDeltaTime = _savedFixedDeltaTime * clampedScale / Mathf.Max(_savedTimeScale, 0.01f);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreTimescale();
            _hitPauseRoutine = null;
        }

        private void RestoreTimescale()
        {
            if (!_hitPauseActive)
            {
                return;
            }

            Time.timeScale = _savedTimeScale;
            Time.fixedDeltaTime = _savedFixedDeltaTime;
            _hitPauseActive = false;
        }

        private void TriggerScreenShake(float damage, bool isCritical)
        {
            if (_cameraTransform == null || screenShakeDuration <= 0f || screenShakeAmplitude <= 0f)
            {
                return;
            }

            if (_screenShakeRoutine != null)
            {
                StopCoroutine(_screenShakeRoutine);
                ResetCameraOffset();
                _screenShakeRoutine = null;
            }

            CacheCameraOrigin();

            var amplitude = screenShakeAmplitude;
            var duration = screenShakeDuration;

            if (isCritical)
            {
                amplitude *= criticalAmplitudeMultiplier;
                duration *= criticalDurationMultiplier;
            }
            else if (highDamageThreshold > 0f)
            {
                var severity = Mathf.Clamp01((damage - highDamageThreshold) / Mathf.Max(highDamageThreshold, 0.01f));
                amplitude *= Mathf.Lerp(0.75f, 1.1f, severity);
            }

            _screenShakeRoutine = StartCoroutine(ScreenShakeRoutine(duration, amplitude));
        }

        private IEnumerator ScreenShakeRoutine(float duration, float amplitude)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                var t = Mathf.Clamp01(elapsed / duration);
                var damper = 1f - t;
                var shakeOffset = UnityEngine.Random.insideUnitCircle * amplitude * damper;

                if (_cameraTransform != null && _hasCameraOrigin)
                {
                    _cameraTransform.localPosition = _cameraLocalOrigin + new Vector3(shakeOffset.x, shakeOffset.y * 0.6f, 0f);
                }

                yield return null;
            }

            ResetCameraOffset();
            _screenShakeRoutine = null;
        }

        private void ResetCameraOffset()
        {
            if (_cameraTransform != null && _hasCameraOrigin)
            {
                _cameraTransform.localPosition = _cameraLocalOrigin;
            }
        }

        private void TriggerRumble(bool isCritical)
        {
#if ENABLE_INPUT_SYSTEM && !UNITY_DISABLE_INPUTSYSTEM
            if (!enableRumble || rumbleDuration <= 0f)
            {
                return;
            }

            var pad = Gamepad.current;
            if (pad == null)
            {
                return;
            }

            if (_rumbleRoutine != null)
            {
                StopCoroutine(_rumbleRoutine);
                pad.SetMotorSpeeds(0f, 0f);
            }

            var low = Mathf.Clamp01(rumbleLowFrequency);
            var high = Mathf.Clamp01(rumbleHighFrequency);

            if (isCritical)
            {
                low = Mathf.Clamp01(low * 1.25f);
                high = Mathf.Clamp01(high * 1.25f);
            }

            _rumbleRoutine = StartCoroutine(RumbleRoutine(pad, low, high, rumbleDuration));
#else
            _ = isCritical;
#endif
        }

#if ENABLE_INPUT_SYSTEM && !UNITY_DISABLE_INPUTSYSTEM
        private IEnumerator RumbleRoutine(Gamepad pad, float low, float high, float duration)
        {
            pad.SetMotorSpeeds(low, high);

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            pad.SetMotorSpeeds(0f, 0f);
            _rumbleRoutine = null;
        }
#endif

        private void EnsureDependencies()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            if (targetCamera == null)
            {
                var playerController = FindObjectOfType<Minecraft.Player.MinecraftPlayerController>();
                if (playerController != null)
                {
                    targetCamera = playerController.GetComponentInChildren<Camera>();
                }

                if (targetCamera == null)
                {
                    targetCamera = Camera.main;
                }
            }

            if (targetCamera != null)
            {
                _cameraTransform = targetCamera.transform;
                CacheCameraOrigin();
            }
        }

        private void CacheCameraOrigin()
        {
            if (_cameraTransform != null)
            {
                _cameraLocalOrigin = _cameraTransform.localPosition;
                _hasCameraOrigin = true;
            }
        }
    }
}
