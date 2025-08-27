using System.Collections.Generic;
using System;

namespace GameServerApp.Models
{
    /// <summary>
    /// 게임 캐릭터 모델
    /// 플레이어의 기본 정보, 위치, 레벨, 체력, 인벤토리 등을 포함합니다.
    /// </summary>
    public class Character
    {
        public string Name { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        
        // 인증 관련 (보안)
        public string PasswordHash { get; set; } = string.Empty;
        public string Salt { get; set; } = string.Empty;
        
        // 게임 스탯
        public int Level { get; set; } = 1;
        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        
        // 인벤토리
        public List<Item> Inventory { get; } = new();
        
        // 생성/로그인 시간
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        public Character(string name, double x = 0, double y = 0, double z = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Character name cannot be empty", nameof(name));
                
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }
        
        /// <summary>
        /// 아이템을 인벤토리에 추가합니다.
        /// </summary>
        public void AddItem(int itemId, string itemName, int quantity = 1)
        {
            var existingItem = Inventory.FirstOrDefault(i => i.Id == itemId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Inventory.Add(new Item(itemId, itemName) { Quantity = quantity });
            }
        }
        
        /// <summary>
        /// 인벤토리에서 아이템을 제거합니다.
        /// </summary>
        public bool RemoveItem(int itemId, int quantity = 1)
        {
            var existingItem = Inventory.FirstOrDefault(i => i.Id == itemId);
            if (existingItem == null || existingItem.Quantity < quantity)
                return false;
                
            existingItem.Quantity -= quantity;
            if (existingItem.Quantity <= 0)
            {
                Inventory.Remove(existingItem);
            }
            
            return true;
        }
        
        /// <summary>
        /// 플레이어가 위치 거리 내에 있는지 확인합니다.
        /// </summary>
        public bool IsWithinRange(Character other, double range)
        {
            if (other == null) return false;
            
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            var distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            
            return distance <= range;
        }
        
        /// <summary>
        /// 플레이어의 상태를 업데이트합니다.
        /// </summary>
        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }
        
        public override string ToString()
        {
            return $"Character[{Name}] Level:{Level} HP:{Health}/{MaxHealth} Pos:({X:F1},{Y:F1},{Z:F1})";
        }
    }
}
