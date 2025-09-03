using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// 配置服务，用于保存和加载用户设置
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configFilePath;
        private Dictionary<string, object> _settings;
        
        public ConfigurationService()
        {
            // 将配置文件保存在用户的应用数据目录
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appDataPath, "MarkdownEditor");
            
            // 确保目录存在
            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }
            
            _configFilePath = Path.Combine(appFolder, "settings.json");
            _settings = new Dictionary<string, object>();
            
            LoadSettings();
        }
        
        /// <summary>
        /// 获取设置值
        /// </summary>
        /// <typeparam name="T">设置值的类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>设置值</returns>
        public T GetSetting<T>(string key, T defaultValue = default(T))
        {
            if (_settings.TryGetValue(key, out object value))
            {
                try
                {
                    if (value is JsonElement jsonElement)
                    {
                        return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                    }
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }
        
        /// <summary>
        /// 设置设置值
        /// </summary>
        /// <typeparam name="T">设置值的类型</typeparam>
        /// <param name="key">设置键</param>
        /// <param name="value">设置值</param>
        public void SetSetting<T>(string key, T value)
        {
            _settings[key] = value;
            SaveSettings();
        }
        
        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
                }
            }
            catch (Exception ex)
            {
                // 如果加载失败，使用默认设置
                _settings = new Dictionary<string, object>();
                System.Diagnostics.Debug.WriteLine($"加载配置文件失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 保存设置
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置文件失败: {ex.Message}");
            }
        }
    }
}