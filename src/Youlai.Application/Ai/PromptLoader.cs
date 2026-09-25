using System.Collections.Concurrent;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;
using IOPath = System.IO.Path;
using IOSearchOption = System.IO.SearchOption;

namespace Youlai.Application.Ai;

/// <summary>
/// 提示词加载器：读取运行目录 prompts 下的全部 md 文件并缓存
/// </summary>
internal sealed class PromptLoader : IPromptLoader
{
    private const string PromptDirectory = "prompts";

    private readonly ConcurrentDictionary<string, string> _prompts = new(StringComparer.OrdinalIgnoreCase);

    public PromptLoader()
    {
        var root = IOPath.Combine(AppContext.BaseDirectory, PromptDirectory);
        if (!IODirectory.Exists(root))
        {
            return;
        }

        foreach (var file in IODirectory.EnumerateFiles(root, "*.md", IOSearchOption.AllDirectories))
        {
            var name = IOPath.GetRelativePath(root, file).Replace('\\', '/');
            _prompts[name] = IOFile.ReadAllText(file).Trim();
        }
    }

    /// <inheritdoc />
    public string Get(string name)
    {
        if (_prompts.TryGetValue(name, out var prompt))
        {
            return prompt;
        }

        throw new InvalidOperationException("提示词不存在：" + name);
    }
}
