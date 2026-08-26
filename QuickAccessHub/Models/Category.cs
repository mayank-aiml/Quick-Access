namespace QuickAccessHub.Models
{
    public class Category
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        public override string ToString() => Name;
    }
}
