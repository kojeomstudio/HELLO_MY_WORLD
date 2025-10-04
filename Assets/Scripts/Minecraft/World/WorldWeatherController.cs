using UnityEngine;
using Minecraft.Core;
using SharedProtocol;

namespace Minecraft.World
{
    /// <summary>
    /// Bridges server weather updates to Unity particle and audio cues.
    /// </summary>
    public class WorldWeatherController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private ParticleSystem rainFx;
        [SerializeField] private ParticleSystem snowFx;
        [SerializeField] private AudioSource rainAudio;
        [SerializeField] private AudioSource stormAudio;

        [Header("Tuning")]
        [SerializeField] private float precipitationRate = 900f;
        [SerializeField] private float snowRate = 450f;
        [SerializeField] private float intensityLerpSpeed = 1.5f;
        [SerializeField] private float audioLerpSpeed = 2f;
        [SerializeField] private float maxRainVolume = 0.6f;
        [SerializeField] private float maxStormVolume = 0.8f;

        public WeatherType CurrentWeather => _currentWeather;
        public float CurrentIntensity => _currentIntensity;

        private WeatherType _currentWeather = WeatherType.Clear;
        private float _targetIntensity;
        private float _currentIntensity;
        private bool _hasSnapshot;

        private void Awake()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }
        }

        private void OnEnable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.WeatherChanged += OnWeatherChanged;

            if (gameClient.TryGetLastWeatherSnapshot(out var snapshot) && snapshot != null)
            {
                ApplyInstant(snapshot);
            }
        }

        private void OnDisable()
        {
            if (gameClient == null)
            {
                return;
            }

            gameClient.WeatherChanged -= OnWeatherChanged;
        }

        private void Update()
        {
            if (!_hasSnapshot)
            {
                return;
            }

            _currentIntensity = Mathf.MoveTowards(_currentIntensity, _targetIntensity, intensityLerpSpeed * Time.deltaTime);
            ApplyParticles(_currentIntensity);
            ApplyAudio(_currentIntensity);
        }

        private void OnWeatherChanged(WeatherChangeMessage message)
        {
            if (message == null)
            {
                _currentWeather = WeatherType.Clear;
                _targetIntensity = 0f;
                _hasSnapshot = false;
                ApplyParticles(0f);
                ApplyAudio(0f);
                return;
            }

            _currentWeather = message.WeatherType;
            _targetIntensity = Mathf.Clamp01(message.Intensity);
            _hasSnapshot = true;
        }

        private void ApplyInstant(WeatherChangeMessage snapshot)
        {
            _currentWeather = snapshot.WeatherType;
            _targetIntensity = Mathf.Clamp01(snapshot.Intensity);
            _currentIntensity = _targetIntensity;
            _hasSnapshot = true;
            ApplyParticles(_currentIntensity);
            ApplyAudio(_currentIntensity);
        }

        private void ApplyParticles(float intensity)
        {
            if (rainFx != null)
            {
                var rainEnabled = _currentWeather == WeatherType.Rain || _currentWeather == WeatherType.Thunderstorm;
                var emission = rainFx.emission;
                emission.enabled = rainEnabled;

                if (rainEnabled)
                {
                    emission.rateOverTimeMultiplier = Mathf.Lerp(0f, precipitationRate, intensity);
                    if (!rainFx.isPlaying)
                    {
                        rainFx.Play();
                    }
                }
                else if (rainFx.isPlaying)
                {
                    rainFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            if (snowFx != null)
            {
                var snowEnabled = _currentWeather == WeatherType.Snow;
                var emission = snowFx.emission;
                emission.enabled = snowEnabled;

                if (snowEnabled)
                {
                    emission.rateOverTimeMultiplier = Mathf.Lerp(0f, snowRate, intensity);
                    if (!snowFx.isPlaying)
                    {
                        snowFx.Play();
                    }
                }
                else if (snowFx.isPlaying)
                {
                    snowFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void ApplyAudio(float intensity)
        {
            if (rainAudio != null)
            {
                var targetVolume = 0f;
                if (_currentWeather == WeatherType.Rain || _currentWeather == WeatherType.Thunderstorm)
                {
                    targetVolume = Mathf.Lerp(0f, maxRainVolume, intensity);
                }

                rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, targetVolume, audioLerpSpeed * Time.deltaTime);

                if (rainAudio.volume <= 0.001f && targetVolume <= 0f)
                {
                    rainAudio.Stop();
                }
                else if (targetVolume > 0f && !rainAudio.isPlaying)
                {
                    rainAudio.Play();
                }
            }

            if (stormAudio != null)
            {
                var targetVolume = _currentWeather == WeatherType.Thunderstorm ? Mathf.Lerp(0f, maxStormVolume, intensity) : 0f;
                stormAudio.volume = Mathf.MoveTowards(stormAudio.volume, targetVolume, audioLerpSpeed * Time.deltaTime);

                if (stormAudio.volume <= 0.001f && targetVolume <= 0f)
                {
                    stormAudio.Stop();
                }
                else if (targetVolume > 0f && !stormAudio.isPlaying)
                {
                    stormAudio.Play();
                }
            }
        }
    }
}
