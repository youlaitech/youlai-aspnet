using Youlai.Application.Results;
using Youlai.Application.Codegen.Models;

namespace Youlai.Application.Codegen;

/// <summary>
/// 代码生成服务
/// </summary>
public interface ICodegenService
{
    /// <summary>分页查询数据源表</summary>
    Task<PageResult<CodegenTableDto>> GetTablePageAsync(CodegenTableQuery query, CancellationToken cancellationToken = default);

    /// <summary>获取指定表的生成配置</summary>
    Task<GenConfigFormDto> GetConfigAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>保存生成配置</summary>
    Task<bool> SaveConfigAsync(string tableName, GenConfigFormDto formData, CancellationToken cancellationToken = default);

    /// <summary>删除生成配置</summary>
    Task<bool> RemoveConfigAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>预览生成代码</summary>
    Task<IReadOnlyCollection<CodegenPreviewDto>> GetPreviewAsync(
        string tableName,
        string pageType,
        string type,
        CancellationToken cancellationToken = default);

    /// <summary>打包下载生成代码</summary>
    Task<(string FileName, byte[] Content)> DownloadAsync(
        string[] tableNames,
        string pageType,
        string type,
        CancellationToken cancellationToken = default);
}
