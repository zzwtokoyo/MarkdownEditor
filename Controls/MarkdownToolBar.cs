using System;
using System.Drawing;
using System.Windows.Forms;

namespace MarkdownEditor.Controls
{
    public class MarkdownToolBar : ToolStrip
    {
        public event EventHandler<string>? FormatRequested;

        public MarkdownToolBar()
        {
            InitializeComponent();
            CreateToolbarButtons();
        }

        private void InitializeComponent()
        {
            this.ImageScalingSize = ScaleSize(new Size(16, 16));
            this.Font = ScaleFont(new Font("Microsoft YaHei UI", 9F));
        }

        private Size ScaleSize(Size originalSize)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiScale = g.DpiX / 96f;
                if (dpiScale > 1.0f)
                {
                    return new Size(
                        (int)(originalSize.Width * dpiScale),
                        (int)(originalSize.Height * dpiScale)
                    );
                }
                return originalSize;
            }
        }

        private Font ScaleFont(Font originalFont)
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiScale = g.DpiX / 96f;
                if (dpiScale > 1.0f)
                {
                    float newSize = originalFont.Size * Math.Min(dpiScale, 1.3f);
                    return new Font(originalFont.FontFamily, newSize, originalFont.Style);
                }
                return originalFont;
            }
        }

        private void CreateToolbarButtons()
        {
            // 粗体按钮
            var boldButton = CreateButton("B", "粗体 (Ctrl+B)", "bold");
            boldButton.Font = new Font(boldButton.Font, FontStyle.Bold);
            this.Items.Add(boldButton);

            // 斜体按钮
            var italicButton = CreateButton("I", "斜体 (Ctrl+I)", "italic");
            italicButton.Font = new Font(italicButton.Font, FontStyle.Italic);
            this.Items.Add(italicButton);

            // 分隔符
            this.Items.Add(new ToolStripSeparator());

            // 标题按钮
            this.Items.Add(CreateButton("H1", "一级标题", "h1"));
            this.Items.Add(CreateButton("H2", "二级标题", "h2"));
            this.Items.Add(CreateButton("H3", "三级标题", "h3"));

            // 分隔符
            this.Items.Add(new ToolStripSeparator());

            // 代码按钮
            this.Items.Add(CreateButton("</>", "行内代码", "code"));

            // 链接按钮
            this.Items.Add(CreateButton("🔗", "链接", "link"));

            // 引用按钮
            this.Items.Add(CreateButton(">", "引用", "quote"));

            // 列表按钮
            this.Items.Add(CreateButton("•", "无序列表", "list"));

            // 分隔符
            this.Items.Add(new ToolStripSeparator());

            // 表格按钮
            this.Items.Add(CreateButton("⊞", "插入表格", "table"));

            // 水平线按钮
            this.Items.Add(CreateButton("—", "水平分割线", "hr"));
        }

        private ToolStripButton CreateButton(string text, string tooltip, string command)
        {
            var button = new ToolStripButton(text)
            {
                ToolTipText = tooltip,
                Tag = command,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                AutoSize = true
            };
            
            button.Click += Button_Click;
            return button;
        }

        private void Button_Click(object? sender, EventArgs e)
        {
            if (sender is ToolStripButton button && button.Tag is string command)
            {
                FormatRequested?.Invoke(this, command);
            }
        }
    }
}