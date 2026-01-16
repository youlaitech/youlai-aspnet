using Youlai.Application.Common.Results;
using Youlai.Application.Platform.Codegen.Dtos;

namespace Youlai.Application.Platform.Codegen.Services;

/// <summary>
/// 代码生成服务
/// </summary>
public interface ICodegenService
{
    Task<PageResult<CodegenTableDto>> GetTablePageAsync(CodegenTableQuery query, CancellationToken cancellationToken = default);

    Task<GenConfigFormDto> GetConfigAsync(string tableName, CancellationToken cancellationToken = default);

    Task<bool> SaveConfigAsync(string tableName, GenConfigFormDto formData, CancellationToken cancellationToken = default);

    Task<bool> DeleteConfigAsync(string tableName, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CodegenPreviewDto>> GetPreviewAsync(
        string tableName,
        string pageType,
        string type,
        CancellationToken cancellationToken = default);

    Task<(string FileName, byte[] Content)> DownloadAsync(
        string[] tableNames,
        string pageType,
        string type,
        CancellationToken cancellationToken = default);
}
