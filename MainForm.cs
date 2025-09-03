using MarkdownEditor.Controls;
using MarkdownEditor.Helpers;
using MarkdownEditor.Models;
using MarkdownEditor.Services;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MarkdownEditor
{
    public partial class MainForm : Form
    {
        private readonly HistoryService _historyService;
        private readonly MarkdownService _markdownService;
        private readonly AutoSaveService _autoSaveService;
        private readonly ConfigurationService _configurationService;
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
            _configurationService = new ConfigurationService();
            _previewTimer = new System.Windows.Forms.Timer();
            _previewTimer.Interval = 500; // 进一步减少频繁更新和滚动
            _previewTimer.Tick += PreviewTimer_Tick;
            
            LoadRecentFiles();
            UpdateUI();
            SetupEventHandlers();
            
            // 加载语法高亮设置
            LoadSyntaxHighlightingSettings();
            
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
            
            // 使用防抖机制，避免频繁更新预览
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
                
                // 重置初始化标志
                _isWebViewInitialized = false;
                
                // Initial empty content
                webView2Preview.CoreWebView2.NavigateToString(
                    _markdownService.ConvertToHtml("# 欢迎使用 Markdown 编辑器\n\n请选择或打开一个 Markdown 文件开始编辑。"));
                
                // 等待页面加载完成后再标记为已初始化
                webView2Preview.CoreWebView2.NavigationCompleted += (sender, args) => {
                    _isWebViewInitialized = true;
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2初始化失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRecentFiles()
        {
            ExceptionHelper.TryExecute(() =>
            {
                var recentFiles = _historyService.GetRecentFiles();
                lstFiles.Items.Clear();
                
                foreach (var file in recentFiles)
                {
                    var displayName = $"{file.FileName} [{Path.GetDirectoryName(file.FilePath)}]";
                    lstFiles.Items.Add(new FileListItem(file, displayName));
                }
                
                UpdateStatusBar($"已加载 {recentFiles.Count} 个历史文件");
            }, "加载历史文件失败", MessageBoxIcon.Warning);
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

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_isContentChanged)
            {
                var result = MessageBox.Show("当前文件已修改，是否保存？", "确认", 
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel) return;
                if (result == DialogResult.Yes) SaveCurrentFile();
            }

            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Markdown文件 (*.md;*.markdown)|*.md;*.markdown|所有文件 (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                LoadFile(openFileDialog.FileName);
            }
        }

        private void LoadFile(string filePath)
        {
            ExceptionHelper.TryExecute(() =>
            {
                // 设置文件加载状态，禁用语法高亮
                markdownEditor.SetFileLoadingState(true);
                
                var content = _markdownService.LoadFileContent(filePath);
                
                // 临时禁用TextChanged事件，避免加载文件时触发预览更新
                markdownEditor.TextChanged -= MarkdownEditor_TextChanged;
                markdownEditor.Text = content;
                markdownEditor.TextChanged += MarkdownEditor_TextChanged;
                
                _currentFilePath = filePath;
                _isContentChanged = false;
                
                _historyService.AddOrUpdateFile(filePath);
                LoadRecentFiles();
                UpdateUI();
                
                // 启动自动保存
                _autoSaveService.StartAutoSave(filePath, () => markdownEditor.Text);
                
                // 恢复语法高亮状态（延迟启动）
                markdownEditor.SetFileLoadingState(false);
                
                // 延迟更新预览，确保WebView2完全准备好，避免滚动问题
                _previewTimer.Stop(); // 停止任何正在进行的预览更新
                System.Windows.Forms.Timer loadTimer = new System.Windows.Forms.Timer();
                loadTimer.Interval = 500; // 增加延迟到500ms
                loadTimer.Tick += (s, e) => {
                    loadTimer.Stop();
                    loadTimer.Dispose();
                    UpdatePreview();
                };
                loadTimer.Start();
                UpdateStatusBar($"已打开文件: {Path.GetFileName(filePath)}");
            }, "打开文件失败", MessageBoxIcon.Error);
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveCurrentFile();
        }

        private void SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsNewFile();
                return;
            }

            ExceptionHelper.TryExecute(() =>
            {
                _markdownService.SaveFileContent(_currentFilePath, markdownEditor.Text);
                _isContentChanged = false;
                _historyService.AddOrUpdateFile(_currentFilePath);
                LoadRecentFiles();
                UpdateUI();
                UpdateStatusBar($"已保存: {Path.GetFileName(_currentFilePath)}");
            }, "保存文件失败", MessageBoxIcon.Error);
        }

        private void SaveAsNewFile()
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
                ExceptionHelper.TryExecute(() =>
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
                }, "保存文件失败", MessageBoxIcon.Error);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAsNewFile();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
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
                        SaveCurrentFile();
                    }
                }

                LoadFile(selectedItem.FileRecord.FilePath);
            }
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            _previewTimer.Stop();
            UpdatePreview();
        }

        private bool _isWebViewInitialized = false;
        
        private void UpdatePreview()
        {
            try
            {
                if (webView2Preview.CoreWebView2 == null)
                    return;
                
                // 保存编辑器的滚动状态，确保预览更新不影响编辑器
                var editorScrollPos = markdownEditor.GetCharIndexFromPosition(new Point(0, 0));
                var editorSelectionStart = markdownEditor.SelectionStart;
                var editorSelectionLength = markdownEditor.SelectionLength;
                
                // 如果是第一次更新或者WebView还没有初始化完成，使用NavigateToString
                if (!_isWebViewInitialized)
                {
                    var fullHtml = _markdownService.ConvertToHtml(markdownEditor.Text);
                    webView2Preview.CoreWebView2.NavigateToString(fullHtml);
                    // 注意：_isWebViewInitialized 将在NavigationCompleted事件中设置为true
                }
                else
                {
                    // 使用JavaScript动态更新内容，避免页面刷新
                    var bodyHtml = _markdownService.ConvertToHtmlBody(markdownEditor.Text);
                    
                    // 改进的HTML转义处理，避免JSON解析错误
                    var escapedHtml = JsonSerializer.Serialize(bodyHtml);
                    
                    // 使用更安全的消息格式
                    var message = $"{{\"action\": \"updateContent\", \"html\": {escapedHtml}}}";
                    webView2Preview.CoreWebView2.PostWebMessageAsJson(message);
                }
                
                // 确保编辑器的滚动状态和选择状态不受预览更新影响
                // 使用BeginInvoke确保在UI线程上异步执行，避免阻塞
                this.BeginInvoke(new Action(() => {
                    try
                    {
                        // 只有在状态发生变化时才恢复，避免不必要的操作
                        if (markdownEditor.SelectionStart != editorSelectionStart || 
                            markdownEditor.SelectionLength != editorSelectionLength)
                        {
                            markdownEditor.SelectionStart = editorSelectionStart;
                            markdownEditor.SelectionLength = editorSelectionLength;
                        }
                        
                        var currentScrollPos = markdownEditor.GetCharIndexFromPosition(new Point(0, 0));
                        if (Math.Abs(currentScrollPos - editorScrollPos) > 5)
                        {
                            markdownEditor.Select(editorScrollPos, 0);
                            markdownEditor.ScrollToCaret();
                            // 恢复原始选择
                            markdownEditor.SelectionStart = editorSelectionStart;
                            markdownEditor.SelectionLength = editorSelectionLength;
                        }
                    }
                    catch
                    {
                        // 忽略恢复过程中的任何错误，避免影响主要功能
                    }
                }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview update failed: {ex.Message}");
                // 发生错误时不进行回退刷新，避免不必要的页面重载
                // 用户可以通过重新打开文件来恢复预览
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

            ExceptionHelper.TryExecuteSilent(() =>
            {
                _markdownService.SaveFileContent(_currentFilePath, markdownEditor.Text);
                _isContentChanged = false;
                _historyService.AddOrUpdateFile(_currentFilePath);
            }, "保存文件");
        }
        
        private void SyntaxHighlightingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem != null)
            {
                // 切换语法高亮状态
                markdownEditor.SyntaxHighlightingEnabled = menuItem.Checked;
                
                // 保存设置
                _configurationService.SetSetting("SyntaxHighlightingEnabled", menuItem.Checked);
            }
        }
        
        /// <summary>
        /// 加载语法高亮设置
        /// </summary>
        private void LoadSyntaxHighlightingSettings()
        {
            // 从配置文件加载语法高亮设置，默认为启用
            bool syntaxHighlightingEnabled = _configurationService.GetSetting("SyntaxHighlightingEnabled", true);
            
            // 设置编辑器的语法高亮状态
            markdownEditor.SyntaxHighlightingEnabled = syntaxHighlightingEnabled;
            
            // 更新菜单项的选中状态
            syntaxHighlightingToolStripMenuItem.Checked = syntaxHighlightingEnabled;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
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