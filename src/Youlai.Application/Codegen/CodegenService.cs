using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Compression;
using System.Text;
using Youlai.Application.Codegen.Models;
using Youlai.Application.Exceptions;
using Youlai.Application.Persistence;
using Youlai.Application.Results;

namespace Youlai.Application.Codegen;

/// <summary>
/// 代码生成服务
/// </summary>
internal sealed class CodegenService : ICodegenService
{
    private const string DownloadFileName = "youlai-admin-code.zip";
    private const string BackendAppName = "youlai-aspnet";
    private const string FrontendAppName = "vue3-element-admin";
    private const string DefaultAuthor = "youlaitech";
    private const string DefaultModuleName = "system";
    private const string DefaultPackageName = "Youlai";
    private const string DefaultRemoveTablePrefix = "sys_";

    private static readonly IReadOnlyDictionary<string, TemplateConfig> TemplateConfigs =
        new Dictionary<string, TemplateConfig>(StringComparer.OrdinalIgnoreCase)
        {
            ["API"] = new TemplateConfig("frontend/ts/api.ts.sbn", "api", ".ts"),
            ["API_TYPES"] = new TemplateConfig("frontend/ts/api-types.ts.sbn", "types", ".ts"),
            ["VIEW"] = new TemplateConfig("frontend/ts/index.vue.sbn", "views", ".vue"),
            ["Controller"] = new TemplateConfig("backend/controller.cs.sbn", "Controllers", ".cs"),
            ["Service"] = new TemplateConfig("backend/service.cs.sbn", "Services", ".cs"),
            ["ServiceImpl"] = new TemplateConfig("backend/service-impl.cs.sbn", "Services", ".cs"),
            ["Form"] = new TemplateConfig("backend/form.cs.sbn", "Dtos", ".cs"),
            ["Query"] = new TemplateConfig("backend/query.cs.sbn", "Dtos", ".cs"),
            ["PageVo"] = new TemplateConfig("backend/page-vo.cs.sbn", "Dtos", ".cs"),
            ["Entity"] = new TemplateConfig("backend/entity.cs.sbn", "Entities", ".cs"),
        };

    private readonly ISqlExecutor _sqlExecutor;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly string _templateRoot;

    public CodegenService(ISqlExecutor sqlExecutor, ITemplateRenderer templateRenderer)
    {
        _sqlExecutor = sqlExecutor;
        _templateRenderer = templateRenderer;
        _templateRoot = Path.Combine(AppContext.BaseDirectory, "CodegenTemplates");
    }

    /// <summary>
    /// 分页查询数据库表，标记每张表是否已配置代码生成
    /// </summary>
    public async Task<PageResult<CodegenTableDto>> GetTablePageAsync(
        CodegenTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var (pageNum, pageSize) = query.Normalize();
        var keywords = query.Keywords?.Trim();

        var where = "t.TABLE_SCHEMA = DATABASE() AND t.TABLE_NAME NOT IN ('gen_table','gen_table_column')";
        var parameters = new List<DbParameter>();
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            where += " AND t.TABLE_NAME LIKE @keywords";
            parameters.Add(await CreateParameterAsync("@keywords", $"%{keywords}%", cancellationToken));
        }

        var countSql = $"SELECT COUNT(1) AS total FROM information_schema.TABLES t WHERE {where}";
        var total = (int)await ExecuteScalarLongAsync(countSql, parameters, cancellationToken).ConfigureAwait(false);

        var offset = (pageNum - 1) * pageSize;
        var listSql = $@"
SELECT
  t.TABLE_NAME AS tableName,
  t.TABLE_COMMENT AS tableComment,
  t.TABLE_COLLATION AS tableCollation,
  t.ENGINE AS engine,
  DATE_FORMAT(t.CREATE_TIME, '%Y-%m-%d %H:%i:%s') AS createTime,
  IF(c.id IS NULL, 0, 1) AS isConfigured
FROM information_schema.TABLES t
LEFT JOIN gen_table c
  ON c.table_name = t.TABLE_NAME AND c.is_deleted = 0
WHERE {where}
ORDER BY t.CREATE_TIME DESC
LIMIT @limit OFFSET @offset";

        var listParams = new List<DbParameter>(parameters)
        {
            await CreateParameterAsync("@limit", pageSize, cancellationToken),
            await CreateParameterAsync("@offset", offset, cancellationToken)
        };

        var list = await ExecuteQueryAsync(listSql, listParams, reader =>
        {
            var collation = reader["tableCollation"]?.ToString() ?? string.Empty;
            var charset = collation.Contains('_') ? collation.Split('_')[0] : string.Empty;
            return new CodegenTableDto
            {
                TableName = reader["tableName"]?.ToString() ?? string.Empty,
                TableComment = reader["tableComment"]?.ToString() ?? string.Empty,
                Engine = reader["engine"]?.ToString() ?? string.Empty,
                TableCollation = collation,
                Charset = charset,
                CreateTime = reader["createTime"]?.ToString() ?? string.Empty,
                IsConfigured = Convert.ToInt32(reader["isConfigured"] ?? 0)
            };
        }, cancellationToken).ConfigureAwait(false);

        return PageResult<CodegenTableDto>.Success(list, total, pageNum, pageSize);
    }

    /// <summary>
    /// 取表的生成配置；未配置过则按表结构和注释推导默认配置
    /// </summary>
    public async Task<GenConfigFormDto> GetConfigAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigRowAsync(tableName, cancellationToken).ConfigureAwait(false);
        if (config is not null)
        {
            var fieldConfigs = await GetFieldConfigsAsync(config.Id, cancellationToken).ConfigureAwait(false);
            return new GenConfigFormDto
            {
                Id = config.Id,
                TableName = config.TableName,
                ModuleName = config.ModuleName,
                PackageName = config.PackageName,
                BusinessName = config.BusinessName,
                EntityName = config.EntityName,
                Author = config.Author,
                ParentMenuId = config.ParentMenuId,
                BackendAppName = BackendAppName,
                FrontendAppName = FrontendAppName,
                RemoveTablePrefix = config.RemoveTablePrefix ?? DefaultRemoveTablePrefix,
                PageType = string.IsNullOrWhiteSpace(config.PageType) ? "classic" : config.PageType,
                FieldConfigs = fieldConfigs
            };
        }

        var tableComment = await GetTableCommentAsync(tableName, cancellationToken).ConfigureAwait(false);
        var businessName = !string.IsNullOrWhiteSpace(tableComment)
            ? tableComment.Replace("Table", string.Empty).Trim()
            : tableName;

        var removePrefix = DefaultRemoveTablePrefix;
        var processed = tableName.StartsWith(removePrefix, StringComparison.OrdinalIgnoreCase)
            ? tableName[removePrefix.Length..]
            : tableName;
        var entityName = ToPascalCase(processed);

        var fieldConfigsFromDb = await GetDefaultFieldConfigsAsync(tableName, cancellationToken).ConfigureAwait(false);

        return new GenConfigFormDto
        {
            TableName = tableName,
            BusinessName = businessName,
            ModuleName = DefaultModuleName,
            PackageName = DefaultPackageName,
            EntityName = entityName,
            Author = DefaultAuthor,
            ParentMenuId = null,
            BackendAppName = BackendAppName,
            FrontendAppName = FrontendAppName,
            RemoveTablePrefix = removePrefix,
            PageType = "classic",
            FieldConfigs = fieldConfigsFromDb
        };
    }

    /// <summary>
    /// 保存表配置及字段配置，同一事务内先写主表再覆盖字段行
    /// </summary>
    public async Task<bool> SaveConfigAsync(string tableName, GenConfigFormDto formData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "表名不能为空");
        }

        var moduleName = string.IsNullOrWhiteSpace(formData.ModuleName) ? DefaultModuleName : formData.ModuleName!.Trim();
        var packageName = string.IsNullOrWhiteSpace(formData.PackageName) ? DefaultPackageName : formData.PackageName!.Trim();
        var businessName = string.IsNullOrWhiteSpace(formData.BusinessName) ? tableName : formData.BusinessName!.Trim();
        var entityName = string.IsNullOrWhiteSpace(formData.EntityName) ? ToPascalCase(tableName) : formData.EntityName!.Trim();
        var author = string.IsNullOrWhiteSpace(formData.Author) ? DefaultAuthor : formData.Author!.Trim();
        var removePrefix = string.IsNullOrWhiteSpace(formData.RemoveTablePrefix) ? DefaultRemoveTablePrefix : formData.RemoveTablePrefix!.Trim();
        var pageType = string.Equals(formData.PageType, "curd", StringComparison.OrdinalIgnoreCase) ? "curd" : "classic";
        var parentMenuId = formData.ParentMenuId;

        var fieldConfigs = formData.FieldConfigs?.ToList() ?? new List<FieldConfigDto>();

        var connection = await _sqlExecutor.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var configId = await GetConfigIdAsync(tableName, connection, transaction, cancellationToken).ConfigureAwait(false);
            if (configId is null)
            {
                var insertSql = @"
INSERT INTO gen_table
(table_name, module_name, package_name, business_name, entity_name, author, parent_menu_id, remove_table_prefix, page_type, create_time, update_time, is_deleted)
VALUES
(@tableName, @moduleName, @packageName, @businessName, @entityName, @author, @parentMenuId, @removeTablePrefix, @pageType, NOW(), NOW(), 0)";

                var insertParams = new List<DbParameter>
                {
                    await CreateParameterAsync("@tableName", tableName, cancellationToken),
                    await CreateParameterAsync("@moduleName", moduleName, cancellationToken),
                    await CreateParameterAsync("@packageName", packageName, cancellationToken),
                    await CreateParameterAsync("@businessName", businessName, cancellationToken),
                    await CreateParameterAsync("@entityName", entityName, cancellationToken),
                    await CreateParameterAsync("@author", author, cancellationToken),
                    await CreateParameterAsync("@parentMenuId", parentMenuId, cancellationToken),
                    await CreateParameterAsync("@removeTablePrefix", removePrefix, cancellationToken),
                    await CreateParameterAsync("@pageType", pageType, cancellationToken)
                };

                await ExecuteNonQueryAsync(insertSql, insertParams, connection, transaction, cancellationToken).ConfigureAwait(false);
                configId = await GetConfigIdAsync(tableName, connection, transaction, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var updateSql = @"
UPDATE gen_table
SET module_name = @moduleName,
    package_name = @packageName,
    business_name = @businessName,
    entity_name = @entityName,
    author = @author,
    parent_menu_id = @parentMenuId,
    remove_table_prefix = @removeTablePrefix,
    page_type = @pageType,
    update_time = NOW(),
    is_deleted = 0
WHERE id = @id";

                var updateParams = new List<DbParameter>
                {
                    await CreateParameterAsync("@moduleName", moduleName, cancellationToken),
                    await CreateParameterAsync("@packageName", packageName, cancellationToken),
                    await CreateParameterAsync("@businessName", businessName, cancellationToken),
                    await CreateParameterAsync("@entityName", entityName, cancellationToken),
                    await CreateParameterAsync("@author", author, cancellationToken),
                    await CreateParameterAsync("@parentMenuId", parentMenuId, cancellationToken),
                    await CreateParameterAsync("@removeTablePrefix", removePrefix, cancellationToken),
                    await CreateParameterAsync("@pageType", pageType, cancellationToken),
                    await CreateParameterAsync("@id", configId, cancellationToken)
                };

                await ExecuteNonQueryAsync(updateSql, updateParams, connection, transaction, cancellationToken).ConfigureAwait(false);
            }

            if (configId is null)
            {
                throw new BusinessException(ResultCode.DatabaseServiceError, "保存配置失败");
            }

            var deleteSql = "DELETE FROM gen_table_column WHERE table_id = @tableId";
            await ExecuteNonQueryAsync(deleteSql, new List<DbParameter> { await CreateParameterAsync("@tableId", configId, cancellationToken) }, connection, transaction, cancellationToken)
                .ConfigureAwait(false);

            var sort = 1;
            foreach (var field in fieldConfigs)
            {
                if (string.IsNullOrWhiteSpace(field.ColumnName))
                {
                    continue;
                }

                var fieldSort = field.FieldSort ?? sort;
                var insertColumnSql = @"
INSERT INTO gen_table_column
(table_id, column_name, column_type, field_name, field_type, field_sort, field_comment, max_length,
 is_required, is_show_in_list, is_show_in_form, is_show_in_query, query_type, form_type, dict_type, create_time, update_time)
VALUES
(@tableId, @columnName, @columnType, @fieldName, @fieldType, @fieldSort, @fieldComment, @maxLength,
 @isRequired, @isShowInList, @isShowInForm, @isShowInQuery, @queryType, @formType, @dictType, NOW(), NOW())";

                var insertColumnParams = new List<DbParameter>
                {
                    await CreateParameterAsync("@tableId", configId, cancellationToken),
                    await CreateParameterAsync("@columnName", field.ColumnName, cancellationToken),
                    await CreateParameterAsync("@columnType", field.ColumnType, cancellationToken),
                    await CreateParameterAsync("@fieldName", string.IsNullOrWhiteSpace(field.FieldName) ? ToCamelCase(field.ColumnName!) : field.FieldName, cancellationToken),
                    await CreateParameterAsync("@fieldType", field.FieldType ?? string.Empty, cancellationToken),
                    await CreateParameterAsync("@fieldSort", fieldSort, cancellationToken),
                    await CreateParameterAsync("@fieldComment", field.FieldComment, cancellationToken),
                    await CreateParameterAsync("@maxLength", field.MaxLength, cancellationToken),
                    await CreateParameterAsync("@isRequired", field.IsRequired ?? 0, cancellationToken),
                    await CreateParameterAsync("@isShowInList", field.IsShowInList ?? 0, cancellationToken),
                    await CreateParameterAsync("@isShowInForm", field.IsShowInForm ?? 0, cancellationToken),
                    await CreateParameterAsync("@isShowInQuery", field.IsShowInQuery ?? 0, cancellationToken),
                    await CreateParameterAsync("@queryType", field.QueryType ?? 1, cancellationToken),
                    await CreateParameterAsync("@formType", field.FormType ?? 1, cancellationToken),
                    await CreateParameterAsync("@dictType", field.DictType, cancellationToken)
                };

                await ExecuteNonQueryAsync(insertColumnSql, insertColumnParams, connection, transaction, cancellationToken)
                    .ConfigureAwait(false);
                sort++;
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await _sqlExecutor.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 逻辑删除表配置并清空其字段行
    /// </summary>
    public async Task<bool> RemoveConfigAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigRowAsync(tableName, cancellationToken).ConfigureAwait(false);
        if (config is null)
        {
            return true;
        }

        var connection = await _sqlExecutor.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var deleteColumnSql = "DELETE FROM gen_table_column WHERE table_id = @tableId";
            await ExecuteNonQueryAsync(deleteColumnSql, new List<DbParameter> { await CreateParameterAsync("@tableId", config.Id, cancellationToken) }, connection, transaction, cancellationToken)
                .ConfigureAwait(false);

            var updateSql = "UPDATE gen_table SET is_deleted = 1, update_time = NOW() WHERE id = @id";
            await ExecuteNonQueryAsync(updateSql, new List<DbParameter> { await CreateParameterAsync("@id", config.Id, cancellationToken) }, connection, transaction, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await _sqlExecutor.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 按配置渲染各模板，返回前后端各文件的预览内容
    /// </summary>
    public async Task<IReadOnlyCollection<CodegenPreviewDto>> GetPreviewAsync(
        string tableName,
        string pageType,
        string type,
        CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(tableName, cancellationToken).ConfigureAwait(false);
        var effectivePageType = string.Equals(config.PageType, "curd", StringComparison.OrdinalIgnoreCase)
            ? "curd"
            : string.Equals(pageType, "curd", StringComparison.OrdinalIgnoreCase)
                ? "curd"
                : "classic";
        var frontendType = string.Equals(type, "js", StringComparison.OrdinalIgnoreCase) ? "js" : "ts";

        var entityName = string.IsNullOrWhiteSpace(config.EntityName)
            ? ToPascalCase(tableName)
            : config.EntityName!.Trim();
        var moduleName = string.IsNullOrWhiteSpace(config.ModuleName) ? DefaultModuleName : config.ModuleName!.Trim();
        var moduleNamePascal = ToPascalCase(moduleName);
        var businessName = string.IsNullOrWhiteSpace(config.BusinessName) ? tableName : config.BusinessName!.Trim();
        var entityKebab = ToKebabCase(entityName);

        var fieldConfigs = config.FieldConfigs?.Select(BuildTemplateFieldConfig).ToList() ?? new List<TemplateFieldConfig>();

        var hasId = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "id", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "id", StringComparison.OrdinalIgnoreCase));
        var hasCreateTime = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "create_time", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "createTime", StringComparison.OrdinalIgnoreCase));
        var hasUpdateTime = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "update_time", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "updateTime", StringComparison.OrdinalIgnoreCase));
        var hasCreateBy = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "create_by", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "createBy", StringComparison.OrdinalIgnoreCase));
        var hasUpdateBy = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "update_by", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "updateBy", StringComparison.OrdinalIgnoreCase));
        var isDeletedType = fieldConfigs
            .FirstOrDefault(f => string.Equals(f.ColumnName, "is_deleted", StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.FieldName, "isDeleted", StringComparison.OrdinalIgnoreCase))
            ?.CsType ?? "bool";
        var hasIsDeleted = fieldConfigs.Any(f =>
            string.Equals(f.ColumnName, "is_deleted", StringComparison.OrdinalIgnoreCase)
            || string.Equals(f.FieldName, "isDeleted", StringComparison.OrdinalIgnoreCase));

        var vars = new Dictionary<string, object?>
        {
            ["tableName"] = config.TableName ?? tableName,
            ["entityName"] = entityName,
            ["entityKebab"] = entityKebab,
            ["entityLowerCamel"] = LowerFirst(entityName),
            ["entityUpperSnake"] = ToSnakeUpper(entityName),
            ["businessName"] = businessName,
            ["moduleName"] = moduleName,
            ["moduleNamePascal"] = moduleNamePascal,
            ["packageName"] = config.PackageName ?? DefaultPackageName,
            ["author"] = config.Author ?? DefaultAuthor,
            ["pageType"] = effectivePageType,
            ["fieldConfigs"] = fieldConfigs,
            ["hasId"] = hasId,
            ["hasCreateTime"] = hasCreateTime,
            ["hasUpdateTime"] = hasUpdateTime,
            ["hasCreateBy"] = hasCreateBy,
            ["hasUpdateBy"] = hasUpdateBy,
            ["isDeletedType"] = isDeletedType,
            ["hasIsDeleted"] = hasIsDeleted
        };

        var previews = new List<CodegenPreviewDto>();
        foreach (var (templateName, templateConfig) in TemplateConfigs)
        {
            if (frontendType == "js" && string.Equals(templateName, "API_TYPES", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var templatePath = ResolveFrontendTemplatePath(templateName, templateConfig, frontendType);
            var extension = ResolveFrontendExtension(templateName, templateConfig, frontendType);
            var fileName = GetFileName(entityName, templateName, extension);
            var filePath = GetFilePath(templateName, moduleName, moduleNamePascal, entityName, templateConfig.SubpackageName);

            var content = RenderTemplate(templateName, templatePath, templateConfig.SubpackageName, vars, effectivePageType);
            previews.Add(new CodegenPreviewDto
            {
                Path = filePath,
                FileName = fileName,
                Content = content,
                Scope = ResolveScope(templateName),
                Language = ResolveLanguage(fileName)
            });
        }

        return previews;
    }

    /// <summary>
    /// 生成多张表的代码并按目录结构打包为 zip
    /// </summary>
    public async Task<(string FileName, byte[] Content)> DownloadAsync(
        string[] tableNames,
        string pageType,
        string type,
        CancellationToken cancellationToken = default)
    {
        var names = tableNames?.Select(t => t?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToArray() ?? Array.Empty<string>();

        if (names.Length == 0)
        {
            throw new BusinessException(ResultCode.RequestRequiredParameterIsEmpty, "表名不能为空");
        }

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            foreach (var table in names)
            {
                var list = await GetPreviewAsync(table!, pageType, type, cancellationToken).ConfigureAwait(false);
                foreach (var item in list)
                {
                    var entryPath = Path.Combine(item.Path, item.FileName).Replace("\\", "/");
                    var entry = archive.CreateEntry(entryPath);
                    await using var entryStream = entry.Open();
                    var bytes = Encoding.UTF8.GetBytes(item.Content);
                    await entryStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return (DownloadFileName, buffer.ToArray());
    }

    /// <summary>
    /// 取连接，未打开则打开（读取 information_schema 需绕过 EF 直接执行 SQL）
    /// </summary>
    private Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
        => _sqlExecutor.OpenConnectionAsync(cancellationToken);

    /// <summary>
    /// 执行 SQL 返回单个 long 值（如 COUNT），空值返回 0
    /// </summary>
    private async Task<long> ExecuteScalarLongAsync(
        string sql,
        IReadOnlyCollection<DbParameter> parameters,
        CancellationToken cancellationToken)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is null || result == DBNull.Value ? 0L : Convert.ToInt64(result);
        }
        finally
        {
            await _sqlExecutor.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 执行查询，用 mapper 把每行映射为 T
    /// </summary>
    private async Task<List<T>> ExecuteQueryAsync<T>(
        string sql,
        IReadOnlyCollection<DbParameter> parameters,
        Func<DbDataReader, T> mapper,
        CancellationToken cancellationToken)
    {
        var list = new List<T>();
        var connection = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var param in parameters)
            {
                command.Parameters.Add(param);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                list.Add(mapper(reader));
            }

            return list;
        }
        finally
        {
            await _sqlExecutor.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 在指定事务内执行增删改 SQL
    /// </summary>
    private async Task ExecuteNonQueryAsync(
        string sql,
        IReadOnlyCollection<DbParameter> parameters,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        foreach (var param in parameters)
        {
            command.Parameters.Add(param);
        }

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 创建 SQL 参数，null 转为 DBNull；参数创建临时开合连接，不依赖外部传入的连接
    /// </summary>
    private async Task<DbParameter> CreateParameterAsync(string name, object? value, CancellationToken cancellationToken)
    {
        var connection = await _sqlExecutor.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var param = connection.CreateCommand().CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            return param;
        }
        finally
        {
            await _sqlExecutor.CloseConnectionAsync(connection, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 从 information_schema 读取表注释
    /// </summary>
    private async Task<string?> GetTableCommentAsync(string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT TABLE_COMMENT AS tableComment
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName
LIMIT 1";

        var list = await ExecuteQueryAsync(sql, new[] { await CreateParameterAsync("@tableName", tableName, cancellationToken) },
            reader => reader["tableComment"]?.ToString(), cancellationToken).ConfigureAwait(false);
        return list.FirstOrDefault();
    }

    /// <summary>
    /// 读取 gen_table 中未删除的表配置行
    /// </summary>
    private async Task<ConfigRow?> GetConfigRowAsync(string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT id, table_name, module_name, package_name, business_name, entity_name, author, parent_menu_id, page_type, remove_table_prefix
FROM gen_table
WHERE table_name = @tableName AND is_deleted = 0
LIMIT 1";

        var list = await ExecuteQueryAsync(sql, new[] { await CreateParameterAsync("@tableName", tableName, cancellationToken) }, reader =>
        {
            return new ConfigRow
            {
                Id = Convert.ToInt64(reader["id"]),
                TableName = reader["table_name"]?.ToString() ?? tableName,
                ModuleName = reader["module_name"]?.ToString() ?? DefaultModuleName,
                PackageName = reader["package_name"]?.ToString() ?? DefaultPackageName,
                BusinessName = reader["business_name"]?.ToString() ?? tableName,
                EntityName = reader["entity_name"]?.ToString() ?? string.Empty,
                Author = reader["author"]?.ToString() ?? DefaultAuthor,
                ParentMenuId = reader["parent_menu_id"] == DBNull.Value ? null : Convert.ToInt64(reader["parent_menu_id"]),
                PageType = reader["page_type"]?.ToString() ?? "classic",
                RemoveTablePrefix = reader["remove_table_prefix"]?.ToString()
            };
        }, cancellationToken).ConfigureAwait(false);

        return list.FirstOrDefault();
    }

    /// <summary>
    /// 事务内查表配置 ID，用于判断是新增还是更新
    /// </summary>
    private async Task<long?> GetConfigIdAsync(
        string tableName,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT id FROM gen_table WHERE table_name = @tableName LIMIT 1";
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.Parameters.Add(await CreateParameterAsync("@tableName", tableName, cancellationToken).ConfigureAwait(false));

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (result is null || result == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt64(result);
    }

    /// <summary>
    /// 读取已保存的字段配置，按 field_sort 升序
    /// </summary>
    private async Task<IReadOnlyCollection<FieldConfigDto>> GetFieldConfigsAsync(long tableId, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
  id,
  column_name,
  column_type,
  field_name,
  field_type,
  field_comment,
  is_show_in_list,
  is_show_in_form,
  is_show_in_query,
  is_required,
  form_type,
  query_type,
  max_length,
  field_sort,
  dict_type
FROM gen_table_column
WHERE table_id = @tableId
ORDER BY field_sort ASC";

        var list = await ExecuteQueryAsync(sql, new[] { await CreateParameterAsync("@tableId", tableId, cancellationToken) }, reader =>
        {
            return new FieldConfigDto
            {
                Id = reader["id"] == DBNull.Value ? null : Convert.ToInt64(reader["id"]),
                ColumnName = reader["column_name"]?.ToString(),
                ColumnType = reader["column_type"]?.ToString(),
                FieldName = reader["field_name"]?.ToString(),
                FieldType = reader["field_type"]?.ToString(),
                FieldComment = reader["field_comment"]?.ToString(),
                IsShowInList = reader["is_show_in_list"] == DBNull.Value ? null : Convert.ToInt32(reader["is_show_in_list"]),
                IsShowInForm = reader["is_show_in_form"] == DBNull.Value ? null : Convert.ToInt32(reader["is_show_in_form"]),
                IsShowInQuery = reader["is_show_in_query"] == DBNull.Value ? null : Convert.ToInt32(reader["is_show_in_query"]),
                IsRequired = reader["is_required"] == DBNull.Value ? null : Convert.ToInt32(reader["is_required"]),
                FormType = reader["form_type"] == DBNull.Value ? null : Convert.ToInt32(reader["form_type"]),
                QueryType = reader["query_type"] == DBNull.Value ? null : Convert.ToInt32(reader["query_type"]),
                MaxLength = reader["max_length"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["max_length"]),
                FieldSort = reader["field_sort"] == DBNull.Value ? null : Convert.ToInt32(reader["field_sort"]),
                DictType = reader["dict_type"]?.ToString()
            };
        }, cancellationToken).ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// 按表结构推导默认字段配置：主键隐藏、非空列必填、日期列用日期控件
    /// </summary>
    private async Task<IReadOnlyCollection<FieldConfigDto>> GetDefaultFieldConfigsAsync(string tableName, CancellationToken cancellationToken)
    {
        const string sql = @"
SELECT
  COLUMN_NAME AS columnName,
  DATA_TYPE AS columnType,
  COLUMN_COMMENT AS columnComment,
  IS_NULLABLE AS isNullable,
  CHARACTER_MAXIMUM_LENGTH AS maxLength,
  ORDINAL_POSITION AS ordinalPosition,
  COLUMN_KEY AS columnKey
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName
ORDER BY ORDINAL_POSITION ASC";

        var list = await ExecuteQueryAsync(sql, new[] { await CreateParameterAsync("@tableName", tableName, cancellationToken) }, reader =>
        {
            var columnName = reader["columnName"]?.ToString() ?? string.Empty;
            var columnType = reader["columnType"]?.ToString() ?? string.Empty;
            var comment = reader["columnComment"]?.ToString() ?? string.Empty;
            var isNullable = string.Equals(reader["isNullable"]?.ToString(), "YES", StringComparison.OrdinalIgnoreCase);
            var maxLength = reader["maxLength"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["maxLength"]);
            var columnKey = reader["columnKey"]?.ToString() ?? string.Empty;
            var isPk = string.Equals(columnKey, "PRI", StringComparison.OrdinalIgnoreCase);

            var formType = isPk ? 10 : GetDefaultFormTypeByColumnType(columnType);
            var isShowInForm = isPk ? 0 : 1;

            return new FieldConfigDto
            {
                ColumnName = columnName,
                ColumnType = columnType,
                FieldName = ToCamelCase(columnName),
                FieldType = GetCsTypeByColumnType(columnType),
                FieldComment = comment,
                IsRequired = isNullable ? 0 : 1,
                FormType = formType,
                QueryType = 1,
                MaxLength = maxLength,
                FieldSort = reader["ordinalPosition"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ordinalPosition"]),
                IsShowInList = 1,
                IsShowInForm = isShowInForm,
                IsShowInQuery = 0,
                DictType = string.Empty
            };
        }, cancellationToken).ConfigureAwait(false);

        return list;
    }

    /// <summary>
    /// 把字段配置转成模板变量，补齐 C#/TS 类型、可空类型、属性名等派生值
    /// </summary>
    private TemplateFieldConfig BuildTemplateFieldConfig(FieldConfigDto field)
    {
        var csType = string.IsNullOrWhiteSpace(field.FieldType)
            ? GetCsTypeByColumnType(field.ColumnType ?? string.Empty)
            : field.FieldType!;
        var csTypeNullable = ToNullableType(csType);

        return new TemplateFieldConfig
        {
            ColumnName = field.ColumnName ?? string.Empty,
            ColumnType = field.ColumnType ?? string.Empty,
            FieldName = field.FieldName ?? string.Empty,
            FieldComment = field.FieldComment ?? string.Empty,
            IsShowInList = field.IsShowInList ?? 0,
            IsShowInForm = field.IsShowInForm ?? 0,
            IsShowInQuery = field.IsShowInQuery ?? 0,
            IsRequired = field.IsRequired ?? 0,
            FormType = GetFormTypeName(field.FormType ?? 1),
            QueryType = GetQueryTypeName(field.QueryType ?? 1),
            MaxLength = field.MaxLength,
            FieldSort = field.FieldSort,
            DictType = field.DictType ?? string.Empty,
            CsType = csType,
            CsTypeNullable = csTypeNullable,
            TsType = GetTsTypeByCsType(csType),
            PropertyName = ToPascalCase(field.FieldName ?? string.Empty)
        };
    }

    /// <summary>
    /// 前端选 js 时切到 js 版模板，否则用默认 ts 模板
    /// </summary>
    private string ResolveFrontendTemplatePath(string templateName, TemplateConfig templateConfig, string frontendType)
    {
        if (frontendType != "js")
        {
            return templateConfig.TemplatePath;
        }

        return templateName switch
        {
            "API" => "frontend/js/api.js.sbn",
            "VIEW" => "frontend/js/index.vue.sbn",
            _ => templateConfig.TemplatePath
        };
    }

    /// <summary>
    /// 前端选 js 时 API 文件用 .js 扩展名
    /// </summary>
    private string ResolveFrontendExtension(string templateName, TemplateConfig templateConfig, string frontendType)
    {
        if (frontendType != "js")
        {
            return templateConfig.Extension;
        }

        return templateName == "API" ? ".js" : templateConfig.Extension;
    }

    /// <summary>
    /// 用模板渲染器渲染模板，curd 页面时切到 curd 版视图模板
    /// </summary>
    private string RenderTemplate(
        string templateName,
        string templatePath,
        string subpackageName,
        IReadOnlyDictionary<string, object?> vars,
        string pageType)
    {
        var tplPath = templatePath;
        if (string.Equals(templateName, "VIEW", StringComparison.OrdinalIgnoreCase) && pageType == "curd")
        {
            if (tplPath.EndsWith("frontend/js/index.vue.sbn", StringComparison.OrdinalIgnoreCase))
            {
                tplPath = tplPath.Replace("frontend/js/index.vue.sbn", "frontend/js/index.curd.vue.sbn", StringComparison.OrdinalIgnoreCase);
            }
            else if (tplPath.EndsWith("frontend/ts/index.vue.sbn", StringComparison.OrdinalIgnoreCase))
            {
                tplPath = tplPath.Replace("frontend/ts/index.vue.sbn", "frontend/ts/index.curd.vue.sbn", StringComparison.OrdinalIgnoreCase);
            }
        }

        var fullPath = Path.Combine(_templateRoot, tplPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!global::System.IO.File.Exists(fullPath))
        {
            throw new BusinessException($"模板不存在: {tplPath}");
        }

        var content = global::System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
        return _templateRenderer.Render(content, vars, subpackageName);
    }

    /// <summary>
    /// 按模板类型拼接生成文件名，如 IXxxService.cs、XxxForm.cs、index.vue
    /// </summary>
    private string GetFileName(string entityName, string templateName, string extension)
    {
        if (templateName == "Entity")
        {
            return $"{entityName}{extension}";
        }

        if (templateName == "API")
        {
            return $"index{extension}";
        }

        if (templateName == "API_TYPES")
        {
            return $"types{extension}";
        }

        if (templateName == "VIEW")
        {
            return "index.vue";
        }

        if (templateName == "PageVo")
        {
            return $"{entityName}PageVo{extension}";
        }

        if (templateName == "Query")
        {
            return $"{entityName}Query{extension}";
        }

        if (templateName == "Form")
        {
            return $"{entityName}Form{extension}";
        }

        if (templateName == "Service")
        {
            return $"I{entityName}Service{extension}";
        }

        if (templateName == "ServiceImpl")
        {
            return $"{entityName}Service{extension}";
        }

        return $"{entityName}{templateName}{extension}";
    }

    /// <summary>
    /// 按模板类型确定生成文件在前后端工程中的目录
    /// </summary>
    private string GetFilePath(
        string templateName,
        string moduleName,
        string moduleNamePascal,
        string entityName,
        string subpackageName)
    {
        var backendRoot = Path.Combine(BackendAppName, "src");
        var frontendRoot = Path.Combine(FrontendAppName, "src");

        if (templateName == "API")
        {
            return Path.Combine(frontendRoot, "api", moduleName, ToKebabCase(entityName));
        }

        if (templateName == "API_TYPES")
        {
            return Path.Combine(frontendRoot, "api", moduleName, ToKebabCase(entityName));
        }

        if (templateName == "VIEW")
        {
            return Path.Combine(frontendRoot, subpackageName, moduleName, ToKebabCase(entityName));
        }

        if (templateName == "Controller")
        {
            return Path.Combine(backendRoot, "Youlai.Api", "Controllers");
        }

        if (templateName == "Service")
        {
            return Path.Combine(backendRoot, "Youlai.Application", moduleNamePascal, subpackageName);
        }

        if (templateName == "ServiceImpl")
        {
            return Path.Combine(backendRoot, "Youlai.Infrastructure", subpackageName);
        }

        if (templateName == "Form" || templateName == "Query" || templateName == "PageVo")
        {
            return Path.Combine(backendRoot, "Youlai.Application", moduleNamePascal, subpackageName);
        }

        if (templateName == "Entity")
        {
            return Path.Combine(backendRoot, "Youlai.Domain", subpackageName);
        }

        return Path.Combine(backendRoot, "Youlai.Application", moduleNamePascal);
    }

    /// <summary>
    /// 判断模板属于 frontend 还是 backend
    /// </summary>
    private static string ResolveScope(string templateName)
    {
        return templateName switch
        {
            "API" => "frontend",
            "API_TYPES" => "frontend",
            "VIEW" => "frontend",
            _ => "backend"
        };
    }

    /// <summary>
    /// 取文件扩展名作为高亮语言标识
    /// </summary>
    private static string ResolveLanguage(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }
        return extension.TrimStart('.').ToLowerInvariant();
    }

    /// <summary>
    /// 表单控件类型码转名称（1=INPUT、2=SELECT…）
    /// </summary>
    private static string GetFormTypeName(int formType)
    {
        return formType switch
        {
            1 => "INPUT",
            2 => "SELECT",
            3 => "RADIO",
            4 => "CHECK_BOX",
            5 => "INPUT_NUMBER",
            6 => "SWITCH",
            7 => "TEXT_AREA",
            8 => "DATE",
            9 => "DATE_TIME",
            10 => "HIDDEN",
            _ => "INPUT"
        };
    }

    /// <summary>
    /// 查询方式码转名称（默认 EQ、2=LIKE、4=BETWEEN…）
    /// </summary>
    private static string GetQueryTypeName(int queryType)
    {
        return queryType switch
        {
            2 => "LIKE",
            3 => "IN",
            4 => "BETWEEN",
            5 => "GT",
            6 => "GE",
            7 => "LT",
            8 => "LE",
            9 => "NE",
            10 => "LIKE_LEFT",
            11 => "LIKE_RIGHT",
            _ => "EQ"
        };
    }

    /// <summary>
    /// 按列类型推默认控件：date→日期、datetime/timestamp→日期时间，其余为输入框
    /// </summary>
    private static int GetDefaultFormTypeByColumnType(string columnType)
    {
        var normalized = NormalizeColumnType(columnType);
        return normalized switch
        {
            "date" => 8,
            "datetime" => 9,
            "timestamp" => 9,
            _ => 1
        };
    }

    /// <summary>
    /// MySQL 列类型映射为 C# 类型
    /// </summary>
    private static string GetCsTypeByColumnType(string columnType)
    {
        var normalized = NormalizeColumnType(columnType);
        if (normalized.Contains("char") || normalized.Contains("text") || normalized.Contains("json"))
        {
            return "string";
        }

        if (normalized.Contains("bigint"))
        {
            return "long";
        }

        if (normalized.Contains("int"))
        {
            return "int";
        }

        if (normalized.Contains("decimal"))
        {
            return "decimal";
        }

        if (normalized.Contains("double") || normalized.Contains("float"))
        {
            return "double";
        }

        if (normalized.Contains("bool") || normalized.Contains("bit"))
        {
            return "bool";
        }

        if (normalized.Contains("date") || normalized.Contains("time"))
        {
            return "DateTime";
        }

        return "string";
    }

    /// <summary>
    /// C# 类型映射为 TypeScript 类型
    /// </summary>
    private static string GetTsTypeByCsType(string csType)
    {
        return csType switch
        {
            "int" => "number",
            "long" => "number",
            "double" => "number",
            "decimal" => "number",
            "bool" => "boolean",
            "DateTime" => "string",
            _ => "string"
        };
    }

    /// <summary>
    /// 转为可空类型，已可空则原样返回
    /// </summary>
    private static string ToNullableType(string csType)
    {
        if (csType == "string")
        {
            return "string?";
        }

        if (csType == "DateTime")
        {
            return "DateTime?";
        }

        if (csType.EndsWith("?", StringComparison.Ordinal))
        {
            return csType;
        }

        return csType + "?";
    }

    /// <summary>
    /// 归一列类型：转小写、去 unsigned/zerofill 和长度括号，如 int(11) unsigned → int
    /// </summary>
    private static string NormalizeColumnType(string columnType)
    {
        var normalized = (columnType ?? string.Empty).Trim().ToLowerInvariant();
        normalized = normalized.Replace("unsigned", string.Empty).Replace("zerofill", string.Empty).Trim();
        var idx = normalized.IndexOf('(');
        if (idx >= 0)
        {
            normalized = normalized.Substring(0, idx);
        }
        return normalized.Trim();
    }

    /// <summary>
    /// 下划线/短横线命名转小驼峰，如 user_name → userName
    /// </summary>
    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder(parts[0].ToLowerInvariant());
        for (var i = 1; i < parts.Length; i++)
        {
            sb.Append(char.ToUpperInvariant(parts[i][0]));
            sb.Append(parts[i].Substring(1).ToLowerInvariant());
        }

        return sb.ToString();
    }

    /// <summary>
    /// 转大驼峰，如 user_name → UserName
    /// </summary>
    private static string ToPascalCase(string value)
    {
        var camel = ToCamelCase(value);
        return string.IsNullOrWhiteSpace(camel)
            ? string.Empty
            : char.ToUpperInvariant(camel[0]) + camel[1..];
    }

    /// <summary>
    /// 首字母小写
    /// </summary>
    private static string LowerFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>
    /// 转短横线命名，如 UserName → user-name
    /// </summary>
    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0 && char.IsUpper(ch))
            {
                sb.Append('-');
            }

            sb.Append(ch == '_' ? '-' : char.ToLowerInvariant(ch));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 转大写下划线命名，如 UserName → USER_NAME
    /// </summary>
    private static string ToSnakeUpper(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0 && char.IsUpper(ch))
            {
                sb.Append('_');
            }

            sb.Append(ch == '-' ? '_' : char.ToUpperInvariant(ch));
        }

        return sb.ToString();
    }

    /// <summary>
    /// 代码生成模板配置
    /// </summary>
    private sealed record TemplateConfig(string TemplatePath, string SubpackageName, string Extension);

    /// <summary>
    /// gen_table 表配置行
    /// </summary>
    private sealed record ConfigRow
    {
        /// <summary>配置行 ID</summary>
        public long Id { get; init; }
        /// <summary>数据库表名</summary>
        public string TableName { get; init; } = string.Empty;
        /// <summary>模块名</summary>
        public string ModuleName { get; init; } = string.Empty;
        /// <summary>包名</summary>
        public string PackageName { get; init; } = string.Empty;
        /// <summary>业务名（表名去前缀转驼峰）</summary>
        public string BusinessName { get; init; } = string.Empty;
        /// <summary>实体类名</summary>
        public string EntityName { get; init; } = string.Empty;
        /// <summary>作者</summary>
        public string Author { get; init; } = string.Empty;
        /// <summary>父菜单 ID，可选</summary>
        public long? ParentMenuId { get; init; }
        /// <summary>页面类型，默认 classic</summary>
        public string PageType { get; init; } = "classic";
        /// <summary>生成时去除的表前缀，可选</summary>
        public string? RemoveTablePrefix { get; init; }
    }

    /// <summary>
    /// 模板字段配置（列名、类型、注释等）
    /// </summary>
    private sealed class TemplateFieldConfig
    {
        /// <summary>列名</summary>
        public string ColumnName { get; init; } = string.Empty;
        /// <summary>数据库列类型</summary>
        public string ColumnType { get; init; } = string.Empty;
        /// <summary>字段名（驼峰）</summary>
        public string FieldName { get; init; } = string.Empty;
        /// <summary>列注释</summary>
        public string FieldComment { get; init; } = string.Empty;
        /// <summary>是否在列表展示(0/1)</summary>
        public int IsShowInList { get; init; }
        /// <summary>是否在表单展示(0/1)</summary>
        public int IsShowInForm { get; init; }
        /// <summary>是否在查询区展示(0/1)</summary>
        public int IsShowInQuery { get; init; }
        /// <summary>是否必填(0/1)</summary>
        public int IsRequired { get; init; }
        /// <summary>表单控件类型</summary>
        public string FormType { get; init; } = string.Empty;
        /// <summary>查询控件类型</summary>
        public string QueryType { get; init; } = string.Empty;
        /// <summary>字段最大长度，可选</summary>
        public int? MaxLength { get; init; }
        /// <summary>字段排序</summary>
        public int? FieldSort { get; init; }
        /// <summary>关联字典类型</summary>
        public string DictType { get; init; } = string.Empty;
        /// <summary>生成的 C# 类型</summary>
        public string CsType { get; init; } = string.Empty;
        /// <summary>可空的 C# 类型</summary>
        public string CsTypeNullable { get; init; } = string.Empty;
        /// <summary>生成的 TypeScript 类型</summary>
        public string TsType { get; init; } = string.Empty;
        /// <summary>属性名（首字母大写）</summary>
        public string PropertyName { get; init; } = string.Empty;
    }
}
