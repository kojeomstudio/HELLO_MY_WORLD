using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SharedProtocol;
using ServerVector3 = GameServerApp.Vector3;
using ProtoVector3 = SharedProtocol.Vector3;

namespace GameServerApp.Systems
{
    /// <summary>
    /// 인게임 명령어 시스템 - GM 명령어, 플레이어 명령어 처리
    /// </summary>
    public class CommandSystem
    {
        private readonly Dictionary<string, ICommand> _commands = new();
        private readonly PermissionSystem _permissionSystem;

        public CommandSystem(PermissionSystem permissionSystem)
        {
            _permissionSystem = permissionSystem;
            RegisterDefaultCommands();
        }

        /// <summary>
        /// 명령어 인터페이스
        /// </summary>
        public interface ICommand
        {
            string Name { get; }
            string Description { get; }
            string Usage { get; }
            PermissionSystem.Permission RequiredPermission { get; }
            Task<CommandResult> ExecuteAsync(CommandContext context);
        }

        /// <summary>
        /// 명령어 컨텍스트
        /// </summary>
        public class CommandContext
        {
            public string PlayerName { get; set; } = string.Empty;
            public string[] Args { get; set; } = Array.Empty<string>();
            public Session Session { get; set; } = null!;
            public SessionManager SessionManager { get; set; } = null!;
            public object? GameServer { get; set; }
        }

        /// <summary>
        /// 명령어 실행 결과
        /// </summary>
        public class CommandResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public bool ShouldBroadcast { get; set; }
        }

        /// <summary>
        /// 명령어 파싱 및 실행
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string playerName, string commandText, Session session, SessionManager sessionManager)
        {
            // 명령어 파싱
            if (string.IsNullOrWhiteSpace(commandText) || !commandText.StartsWith("/"))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "Invalid command format. Commands must start with /"
                };
            }

            var parts = commandText[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return new CommandResult
                {
                    Success = false,
                    Message = "No command specified"
                };
            }

            var commandName = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            // 명령어 조회
            if (!_commands.TryGetValue(commandName, out var command))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"Unknown command: {commandName}. Type /help for a list of commands."
                };
            }

            // 권한 확인
            if (!_permissionSystem.HasPermission(playerName, command.RequiredPermission))
            {
                return new CommandResult
                {
                    Success = false,
                    Message = $"You don't have permission to use /{commandName}"
                };
            }

            // 컨텍스트 생성
            var context = new CommandContext
            {
                PlayerName = playerName,
                Args = args,
                Session = session,
                SessionManager = sessionManager
            };

            // 명령어 실행
            try
            {
                return await command.ExecuteAsync(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CommandSystem] Error executing command /{commandName}: {ex.Message}");
                return new CommandResult
                {
                    Success = false,
                    Message = $"Error executing command: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 명령어 등록
        /// </summary>
        public void RegisterCommand(ICommand command)
        {
            _commands[command.Name.ToLower()] = command;
            Console.WriteLine($"[CommandSystem] Registered command: /{command.Name}");
        }

        /// <summary>
        /// 기본 명령어 등록
        /// </summary>
        private void RegisterDefaultCommands()
        {
            // 플레이어 명령어
            RegisterCommand(new HelpCommand());
            RegisterCommand(new SpawnCommand());
            RegisterCommand(new TpaCommand());
            RegisterCommand(new TpAcceptCommand());

            // 중재자 명령어
            RegisterCommand(new TeleportCommand());
            RegisterCommand(new GiveCommand());
            RegisterCommand(new KickCommand());

            // 관리자 명령어
            RegisterCommand(new GameModeCommand());
            RegisterCommand(new TimeCommand());
            RegisterCommand(new WeatherCommand());
            RegisterCommand(new BanCommand());
            RegisterCommand(new UnbanCommand());
        }

        // ===== 명령어 구현 =====

        /// <summary>
        /// /help - 명령어 목록 표시
        /// </summary>
        private class HelpCommand : ICommand
        {
            public string Name => "help";
            public string Description => "Display available commands";
            public string Usage => "/help [command]";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.Chat;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                var message = "Available commands:\n" +
                              "/help - This help message\n" +
                              "/spawn - Teleport to spawn\n" +
                              "/tpa <player> - Request teleport to player\n" +
                              "/tp <player> - Teleport to player (Moderator)\n" +
                              "/give <item> <amount> - Give items (Moderator)\n" +
                              "/gamemode <mode> - Change game mode (Admin)\n" +
                              "/time <set|add> <value> - Set time (Admin)\n" +
                              "/weather <clear|rain|thunder> - Set weather (Admin)\n" +
                              "/kick <player> - Kick player (Moderator)\n" +
                              "/ban <player> - Ban player (Admin)";

                return Task.FromResult(new CommandResult { Success = true, Message = message });
            }
        }

        /// <summary>
        /// /spawn - 스폰 지점으로 이동
        /// </summary>
        private class SpawnCommand : ICommand
        {
            public string Name => "spawn";
            public string Description => "Teleport to spawn point";
            public string Usage => "/spawn";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.TeleportSelf;

            public async Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                var spawnPosition = new ServerVector3 { X = 0, Y = 70, Z = 0 }; // 기본 스폰

                // 플레이어 위치 업데이트
                var playerState = context.SessionManager.GetPlayerState(context.PlayerName);
                if (playerState != null)
                {
                    playerState.Position = spawnPosition;
                }

                // 위치 브로드캐스트
                var moveResponse = new MoveResponse
                {
                    Success = true,
                    NewPosition = new ProtoVector3
                    {
                        X = (float)spawnPosition.X,
                        Y = (float)spawnPosition.Y,
                        Z = (float)spawnPosition.Z
                    },
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await context.Session.SendAsync(MessageType.MoveResponse, moveResponse);

                return new CommandResult
                {
                    Success = true,
                    Message = "Teleported to spawn"
                };
            }
        }

        /// <summary>
        /// /tpa - 플레이어에게 텔레포트 요청
        /// </summary>
        private class TpaCommand : ICommand
        {
            public string Name => "tpa";
            public string Description => "Request teleport to another player";
            public string Usage => "/tpa <player>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.TeleportSelf;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /tpa <player>" });
                }

                var targetPlayer = context.Args[0];

                // TODO: 텔레포트 요청 시스템 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Teleport request sent to {targetPlayer}"
                });
            }
        }

        /// <summary>
        /// /tpaccept - 텔레포트 요청 수락
        /// </summary>
        private class TpAcceptCommand : ICommand
        {
            public string Name => "tpaccept";
            public string Description => "Accept teleport request";
            public string Usage => "/tpaccept";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.TeleportSelf;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                // TODO: 텔레포트 요청 수락 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = "Teleport request accepted"
                });
            }
        }

        /// <summary>
        /// /tp - 플레이어에게 강제 텔레포트
        /// </summary>
        private class TeleportCommand : ICommand
        {
            public string Name => "tp";
            public string Description => "Teleport to another player";
            public string Usage => "/tp <player>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.TeleportOthers;

            public async Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return new CommandResult { Success = false, Message = "Usage: /tp <player>" };
                }

                var targetPlayer = context.Args[0];
                var targetState = context.SessionManager.GetPlayerState(targetPlayer);

                if (targetState == null)
                {
                    return new CommandResult { Success = false, Message = $"Player {targetPlayer} not found" };
                }

                // 플레이어 텔레포트
                var playerState = context.SessionManager.GetPlayerState(context.PlayerName);
                if (playerState != null)
                {
                    playerState.Position = targetState.Position;
                }

                var moveResponse = new MoveResponse
                {
                    Success = true,
                    NewPosition = new ProtoVector3
                    {
                        X = (float)targetState.Position.X,
                        Y = (float)targetState.Position.Y,
                        Z = (float)targetState.Position.Z
                    },
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await context.Session.SendAsync(MessageType.MoveResponse, moveResponse);

                return new CommandResult
                {
                    Success = true,
                    Message = $"Teleported to {targetPlayer}"
                };
            }
        }

        /// <summary>
        /// /give - 아이템 지급
        /// </summary>
        private class GiveCommand : ICommand
        {
            public string Name => "give";
            public string Description => "Give items to player";
            public string Usage => "/give <player> <item> <amount>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.SpawnItems;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length < 3)
                {
                    return Task.FromResult(new CommandResult
                    {
                        Success = false,
                        Message = "Usage: /give <player> <item> <amount>"
                    });
                }

                var targetPlayer = context.Args[0];
                var itemName = context.Args[1];
                var amount = int.TryParse(context.Args[2], out var amt) ? amt : 1;

                // TODO: 실제 인벤토리 시스템과 연동
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Gave {amount}x {itemName} to {targetPlayer}",
                    ShouldBroadcast = true
                });
            }
        }

        /// <summary>
        /// /kick - 플레이어 강퇴
        /// </summary>
        private class KickCommand : ICommand
        {
            public string Name => "kick";
            public string Description => "Kick a player from the server";
            public string Usage => "/kick <player> [reason]";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.KickPlayer;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /kick <player> [reason]" });
                }

                var targetPlayer = context.Args[0];
                var reason = context.Args.Length > 1 ? string.Join(" ", context.Args.Skip(1)) : "Kicked by moderator";

                // TODO: 실제 킥 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Kicked {targetPlayer}: {reason}",
                    ShouldBroadcast = true
                });
            }
        }

        /// <summary>
        /// /gamemode - 게임 모드 변경
        /// </summary>
        private class GameModeCommand : ICommand
        {
            public string Name => "gamemode";
            public string Description => "Change game mode";
            public string Usage => "/gamemode <survival|creative|adventure> [player]";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.ChangeGameMode;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /gamemode <mode> [player]" });
                }

                var mode = context.Args[0].ToLower();
                var targetPlayer = context.Args.Length > 1 ? context.Args[1] : context.PlayerName;

                // TODO: 게임 모드 시스템 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Set {targetPlayer}'s game mode to {mode}"
                });
            }
        }

        /// <summary>
        /// /time - 시간 설정
        /// </summary>
        private class TimeCommand : ICommand
        {
            public string Name => "time";
            public string Description => "Set world time";
            public string Usage => "/time <set|add> <value>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.ServerCommands;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length < 2)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /time <set|add> <value>" });
                }

                // TODO: WorldTimeSystem과 연동
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Time {context.Args[0]} to {context.Args[1]}"
                });
            }
        }

        /// <summary>
        /// /weather - 날씨 설정
        /// </summary>
        private class WeatherCommand : ICommand
        {
            public string Name => "weather";
            public string Description => "Set weather";
            public string Usage => "/weather <clear|rain|thunder>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.ServerCommands;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /weather <clear|rain|thunder>" });
                }

                // TODO: WeatherSystem과 연동
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Weather set to {context.Args[0]}"
                });
            }
        }

        /// <summary>
        /// /ban - 플레이어 차단
        /// </summary>
        private class BanCommand : ICommand
        {
            public string Name => "ban";
            public string Description => "Ban a player";
            public string Usage => "/ban <player> [reason]";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.BanPlayer;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /ban <player> [reason]" });
                }

                var targetPlayer = context.Args[0];
                var reason = context.Args.Length > 1 ? string.Join(" ", context.Args.Skip(1)) : "Banned by admin";

                // TODO: 실제 밴 시스템 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Banned {targetPlayer}: {reason}",
                    ShouldBroadcast = true
                });
            }
        }

        /// <summary>
        /// /unban - 플레이어 차단 해제
        /// </summary>
        private class UnbanCommand : ICommand
        {
            public string Name => "unban";
            public string Description => "Unban a player";
            public string Usage => "/unban <player>";
            public PermissionSystem.Permission RequiredPermission => PermissionSystem.Permission.UnbanPlayer;

            public Task<CommandResult> ExecuteAsync(CommandContext context)
            {
                if (context.Args.Length == 0)
                {
                    return Task.FromResult(new CommandResult { Success = false, Message = "Usage: /unban <player>" });
                }

                var targetPlayer = context.Args[0];

                // TODO: 실제 언밴 시스템 구현
                return Task.FromResult(new CommandResult
                {
                    Success = true,
                    Message = $"Unbanned {targetPlayer}"
                });
            }
        }
    }
}
