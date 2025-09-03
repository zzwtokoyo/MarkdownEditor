using LiteDB;
using MarkdownEditor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarkdownEditor.Services
{
    public class HistoryService
    {
        private readonly string _dbPath;
        
        public HistoryService()
        {
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MarkdownEditor");
            Directory.CreateDirectory(appDataPath);
            _dbPath = Path.Combine(appDataPath, "history.db");
        }

        public void AddOrUpdateFile(string filePath)
        {
            try
            {
                using var db = new LiteDatabase(_dbPath);
                var collection = db.GetCollection<FileHistoryRecord>("files");
                
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) return;

                var existing = collection.FindOne(x => x.FilePath == filePath);
                
                if (existing != null)
                {
                    existing.LastOpenTime = DateTime.Now;
                    existing.LastModifyTime = fileInfo.LastWriteTime;
                    existing.FileSize = fileInfo.Length;
                    collection.Update(existing);
                }
                else
                {
                    var record = new FileHistoryRecord
                    {
                        FilePath = filePath,
                        FileName = Path.GetFileName(filePath),
                        LastOpenTime = DateTime.Now,
                        LastModifyTime = fileInfo.LastWriteTime,
                        FileSize = fileInfo.Length,
                        IsFavorite = false
                    };
                    collection.Insert(record);
                }
            }
            catch (Exception ex)
            {
                // Log error - in a real app you'd use proper logging
                System.Diagnostics.Debug.WriteLine($"Error updating file history: {ex.Message}");
            }
        }

        public List<FileHistoryRecord> GetRecentFiles(int count = 50)
        {
            try
            {
                using var db = new LiteDatabase(_dbPath);
                var collection = db.GetCollection<FileHistoryRecord>("files");
                
                return collection.FindAll()
                    .Where(x => File.Exists(x.FilePath))
                    .OrderByDescending(x => x.LastOpenTime)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting recent files: {ex.Message}");
                return new List<FileHistoryRecord>();
            }
        }

        public void RemoveFile(string filePath)
        {
            try
            {
                using var db = new LiteDatabase(_dbPath);
                var collection = db.GetCollection<FileHistoryRecord>("files");
                collection.DeleteMany(x => x.FilePath == filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing file from history: {ex.Message}");
            }
        }

        public void ToggleFavorite(string filePath)
        {
            try
            {
                using var db = new LiteDatabase(_dbPath);
                var collection = db.GetCollection<FileHistoryRecord>("files");
                
                var record = collection.FindOne(x => x.FilePath == filePath);
                if (record != null)
                {
                    record.IsFavorite = !record.IsFavorite;
                    collection.Update(record);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling favorite: {ex.Message}");
            }
        }
    }
}