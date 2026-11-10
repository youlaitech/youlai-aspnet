using System.Text.Json;
using Microsoft.Extensions.Logging;
using Youlai.Application.Exceptions;
using Youlai.Application.Form.Models;
using Youlai.Application.Persistence;
using Youlai.Application.Results;
using Youlai.Application.Security;
using Youlai.Domain.Entities;

namespace Youlai.Application.Form;

/// <summary>
/// 表单数据业务实现（提交校验、公开提交限流、分页与回显）
/// </summary>
internal sealed class FormDataService : IFormDataService
{
    private const int StatusPublished = 1;

    private readonly IReadOnlyDbContext _readDb;
    private readonly IRepository<FormData> _formDataRepo;
    private readonly IFormDefinitionService _definitionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublicSubmitLimiter _submitLimiter;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<FormDataService> _logger;

    public FormDataService(
        IReadOnlyDbContext readDb,
        IRepository<FormData> formDataRepo,
        IFormDefinitionService definitionService,
        IUnitOfWork unitOfWork,
        IPublicSubmitLimiter submitLimiter,
        ICurrentUser currentUser,
        ILogger<FormDataService> logger)
    {
        _readDb = readDb;
        _formDataRepo = formDataRepo;
        _definitionService = definitionService;
        _unitOfWork = unitOfWork;
        _submitLimiter = submitLimiter;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// 提交表单数据（仅已发布表单可提交）
    /// </summary>
    public async Task<bool> SaveFormDataAsync(string formKey, JsonElement data, CancellationToken cancellationToken = default)
    {
        var form = await _definitionService.GetByFormKeyAsync(formKey, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");

        if (form.Status != StatusPublished)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单未发布或已停用");
        }

        return await PersistFormDataAsync(form, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 匿名提交公开表单数据（公开分享链接入口）
    /// </summary>
    public async Task<bool> SavePublicFormDataAsync(string formKey, JsonElement data, string ip, CancellationToken cancellationToken = default)
    {
        var form = await _definitionService.GetByFormKeyAsync(formKey, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");

        if (form.Status != StatusPublished || form.IsPublic != 1)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在或未开放公开访问");
        }

        var allowed = await _submitLimiter.IsAllowedAsync(formKey, ip, cancellationToken).ConfigureAwait(false);
        if (!allowed)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "提交过于频繁，请稍后再试");
        }

        _logger.LogInformation("公开表单匿名提交 formKey={FormKey} ip={Ip}", formKey, ip);
        return await PersistFormDataAsync(form, data, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 表单数据分页列表（联查提交人昵称）
    /// </summary>
    public async Task<PageResult<FormDataVo>> GetFormDataPageAsync(string formKey, FormDataQuery queryParams, CancellationToken cancellationToken = default)
    {
        var form = await _definitionService.GetByFormKeyAsync(formKey, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessException(ResultCode.InvalidUserInput, "表单不存在");

        var (pageNum, pageSize) = queryParams.Normalize();

        var rows = from d in _readDb.Query<FormData>().Where(d => d.FormId == form.Id)
                   join u in _readDb.Query<SysUser>() on d.CreateBy equals u.Id into users
                   from u in users.DefaultIfEmpty()
                   orderby d.CreateTime descending
                   select new FormDataVo
                   {
                       Id = d.Id,
                       FormVersion = d.FormVersion,
                       DataJson = d.DataJson,
                       CreateBy = d.CreateBy,
                       CreateByName = u != null ? u.Nickname : null,
                       CreateTime = d.CreateTime,
                   };

        return await _readDb.PageAsync(rows, pageNum, pageSize, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 表单数据详情（只读回显，规则取提交时版本的快照）
    /// </summary>
    public async Task<FormDataVo?> GetFormDataDetailAsync(long dataId, CancellationToken cancellationToken = default)
    {
        var entity = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormData>().Where(d => d.Id == dataId),
            cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return null;
        }

        var vo = new FormDataVo
        {
            Id = entity.Id,
            FormVersion = entity.FormVersion,
            DataJson = entity.DataJson,
            CreateBy = entity.CreateBy,
            CreateTime = entity.CreateTime,
        };

        // 命中版本快照用快照规则，否则回退当前定义（兼容快照表启用前提交的历史数据）
        var snapshot = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormSnapshot>().Where(s => s.FormId == entity.FormId && s.Version == entity.FormVersion),
            cancellationToken).ConfigureAwait(false);
        if (snapshot is not null)
        {
            vo.FormJson = snapshot.FormJson;
            vo.OptionsJson = snapshot.OptionsJson;
            return vo;
        }

        var form = await _readDb.FirstOrDefaultAsync(
            _readDb.Query<FormDefinition>().Where(f => f.Id == entity.FormId),
            cancellationToken).ConfigureAwait(false);
        if (form is not null)
        {
            vo.FormJson = form.FormJson;
            vo.OptionsJson = form.OptionsJson;
        }

        return vo;
    }

    /// <summary>
    /// 批量删除表单数据（逻辑删除）
    /// </summary>
    public async Task<bool> RemoveFormDataAsync(string ids, CancellationToken cancellationToken = default)
    {
        var idList = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => long.TryParse(s, out var value) ? value : 0)
            .Where(value => value > 0)
            .Distinct()
            .ToList();
        if (idList.Count == 0)
        {
            throw new BusinessException(ResultCode.InvalidUserInput, "删除的表单数据为空");
        }

        var exists = await _readDb.ToListAsync(
            _readDb.Query<FormData>().Where(d => idList.Contains(d.Id)).Select(d => d.Id),
            cancellationToken).ConfigureAwait(false);
        if (exists.Count == 0)
        {
            return true;
        }

        await _formDataRepo.BatchSoftDeleteAsync(exists, _currentUser.UserId, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// 数据落库（登录/匿名提交共用）
    /// </summary>
    private async Task<bool> PersistFormDataAsync(FormDefinition form, JsonElement data, CancellationToken cancellationToken)
    {
        var filtered = FormRuleValidator.ValidateAndFilter(form.FormJson, data);

        _formDataRepo.Add(new FormData
        {
            FormId = form.Id,
            FormVersion = form.Version,
            DataJson = JsonSerializer.Serialize(filtered),
        });

        return await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false) > 0;
    }

}
