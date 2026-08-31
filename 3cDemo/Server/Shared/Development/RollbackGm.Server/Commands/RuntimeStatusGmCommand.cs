namespace ThirdPerson.Development.Gm.Rollback;

public sealed class RuntimeStatusGmCommand : IGmCommandHandler
{
    readonly IRollbackGmQuerySource m_Source;

    public RuntimeStatusGmCommand(IRollbackGmQuerySource source) =>
        m_Source = source ?? throw new ArgumentNullException(nameof(source));

    public GmCommandDefinition Definition { get; } = new()
    {
        id = "runtime.status",
        description = "查看 Relay 的网络计数与 canonical/confirmed 前沿。",
        usage = "runtime.status"
    };

    public async Task<GmCommandResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        RollbackGmRuntimeSnapshot value = await m_Source.CaptureRuntimeAsync(cancellation);
        return new GmCommandResult(GmResultCode.Success, "Relay 运行状态", new GmResultSection
        {
            title = "网络与帧进度",
            fields = new[]
            {
                GmResultField.Boolean("rosterLocked", "名单已锁定", value.RosterLocked),
                GmResultField.Signed("receivedDatagrams", "接收报文", value.ReceivedDatagrams),
                GmResultField.Signed("sentDatagrams", "发送报文", value.SentDatagrams),
                GmResultField.Unsigned("inputBatches", "输入批次", value.InputBatches),
                GmResultField.Unsigned("forwardedBatches", "立即转发", value.ForwardedBatches),
                GmResultField.Unsigned("deduplicatedInputs", "去重输入", value.DeduplicatedInputs),
                GmResultField.Unsigned("invalidInputs", "非法输入", value.InvalidInputs),
                GmResultField.Unsigned("canonicalBundles", "Canonical Bundle", value.CanonicalBundles),
                GmResultField.Unsigned("nextCanonicalTick", "下一 Canonical Tick", value.NextCanonicalTick),
                GmResultField.Unsigned("confirmedTick", "Confirmed Tick", value.ConfirmedTick),
                GmResultField.Unsigned("confirmationBroadcasts", "确认广播", value.ConfirmationBroadcasts),
                GmResultField.Unsigned("hashReports", "Hash 报告", value.HashReports),
                GmResultField.Signed("pendingReliable", "待确认可靠消息", value.PendingReliable),
                GmResultField.Signed("droppedDatagrams", "接收丢弃", value.DroppedDatagrams),
                GmResultField.Signed("receiveQueueDepth", "接收队列", value.ReceiveQueueDepth),
                GmResultField.Signed("sendQueueDepth", "发送队列", value.SendQueueDepth)
            }
        });
    }
}
