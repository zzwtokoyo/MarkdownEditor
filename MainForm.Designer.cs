namespace MarkdownEditor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem syntaxHighlightingToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel leftPanel;
        private System.Windows.Forms.Label lblFileList;
        private System.Windows.Forms.ListBox lstFiles;
        private System.Windows.Forms.Panel rightPanel;
        private System.Windows.Forms.SplitContainer splitContainer2;

        private MarkdownEditor.Controls.MarkdownRichTextBox markdownEditor;
        private MarkdownEditor.Controls.MarkdownToolBar markdownToolBar;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView2Preview;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;

        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.syntaxHighlightingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.leftPanel = new System.Windows.Forms.Panel();
            this.lstFiles = new System.Windows.Forms.ListBox();
            this.lblFileList = new System.Windows.Forms.Label();
            this.rightPanel = new System.Windows.Forms.Panel();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.markdownToolBar = new MarkdownEditor.Controls.MarkdownToolBar();
            this.markdownEditor = new MarkdownEditor.Controls.MarkdownRichTextBox();

            this.webView2Preview = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.leftPanel.SuspendLayout();
            this.rightPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView2Preview)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1200, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.fileToolStripMenuItem.Text = "文件(&F)";
            
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.openToolStripMenuItem.Text = "打开(&O)";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.OpenToolStripMenuItem_Click);
            
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.saveToolStripMenuItem.Text = "保存(&S)";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.SaveToolStripMenuItem_Click);
            
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.saveAsToolStripMenuItem.Text = "另存为(&A)";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(147, 6);
            
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.exitToolStripMenuItem.Text = "退出(&X)";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.ExitToolStripMenuItem_Click);
            
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.syntaxHighlightingToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.viewToolStripMenuItem.Text = "视图(&V)";
            
            // 
            // syntaxHighlightingToolStripMenuItem
            // 
            this.syntaxHighlightingToolStripMenuItem.Checked = true;
            this.syntaxHighlightingToolStripMenuItem.CheckOnClick = true;
            this.syntaxHighlightingToolStripMenuItem.Name = "syntaxHighlightingToolStripMenuItem";
            this.syntaxHighlightingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.syntaxHighlightingToolStripMenuItem.Text = "语法高亮(&S)";
            this.syntaxHighlightingToolStripMenuItem.Click += new System.EventHandler(this.SyntaxHighlightingToolStripMenuItem_Click);
            
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.leftPanel);
            
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.rightPanel);
            this.splitContainer1.Size = new System.Drawing.Size(1200, 704);
            this.splitContainer1.SplitterDistance = 300;
            this.splitContainer1.TabIndex = 1;
            
            // 
            // leftPanel
            // 
            this.leftPanel.Controls.Add(this.lstFiles);
            this.leftPanel.Controls.Add(this.lblFileList);
            this.leftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftPanel.Location = new System.Drawing.Point(0, 0);
            this.leftPanel.Name = "leftPanel";
            this.leftPanel.Size = new System.Drawing.Size(300, 704);
            this.leftPanel.TabIndex = 0;
            
            // 
            // lstFiles
            // 
            this.lstFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstFiles.FormattingEnabled = true;
            this.lstFiles.IntegralHeight = false;
            this.lstFiles.ItemHeight = 17;
            this.lstFiles.Location = new System.Drawing.Point(3, 26);
            this.lstFiles.Name = "lstFiles";
            this.lstFiles.Size = new System.Drawing.Size(294, 675);
            this.lstFiles.TabIndex = 1;
            this.lstFiles.SelectedIndexChanged += new System.EventHandler(this.LstFiles_SelectedIndexChanged);
            
            // 
            // lblFileList
            // 
            this.lblFileList.AutoSize = true;
            this.lblFileList.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblFileList.Location = new System.Drawing.Point(3, 6);
            this.lblFileList.Name = "lblFileList";
            this.lblFileList.Size = new System.Drawing.Size(68, 17);
            this.lblFileList.TabIndex = 0;
            this.lblFileList.Text = "文件列表";
            
            // 
            // rightPanel
            // 
            this.rightPanel.Controls.Add(this.splitContainer2);
            this.rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanel.Location = new System.Drawing.Point(0, 0);
            this.rightPanel.Name = "rightPanel";
            this.rightPanel.Size = new System.Drawing.Size(896, 704);
            this.rightPanel.TabIndex = 0;
            
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.markdownEditor);
            this.splitContainer2.Panel1.Controls.Add(this.markdownToolBar);
            
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.webView2Preview);
            this.splitContainer2.Size = new System.Drawing.Size(896, 704);
            this.splitContainer2.SplitterDistance = 448;
            this.splitContainer2.TabIndex = 0;
            
            // 
            // markdownToolBar
            // 
            this.markdownToolBar.Dock = System.Windows.Forms.DockStyle.Top;
            this.markdownToolBar.Location = new System.Drawing.Point(0, 0);
            this.markdownToolBar.Name = "markdownToolBar";
            this.markdownToolBar.Size = new System.Drawing.Size(448, 25);
            this.markdownToolBar.TabIndex = 1;
            this.markdownToolBar.Text = "markdownToolBar";
            
            // 
            // markdownEditor
            // 
            this.markdownEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.markdownEditor.Location = new System.Drawing.Point(0, 25);
            this.markdownEditor.Name = "markdownEditor";
            this.markdownEditor.Size = new System.Drawing.Size(448, 679);
            this.markdownEditor.TabIndex = 0;
            this.markdownEditor.Text = "";
            

            
            // 
            // webView2Preview
            // 
            this.webView2Preview.AllowExternalDrop = true;
            this.webView2Preview.CreationProperties = null;
            this.webView2Preview.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView2Preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webView2Preview.Location = new System.Drawing.Point(0, 0);
            this.webView2Preview.Name = "webView2Preview";
            this.webView2Preview.Size = new System.Drawing.Size(444, 704);
            this.webView2Preview.TabIndex = 0;
            this.webView2Preview.ZoomFactor = 1D;
            
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lblStatus});
            this.statusStrip1.Location = new System.Drawing.Point(0, 728);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1200, 22);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(32, 17);
            this.toolStripStatusLabel1.Text = "就绪";
            
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(1153, 17);
            this.lblStatus.Spring = true;
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Markdown 编辑器";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.leftPanel.ResumeLayout(false);
            this.leftPanel.PerformLayout();
            this.rightPanel.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.webView2Preview)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}