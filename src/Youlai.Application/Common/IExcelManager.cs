namespace Youlai.Application.Common;

/// <summary>Excel 列定义：表头文本与对应实体属性名</summary>
public sealed record ExcelColumnSpec(string Header, string PropertyName);

/// <summary>导出表定义：工作表名与列集合</summary>
public sealed record ExcelSheetSpec(string SheetName, IReadOnlyList<ExcelColumnSpec> Columns);

/// <summary>导入表定义：工作表名与列集合（按表头文本映射到实体属性）</summary>
public sealed record ExcelImportSpec<T>(string SheetName, IReadOnlyList<ExcelColumnSpec> Columns) where T : new();

/// <summary>导入结果：有效/无效行数与消息、已解析的行数据</summary>
public sealed record ExcelImportResult<T>(int ValidCount, int InvalidCount, IReadOnlyList<string> Messages, IReadOnlyList<T> Rows) where T : class;

/// <summary>
/// Excel 文件读写
/// </summary>
public interface IExcelManager
{
    /// <summary>将行数据导出为 xlsx 字节流</summary>
    Task<byte[]> ExportAsync<T>(IEnumerable<T> rows, ExcelSheetSpec spec, CancellationToken cancellationToken = default);

    /// <summary>从 xlsx 字节流读取行，按表头映射到 T</summary>
    Task<ExcelImportResult<T>> ImportAsync<T>(Stream content, ExcelImportSpec<T> spec, CancellationToken cancellationToken = default) where T : class, new();
}
