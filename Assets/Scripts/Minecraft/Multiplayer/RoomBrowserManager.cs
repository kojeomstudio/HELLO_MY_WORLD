using System.Collections.Generic;
using SharedProtocol;
using UnityEngine;
using Minecraft.Core;

namespace Minecraft.Multiplayer
{
    /// <summary>
    /// Keyboard-driven room browser that integrates with MinecraftGameClient events.
    /// </summary>
    public class RoomBrowserManager : MonoBehaviour
    {
        [Header("Key Bindings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.L;
        [SerializeField] private KeyCode refreshKey = KeyCode.R;
        [SerializeField] private KeyCode joinKey = KeyCode.J;
        [SerializeField] private KeyCode leaveKey = KeyCode.K;

        private MinecraftGameClient _client;
        private RoomBrowserOverlay _overlay;
        private readonly List<RoomInfo> _rooms = new();
        private int _selectedIndex;
        private bool _visible;

        private void Awake()
        {
            _client = FindObjectOfType<MinecraftGameClient>();
            if (_client == null)
            {
                Debug.LogWarning("RoomBrowserManager disabled: MinecraftGameClient not found.");
                enabled = false;
                return;
            }

            var overlayObj = new GameObject("RoomBrowserOverlay");
            DontDestroyOnLoad(overlayObj);
            _overlay = overlayObj.AddComponent<RoomBrowserOverlay>();
            _overlay.Initialize();

            _client.RoomListReceived += OnRoomListReceived;
            _client.RoomEntered += OnRoomEntered;
            _client.RoomLeft += OnRoomLeft;
        }

        private void OnDestroy()
        {
            if (_client != null)
            {
                _client.RoomListReceived -= OnRoomListReceived;
                _client.RoomEntered -= OnRoomEntered;
                _client.RoomLeft -= OnRoomLeft;
            }

            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
            }
        }

        private void Update()
        {
            if (_client == null || !_client.IsConnected)
            {
                return;
            }

            if (Input.GetKeyDown(toggleKey))
            {
                Toggle();
            }

            if (!_visible)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                _selectedIndex = (_selectedIndex - 1 + Mathf.Max(_rooms.Count, 1)) % Mathf.Max(_rooms.Count, 1);
                RefreshOverlay();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                _selectedIndex = (_selectedIndex + 1) % Mathf.Max(_rooms.Count, 1);
                RefreshOverlay();
            }

            if (Input.GetKeyDown(refreshKey))
            {
                RequestRoomList();
            }

            if (Input.GetKeyDown(joinKey))
            {
                JoinSelectedRoom();
            }

            if (Input.GetKeyDown(leaveKey))
            {
                _client.LeaveCurrentRoom();
                _overlay.ShowStatus("Leaving current room...", Color.yellow);
            }
        }

        private void Toggle()
        {
            _visible = !_visible;
            _overlay.SetVisible(_visible);

            if (_visible)
            {
                if (_rooms.Count == 0)
                {
                    RequestRoomList();
                    _overlay.ShowStatus("Fetching room list...", Color.cyan);
                }
                else
                {
                    RefreshOverlay();
                }
            }
        }

        private void RequestRoomList()
        {
            _client.RequestRoomList(includeMembers: false, worldFilter: -1);
        }

        private void JoinSelectedRoom()
        {
            if (_rooms.Count == 0)
            {
                _overlay.ShowStatus("No room selected.", Color.red);
                return;
            }

            var room = _rooms[_selectedIndex];
            _client.EnterRoom(room.RoomId);
            _overlay.ShowStatus($"Joining {room.DisplayName}...", Color.green);
        }

        private void RefreshOverlay()
        {
            if (!_visible)
                return;

            _overlay.ShowRooms(_rooms, _rooms.Count == 0 ? 0 : _selectedIndex);
            if (_rooms.Count > 0)
            {
                var room = _rooms[_selectedIndex];
                _overlay.ShowStatus($"Selected: {room.DisplayName} (Players: {room.PlayerCount}/{(room.Capacity <= 0 ? "∞" : room.Capacity.ToString())})", Color.white);
            }
            else
            {
                _overlay.ShowStatus("No rooms available.", Color.yellow);
            }
        }

        private void OnRoomListReceived(IReadOnlyList<RoomInfo> rooms)
        {
            _rooms.Clear();
            if (rooms != null)
            {
                _rooms.AddRange(rooms);
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _rooms.Count - 1));
            RefreshOverlay();
        }

        private void OnRoomEntered(RoomEnterResponse response)
        {
            if (!_visible)
                return;

            var color = response.Success ? Color.green : Color.red;
            var message = response.Success
                ? $"Entered {response.Room?.DisplayName ?? response.Room?.RoomId ?? "room"}."
                : response.Message;
            _overlay.ShowStatus(message ?? "Room enter response.", color);
            RequestRoomList();
        }

        private void OnRoomLeft(RoomLeaveResponse response)
        {
            if (!_visible)
                return;

            var color = response.Success ? Color.green : Color.red;
            var message = response.Success ? "Returned to lobby." : response.Message;
            _overlay.ShowStatus(message ?? "Room leave response.", color);
            RequestRoomList();
        }
    }
}
