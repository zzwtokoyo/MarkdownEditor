using System;
using System.Windows.Forms;

namespace MarkdownEditor.Helpers
{
    /// <summary>
    /// 异常处理辅助类，提供统一的异常处理方法
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// 执行操作并处理异常，显示错误消息框
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="errorMessage">错误消息前缀</param>
        /// <param name="icon">消息框图标</param>
        /// <returns>操作是否成功</returns>
        public static bool TryExecute(Action action, string errorMessage, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, icon);
                return false;
            }
        }

        /// <summary>
        /// 执行操作并处理异常，返回结果或默认值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <param name="errorMessage">错误消息前缀</param>
        /// <param name="showMessage">是否显示错误消息</param>
        /// <returns>函数结果或默认值</returns>
        public static T TryExecute<T>(Func<T> func, T defaultValue, string errorMessage = "", bool showMessage = false)
        {
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                if (showMessage && !string.IsNullOrEmpty(errorMessage))
                {
                    MessageBox.Show($"{errorMessage}: {ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"Exception in {errorMessage}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// 执行操作并处理异常，仅记录调试信息
        /// </summary>
        /// <param name="action">要执行的操作</param>
        /// <param name="operationName">操作名称，用于调试</param>
        /// <returns>操作是否成功</returns>
        public static bool TryExecuteSilent(Action action, string operationName = "")
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent exception in {operationName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 异步执行操作并处理异常，显示错误消息框
        /// </summary>
        /// <param name="asyncAction">要执行的异步操作</param>
        /// <param name="errorMessage">错误消息前缀</param>
        /// <param name="icon">消息框图标</param>
        /// <returns>操作是否成功</returns>
        public static async System.Threading.Tasks.Task<bool> TryExecute(Func<System.Threading.Tasks.Task> asyncAction, string errorMessage, MessageBoxIcon icon = MessageBoxIcon.Error)
        {
            try
            {
                await asyncAction();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{errorMessage}: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, icon);
                return false;
            }
        }

        /// <summary>
        /// 异步执行操作并处理异常，返回结果或默认值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="asyncFunc">要执行的异步函数</param>
        /// <param name="defaultValue">异常时返回的默认值</param>
        /// <param name="errorMessage">错误消息前缀</param>
        /// <param name="showMessage">是否显示错误消息</param>
        /// <returns>函数结果或默认值</returns>
        public static async System.Threading.Tasks.Task<T> TryExecute<T>(Func<System.Threading.Tasks.Task<T>> asyncFunc, T defaultValue, string errorMessage = "", bool showMessage = false)
        {
            try
            {
                return await asyncFunc();
            }
            catch (Exception ex)
            {
                if (showMessage && !string.IsNullOrEmpty(errorMessage))
                {
                    MessageBox.Show($"{errorMessage}: {ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"Exception in {errorMessage}: {ex.Message}");
                return defaultValue;
            }
        }
    }
}