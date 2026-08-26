using System;

namespace QuickAccessHub.Models
{
    public class QuickItem
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "File"; // "File", "Folder", "Url"
        public string? Path { get; set; }
        public string? Url { get; set; }
        public long? CategoryId { get; set; }
        public string CategoryName { get; set; } = "General";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Dynamic UI status property
        public bool IsMissing { get; set; }

        public string TypeIcon => Type switch
        {
            "File" => "📄",
            "Folder" => "📁",
            "Url" => "🔗",
            _ => "📌"
        };

        public string StatusBadge => IsMissing ? "⚠ Missing" : "";

        public string PreviewText => Type switch
        {
            "Url" => Url ?? "",
            _ => Path ?? ""
        };

        public bool IsUrl => Type == "Url";
        public bool IsFileOrFolder => Type == "File" || Type == "Folder";
    }
}
