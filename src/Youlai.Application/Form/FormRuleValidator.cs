using System.Text.Json;
using Youlai.Application.Exceptions;
using Youlai.Application.Results;

namespace Youlai.Application.Form;

/// <summary>
/// 表单规则校验器（后端基础校验 + 字段白名单）
/// </summary>
public static class FormRuleValidator
{
    /// <summary>
    /// 校验并过滤表单数据：仅保留 rule 中声明 field 的数据项，逐字段执行必填与类型校验
    /// </summary>
    public static Dictionary<string, JsonElement> ValidateAndFilter(string formJson, JsonElement data)
    {
        var rules = ParseRules(formJson);
        if (rules.ValueKind != JsonValueKind.Array)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单规则格式非法");
        }

        var filtered = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var errors = new List<string>();
        Walk(rules, data, filtered, errors);
        if (errors.Count > 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, string.Join("；", errors));
        }

        return filtered;
    }

    /// <summary>
    /// 校验表单规则结构（AI 生成或外部导入的规则先过这一层）
    /// </summary>
    public static string ValidateRule(string formJson)
    {
        var rules = ParseRules(formJson);
        if (rules.ValueKind != JsonValueKind.Array || rules.GetArrayLength() == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单规则格式非法：应为非空数组");
        }

        CheckNodes(rules);
        return formJson;
    }

    /// <summary>
    /// 解析规则里 field -> title 的映射
    /// </summary>
    public static Dictionary<string, string> ResolveTitles(string? formJson)
    {
        var titles = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(formJson))
        {
            return titles;
        }

        try
        {
            CollectTitles(ParseRules(formJson), titles);
        }
        catch (JsonException)
        {
            // 规则损坏时按无标题处理
        }

        return titles;
    }

    private static JsonElement ParseRules(string formJson)
    {
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(formJson) ? "[]" : formJson);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单规则解析失败");
        }
    }

    /// <summary>
    /// 递归校验规则节点：每个节点必须有 type，非容器节点必须有 field
    /// </summary>
    private static void CheckNodes(JsonElement nodes)
    {
        foreach (var node in nodes.EnumerateArray())
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "表单规则格式非法：节点必须为对象");
            }

            if (!node.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "表单规则格式非法：节点缺少 type");
            }

            if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array && children.GetArrayLength() > 0)
            {
                CheckNodes(children);
            }
            else if (!node.TryGetProperty("field", out var field) || field.ValueKind != JsonValueKind.String)
            {
                throw new BusinessException(ResultCode.InvalidUserInput, "表单规则格式非法：字段节点缺少 field");
            }
        }
    }

    /// <summary>
    /// 递归收集 field 与 title
    /// </summary>
    private static void CollectTitles(JsonElement nodes, Dictionary<string, string> titles)
    {
        if (nodes.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var node in nodes.EnumerateArray())
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (node.TryGetProperty("field", out var field) && field.ValueKind == JsonValueKind.String
                && node.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                titles.TryAdd(field.GetString()!, title.GetString()!);
            }

            if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
            {
                CollectTitles(children, titles);
            }
        }
    }

    /// <summary>
    /// 递归遍历规则节点：布局容器自身无 field，其 children 递归收集
    /// </summary>
    private static void Walk(JsonElement nodes, JsonElement data, Dictionary<string, JsonElement> filtered, List<string> errors)
    {
        if (nodes.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var node in nodes.EnumerateArray())
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (node.TryGetProperty("field", out var fieldNode) && fieldNode.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(fieldNode.GetString()))
            {
                var field = fieldNode.GetString()!;
                // 字段白名单：rule 未声明的 field 直接丢弃
                if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty(field, out var value))
                {
                    filtered[field] = value.Clone();
                }

                ValidateField(node, field, data, errors);
            }

            if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
            {
                Walk(children, data, filtered, errors);
            }
        }
    }

    /// <summary>
    /// 单字段校验：必填与基础类型
    /// </summary>
    private static void ValidateField(JsonElement ruleNode, string field, JsonElement data, List<string> errors)
    {
        if (!ruleNode.TryGetProperty("validate", out var validateArray) || validateArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var title = ResolveTitle(ruleNode, field);
        JsonElement value = default;
        var hasValue = data.ValueKind == JsonValueKind.Object && data.TryGetProperty(field, out value);
        var empty = !hasValue || IsEmpty(value);

        foreach (var validate in validateArray.EnumerateArray())
        {
            if (validate.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // 必填：null/空串/空集合均视为未填写
            if (validate.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True && empty)
            {
                errors.Add(RequiredMessage(validate, title));
            }

            // 类型：仅值非空时校验，避免与 required 错误重复
            if (!empty && validate.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String)
            {
                if (!CheckType(type.GetString(), value))
                {
                    errors.Add("「" + title + "」数据类型非法");
                }
            }
        }
    }

    /// <summary>
    /// 解析字段标题（错误提示用），缺省回退字段名
    /// </summary>
    private static string ResolveTitle(JsonElement ruleNode, string field)
    {
        return ruleNode.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(title.GetString())
            ? title.GetString()!
            : field;
    }

    /// <summary>
    /// 必填错误消息：优先取 rule validate 里配置的 message
    /// </summary>
    private static string RequiredMessage(JsonElement validate, string title)
    {
        return validate.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
            && !string.IsNullOrEmpty(message.GetString())
            ? message.GetString()!
            : "「" + title + "」不能为空";
    }

    /// <summary>
    /// 空值判定：JSON null、空字符串、空数组
    /// </summary>
    private static bool IsEmpty(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => true,
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Array => value.GetArrayLength() == 0,
            _ => false,
        };
    }

    /// <summary>
    /// 基础类型校验：number（数字或数字串）/ array（数组），文本类型不做强校验
    /// </summary>
    private static bool CheckType(string? type, JsonElement value)
    {
        return type switch
        {
            "number" or "integer" or "float" => IsNumeric(value),
            "array" => value.ValueKind == JsonValueKind.Array,
            _ => true,
        };
    }

    /// <summary>
    /// 数字判定：JSON 数字或可解析为 double 的字符串
    /// </summary>
    private static bool IsNumeric(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number)
        {
            return true;
        }

        return value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), out _);
    }
}
