using System;

namespace MarkdownEditor.Models
{
    public class FileHistoryRecord
    {
        public int Id { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime LastOpenTime { get; set; }
        public DateTime LastModifyTime { get; set; }
        public long FileSize { get; set; }
        public bool IsFavorite { get; set; }
    }
}