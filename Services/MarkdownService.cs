using MarkdownSharp;
using System;
using System.IO;
using System.Text;

namespace MarkdownEditor.Services
{
    public class MarkdownService
    {
        private readonly Markdown _markdown;

        public MarkdownService()
        {
            _markdown = new Markdown();
        }

        public string ConvertToHtml(string markdownText)
        {
            try
            {
                if (string.IsNullOrEmpty(markdownText))
                    return "<p>No content to display</p>";

                var html = _markdown.Transform(markdownText);
                return WrapInHtmlDocument(html);
            }
            catch (Exception ex)
            {
                return $"<p>Error converting markdown: {ex.Message}</p>";
            }
        }

        private string WrapInHtmlDocument(string bodyHtml)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Markdown Preview</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 900px;
            margin: 0 auto;
            padding: 20px;
            background-color: #fff;
        }}
        
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        
        h1 {{ border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
        h2 {{ border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
        
        p {{ margin-bottom: 16px; }}
        
        code {{
            background-color: rgba(27,31,35,0.05);
            border-radius: 3px;
            font-size: 85%;
            margin: 0;
            padding: 0.2em 0.4em;
        }}
        
        pre {{
            background-color: #f6f8fa;
            border-radius: 6px;
            font-size: 85%;
            line-height: 1.45;
            overflow: auto;
            padding: 16px;
        }}
        
        pre code {{
            background-color: transparent;
            border: 0;
            display: inline;
            line-height: inherit;
            margin: 0;
            overflow: visible;
            padding: 0;
        }}
        
        blockquote {{
            border-left: 4px solid #dfe2e5;
            margin: 0;
            padding: 0 16px;
            color: #6a737d;
        }}
        
        table {{
            border-collapse: collapse;
            border-spacing: 0;
            width: 100%;
        }}
        
        table th, table td {{
            border: 1px solid #dfe2e5;
            padding: 6px 13px;
        }}
        
        table th {{
            background-color: #f6f8fa;
            font-weight: 600;
        }}
        
        ul, ol {{
            padding-left: 30px;
        }}
        
        li {{
            margin-bottom: 4px;
        }}
        
        hr {{
            border: none;
            height: 1px;
            background-color: #e1e4e8;
            margin: 24px 0;
        }}
        
        a {{
            color: #0366d6;
            text-decoration: none;
        }}
        
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    {bodyHtml}
</body>
</html>";
        }

        public string LoadFileContent(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return string.Empty;

                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to load file: {ex.Message}", ex);
            }
        }

        public void SaveFileContent(string filePath, string content)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(filePath, content, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save file: {ex.Message}", ex);
            }
        }
    }
}