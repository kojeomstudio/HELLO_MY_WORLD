namespace GameServerApp.Models
{
    public class Map
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Map(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
