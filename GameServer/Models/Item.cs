using System;

namespace GameServerApp.Models
{
    /// <summary>
    /// 게임 아이템 모델
    /// 아이템의 기본 정보와 수량을 관리합니다.
    /// </summary>
    public class Item
    {
        public int Id { get; }
        public string Name { get; }
        public int Quantity { get; set; } = 1;
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; } = ItemType.Misc;
        public int MaxStack { get; set; } = 99; // 최대 스택 개수
        
        public Item(int id, string name)
        {
            if (id < 0)
                throw new ArgumentException("Item ID cannot be negative", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Item name cannot be empty", nameof(name));
                
            Id = id;
            Name = name;
        }
        
        /// <summary>
        /// 아이템을 스택할 수 있는지 확인합니다.
        /// </summary>
        public bool CanStack(int additionalQuantity)
        {
            return Quantity + additionalQuantity <= MaxStack;
        }
        
        /// <summary>
        /// 아이템을 스택합니다.
        /// </summary>
        public bool TryStack(int additionalQuantity)
        {
            if (!CanStack(additionalQuantity)) return false;
            
            Quantity += additionalQuantity;
            return true;
        }
        
        /// <summary>
        /// 아이템을 소모합니다.
        /// </summary>
        public bool TryConsume(int consumeQuantity)
        {
            if (Quantity < consumeQuantity) return false;
            
            Quantity -= consumeQuantity;
            return true;
        }
        
        public override string ToString()
        {
            return $"{Name} x{Quantity}";
        }
        
        public override bool Equals(object obj)
        {
            return obj is Item other && Id == other.Id;
        }
        
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
    
    /// <summary>
    /// 아이템 타입 열거형
    /// </summary>
    public enum ItemType
    {
        Misc = 0,      // 기타
        Block = 1,     // 블록
        Tool = 2,      // 도구
        Weapon = 3,    // 무기
        Armor = 4,     // 방어구
        Consumable = 5, // 소모품
        Resource = 6   // 자원
    }
}
