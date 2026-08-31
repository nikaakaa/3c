namespace ThirdPerson.Development.Gm.Rollback;

public sealed class SessionInfoGmCommand : IGmCommandHandler
{
    readonly IRollbackGmQuerySource m_Source;

    public SessionInfoGmCommand(IRollbackGmQuerySource source) =>
        m_Source = source ?? throw new ArgumentNullException(nameof(source));

    public GmCommandDefinition Definition { get; } = new()
    {
        id = "session.info",
        description = "查看当前 Relay 会话和已发布内容身份。",
        usage = "session.info"
    };

    public async Task<GmCommandResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        RollbackGmSessionSnapshot value = await m_Source.CaptureSessionAsync(cancellation);
        return new GmCommandResult(GmResultCode.Success, "服务端会话", new GmResultSection
        {
            title = value.SessionId,
            fields = new[]
            {
                GmResultField.Text("buildId", "构建", value.BuildId),
                GmResultField.Text("relayPeerId", "Relay", value.RelayPeerId),
                GmResultField.Text("endpoint", "Gameplay UDP", value.Endpoint),
                GmResultField.Text("modelIdentity", "模型", value.ModelIdentity),
                GmResultField.Text("protocolIdentity", "协议", value.ProtocolIdentity),
                GmResultField.Text("programId", "Program", value.ProgramId),
                GmResultField.Text("programHash", "Program Hash", value.ProgramHash),
                GmResultField.Signed("tickRate", "Tick Rate", value.TickRate),
                GmResultField.Signed("maximumPredictionLeadTicks", "最大预测领先 Tick", value.MaximumPredictionLeadTicks),
                GmResultField.Signed("confirmationDelayTicks", "确认延迟 Tick", value.ConfirmationDelayTicks)
            }
        });
    }
}
