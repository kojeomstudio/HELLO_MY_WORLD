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

        [Header("Culling & Pooling")]
        [SerializeField] private float cullDistance = 96f;
        [SerializeField] private float reactivationBuffer = 12f;
        [SerializeField] private int remoteAvatarPoolCapacity = 32;

        private readonly Dictionary<string, RemoteEntity> _entities = new(StringComparer.OrdinalIgnoreCase);
        private RemoteAvatarPool _avatarPool;
        private Vector3 _lastLocalPlayerPosition;
        private bool _hasLocalPlayerPosition;
        private string _localPlayerId = string.Empty;

        private void Awake()
        {
            if (gameClient == null)
            {
                gameClient = FindObjectOfType<MinecraftGameClient>();
            }

            _avatarPool = new RemoteAvatarPool(this, remoteAvatarPoolCapacity);
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
                UpdateLocalPlayerState(gameClient.PlayerState);
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
            _avatarPool?.Clear();
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

            var hasLocalPosition = TryGetLocalPlayerPosition(out var localPosition);
            var maxDistance = Mathf.Max(cullDistance, 0f);
            var activationDistance = maxDistance;

            if (maxDistance > 0f && reactivationBuffer > 0f)
            {
                activationDistance = Mathf.Max(0.1f, maxDistance - reactivationBuffer);
                if (activationDistance > maxDistance)
                {
                    activationDistance = maxDistance;
                }
            }

            foreach (var remote in _entities.Values)
            {
                remote.Update(deltaTime, currentTime, in config);

                if (!hasLocalPosition || maxDistance <= 0f)
                {
                    continue;
                }

                var distance = remote.DistanceTo(localPosition);
                if (!remote.IsCulled && distance > maxDistance)
                {
                    remote.SetCulled(true);
                }
                else if (remote.IsCulled && distance <= activationDistance)
                {
                    remote.SetCulled(false);
                }
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
            UpdateLocalPlayerState(state);
        }

        private void UpdateLocalPlayerState(PlayerStateInfo state)
        {
            if (state == null)
            {
                return;
            }

            var nextId = !string.IsNullOrWhiteSpace(state.PlayerId) ? state.PlayerId : state.Username;
            if (!string.Equals(_localPlayerId, nextId, StringComparison.OrdinalIgnoreCase))
            {
                _localPlayerId = nextId ?? string.Empty;
                PruneLocalGhost();
            }

            if (state.Position != null)
            {
                _lastLocalPlayerPosition = ToUnityVector(state.Position);
                _hasLocalPlayerPosition = true;
            }
        }

        private bool TryGetLocalPlayerPosition(out Vector3 position)
        {
            if (_hasLocalPlayerPosition)
            {
                position = _lastLocalPlayerPosition;
                return true;
            }

            var snapshot = gameClient != null ? gameClient.PlayerState : null;
            if (snapshot?.Position != null)
            {
                position = ToUnityVector(snapshot.Position);
                _lastLocalPlayerPosition = position;
                _hasLocalPlayerPosition = true;
                return true;
            }

            position = Vector3.zero;
            return false;
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
                var avatar = AcquireAvatar(snapshot);
                remote = new RemoteEntity(entityId, avatar, velocityDeadZone);
                _entities[entityId] = remote;
            }

            return remote;
        }

        private GameObject AcquireAvatar(EntityInfo snapshot)
        {
            if (_avatarPool != null)
            {
                return _avatarPool.Rent(snapshot);
            }

            var instance = InstantiateAvatar();
            ConfigureAvatar(instance, snapshot);
            return instance;
        }

        private GameObject InstantiateAvatar()
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

            return instance;
        }

        private void ConfigureAvatar(GameObject instance, EntityInfo snapshot)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.SetParent(transform, false);

            var label = string.IsNullOrEmpty(snapshot?.EntityId)
                ? "RemotePlayer"
                : $"RemotePlayer-{snapshot.EntityId}";
            instance.name = label;

            if (snapshot?.Position != null)
            {
                instance.transform.position = ToUnityVector(snapshot.Position);
            }

            if (!instance.activeSelf)
            {
                instance.SetActive(true);
            }
        }

        private void RemoveEntity(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
            {
                return;
            }

            if (_entities.Remove(entityId, out var remote))
            {
                ReleaseEntity(remote);
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
                ReleaseEntity(remote);
            }
        }

        private void ClearEntities()
        {
            foreach (var remote in _entities.Values)
            {
                ReleaseEntity(remote);
            }

            _entities.Clear();
        }

        private void ReleaseEntity(RemoteEntity remote)
        {
            if (remote == null)
            {
                return;
            }

            var avatar = remote.Detach();
            if (avatar == null)
            {
                return;
            }

            if (_avatarPool != null)
            {
                _avatarPool.Return(avatar);
            }
            else
            {
                Destroy(avatar);
            }
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
            private readonly string _entityId;
            private readonly float _velocityDeadZone;
            private GameObject _root;
            private Transform _transform;
            private Vector3 _targetPosition;
            private Quaternion _targetRotation = Quaternion.identity;
            private Vector3 _targetVelocity;
            private Vector3 _currentVelocity;
            private bool _hasTarget;
            private bool _hasRotation;
            private bool _hasVelocity;
            private float _lastSnapshotTime;
            private bool _isCulled;

            public RemoteEntity(string entityId, GameObject root, float velocityDeadZone)
            {
                _entityId = entityId ?? string.Empty;
                _velocityDeadZone = Mathf.Max(velocityDeadZone, 0f);
                Attach(root);
            }

            public bool IsCulled => _isCulled;

            public void Attach(GameObject root)
            {
                if (root == null)
                {
                    throw new ArgumentNullException(nameof(root));
                }

                _root = root;
                _transform = root.transform;
                _targetPosition = _transform.position;
                _targetRotation = _transform.rotation;
                _targetVelocity = Vector3.zero;
                _currentVelocity = Vector3.zero;
                _hasTarget = true;
                _hasRotation = true;
                _hasVelocity = false;
                _isCulled = false;
                _lastSnapshotTime = Time.time;

                if (!_root.activeSelf)
                {
                    _root.SetActive(true);
                }
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

                if (_transform == null)
                {
                    return;
                }

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
                if (_transform == null || _isCulled)
                {
                    return;
                }

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

            public void SetCulled(bool culled)
            {
                if (_transform == null || _isCulled == culled)
                {
                    return;
                }

                _isCulled = culled;

                if (culled)
                {
                    _transform.gameObject.SetActive(false);
                    _currentVelocity = Vector3.zero;
                }
                else
                {
                    _transform.gameObject.SetActive(true);
                    if (_hasTarget)
                    {
                        _transform.position = _targetPosition;
                    }

                    if (_hasRotation)
                    {
                        _transform.rotation = _targetRotation;
                    }

                    _lastSnapshotTime = Time.time;
                }
            }

            public float DistanceTo(Vector3 position)
            {
                if (_hasTarget)
                {
                    return Vector3.Distance(_targetPosition, position);
                }

                if (_transform != null)
                {
                    return Vector3.Distance(_transform.position, position);
                }

                return float.PositiveInfinity;
            }

            public GameObject Detach()
            {
                var avatar = _root;
                _root = null;
                _transform = null;
                _currentVelocity = Vector3.zero;
                _targetVelocity = Vector3.zero;
                _hasTarget = false;
                _hasRotation = false;
                _hasVelocity = false;
                _isCulled = false;
                return avatar;
            }
        }

        private sealed class RemoteAvatarPool
        {
            private readonly RemoteEntityManager _owner;
            private readonly Queue<GameObject> _pool;
            private readonly int _capacity;

            public RemoteAvatarPool(RemoteEntityManager owner, int capacity)
            {
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _capacity = Mathf.Max(capacity, 0);
                _pool = new Queue<GameObject>(_capacity > 0 ? _capacity : 0);
            }

            public GameObject Rent(EntityInfo snapshot)
            {
                GameObject avatar = _pool.Count > 0 ? _pool.Dequeue() : _owner.InstantiateAvatar();
                if (avatar != null)
                {
                    _owner.ConfigureAvatar(avatar, snapshot);
                }

                return avatar;
            }

            public void Return(GameObject avatar)
            {
                if (avatar == null)
                {
                    return;
                }

                avatar.SetActive(false);
                avatar.transform.SetParent(_owner.transform, false);

                if (_pool.Count < _capacity)
                {
                    _pool.Enqueue(avatar);
                }
                else
                {
                    UnityEngine.Object.Destroy(avatar);
                }
            }

            public void Clear()
            {
                while (_pool.Count > 0)
                {
                    var avatar = _pool.Dequeue();
                    if (avatar != null)
                    {
                        UnityEngine.Object.Destroy(avatar);
                    }
                }
            }
        }

    }
}

