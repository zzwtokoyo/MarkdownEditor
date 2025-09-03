using MarkdownEditor.Controls;
using MarkdownEditor.Models;
using MarkdownEditor.Services;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarkdownEditor
{
    public partial class MainForm : Form
    {
        private readonly HistoryService _historyService;
        private readonly MarkdownService _markdownService;
        private readonly AutoSaveService _autoSaveService;
        private string? _currentFilePath;
        private bool _isContentChanged;
        private System.Windows.Forms.Timer _previewTimer;

        public MainForm()
        {
            // 设置DPI感知
            SetProcessDpiAwareness();
            
            InitializeComponent();
            
            // 设置窗体图标
            try
            {
                this.Icon = new Icon("icon.ico");
            }
            catch
            {
                // 如果图标文件不存在，忽略错误
            }
            
            _historyService = new HistoryService();
            _markdownService = new MarkdownService();
            _autoSaveService = new AutoSaveService(30); // 30秒自动保存间隔
            _previewTimer = new System.Windows.Forms.Timer();
            _previewTimer.Interval = 500; // 500ms delay for preview update
            _previewTimer.Tick += PreviewTimer_Tick;
            
            LoadRecentFiles();
            UpdateUI();
            SetupEventHandlers();
            
            // Initialize WebView2 when form is loaded
            this.Load += MainForm_Load;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);
        
        private void SetProcessDpiAwareness()
        {
            try
            {
                // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
                SetProcessDpiAwarenessContext(-4);
            }
            catch
            {
                // 如果设置失败，忽略错误
            }
        }
        
        private void SetupEventHandlers()
        {
            // 设置工具栏事件
            markdownToolBar.FormatRequested += MarkdownToolBar_FormatRequested;
            
            // 设置编辑器事件
            markdownEditor.TextChanged += MarkdownEditor_TextChanged;
            
            // 设置自动保存事件
            _autoSaveService.AutoSaved += AutoSaveService_AutoSaved;
            _autoSaveService.AutoSaveError += AutoSaveService_AutoSaveError;
        }

        private void MarkdownToolBar_FormatRequested(object? sender, string format)
        {
            markdownEditor.InsertMarkdownElement(format);
        }
        
        private void MarkdownEditor_TextChanged(object? sender, EventArgs e)
        {
            _isContentChanged = true;
            UpdateUI();
            
            // 通知自动保存服务
            _autoSaveService.MarkContentChanged();
            
            // Restart the timer for preview update
            _previewTimer.Stop();
            _previewTimer.Start();
        }
        
        private void AutoSaveService_AutoSaved(object? sender, string message)
        {
            UpdateStatusBar(message);
        }
        
        private void AutoSaveService_AutoSaveError(object? sender, string message)
        {
            UpdateStatusBar($"错误: {message}");
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            await InitializeWebView();
        }

        private async Task InitializeWebView()
        {
            try
            {
                await webView2Preview.EnsureCoreWebView2Async(null);
                
                // Initial empty content
                webView2Preview.CoreWebView2.NavigateToString(
                    _markdownService.ConvertToHtml("# 欢迎使用 Markdown 编辑器\n\n请选择或打开一个 Markdown 文件开始编辑。"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2初始化失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRecentFiles()
        {
            try
            {
                var recentFiles = _historyService.GetRecentFiles();
                lstFiles.Items.Clear();
                
                foreach (var file in recentFiles)
                {
                    var displayName = $"{file.FileName} [{Path.GetDirectoryName(file.FilePath)}]";
                    lstFiles.Items.Add(new FileListItem(file, displayName));
                }
                
                UpdateStatusBar($"已加载 {recentFiles.Count} 个历史文件");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载历史文件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateUI()
        {
            var hasFile = !string.IsNullOrEmpty(_currentFilePath);
            saveToolStripMenuItem.Enabled = hasFile && _isContentChanged;
            
            var title = "Markdown 编辑器";
            if (hasFile)
            {
                var fileName = Path.GetFileName(_currentFilePath);
                title = $"{fileName}{(_isContentChanged ? " *" : "")} - {title}";
            }
            
            this.Text = title;
        }

        private void UpdateStatusBar(string message)
        {
            lblStatus.Text = message;
        }

        private async void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isContentChanged)
            {
                var result = MessageBox.Show("当前文件已修改，是否保存？", "确认", 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel) return;
                if (result == DialogResult.Yes) await SaveCurrentFile();
            }

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Markdown文件 (*.md;*.markdown)|*.md;*.markdown|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                await LoadFile(openFileDialog.FileName);
            }
        }

        private async Task LoadFile(string filePath)
        {
            try
            {
                var content = _markdownService.LoadFileContent(filePath);
                markdownEditor.Text = content;
                _currentFilePath = filePath;
                _isContentChanged = false;
                
                _historyService.AddOrUpdateFile(filePath);
                LoadRecentFiles();
                UpdateUI();
                
                // 启动自动保存
                _autoSaveService.StartAutoSave(filePath, () => markdownEditor.Text);
                
                await UpdatePreview();
                UpdateStatusBar($"已打开文件: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SaveCurrentFile();
        }

        private async Task SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                await SaveAsNewFile();
                return;
            }

            try
            {
                _markdownService.SaveFileContent(_currentFilePath, markdownEditor.Text);
                _isContentChanged = false;
                _historyService.AddOrUpdateFile(_currentFilePath);
                LoadRecentFiles();
                UpdateUI();
                UpdateStatusBar($"已保存: {Path.GetFileName(_currentFilePath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SaveAsNewFile()
        {
            using var saveFileDialog = new SaveFileDialog
            {
                Filter = "Markdown文件 (*.md)|*.md|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true,
                FileName = string.IsNullOrEmpty(_currentFilePath) ? 
                    "新建文档.md" : Path.GetFileName(_currentFilePath)
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _markdownService.SaveFileContent(saveFileDialog.FileName, markdownEditor.Text);
                    _currentFilePath = saveFileDialog.FileName;
                    _isContentChanged = false;
                    _historyService.AddOrUpdateFile(_currentFilePath);
                    
                    // 更新自动保存路径
                    _autoSaveService.SetFilePath(_currentFilePath);
                    
                    LoadRecentFiles();
                    UpdateUI();
                    UpdateStatusBar($"已保存: {Path.GetFileName(_currentFilePath)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存文件失败: {ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            await SaveAsNewFile();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItem is FileListItem selectedItem)
            {
                if (_isContentChanged)
                {
                    var result = MessageBox.Show("当前文件已修改，是否保存？", "确认", 
                        MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Cancel) 
                    {
                        // Reset selection to prevent change
                        return;
                    }
                    if (result == DialogResult.Yes) 
                    {
                        await SaveCurrentFile();
                    }
                }

                await LoadFile(selectedItem.FileRecord.FilePath);
            }
        }

        private async void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            _previewTimer.Stop();
            await UpdatePreview();
        }

        private Task UpdatePreview()
        {
            try
            {
                var html = _markdownService.ConvertToHtml(markdownEditor.Text);
                webView2Preview.CoreWebView2.NavigateToString(html);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview update failed: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isContentChanged)
            {
                var result = MessageBox.Show("当前文件已修改，是否保存？", "确认", 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                if (result == DialogResult.Yes)
                {
                    // Synchronous save for form closing
                    SaveCurrentFileSync();
                }
            }
            
            base.OnFormClosing(e);
        }

        private void SaveCurrentFileSync()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                return; // Skip save as for sync operation
            }

            try
            {
                _markdownService.SaveFileContent(_currentFilePath, markdownEditor.Text);
                _isContentChanged = false;
                _historyService.AddOrUpdateFile(_currentFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存文件失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _autoSaveService?.Dispose();
                _previewTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class FileListItem
    {
        public FileHistoryRecord FileRecord { get; }
        public string DisplayText { get; }

        public FileListItem(FileHistoryRecord fileRecord, string displayText)
        {
            FileRecord = fileRecord;
            DisplayText = displayText;
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}