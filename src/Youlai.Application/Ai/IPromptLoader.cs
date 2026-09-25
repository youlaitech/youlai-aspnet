namespace Youlai.Application.Ai;

/// <summary>
/// 提示词加载器
/// </summary>
public interface IPromptLoader
{
    /// <summary>
    /// 获取提示词原文
    /// </summary>
    /// <param name="name">提示词名称，prompts 下的相对路径，如 form/system.md</param>
    string Get(string name);
}
