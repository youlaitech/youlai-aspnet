using System.Collections.Generic;

namespace Youlai.Application.Codegen;

/// <summary>
/// 模板引擎渲染
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// 渲染模板文本
    /// </summary>
    /// <param name="templateText">模板原文</param>
    /// <param name="variables">顶层模板变量</param>
    /// <param name="subpackageName">可选的子包名变量，实现层会注入为 subpackageName</param>
    /// <returns>渲染后的内容</returns>
    string Render(string templateText, IReadOnlyDictionary<string, object?> variables, string? subpackageName = null);
}
