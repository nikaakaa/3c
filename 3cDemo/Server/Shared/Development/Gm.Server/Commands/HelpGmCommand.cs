namespace ThirdPerson.Development.Gm;

public sealed class HelpGmCommand : IGmCommandHandler
{
    readonly IGmCommandCatalog m_Catalog;

    public HelpGmCommand(IGmCommandCatalog catalog) =>
        m_Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public GmCommandDefinition Definition { get; } = new()
    {
        id = "help",
        description = "查看已安装命令或指定命令的用法。",
        usage = "help [command]",
        resultContract = "thirdperson.gm.result.help/1",
        arguments = new[]
        {
            new GmCommandArgument { name = "command", description = "命令名称", optional = true }
        }
    };

    public Task<GmCommandResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        if (arguments.Count == 1)
        {
            if (!m_Catalog.TryGetDefinition(arguments[0], out GmCommandDefinition definition))
                return Task.FromResult(new GmCommandResult(GmResultCode.UnknownCommand, $"未安装命令：{arguments[0]}"));
            return Task.FromResult(new GmCommandResult(GmResultCode.Success, "命令用法", Describe(definition)));
        }
        return Task.FromResult(new GmCommandResult(
            GmResultCode.Success,
            "服务端已安装命令",
            m_Catalog.Definitions.Select(Describe).ToArray()));
    }

    static GmResultSection Describe(GmCommandDefinition definition) => new()
    {
        title = definition.id,
        fields = new[]
        {
            GmResultField.Text("description", "说明", definition.description),
            GmResultField.Text("usage", "用法", definition.usage),
            GmResultField.Text("resultContract", "结果合同", definition.resultContract),
            GmResultField.Signed("version", "版本", definition.version)
        }
    };
}
