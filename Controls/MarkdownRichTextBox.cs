using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace MarkdownEditor.Controls
{
    public class MarkdownRichTextBox : RichTextBox
    {
        private System.Windows.Forms.Timer? _syntaxTimer;
        private bool _updating = false;
        private int _lastTextLength = 0;
        private string _lastProcessedText = string.Empty;
        private Dictionary<string, (Color color, FontStyle style)> _syntaxStyles = new();

        public MarkdownRichTextBox()
        {
            InitializeComponent();
            InitializeSyntaxStyles();
            SetupSyntaxHighlighting();
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
            _syntaxTimer.Interval = 300; // 300ms延迟
            _syntaxTimer.Tick += SyntaxTimer_Tick;
            
            this.TextChanged += MarkdownRichTextBox_TextChanged;
        }

        private void MarkdownRichTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (!_updating && ShouldUpdateSyntax())
            {
                _syntaxTimer?.Stop();
                _syntaxTimer?.Start();
            }
        }

        private bool ShouldUpdateSyntax()
        {
            // 只有在文本长度变化较大或内容显著变化时才更新
            int currentLength = this.Text.Length;
            int lengthDiff = Math.Abs(currentLength - _lastTextLength);
            
            // 如果长度变化很小，检查是否是特殊字符变化
            if (lengthDiff <= 3)
            {
                string currentText = this.Text;
                if (currentText == _lastProcessedText)
                    return false;
                    
                // 检查是否包含Markdown特殊字符
                if (HasMarkdownSyntaxChanges(currentText))
                {
                    return true;
                }
                
                // 如果只是普通文本输入，不需要立即更新
                return false;
            }
            
            return true;
        }
        
        private bool HasMarkdownSyntaxChanges(string text)
        {
            // 检查是否包含可能影响语法高亮的字符
            char[] syntaxChars = {'#', '*', '_', '`', '[', ']', '(', ')', '>', '~', '\n'};
            
            if (_lastProcessedText.Length == 0)
                return text.IndexOfAny(syntaxChars) >= 0;
                
            // 比较当前文本和上次处理的文本，看是否有语法字符变化
            int minLength = Math.Min(text.Length, _lastProcessedText.Length);
            
            for (int i = 0; i < minLength; i++)
            {
                if (text[i] != _lastProcessedText[i] && syntaxChars.Contains(text[i]))
                    return true;
            }
            
            // 检查新增或删除的部分是否包含语法字符
            if (text.Length > _lastProcessedText.Length)
            {
                string newPart = text.Substring(_lastProcessedText.Length);
                return newPart.IndexOfAny(syntaxChars) >= 0;
            }
            
            return false;
        }

        private void SyntaxTimer_Tick(object? sender, EventArgs e)
        {
            _syntaxTimer?.Stop();
            ApplySyntaxHighlighting();
        }

        private void ApplySyntaxHighlighting()
        {
            if (_updating || this.Text.Length == 0) return;

            _updating = true;
            
            // 保存当前状态
            int selectionStart = this.SelectionStart;
            int selectionLength = this.SelectionLength;
            int scrollPos = GetScrollPos();

            try
            {
                // 使用更高效的区域更新策略
                ApplySmartSyntaxHighlighting();
                
                // 更新缓存
                _lastTextLength = this.Text.Length;
                _lastProcessedText = this.Text;
            }
            finally
            {
                // 恢复状态
                this.Select(selectionStart, selectionLength);
                SetScrollPos(scrollPos);
                _updating = false;
            }
        }
        
        private void ApplySmartSyntaxHighlighting()
        {
            // 首先重置所有格式到默认状态（只重置格式，不重置文本）
            this.SelectAll();
            this.SelectionColor = _syntaxStyles["normal"].color;
            this.SelectionFont = this.Font;
            
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
                return this.GetPositionFromCharIndex(0).Y;
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
                this.ScrollToCaret();
            }
            catch
            {
                // 忽略滚动错误
            }
        }

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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _syntaxTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}