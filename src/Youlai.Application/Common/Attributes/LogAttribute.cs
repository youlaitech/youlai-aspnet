using Youlai.Application.Common.Enums;

namespace Youlai.Application.Common.Attributes;

/// <summary>
/// 操作日志标记属性
/// 用于标注需要记录操作日志的控制器方法
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class LogAttribute : Attribute
{
    /// <summary>
    /// 模块
    /// </summary>
    public LogModule Module { get; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public ActionType Value { get; }

    /// <summary>
    /// 操作标题（可选，默认使用枚举描述）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 自定义日志内容（可选，用于记录操作细节）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public LogAttribute(LogModule module, ActionType value)
    {
        Module = module;
        Value = value;
    }
}
