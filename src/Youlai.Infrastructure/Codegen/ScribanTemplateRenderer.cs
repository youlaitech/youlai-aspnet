using System.Collections.Generic;
using System.Globalization;
using Scriban;
using Scriban.Runtime;
using Youlai.Application.Codegen;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;

namespace Youlai.Infrastructure.Codegen;

/// <summary>
/// 基于 Scriban 的模板渲染，注入 date/subpackageName 等变量
/// </summary>
internal sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    public string Render(string templateText, IReadOnlyDictionary<string, object?> variables, string? subpackageName = null)
    {
        var template = Template.Parse(templateText);
        if (template.HasErrors)
        {
            var details = string.Join("; ", template.Messages
                .Select(message => $"{message.Message} (line {message.Span.Start.Line + 1}, col {message.Span.Start.Column + 1})"));
            throw new BusinessException(ResultCode.SystemError, $"模板解析失败. {details}");
        }

        var scriptObject = new ScriptObject();
        foreach (var (key, value) in variables)
        {
            scriptObject[key] = value;
        }

        if (!string.IsNullOrEmpty(subpackageName))
        {
            scriptObject["subpackageName"] = subpackageName;
        }

        scriptObject["date"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var context = new TemplateContext
        {
            MemberRenamer = member => member.Name
        };
        context.PushGlobal(scriptObject);

        return template.Render(context);
    }
}
