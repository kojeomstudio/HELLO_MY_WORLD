using System.Collections.Generic;

namespace GameServerApp.Models
{
    public class Character
    {
        public string Name { get; }
        public double X { get; set; }
        public double Y { get; set; }
        public List<Item> Inventory { get; } = new();

        public Character(string name, double x = 0, double y = 0)
        {
            Name = name;
            X = x;
            Y = y;
        }
    }
}
