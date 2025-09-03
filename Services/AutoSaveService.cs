using System;
using System.IO;
using System.Windows.Forms;

namespace MarkdownEditor.Services
{
    public class AutoSaveService
    {
        private System.Windows.Forms.Timer _autoSaveTimer;
        private string? _currentFilePath;
        private Func<string>? _getContent;
        private bool _hasChanges;
        private readonly int _autoSaveInterval;

        public event EventHandler<string>? AutoSaved;
        public event EventHandler<string>? AutoSaveError;

        public AutoSaveService(int intervalSeconds = 30)
        {
            _autoSaveInterval = intervalSeconds * 1000; // 转换为毫秒
            _autoSaveTimer = new System.Windows.Forms.Timer();
            _autoSaveTimer.Interval = _autoSaveInterval;
            _autoSaveTimer.Tick += AutoSaveTimer_Tick;
        }

        public void StartAutoSave(string filePath, Func<string> getContentFunc)
        {
            _currentFilePath = filePath;
            _getContent = getContentFunc;
            _hasChanges = false;
            
            if (!string.IsNullOrEmpty(filePath))
            {
                _autoSaveTimer.Start();
            }
        }

        public void StopAutoSave()
        {
            _autoSaveTimer.Stop();
            _currentFilePath = null;
            _getContent = null;
            _hasChanges = false;
        }

        public void MarkContentChanged()
        {
            _hasChanges = true;
            
            // 如果还没有开始自动保存计时器，重启它
            if (!_autoSaveTimer.Enabled && !string.IsNullOrEmpty(_currentFilePath))
            {
                _autoSaveTimer.Start();
            }
        }

        public void SetFilePath(string filePath)
        {
            _currentFilePath = filePath;
            
            if (!string.IsNullOrEmpty(filePath) && _getContent != null)
            {
                _autoSaveTimer.Start();
            }
        }

        private async void AutoSaveTimer_Tick(object? sender, EventArgs e)
        {
            if (!_hasChanges || string.IsNullOrEmpty(_currentFilePath) || _getContent == null)
            {
                return;
            }

            try
            {
                await SaveToFile();
                _hasChanges = false;
                AutoSaved?.Invoke(this, $"自动保存成功: {Path.GetFileName(_currentFilePath)}");
            }
            catch (Exception ex)
            {
                AutoSaveError?.Invoke(this, $"自动保存失败: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task SaveToFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _getContent == null)
                return;

            string content = _getContent();
            
            // 创建临时文件进行原子性保存
            string tempFilePath = _currentFilePath + ".tmp";
            string backupFilePath = _currentFilePath + ".bak";

            try
            {
                // 先写入临时文件
                await File.WriteAllTextAsync(tempFilePath, content, System.Text.Encoding.UTF8);

                // 创建备份文件（如果原文件存在）
                if (File.Exists(_currentFilePath))
                {
                    if (File.Exists(backupFilePath))
                        File.Delete(backupFilePath);
                    
                    File.Move(_currentFilePath, backupFilePath);
                }

                // 将临时文件移动为目标文件
                File.Move(tempFilePath, _currentFilePath);

                // 清理备份文件（保存成功后）
                if (File.Exists(backupFilePath))
                    File.Delete(backupFilePath);
            }
            catch
            {
                // 如果保存失败，尝试恢复备份文件
                if (File.Exists(backupFilePath) && !File.Exists(_currentFilePath))
                {
                    File.Move(backupFilePath, _currentFilePath);
                }

                // 清理临时文件
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);

                throw;
            }
        }

        public void ForceAutoSave()
        {
            if (_hasChanges && !string.IsNullOrEmpty(_currentFilePath) && _getContent != null)
            {
                AutoSaveTimer_Tick(null, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _autoSaveTimer?.Dispose();
        }
    }
}