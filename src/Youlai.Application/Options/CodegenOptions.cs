namespace Youlai.Application.Options;

/// <summary>
/// 代码生成配置（对应 appsettings.json 的 Codegen 节点）
/// </summary>
public sealed class CodegenOptions
{
    public const string SectionName = "Codegen";

    /// <summary>保存配置时是否自动创建页面菜单与增删改查按钮，生产环境应关闭</summary>
    public bool CreateMenu { get; init; } = true;
}
