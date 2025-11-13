using System;
using System.Threading.Tasks;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// 플레이어 공격 핸들러 - PvP 전투 처리
    /// </summary>
    public class PlayerAttackHandler : MessageHandler<PlayerAttackRequest>
    {
        private readonly CombatSystem _combatSystem;
        private readonly HealthAndHungerSystem _healthSystem;
        private readonly SessionManager _sessionManager;

        public PlayerAttackHandler(CombatSystem combatSystem, HealthAndHungerSystem healthSystem, SessionManager sessionManager)
            : base(MessageType.PlayerAttackRequest)
        {
            _combatSystem = combatSystem;
            _healthSystem = healthSystem;
            _sessionManager = sessionManager;
        }

        protected override async Task HandleAsync(Session session, PlayerAttackRequest message)
        {
            if (string.IsNullOrEmpty(session.UserName))
            {
                await session.SendAsync(MessageType.PlayerAttackResponse, new PlayerAttackResponse
                {
                    Success = false,
                    Message = "Not authenticated",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                return;
            }

            // 공격자 위치 조회
            var attackerState = _sessionManager.GetPlayerState(session.UserName);
            if (attackerState == null)
            {
                await session.SendAsync(MessageType.PlayerAttackResponse, new PlayerAttackResponse
                {
                    Success = false,
                    Message = "Player state not found",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                return;
            }

            // 타겟 위치 조회
            var targetState = _sessionManager.GetPlayerState(message.TargetPlayerName);
            if (targetState == null)
            {
                await session.SendAsync(MessageType.PlayerAttackResponse, new PlayerAttackResponse
                {
                    Success = false,
                    Message = $"Target player {message.TargetPlayerName} not found",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                });
                return;
            }

            // 무기 스탯 조회
            var weaponStats = !string.IsNullOrEmpty(message.WeaponName)
                ? CombatSystem.GetWeaponStats(message.WeaponName)
                : null;

            // 공격 처리
            var attackResult = _combatSystem.ProcessPlayerAttack(
                session.UserName,
                message.TargetPlayerName,
                attackerState.Position,
                targetState.Position,
                weaponStats
            );

            // 응답 전송
            var response = new PlayerAttackResponse
            {
                Success = attackResult.Success,
                Message = attackResult.Message,
                Damage = attackResult.Damage,
                IsCritical = attackResult.IsCritical,
                IsBlocked = attackResult.IsBlocked,
                Knockback = attackResult.Knockback,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await session.SendAsync(MessageType.PlayerAttackResponse, response);

            if (attackResult.Success)
            {
                // 타겟에게 데미지 적용
                var targetSession = _sessionManager.GetSession(message.TargetPlayerName);
                if (targetSession != null)
                {
                    // HealthSystem을 통해 데미지 적용
                    await _healthSystem.ApplyDamageAsync(
                        targetSession,
                        attackResult.Damage,
                        (int)CombatSystem.DamageType.Melee,
                        session.UserName,
                        message.WeaponName
                    );
                }

                // 주변 플레이어에게 브로드캐스트
                var broadcast = new PlayerAttackBroadcast
                {
                    AttackerName = session.UserName,
                    TargetName = message.TargetPlayerName,
                    Damage = attackResult.Damage,
                    IsCritical = attackResult.IsCritical,
                    KnockbackVector = new Vector3(0, attackResult.Knockback, 0),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await _sessionManager.BroadcastToNearbyPlayersAsync(
                    attackerState.Position,
                    128f,
                    MessageType.PlayerAttackBroadcast,
                    broadcast
                );

                Console.WriteLine($"[Combat] {session.UserName} attacked {message.TargetPlayerName} for {attackResult.Damage:F1} damage" +
                                  (attackResult.IsCritical ? " (CRITICAL)" : ""));
            }
        }
    }
}
