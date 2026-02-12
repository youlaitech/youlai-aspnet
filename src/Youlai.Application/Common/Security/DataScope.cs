namespace Youlai.Application.Common.Security;

/// <summary>
/// 数据权限范围
/// value 越小，数据权限范围越大。
/// 多角色数据权限合并策略：取并集（OR），即用户能看到所有角色权限范围内的数据。
/// 如果任一角色是 ALL，则直接跳过数据权限过滤。
/// </summary>
public enum DataScope
{
    /// <summary>
    /// 所有数据权限 - 最高权限，可查看所有数据
    /// </summary>
    All = 1,

    /// <summary>
    /// 部门及子部门数据 - 可查看本部门及其下属所有部门的数据
    /// </summary>
    DeptAndSub = 2,

    /// <summary>
    /// 本部门数据 - 仅可查看本部门的数据
    /// </summary>
    Dept = 3,

    /// <summary>
    /// 本人数据 - 仅可查看自己的数据
    /// </summary>
    Self = 4,

    /// <summary>
    /// 自定义部门数据 - 可查看指定部门的数据
    /// 需要配合 sys_role_dept 表使用，存储角色可访问的部门ID列表
    /// </summary>
    Custom = 5,
}

/// <summary>
/// 数据权限枚举扩展方法
/// </summary>
public static class DataScopeExtensions
{
    /// <summary>
    /// 判断是否为全部数据权限
    /// </summary>
    public static bool IsAll(this DataScope dataScope)
    {
        return dataScope == DataScope.All;
    }

    /// <summary>
    /// 根据值获取数据权限枚举
    /// </summary>
    public static DataScope? GetByValue(int? value)
    {
        if (value == null) return null;
        return Enum.IsDefined(typeof(DataScope), value) ? (DataScope)value : null;
    }
}
