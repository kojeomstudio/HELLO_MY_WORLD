using System;
using System.Collections.Generic;
using UnityEngine;
using Minecraft.Core;
using SharedProtocol;

namespace Minecraft.World
{
    [AddComponentMenu("Minecraft/Remote Entity Manager")]
    public sealed class RemoteEntityManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MinecraftGameClient gameClient;
        [SerializeField] private GameObject remotePlayerPrefab;

        [Header("Smoothing")]
        [SerializeField] private float positionLerpSpeed = 8f;
        [SerializeField] private float rotationLerpSpeed = 6f;
        [SerializeField] private float teleportThreshold = 12f;
        [SerializeField] private float positionSmoothTime = 0.12f;
        [SerializeField] private float predictionLeadTime = 0.2f;
        [SerializeField] private float jitterBufferDistance = 0.05f;
        [SerializeField] private float velocityDeadZone = 0.05f;

        private readonly Dictionary<string, RemoteEntity> _entities = new(StringComparer.OrdinalIgnoreCase);
        private string _localPlayerId = string.Empty;

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

            gameClient.EntitySpawned += OnEntitySpawned;
            gameClient.EntityUpdated += OnEntityUpdated;
            gameClient.EntityDespawned += OnEntityDespawned;
            gameClient.PlayerStateUpdated += OnPlayerStateUpdated;

            if (gameClient.PlayerState != null)
            {
                UpdateLocalPlayerId(gameClient.PlayerState);
            }

            SyncExistingEntities();
        }

        private void OnDisable()
        {
            if (gameClient != null)
            {
                gameClient.EntitySpawned -= OnEntitySpawned;
                gameClient.EntityUpdated -= OnEntityUpdated;
                gameClient.EntityDespawned -= OnEntityDespawned;
                gameClient.PlayerStateUpdated -= OnPlayerStateUpdated;
            }

            ClearEntities();
        }

        private void Update()
        {
            if (_entities.Count == 0)
            {
                return;
            }

            var deltaTime = Time.deltaTime;
            var currentTime = Time.time;
            var config = new RemoteSmoothingConfig(
                positionSmoothTime,
                positionLerpSpeed,
                rotationLerpSpeed,
                teleportThreshold,
                jitterBufferDistance,
                predictionLeadTime);

            foreach (var remote in _entities.Values)
            {
                remote.Update(deltaTime, currentTime, in config);
            }
        }

        private void OnEntitySpawned(EntityInfo entity)
        {
            if (!ShouldTrack(entity))
            {
                return;
            }

            var remote = GetOrCreateEntity(entity.EntityId, entity);
            remote.ApplySnapshot(entity, true);
        }

        private void OnEntityUpdated(EntityInfo entity)
        {
            if (!ShouldTrack(entity))
            {
                RemoveEntity(entity?.EntityId);
                return;
            }

            var remote = GetOrCreateEntity(entity.EntityId, entity);
            remote.ApplySnapshot(entity, false);
        }

        private void OnEntityDespawned(string entityId)
        {
            RemoveEntity(entityId);
        }

        private void OnPlayerStateUpdated(PlayerStateInfo state)
        {
            UpdateLocalPlayerId(state);
        }

        private void UpdateLocalPlayerId(PlayerStateInfo state)
        {
            if (state == null)
            {
                return;
            }

            var nextId = !string.IsNullOrWhiteSpace(state.PlayerId) ? state.PlayerId : state.Username;
            if (string.Equals(_localPlayerId, nextId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _localPlayerId = nextId ?? string.Empty;
            PruneLocalGhost();
        }

        private void SyncExistingEntities()
        {
            if (gameClient == null)
            {
                return;
            }

            foreach (var entity in gameClient.GetEntitySnapshot())
            {
                if (!ShouldTrack(entity))
                {
                    continue;
                }

                var remote = GetOrCreateEntity(entity.EntityId, entity);
                remote.ApplySnapshot(entity, true);
            }
        }

        private bool ShouldTrack(EntityInfo entity)
        {
            if (entity == null)
            {
                return false;
            }

            if (entity.EntityType != EntityType.Player)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(entity.EntityId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(_localPlayerId) &&
                string.Equals(entity.EntityId, _localPlayerId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private RemoteEntity GetOrCreateEntity(string entityId, EntityInfo snapshot)
        {
            if (!_entities.TryGetValue(entityId, out var remote))
            {
                var avatar = CreateAvatar(snapshot);
                remote = new RemoteEntity(avatar, velocityDeadZone);
                _entities[entityId] = remote;
            }

            return remote;
        }

        private GameObject CreateAvatar(EntityInfo snapshot)
        {
            GameObject instance;

            if (remotePlayerPrefab != null)
            {
                instance = Instantiate(remotePlayerPrefab, transform);
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                instance.transform.SetParent(transform, false);
                var collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = false;
                }
            }

            instance.name = string.IsNullOrEmpty(snapshot?.EntityId)
                ? "RemotePlayer"
                : $"RemotePlayer-{snapshot.EntityId}";

            if (snapshot?.Position != null)
            {
                instance.transform.position = ToUnityVector(snapshot.Position);
            }

            return instance;
        }

        private void RemoveEntity(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return;
            }

            if (_entities.Remove(entityId, out var remote))
            {
                remote.Destroy();
            }
        }

        private void PruneLocalGhost()
        {
            if (string.IsNullOrEmpty(_localPlayerId))
            {
                return;
            }

            if (_entities.Remove(_localPlayerId, out var remote))
            {
                remote.Destroy();
            }
        }

        private void ClearEntities()
        {
            foreach (var remote in _entities.Values)
            {
                remote.Destroy();
            }

            _entities.Clear();
        }

        private static Vector3 ToUnityVector(Vector3D value)
        {
            if (value == null)
            {
                return Vector3.zero;
            }

            return new Vector3((float)value.X, (float)value.Y, (float)value.Z);
        }

        private static Quaternion ToUnityRotation(Vector3D rotation)
        {
            if (rotation == null)
            {
                return Quaternion.identity;
            }

            return Quaternion.Euler((float)rotation.X, (float)rotation.Y, (float)rotation.Z);
        }

        private readonly struct RemoteSmoothingConfig
        {
            public RemoteSmoothingConfig(float positionSmoothTime, float maxCatchupSpeed, float rotationLerpSpeed, float teleportThreshold, float jitterBuffer, float predictionLeadTime)
            {
                PositionSmoothTime = Mathf.Max(positionSmoothTime, 0.0001f);
                MaxCatchupSpeed = Mathf.Max(maxCatchupSpeed, 0f);
                RotationLerpSpeed = Mathf.Max(rotationLerpSpeed, 0f);
                TeleportThreshold = Mathf.Max(teleportThreshold, 0f);
                JitterBuffer = Mathf.Max(jitterBuffer, 0f);
                PredictionLeadTime = Mathf.Max(predictionLeadTime, 0f);
            }

            public float PositionSmoothTime { get; }
            public float MaxCatchupSpeed { get; }
            public float RotationLerpSpeed { get; }
            public float TeleportThreshold { get; }
            public float JitterBuffer { get; }
            public float PredictionLeadTime { get; }
        }

        private sealed class RemoteEntity
        {
            private readonly GameObject _root;
            private readonly Transform _transform;
            private readonly float _velocityDeadZone;
            private Vector3 _targetPosition;
            private Quaternion _targetRotation = Quaternion.identity;
            private Vector3 _targetVelocity;
            private Vector3 _currentVelocity;
            private bool _hasTarget;
            private bool _hasRotation;
            private bool _hasVelocity;
            private float _lastSnapshotTime;

            public RemoteEntity(GameObject root, float velocityDeadZone)
            {
                _root = root;
                _transform = root.transform;
                _targetPosition = _transform.position;
                _velocityDeadZone = Mathf.Max(velocityDeadZone, 0f);
                _lastSnapshotTime = Time.time;
            }

            public void ApplySnapshot(EntityInfo snapshot, bool immediate)
            {
                if (snapshot?.Position != null)
                {
                    _targetPosition = ToUnityVector(snapshot.Position);
                    _hasTarget = true;
                }

                if (snapshot?.Rotation != null)
                {
                    _targetRotation = ToUnityRotation(snapshot.Rotation);
                    _hasRotation = true;
                }

                if (snapshot?.Velocity != null)
                {
                    var velocity = ToUnityVector(snapshot.Velocity);
                    if (velocity.sqrMagnitude <= _velocityDeadZone * _velocityDeadZone)
                    {
                        _targetVelocity = Vector3.zero;
                        _hasVelocity = false;
                    }
                    else
                    {
                        _targetVelocity = velocity;
                        _hasVelocity = true;
                    }
                }
                else
                {
                    _targetVelocity = Vector3.zero;
                    _hasVelocity = false;
                }

                _lastSnapshotTime = Time.time;

                if (immediate && _hasTarget)
                {
                    _transform.position = _targetPosition;
                    _currentVelocity = Vector3.zero;
                }

                if (immediate && _hasRotation)
                {
                    _transform.rotation = _targetRotation;
                }
            }

            public void Update(float deltaTime, float currentTime, in RemoteSmoothingConfig config)
            {
                if (!_hasTarget)
                {
                    return;
                }

                var predictedTarget = _targetPosition;
                if (_hasVelocity && config.PredictionLeadTime > 0f)
                {
                    var elapsed = Mathf.Clamp(currentTime - _lastSnapshotTime, 0f, config.PredictionLeadTime);
                    predictedTarget += _targetVelocity * elapsed;
                }

                var current = _transform.position;
                var distance = Vector3.Distance(current, predictedTarget);

                if (config.TeleportThreshold > 0f && distance > config.TeleportThreshold)
                {
                    _transform.position = predictedTarget;
                    _currentVelocity = Vector3.zero;
                }
                else if (distance <= config.JitterBuffer)
                {
                    _transform.position = predictedTarget;
                    _currentVelocity = Vector3.zero;
                }
                else
                {
                    var smoothTime = Mathf.Max(config.PositionSmoothTime, 0.0001f);
                    var maxSpeed = config.MaxCatchupSpeed > 0f ? config.MaxCatchupSpeed : float.PositiveInfinity;
                    _transform.position = Vector3.SmoothDamp(current, predictedTarget, ref _currentVelocity, smoothTime, maxSpeed, deltaTime);
                }

                if (_hasRotation)
                {
                    var tRot = config.RotationLerpSpeed <= 0f ? 1f : Mathf.Clamp01(config.RotationLerpSpeed * deltaTime);
                    _transform.rotation = Quaternion.Slerp(_transform.rotation, _targetRotation, tRot);
                }
            }

            public void Destroy()
            {
                if (_root != null)
                {
                    UnityEngine.Object.Destroy(_root);
                }
            }
        }
    }
}

