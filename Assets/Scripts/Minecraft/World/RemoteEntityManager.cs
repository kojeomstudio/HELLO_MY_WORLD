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
            foreach (var remote in _entities.Values)
            {
                remote.Update(deltaTime, positionLerpSpeed, rotationLerpSpeed, teleportThreshold);
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
                remote = new RemoteEntity(avatar);
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

        private sealed class RemoteEntity
        {
            private readonly GameObject _root;
            private readonly Transform _transform;
            private Vector3 _targetPosition;
            private Quaternion _targetRotation = Quaternion.identity;
            private bool _hasTarget;
            private bool _hasRotation;

            public RemoteEntity(GameObject root)
            {
                _root = root;
                _transform = root.transform;
                _targetPosition = _transform.position;
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

                if (immediate && _hasTarget)
                {
                    _transform.position = _targetPosition;
                }

                if (immediate && _hasRotation)
                {
                    _transform.rotation = _targetRotation;
                }
            }

            public void Update(float deltaTime, float positionSpeed, float rotationSpeed, float teleportThreshold)
            {
                if (!_hasTarget)
                {
                    return;
                }

                var current = _transform.position;
                var distance = Vector3.Distance(current, _targetPosition);

                if (teleportThreshold > 0f && distance > teleportThreshold)
                {
                    _transform.position = _targetPosition;
                }
                else
                {
                    var t = positionSpeed <= 0f ? 1f : Mathf.Clamp01(positionSpeed * deltaTime);
                    _transform.position = Vector3.Lerp(current, _targetPosition, t);
                }

                if (_hasRotation)
                {
                    var tRot = rotationSpeed <= 0f ? 1f : Mathf.Clamp01(rotationSpeed * deltaTime);
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

