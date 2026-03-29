using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SharedProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// 권한 관리 시스템 - 역할 기반 접근 제어 (RBAC)
    /// </summary>
    public class PermissionSystem
    {
        // 플레이어별 역할 저장
        private readonly ConcurrentDictionary<string, PlayerRole> _playerRoles = new();

        // 역할별 권한 매핑
        private readonly Dictionary<PlayerRole, HashSet<Permission>> _rolePermissions;

        /// <summary>
        /// 플레이어 역할
        /// </summary>
        public enum PlayerRole
        {
            Guest = 0,          // 관전자/게스트
            Player = 1,         // 일반 플레이어
            VIP = 2,            // VIP 플레이어
            Moderator = 3,      // 중재자
            Admin = 4,          // 관리자
            Owner = 5           // 서버 소유자
        }

        /// <summary>
        /// 권한 종류
        /// </summary>
        public enum Permission
        {
            // 기본 권한
            Chat,
            Move,
            Interact,

            // 월드 편집
            PlaceBlock,
            BreakBlock,
            UseItems,

            // 고급 기능
            TeleportSelf,
            TeleportOthers,
            ChangeGameMode,
            SpawnItems,
            SpawnEntities,

            // 중재 권한
            KickPlayer,
            BanPlayer,
            MutePlayer,
            UnbanPlayer,

            // 관리 권한
            ManageRoles,
            ServerCommands,
            ConfigAccess,
            DatabaseAccess,

            // 특수 권한
            Fly,
            NoClip,
            Invincible,
            BypassLimits
        }

        public PermissionSystem()
        {
            _rolePermissions = InitializeRolePermissions();
        }

        /// <summary>
        /// 역할별 기본 권한 초기화
        /// </summary>
        private Dictionary<PlayerRole, HashSet<Permission>> InitializeRolePermissions()
        {
            return new Dictionary<PlayerRole, HashSet<Permission>>
            {
                // Guest - 최소 권한
                [PlayerRole.Guest] = new HashSet<Permission>
                {
                    Permission.Move,
                    Permission.Interact
                },

                // Player - 일반 플레이어
                [PlayerRole.Player] = new HashSet<Permission>
                {
                    Permission.Chat,
                    Permission.Move,
                    Permission.Interact,
                    Permission.PlaceBlock,
                    Permission.BreakBlock,
                    Permission.UseItems
                },

                // VIP - 추가 특전
                [PlayerRole.VIP] = new HashSet<Permission>
                {
                    Permission.Chat,
                    Permission.Move,
                    Permission.Interact,
                    Permission.PlaceBlock,
                    Permission.BreakBlock,
                    Permission.UseItems,
                    Permission.TeleportSelf,
                    Permission.Fly
                },

                // Moderator - 중재 권한
                [PlayerRole.Moderator] = new HashSet<Permission>
                {
                    Permission.Chat,
                    Permission.Move,
                    Permission.Interact,
                    Permission.PlaceBlock,
                    Permission.BreakBlock,
                    Permission.UseItems,
                    Permission.TeleportSelf,
                    Permission.TeleportOthers,
                    Permission.Fly,
                    Permission.KickPlayer,
                    Permission.MutePlayer,
                    Permission.SpawnItems,
                    Permission.SpawnEntities
                },

                // Admin - 대부분의 권한
                [PlayerRole.Admin] = new HashSet<Permission>
                {
                    Permission.Chat,
                    Permission.Move,
                    Permission.Interact,
                    Permission.PlaceBlock,
                    Permission.BreakBlock,
                    Permission.UseItems,
                    Permission.TeleportSelf,
                    Permission.TeleportOthers,
                    Permission.ChangeGameMode,
                    Permission.SpawnItems,
                    Permission.SpawnEntities,
                    Permission.Fly,
                    Permission.NoClip,
                    Permission.Invincible,
                    Permission.KickPlayer,
                    Permission.BanPlayer,
                    Permission.MutePlayer,
                    Permission.UnbanPlayer,
                    Permission.ServerCommands,
                    Permission.BypassLimits
                },

                // Owner - 모든 권한
                [PlayerRole.Owner] = new HashSet<Permission>(Enum.GetValues<Permission>())
            };
        }

        /// <summary>
        /// 플레이어 역할 설정
        /// </summary>
        public void SetPlayerRole(string playerName, PlayerRole role)
        {
            _playerRoles[playerName] = role;
            Console.WriteLine($"[PermissionSystem] {playerName} role set to {role}");
        }

        /// <summary>
        /// 플레이어 역할 조회
        /// </summary>
        public PlayerRole GetPlayerRole(string playerName)
        {
            return _playerRoles.GetOrAdd(playerName, _ => PlayerRole.Player);
        }

        /// <summary>
        /// 권한 확인
        /// </summary>
        public bool HasPermission(string playerName, Permission permission)
        {
            var role = GetPlayerRole(playerName);
            return _rolePermissions.TryGetValue(role, out var permissions) && permissions.Contains(permission);
        }

        /// <summary>
        /// 다중 권한 확인 (하나라도 있으면 true)
        /// </summary>
        public bool HasAnyPermission(string playerName, params Permission[] permissions)
        {
            var role = GetPlayerRole(playerName);
            if (!_rolePermissions.TryGetValue(role, out var rolePerms))
            {
                return false;
            }

            return permissions.Any(p => rolePerms.Contains(p));
        }

        /// <summary>
        /// 모든 권한 확인 (모두 있어야 true)
        /// </summary>
        public bool HasAllPermissions(string playerName, params Permission[] permissions)
        {
            var role = GetPlayerRole(playerName);
            if (!_rolePermissions.TryGetValue(role, out var rolePerms))
            {
                return false;
            }

            return permissions.All(p => rolePerms.Contains(p));
        }

        /// <summary>
        /// 블록 상호작용 권한 검증
        /// </summary>
        public bool CanModifyBlock(string playerName, Vector3Int blockPosition, string worldId)
        {
            var role = GetPlayerRole(playerName);

            // 관리자는 모든 곳에서 편집 가능
            if (role >= PlayerRole.Admin)
            {
                return true;
            }

            // 일반 플레이어는 기본 권한 필요
            if (!HasPermission(playerName, Permission.PlaceBlock) && !HasPermission(playerName, Permission.BreakBlock))
            {
                return false;
            }

            // TODO: 지역 보호 시스템 (Protected Regions)
            // - 스폰 지역 보호
            // - 플레이어별 영역 보호
            // - 특수 지역 (PvP 구역, 안전 지대 등)

            return true;
        }

        /// <summary>
        /// 명령어 실행 권한 검증
        /// </summary>
        public bool CanExecuteCommand(string playerName, string command)
        {
            var role = GetPlayerRole(playerName);

            // 명령어별 최소 필요 역할
            var commandPermissions = new Dictionary<string, PlayerRole>
            {
                // 플레이어 명령어
                ["help"] = PlayerRole.Player,
                ["spawn"] = PlayerRole.Player,
                ["home"] = PlayerRole.Player,
                ["sethome"] = PlayerRole.Player,

                // VIP 명령어
                ["tpa"] = PlayerRole.VIP,
                ["fly"] = PlayerRole.VIP,

                // 중재자 명령어
                ["kick"] = PlayerRole.Moderator,
                ["mute"] = PlayerRole.Moderator,
                ["warn"] = PlayerRole.Moderator,
                ["tp"] = PlayerRole.Moderator,
                ["give"] = PlayerRole.Moderator,

                // 관리자 명령어
                ["ban"] = PlayerRole.Admin,
                ["unban"] = PlayerRole.Admin,
                ["gamemode"] = PlayerRole.Admin,
                ["time"] = PlayerRole.Admin,
                ["weather"] = PlayerRole.Admin,
                ["setblock"] = PlayerRole.Admin,

                // 소유자 전용
                ["stop"] = PlayerRole.Owner,
                ["reload"] = PlayerRole.Owner,
                ["op"] = PlayerRole.Owner,
                ["deop"] = PlayerRole.Owner
            };

            if (commandPermissions.TryGetValue(command.ToLower(), out var requiredRole))
            {
                return role >= requiredRole;
            }

            // 등록되지 않은 명령어는 기본적으로 Player 권한 필요
            return role >= PlayerRole.Player;
        }

        /// <summary>
        /// 플레이어 권한 목록 조회
        /// </summary>
        public List<Permission> GetPlayerPermissions(string playerName)
        {
            var role = GetPlayerRole(playerName);
            if (_rolePermissions.TryGetValue(role, out var permissions))
            {
                return permissions.ToList();
            }
            return new List<Permission>();
        }

        /// <summary>
        /// 플레이어 데이터 제거 (로그아웃 시)
        /// </summary>
        public void ClearPlayerData(string playerName)
        {
            _playerRoles.TryRemove(playerName, out _);
        }

        /// <summary>
        /// 통계 조회
        /// </summary>
        public PermissionStatistics GetStatistics()
        {
            var stats = new PermissionStatistics();

            foreach (var kvp in _playerRoles)
            {
                stats.TotalPlayers++;

                if (!stats.PlayersByRole.ContainsKey(kvp.Value))
                {
                    stats.PlayersByRole[kvp.Value] = 0;
                }
                stats.PlayersByRole[kvp.Value]++;
            }

            return stats;
        }

        public class PermissionStatistics
        {
            public int TotalPlayers { get; set; }
            public Dictionary<PlayerRole, int> PlayersByRole { get; set; } = new();
        }
    }
}
