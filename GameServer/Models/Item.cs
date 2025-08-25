namespace GameServerApp.Models
{
    public class Item
    {
        public int Id { get; }
        public string Name { get; }

        public Item(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
