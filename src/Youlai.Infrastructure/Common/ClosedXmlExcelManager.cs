using System.Globalization;
using ClosedXML.Excel;
using Youlai.Application.Common;

namespace Youlai.Infrastructure.Common;

/// <summary>
/// 基于 ClosedXML 的 Excel 读写实现；列映射由 ExcelSheetSpec / ExcelImportSpec 驱动
/// </summary>
internal sealed class ClosedXmlExcelManager : IExcelManager
{
    public Task<byte[]> ExportAsync<T>(IEnumerable<T> rows, ExcelSheetSpec spec, CancellationToken cancellationToken = default)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(spec.SheetName);

        for (var i = 0; i < spec.Columns.Count; i++)
        {
            worksheet.Cell(1, i + 1).Value = spec.Columns[i].Header;
        }

        var props = spec.Columns
            .Select(c => typeof(T).GetProperty(c.PropertyName))
            .ToArray();

        var rowIndex = 2;
        foreach (var item in rows)
        {
            for (var i = 0; i < spec.Columns.Count; i++)
            {
                var prop = props[i];
                var value = prop is null || item is null ? null : prop.GetValue(item);
                SetCellValue(worksheet.Cell(rowIndex, i + 1), value);
            }

            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }

    public Task<ExcelImportResult<T>> ImportAsync<T>(Stream content, ExcelImportSpec<T> spec, CancellationToken cancellationToken = default)
        where T : class, new()
    {
        using var workbook = new XLWorkbook(content);
        var worksheet = workbook.Worksheets
            .FirstOrDefault(w => w.Name == spec.SheetName)
            ?? workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            return Task.FromResult(new ExcelImportResult<T>(0, 0, Array.Empty<string>(), Array.Empty<T>()));
        }

        // 表头行：表头文本 -> 列索引 + 属性
        var headerMap = new List<(int ColumnIndex, System.Reflection.PropertyInfo? Prop)>();
        var headerRow = worksheet.Row(1);
        foreach (var column in spec.Columns)
        {
            var colIndex = 0;
            foreach (var cell in headerRow.Cells())
            {
                colIndex++;
                if (string.Equals(cell.GetValue<string>().Trim(), column.Header, StringComparison.Ordinal))
                {
                    headerMap.Add((colIndex, typeof(T).GetProperty(column.PropertyName)));
                    break;
                }
            }
        }

        var rows = new List<T>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var row = 2; row <= lastRow; row++)
        {
            var item = new T();
            foreach (var (columnIndex, prop) in headerMap)
            {
                if (prop is null)
                {
                    continue;
                }

                var raw = worksheet.Cell(row, columnIndex).GetValue<string>()?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                SetProperty(prop, item, raw);
            }

            rows.Add(item);
        }

        return Task.FromResult(new ExcelImportResult<T>(rows.Count, 0, Array.Empty<string>(), rows));
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case string s:
                cell.Value = s;
                break;
            case int n:
                cell.Value = n;
                break;
            case long n:
                cell.Value = n;
                break;
            case double n:
                cell.Value = n;
                break;
            case decimal n:
                cell.Value = n;
                break;
            case bool b:
                cell.Value = b;
                break;
            case DateTime dt:
                cell.Value = dt;
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    private static void SetProperty(System.Reflection.PropertyInfo prop, object item, string raw)
    {
        var target = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
        object? converted = target switch
        {
            Type t when t == typeof(string) => raw,
            Type t when t == typeof(int) => int.TryParse(raw, out var v) ? v : default(int),
            Type t when t == typeof(long) => long.TryParse(raw, out var v) ? v : default(long),
            Type t when t == typeof(double) => double.TryParse(raw, out var v) ? v : default(double),
            Type t when t == typeof(decimal) => decimal.TryParse(raw, out var v) ? v : default(decimal),
            Type t when t == typeof(bool) => bool.TryParse(raw, out var v) ? v : raw == "1" || raw == "true",
            Type t when t == typeof(DateTime) => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : default(DateTime),
            Type t when t.IsEnum => Enum.TryParse(t, raw, out var v) ? v : default,
            _ => raw,
        };

        prop.SetValue(item, converted);
    }
}
