namespace ThirdPerson.Development.Gm;

public interface IGmCommandHandler
{
    GmCommandDefinition Definition { get; }
    GmCommandResult Execute(IReadOnlyList<string> arguments);
}

public interface IGmCommandCatalog
{
    IReadOnlyList<GmCommandDefinition> Definitions { get; }
    bool TryGetDefinition(string id, out GmCommandDefinition definition);
}

public sealed class GmCommandResult
{
    public GmCommandResult(GmResultCode code, string message, params GmResultSection[] sections)
    {
        Code = code;
        Message = message;
        Sections = sections;
    }

    public GmResultCode Code { get; }
    public string Message { get; }
    public GmResultSection[] Sections { get; }
}

public sealed class GmCommandRegistry : IGmCommandCatalog
{
    readonly Dictionary<string, IGmCommandHandler> m_Handlers = new(StringComparer.Ordinal);
    readonly List<GmCommandDefinition> m_Definitions = new();
    readonly IReadOnlyList<GmCommandDefinition> m_View;
    bool m_Sealed;

    public GmCommandRegistry() => m_View = m_Definitions.AsReadOnly();

    public IReadOnlyList<GmCommandDefinition> Definitions => m_View;

    public void Register(IGmCommandHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (m_Sealed)
            throw new InvalidOperationException("GM 命令目录已锁定。");
        GmCommandDefinition definition = handler.Definition;
        if (!GmCommandSyntax.IsValidCommandId(definition.id) || definition.version <= 0 ||
            string.IsNullOrWhiteSpace(definition.usage) || definition.permission == GmPermission.None)
            throw new ArgumentException("GM 命令描述不完整。", nameof(handler));
        bool optional = false;
        foreach (GmCommandArgument argument in definition.arguments)
        {
            if (string.IsNullOrWhiteSpace(argument.name) || optional && !argument.optional)
                throw new ArgumentException("GM 参数名称或可选参数顺序无效。", nameof(handler));
            optional |= argument.optional;
        }
        if (!m_Handlers.TryAdd(definition.id, handler))
            throw new InvalidOperationException($"GM 命令重复注册：{definition.id}");
        m_Definitions.Add(definition);
    }

    public void Seal()
    {
        m_Definitions.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
        m_Sealed = true;
    }

    public bool TryGetDefinition(string id, out GmCommandDefinition definition)
    {
        bool found = m_Handlers.TryGetValue(id, out IGmCommandHandler? handler);
        definition = found ? handler!.Definition : null!;
        return found;
    }

    internal bool TryGetHandler(string id, out IGmCommandHandler handler)
    {
        if (!m_Sealed)
            throw new InvalidOperationException("GM 命令目录尚未锁定。");
        return m_Handlers.TryGetValue(id, out handler!);
    }
}
