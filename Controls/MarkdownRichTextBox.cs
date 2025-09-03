using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MarkdownEditor.Controls
{
    public class MarkdownRichTextBox : RichTextBox
    {
        private System.Windows.Forms.Timer? _syntaxTimer;
        private bool _updating = false;
        private int _lastTextLength = 0;
        private string _lastProcessedText = string.Empty;
        private Dictionary<string, (Color color, FontStyle style)> _syntaxStyles = new();
        
        // 后台线程相关字段
        private BackgroundWorker? _backgroundWorker;
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly object _lockObject = new object();
        private volatile bool _isBackgroundProcessing = false;
        private string _pendingText = string.Empty;
        
        // 语法高亮控制标志
        private bool _syntaxHighlightingEnabled = true;
        private bool _isFileLoading = false;
        
        /// <summary>
        /// 获取或设置是否启用语法高亮
        /// </summary>
        public bool SyntaxHighlightingEnabled
        {
            get { return _syntaxHighlightingEnabled; }
            set 
            { 
                if (_syntaxHighlightingEnabled != value)
                {
                    _syntaxHighlightingEnabled = value;
                    if (value)
                    {
                        // 启用语法高亮时，立即应用高亮
                        ApplySyntaxHighlighting();
                    }
                    else
                    {
                        // 禁用语法高亮时，清除所有格式
                        ClearSyntaxHighlighting();
                    }
                }
            }
        }
        
        // 语法高亮信息结构
        private class HighlightInfo
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public Color Color { get; set; }
            public FontStyle Style { get; set; }
        }
        
        // 选择状态结构
        private struct SelectionState
        {
            public int Start { get; set; }
            public int Length { get; set; }
        }
        
        /// <summary>
        /// 保存当前选择状态
        /// </summary>
        private SelectionState SaveSelectionState()
        {
            return new SelectionState
            {
                Start = this.SelectionStart,
                Length = this.SelectionLength
            };
        }
        
        /// <summary>
        /// 恢复选择状态
        /// </summary>
        private void RestoreSelectionState(SelectionState state)
        {
            this.SelectionStart = state.Start;
            this.SelectionLength = state.Length;
        }

        public MarkdownRichTextBox()
        {
            InitializeComponent();
            InitializeSyntaxStyles();
            SetupSyntaxHighlighting();
            SetupBackgroundWorker();
        }

        private void InitializeComponent()
        {
            this.Font = new Font("Consolas", 11F, FontStyle.Regular);
            this.AcceptsTab = true;
            this.HideSelection = false;
            this.WordWrap = true;
            this.ScrollBars = RichTextBoxScrollBars.Both;
            this.DetectUrls = false;
            
            // 高DPI支持
            this.Font = ScaleFont(this.Font);
        }

        private void InitializeSyntaxStyles()
        {
            _syntaxStyles = new Dictionary<string, (Color, FontStyle)>
            {
                ["header"] = (Color.DarkBlue, FontStyle.Bold),
                ["bold"] = (Color.Black, FontStyle.Bold),
                ["italic"] = (Color.Black, FontStyle.Italic),
                ["strikethrough"] = (Color.Gray, FontStyle.Strikeout),
                ["code_inline"] = (Color.DarkRed, FontStyle.Regular),
                ["code_block"] = (Color.DarkGreen, FontStyle.Regular),
                ["link"] = (Color.Blue, FontStyle.Underline),
                ["quote"] = (Color.DarkGray, FontStyle.Italic),
                ["list"] = (Color.DarkOrange, FontStyle.Regular),
                ["normal"] = (Color.Black, FontStyle.Regular)
            };
        }

        private Font ScaleFont(Font originalFont)
        {
            using (Graphics g = this.CreateGraphics())
            {
                float dpiScale = g.DpiX / 96f; // 96 DPI是标准DPI
                if (dpiScale > 1.0f)
                {
                    float newSize = originalFont.Size * Math.Min(dpiScale, 1.5f); // 限制最大缩放比例
                    return new Font(originalFont.FontFamily, newSize, originalFont.Style);
                }
                return originalFont;
            }
        }

        private void SetupSyntaxHighlighting()
        {
            _syntaxTimer = new System.Windows.Forms.Timer();
            _syntaxTimer.Interval = 1500; // 进一步增加延迟，减少滚动干扰
            _syntaxTimer.Tick += SyntaxTimer_Tick;
            
            this.TextChanged += MarkdownRichTextBox_TextChanged;
        }
        
        private void SetupBackgroundWorker()
        {
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.WorkerSupportsCancellation = true;
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        private void MarkdownRichTextBox_TextChanged(object? sender, EventArgs e)
        {
            // 如果语法高亮被禁用或正在加载文件，则跳过处理
            if (!_syntaxHighlightingEnabled || _isFileLoading || _updating)
            {
                return;
            }
            
            if (ShouldUpdateSyntax())
            {
                // 取消当前的定时器，使用后台线程处理
                _syntaxTimer?.Stop();
                
                // 保存待处理的文本
                lock (_lockObject)
                {
                    _pendingText = this.Text;
                }
                
                // 启动后台处理
                StartBackgroundSyntaxHighlighting();
            }
        }

        private bool ShouldUpdateSyntax()
        {
            // 如果语法高亮被禁用，则不更新
            if (!_syntaxHighlightingEnabled)
                return false;
                
            // 只有在文本长度变化较大或内容显著变化时才更新
            int currentLength = this.Text.Length;
            int lengthDiff = Math.Abs(currentLength - _lastTextLength);
            
            // 进一步提高触发阈值，减少编辑时的频繁更新
            if (lengthDiff <= 50)  // 从20增加到50
            {
                string currentText = this.Text;
                if (currentText == _lastProcessedText)
                    return false;
                    
                // 只有在检测到关键语法字符变化时才更新
                if (HasSignificantMarkdownChanges(currentText))
                {
                    return true;
                }
                
                // 普通文本输入不触发更新
                return false;
            }
            
            // 只有在文本变化超过50个字符时才强制更新
            return lengthDiff > 50;
        }
        
        private bool HasSignificantMarkdownChanges(string text)
        {
            // 只检查真正重要的语法字符变化，减少不必要的触发
            char[] criticalSyntaxChars = {'#', '`', '[', ']', '\n', '*', '_', '>', '|'};
            
            if (_lastProcessedText.Length == 0)
                return text.IndexOfAny(criticalSyntaxChars) >= 0;
                
            // 更严格的比较逻辑，只有在关键语法结构发生变化时才触发
            int currentCriticalCount = CountCriticalChars(text, criticalSyntaxChars);
            int lastCriticalCount = CountCriticalChars(_lastProcessedText, criticalSyntaxChars);
            
            // 只有在关键字符数量变化超过阈值时才认为有显著变化
            return Math.Abs(currentCriticalCount - lastCriticalCount) >= 3;
        }
        
        private int CountCriticalChars(string text, char[] chars)
        {
            int count = 0;
            foreach (char c in text)
            {
                if (chars.Contains(c))
                {
                    count++;
                }
            }
            return count;
        }

        private void SyntaxTimer_Tick(object? sender, EventArgs e)
        {
            _syntaxTimer?.Stop();
            
            // 如果语法高亮被禁用，则不执行语法高亮
            if (!_syntaxHighlightingEnabled)
                return;
                
            ApplySyntaxHighlighting();
        }
        
        private void StartBackgroundSyntaxHighlighting()
        {
            // 如果语法高亮被禁用，则不启动后台处理
            if (!_syntaxHighlightingEnabled)
                return;
                
            // 如果已经在处理中，取消当前处理
            if (_isBackgroundProcessing)
            {
                _cancellationTokenSource?.Cancel();
            }
            
            // 创建新的取消令牌
            _cancellationTokenSource = new CancellationTokenSource();
            
            // 延迟启动后台处理，避免频繁触发 - 增加到1000ms减少编辑时的刷新
            Task.Delay(1000, _cancellationTokenSource?.Token ?? CancellationToken.None).ContinueWith(t =>
            {
                if (!t.IsCanceled && _backgroundWorker?.IsBusy == false)
                {
                    _isBackgroundProcessing = true;
                    string textToProcess;
                    lock (_lockObject)
                    {
                        textToProcess = _pendingText;
                    }
                    _backgroundWorker.RunWorkerAsync(textToProcess);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private void BackgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (e.Argument is not string textToProcess)
                return;
                
            // 在后台线程中计算语法高亮信息
            var highlightInfo = CalculateSyntaxHighlighting(textToProcess);
            e.Result = highlightInfo;
        }
        
        private void BackgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            _isBackgroundProcessing = false;
            
            // 如果语法高亮被禁用，则不应用高亮结果
            if (!_syntaxHighlightingEnabled)
                return;
            
            if (e.Error == null && e.Result is List<HighlightInfo> highlightInfo)
            {
                // 在UI线程中无感知地应用语法高亮
                ApplySyntaxHighlightingFromBackground(highlightInfo);
            }
         }
         
        private List<HighlightInfo> CalculateSyntaxHighlighting(string text)
        {
            var highlights = new List<HighlightInfo>();
            
            // 在后台线程中计算所有语法高亮位置和样式
            var patterns = new List<(string pattern, string style, RegexOptions options)>
            {
                (@"```[\s\S]*?```", "code_block", RegexOptions.Multiline),
                (@"^#{1,6}\s+.*$", "header", RegexOptions.Multiline),
                (@"\*\*([^*]+)\*\*", "bold", RegexOptions.None),
                (@"\*([^*]+)\*", "italic", RegexOptions.None),
                (@"~~([^~]+)~~", "strikethrough", RegexOptions.None),
                (@"`([^`]+)`", "code_inline", RegexOptions.None),
                (@"\[([^\]]+)\]\([^)]+\)", "link", RegexOptions.None),
                (@"^>\s+.*$", "quote", RegexOptions.Multiline),
                (@"^[-*+]\s+.*$", "list", RegexOptions.Multiline)
            };
            
            foreach (var (pattern, style, options) in patterns)
            {
                if (_syntaxStyles.TryGetValue(style, out var styleInfo))
                {
                    var matches = Regex.Matches(text, pattern, options);
                    foreach (Match match in matches)
                    {
                        highlights.Add(new HighlightInfo
                        {
                            Start = match.Index,
                            Length = match.Length,
                            Color = styleInfo.color,
                            Style = styleInfo.style
                        });
                    }
                }
            }
            
            return highlights;
        }
        
        private void ApplySyntaxHighlightingFromBackground(List<HighlightInfo> highlights)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<List<HighlightInfo>>(ApplySyntaxHighlightingFromBackground), highlights);
                return;
            }
            
            if (_updating || this.IsDisposed)
                return;
                
            _updating = true;
            
            try
            {
                // 保存当前状态和滚动位置 - 增强保护机制
// 保存当前状态
                var originalSelection = SaveSelectionState();
                int originalScrollPos = this.GetCharIndexFromPosition(new Point(0, 0));
                Point originalScrollPoint = new Point(0, 0);
                int originalFirstVisibleLine = this.GetLineFromCharIndex(originalScrollPos);
                
                // 完全禁用重绘和事件处理
                this.SuspendLayout();
                
                // 临时禁用滚动事件
                RichTextBoxScrollBars originalScrollBars = this.ScrollBars;
                this.ScrollBars = RichTextBoxScrollBars.None;
                
                // 使用更高效的批量格式设置
                // 先重置整个文本为默认格式（一次性操作）
                this.SelectionStart = 0;
                this.SelectionLength = this.Text.Length;
                this.SelectionColor = _syntaxStyles["normal"].color;
                this.SelectionFont = this.Font;
                
                // 批量应用高亮（减少选择操作次数）
                foreach (var highlight in highlights)
                {
                    if (highlight.Start >= 0 && highlight.Start + highlight.Length <= this.Text.Length)
                    {
                        this.SelectionStart = highlight.Start;
                        this.SelectionLength = highlight.Length;
                        this.SelectionColor = highlight.Color;
                        if (highlight.Style != FontStyle.Regular)
                         {
                             this.SelectionFont = new Font(this.Font, highlight.Style);
                         }
                    }
                }
                
                // 恢复滚动条
                this.ScrollBars = originalScrollBars;
                
                // 恢复原始状态
                RestoreSelectionState(originalSelection);
                
                // 增强的滚动位置恢复机制
                int currentScrollPos = this.GetCharIndexFromPosition(new Point(0, 0));
                int currentFirstVisibleLine = this.GetLineFromCharIndex(currentScrollPos);
                
                // 只有在可见行发生变化时才进行滚动恢复
                if (Math.Abs(originalFirstVisibleLine - currentFirstVisibleLine) > 0)
                {
                    // 使用异步延迟恢复，完全避免阻塞UI
                    Task.Delay(30).ContinueWith(_ =>
                    {
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    // 精确恢复到原始可见行
                                    int targetCharIndex = this.GetFirstCharIndexFromLine(originalFirstVisibleLine);
                                    if (targetCharIndex >= 0)
                                    {
                                        var tempSelection = SaveSelectionState();
                                        this.SelectionStart = targetCharIndex;
                                        this.ScrollToCaret();
                                        RestoreSelectionState(tempSelection);
                                    }
                                }
                                catch
                                {
                                    // 忽略滚动恢复错误
                                }
                            }));
                        }
                    }, TaskScheduler.Default);
                }
                
                // 更新缓存
                _lastTextLength = this.Text.Length;
                _lastProcessedText = this.Text;
            }
            finally
            {
                this.ResumeLayout(false); // 不立即重绘
                // 延迟重绘，减少视觉闪烁
                Task.Delay(10).ContinueWith(_ =>
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        this.BeginInvoke(new Action(() => this.Invalidate()));
                    }
                }, TaskScheduler.Default);
                _updating = false;
            }
        }

        private void ApplySyntaxHighlighting()
        {
            // 如果语法高亮被禁用，则不执行语法高亮
            if (!_syntaxHighlightingEnabled || _updating || this.Text.Length == 0) return;

            _updating = true;
            
            // 保存当前状态
            var originalSelection = SaveSelectionState();

            try
            {
                // 使用更轻量级的格式重置方法
                // 避免大范围的Select操作，减少滚动干扰
                this.SelectionStart = 0;
                this.SelectionLength = this.Text.Length;
                this.SelectionColor = _syntaxStyles["normal"].color;
                this.SelectionFont = this.Font;
                
                // 立即恢复原始选择，减少视觉干扰
                RestoreSelectionState(originalSelection);
            
            // 按优先级应用语法高亮（避免重叠）
            var patterns = new List<(string pattern, string style, RegexOptions options)>
            {
                // 代码块优先级最高（避免内部被其他规则影响）
                (@"```[\s\S]*?```", "code_block", RegexOptions.Multiline),
                
                // 标题
                (@"^(#{1,6})\s+(.*)$", "header", RegexOptions.Multiline),
                
                // 行内代码（不在代码块内）
                (@"`([^`\r\n]+)`", "code_inline", RegexOptions.None),
                
                // 链接
                (@"\[([^\]]+)\]\([^\)]+\)", "link", RegexOptions.None),
                
                // 粗体
                (@"\*\*([^\*\r\n]+)\*\*", "bold", RegexOptions.None),
                (@"__([^_\r\n]+)__", "bold", RegexOptions.None),
                
                // 斜体
                (@"\*([^\*\r\n]+)\*", "italic", RegexOptions.None),
                (@"_([^_\r\n]+)_", "italic", RegexOptions.None),
                
                // 删除线
                (@"~~([^~\r\n]+)~~", "strikethrough", RegexOptions.None),
                
                // 引用
                (@"^>\s*(.*)$", "quote", RegexOptions.Multiline),
                
                // 列表项
                (@"^[\s]*[-\+\*]\s+(.*)$", "list", RegexOptions.Multiline),
                (@"^[\s]*\d+\.\s+(.*)$", "list", RegexOptions.Multiline)
            };
            
                // 应用每个模式
                foreach (var (pattern, style, options) in patterns)
                {
                    ApplyPatternSafely(pattern, style, options);
                }
                
                // 更新缓存
                _lastTextLength = this.Text.Length;
                _lastProcessedText = this.Text;
            }
            finally
            {
                // 恢复原始选择状态
                RestoreSelectionState(originalSelection);
                _updating = false;
            }
        }
        
        private void ApplyPatternSafely(string pattern, string styleName, RegexOptions options)
        {
            try
            {
                var style = _syntaxStyles[styleName];
                Regex regex = new Regex(pattern, options);
                MatchCollection matches = regex.Matches(this.Text);

                foreach (Match match in matches)
                {
                    if (match.Index + match.Length <= this.Text.Length)
                    {
                        this.Select(match.Index, match.Length);
                        this.SelectionColor = style.color;
                        
                        Font currentFont = this.SelectionFont ?? this.Font;
                        this.SelectionFont = new Font(currentFont.FontFamily, currentFont.Size, style.style);
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略正则表达式错误，继续处理其他样式
                System.Diagnostics.Debug.WriteLine($"Syntax highlighting error for pattern {pattern}: {ex.Message}");
            }
        }
        
        private int GetScrollPos()
        {
            try
            {
                // 使用Win32 API获取真实的滚动位置
                return GetScrollPos(this.Handle, 1); // SB_VERT = 1
            }
            catch
            {
                return 0;
            }
        }
        
        private void SetScrollPos(int pos)
        {
            try
            {
                // 使用Win32 API设置真实的滚动位置
                SetScrollPos(this.Handle, 1, pos, true); // SB_VERT = 1
            }
            catch
            {
                // 忽略滚动错误
            }
        }
        
        // Win32 API声明
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        public void InsertMarkdownElement(string element)
        {
            int start = this.SelectionStart;
            string selectedText = this.SelectedText;

            // 暂时禁用语法高亮
            _updating = true;
            
            try
            {
                switch (element.ToLower())
                {
                    case "bold":
                        this.SelectedText = $"**{selectedText}**";
                        break;
                    case "italic":
                        this.SelectedText = $"*{selectedText}*";
                        break;
                    case "code":
                        this.SelectedText = $"`{selectedText}`";
                        break;
                    case "h1":
                        this.SelectedText = $"# {selectedText}";
                        break;
                    case "h2":
                        this.SelectedText = $"## {selectedText}";
                        break;
                    case "h3":
                        this.SelectedText = $"### {selectedText}";
                        break;
                    case "link":
                        this.SelectedText = $"[{selectedText}](url)";
                        break;
                    case "quote":
                        this.SelectedText = $"> {selectedText}";
                        break;
                    case "list":
                        this.SelectedText = $"- {selectedText}";
                        break;
                    case "table":
                        this.SelectedText = $"| 列1 | 列2 | 列3 |\n|-----|-----|-----|\n| {selectedText} |  |  |";
                        break;
                    case "hr":
                        this.SelectedText = $"\n---\n{selectedText}";
                        break;
                }
                
                // 立即触发语法高亮更新
                _syntaxTimer?.Stop();
                _syntaxTimer?.Start();
            }
            finally
            {
                _updating = false;
            }
        }

        /// <summary>
        /// 设置文件加载状态，禁用语法高亮
        /// </summary>
        /// <param name="isLoading">是否正在加载文件</param>
        public void SetFileLoadingState(bool isLoading)
        {
            _isFileLoading = isLoading;
            if (!isLoading && _syntaxHighlightingEnabled)
            {
                // 文件加载完成后，延迟启动语法高亮
                System.Windows.Forms.Timer delayTimer = new System.Windows.Forms.Timer();
                delayTimer.Interval = 1000; // 延迟1秒
                delayTimer.Tick += (s, e) =>
                {
                    delayTimer.Stop();
                    delayTimer.Dispose();
                    if (!string.IsNullOrEmpty(this.Text))
                    {
                        lock (_lockObject)
                        {
                            _pendingText = this.Text;
                        }
                        StartBackgroundSyntaxHighlighting();
                    }
                };
                delayTimer.Start();
            }
        }


        
        /// <summary>
        /// 清除所有语法高亮格式
        /// </summary>
        private void ClearSyntaxHighlighting()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ClearSyntaxHighlighting));
                return;
            }
            
            if (_updating || this.IsDisposed)
                return;
                
            _updating = true;
            
            try
            {
                // 保存当前状态
                var originalSelection = SaveSelectionState();
                
                // 禁用重绘
                this.SuspendLayout();
                
                // 取消当前的处理
                _syntaxTimer?.Stop();
                _cancellationTokenSource?.Cancel();
                
                // 重置整个文本为默认格式
                this.SelectionStart = 0;
                this.SelectionLength = this.Text.Length;
                this.SelectionColor = _syntaxStyles["normal"].color;
                this.SelectionFont = this.Font;
                
                // 恢复原始状态
                RestoreSelectionState(originalSelection);
            }
            finally
            {
                this.ResumeLayout(false);
                this.Invalidate();
                _updating = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消后台处理
                _cancellationTokenSource?.Cancel();
                
                // 等待后台工作完成
                if (_backgroundWorker?.IsBusy == true)
                {
                    _backgroundWorker.CancelAsync();
                }
                
                // 清理资源
                _syntaxTimer?.Dispose();
                _backgroundWorker?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}