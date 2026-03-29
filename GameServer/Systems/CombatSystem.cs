using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SharedProtocol;

namespace GameServerApp.Systems
{
    /// <summary>
    /// 전투 시스템 - PvP/PvE 공격, 무기 시스템, 방어구 시스템
    /// </summary>
    public class CombatSystem
    {
        // 공격 쿨다운
        private const float BaseAttackCooldown = 0.5f; // 초
        private const float MaxReach = 4.0f; // 공격 최대 거리

        // 플레이어별 전투 데이터
        private readonly ConcurrentDictionary<string, PlayerCombatData> _playerCombatData = new();

        /// <summary>
        /// 플레이어 전투 데이터
        /// </summary>
        private class PlayerCombatData
        {
            public DateTime LastAttackTime { get; set; } = DateTime.MinValue;
            public string? LastAttacker { get; set; }
            public DateTime LastDamageTime { get; set; } = DateTime.MinValue;
            public Dictionary<DamageType, float> DamageResistance { get; set; } = new();
            public float CriticalChance { get; set; } = 0.05f; // 5% 기본 크리티컬
        }

        /// <summary>
        /// 데미지 타입
        /// </summary>
        public enum DamageType
        {
            Melee = 0,
            Ranged = 1,
            Magic = 2,
            Fire = 3,
            Explosion = 4,
            Fall = 5,
            Suffocation = 6,
            Drowning = 7,
            Poison = 8,
            Wither = 9,
            Void = 10,
            Starvation = 11
        }

        /// <summary>
        /// 무기 종류
        /// </summary>
        public enum WeaponType
        {
            None,
            Sword,
            Axe,
            Pickaxe,
            Bow,
            Crossbow,
            Trident
        }

        /// <summary>
        /// 무기 스탯
        /// </summary>
        public class WeaponStats
        {
            public WeaponType Type { get; set; }
            public float BaseDamage { get; set; }
            public float AttackSpeed { get; set; } // 공격 속도 배율
            public float CriticalBonus { get; set; } = 0.5f; // 크리티컬 시 추가 데미지
            public DamageType DamageType { get; set; } = DamageType.Melee;
            public int Durability { get; set; }
            public int MaxDurability { get; set; }
        }

        /// <summary>
        /// 공격 결과
        /// </summary>
        public class AttackResult
        {
            public bool Success { get; set; }
            public float Damage { get; set; }
            public bool IsCritical { get; set; }
            public bool IsBlocked { get; set; }
            public string Message { get; set; } = string.Empty;
            public float Knockback { get; set; }
        }

        /// <summary>
        /// 플레이어 공격 처리
        /// </summary>
        public AttackResult ProcessPlayerAttack(string attackerName, string targetName, Vector3 attackerPos, Vector3 targetPos, WeaponStats? weapon = null)
        {
            var result = new AttackResult();

            // 1. 공격 쿨다운 확인
            var attackerData = _playerCombatData.GetOrAdd(attackerName, _ => new PlayerCombatData());
            var now = DateTime.UtcNow;
            var timeSinceLastAttack = (now - attackerData.LastAttackTime).TotalSeconds;

            var attackSpeed = weapon?.AttackSpeed ?? 1.0f;
            var cooldown = BaseAttackCooldown / attackSpeed;

            if (timeSinceLastAttack < cooldown)
            {
                result.Success = false;
                result.Message = $"Attack on cooldown ({cooldown - timeSinceLastAttack:F2}s remaining)";
                return result;
            }

            // 2. 거리 확인
            var distance = GetDistance(attackerPos, targetPos);
            if (distance > MaxReach)
            {
                result.Success = false;
                result.Message = $"Target too far ({distance:F2}m, max {MaxReach}m)";
                return result;
            }

            // 3. 기본 데미지 계산
            float baseDamage = weapon?.BaseDamage ?? 1.0f;

            // 4. 크리티컬 판정
            var random = new Random();
            var critChance = attackerData.CriticalChance + (weapon?.CriticalBonus ?? 0);
            result.IsCritical = random.NextDouble() < critChance;

            if (result.IsCritical)
            {
                baseDamage *= 1.5f; // 크리티컬 시 1.5배
            }

            // 5. 방어구 계산 (타겟)
            var targetData = _playerCombatData.GetOrAdd(targetName, _ => new PlayerCombatData());
            var damageType = weapon?.DamageType ?? DamageType.Melee;

            float resistance = 0f;
            if (targetData.DamageResistance.TryGetValue(damageType, out var res))
            {
                resistance = res;
            }

            float finalDamage = baseDamage * (1.0f - resistance);

            // 6. 블록 판정 (방패 사용 시)
            // TODO: 방패 아이템 체크
            result.IsBlocked = false; // 현재는 미구현

            if (result.IsBlocked)
            {
                finalDamage *= 0.33f; // 블록 시 66% 감소
            }

            // 7. 넉백 계산
            result.Knockback = CalculateKnockback(baseDamage, result.IsCritical);

            // 8. 쿨다운 업데이트
            attackerData.LastAttackTime = now;
            targetData.LastAttacker = attackerName;
            targetData.LastDamageTime = now;

            result.Success = true;
            result.Damage = finalDamage;
            result.Message = result.IsCritical ? "Critical Hit!" : "Hit";

            return result;
        }

        /// <summary>
        /// AI 공격 처리
        /// </summary>
        public AttackResult ProcessAIAttack(string aiId, string targetPlayerName, Vector3 aiPos, Vector3 playerPos, float aiBaseDamage = 2.0f)
        {
            var result = new AttackResult();

            // AI는 쿨다운 체크 없음 (AI Manager에서 관리)

            // 거리 확인
            var distance = GetDistance(aiPos, playerPos);
            if (distance > MaxReach)
            {
                result.Success = false;
                result.Message = "Target out of range";
                return result;
            }

            // 기본 데미지 적용
            var targetData = _playerCombatData.GetOrAdd(targetPlayerName, _ => new PlayerCombatData());

            float resistance = 0f;
            if (targetData.DamageResistance.TryGetValue(DamageType.Melee, out var res))
            {
                resistance = res;
            }

            float finalDamage = aiBaseDamage * (1.0f - resistance);

            result.Success = true;
            result.Damage = finalDamage;
            result.Knockback = CalculateKnockback(aiBaseDamage, false);
            result.Message = $"AI {aiId} attacked for {finalDamage:F1} damage";

            targetData.LastAttacker = aiId;
            targetData.LastDamageTime = DateTime.UtcNow;

            return result;
        }

        /// <summary>
        /// 넉백 계산
        /// </summary>
        private float CalculateKnockback(float damage, bool isCritical)
        {
            float knockback = damage * 0.4f;

            if (isCritical)
            {
                knockback *= 1.5f; // 크리티컬 시 넉백 증가
            }

            return Math.Min(knockback, 5.0f); // 최대 넉백 제한
        }

        /// <summary>
        /// 방어구 장착 (방어력 적용)
        /// </summary>
        public void EquipArmor(string playerName, ArmorSet armor)
        {
            var data = _playerCombatData.GetOrAdd(playerName, _ => new PlayerCombatData());

            // 방어구에 따른 저항력 설정
            data.DamageResistance[DamageType.Melee] = armor.MeleeResistance;
            data.DamageResistance[DamageType.Ranged] = armor.RangedResistance;
            data.DamageResistance[DamageType.Explosion] = armor.ExplosionResistance;
            data.DamageResistance[DamageType.Fire] = armor.FireResistance;
        }

        /// <summary>
        /// 방어구 세트
        /// </summary>
        public class ArmorSet
        {
            public string Name { get; set; } = string.Empty;
            public float MeleeResistance { get; set; }      // 0.0 ~ 1.0 (0% ~ 100%)
            public float RangedResistance { get; set; }
            public float ExplosionResistance { get; set; }
            public float FireResistance { get; set; }
            public int Durability { get; set; }
        }

        /// <summary>
        /// 미리 정의된 무기 스탯
        /// </summary>
        public static WeaponStats GetWeaponStats(string weaponName)
        {
            return weaponName.ToLower() switch
            {
                "wooden_sword" => new WeaponStats { Type = WeaponType.Sword, BaseDamage = 4f, AttackSpeed = 1.6f, MaxDurability = 60 },
                "stone_sword" => new WeaponStats { Type = WeaponType.Sword, BaseDamage = 5f, AttackSpeed = 1.6f, MaxDurability = 132 },
                "iron_sword" => new WeaponStats { Type = WeaponType.Sword, BaseDamage = 6f, AttackSpeed = 1.6f, MaxDurability = 251 },
                "diamond_sword" => new WeaponStats { Type = WeaponType.Sword, BaseDamage = 7f, AttackSpeed = 1.6f, MaxDurability = 1562 },

                "wooden_axe" => new WeaponStats { Type = WeaponType.Axe, BaseDamage = 7f, AttackSpeed = 0.8f, MaxDurability = 60 },
                "stone_axe" => new WeaponStats { Type = WeaponType.Axe, BaseDamage = 9f, AttackSpeed = 0.8f, MaxDurability = 132 },
                "iron_axe" => new WeaponStats { Type = WeaponType.Axe, BaseDamage = 9f, AttackSpeed = 0.9f, MaxDurability = 251 },
                "diamond_axe" => new WeaponStats { Type = WeaponType.Axe, BaseDamage = 9f, AttackSpeed = 1.0f, MaxDurability = 1562 },

                "bow" => new WeaponStats { Type = WeaponType.Bow, BaseDamage = 9f, AttackSpeed = 0.8f, DamageType = DamageType.Ranged, MaxDurability = 385 },
                "crossbow" => new WeaponStats { Type = WeaponType.Crossbow, BaseDamage = 11f, AttackSpeed = 0.5f, DamageType = DamageType.Ranged, MaxDurability = 465 },

                _ => new WeaponStats { Type = WeaponType.None, BaseDamage = 1f, AttackSpeed = 1.0f, MaxDurability = 0 }
            };
        }

        /// <summary>
        /// 미리 정의된 방어구 스탯
        /// </summary>
        public static ArmorSet GetArmorStats(string armorName)
        {
            return armorName.ToLower() switch
            {
                "leather" => new ArmorSet { Name = "Leather", MeleeResistance = 0.28f, RangedResistance = 0.28f, ExplosionResistance = 0.14f, FireResistance = 0.0f, Durability = 80 },
                "chainmail" => new ArmorSet { Name = "Chainmail", MeleeResistance = 0.48f, RangedResistance = 0.48f, ExplosionResistance = 0.24f, FireResistance = 0.0f, Durability = 240 },
                "iron" => new ArmorSet { Name = "Iron", MeleeResistance = 0.60f, RangedResistance = 0.60f, ExplosionResistance = 0.30f, FireResistance = 0.0f, Durability = 240 },
                "diamond" => new ArmorSet { Name = "Diamond", MeleeResistance = 0.80f, RangedResistance = 0.80f, ExplosionResistance = 0.40f, FireResistance = 0.20f, Durability = 528 },
                _ => new ArmorSet { Name = "None", MeleeResistance = 0.0f, RangedResistance = 0.0f, ExplosionResistance = 0.0f, FireResistance = 0.0f, Durability = 0 }
            };
        }

        /// <summary>
        /// 전투 통계 조회
        /// </summary>
        public CombatStatistics GetStatistics()
        {
            var stats = new CombatStatistics
            {
                TotalPlayers = _playerCombatData.Count
            };

            var now = DateTime.UtcNow;

            foreach (var kvp in _playerCombatData)
            {
                if ((now - kvp.Value.LastAttackTime).TotalMinutes < 5)
                {
                    stats.ActiveCombatants++;
                }
            }

            return stats;
        }

        /// <summary>
        /// 플레이어 데이터 제거
        /// </summary>
        public void ClearPlayerData(string playerName)
        {
            _playerCombatData.TryRemove(playerName, out _);
        }

        /// <summary>
        /// 거리 계산
        /// </summary>
        private float GetDistance(Vector3 a, Vector3 b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public class CombatStatistics
        {
            public int TotalPlayers { get; set; }
            public int ActiveCombatants { get; set; }
        }
    }
}
