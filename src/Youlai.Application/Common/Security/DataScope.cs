namespace Youlai.Application.Common.Security;

/// <summary>
/// 数据权限范围
/// </summary>
public enum DataScope
{
    All = 1,
    DeptAndSub = 2,
    Dept = 3,
    Self = 4,
}
