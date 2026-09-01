namespace ThirdPerson.Development.Gm.Rollback;

public sealed class ActorListGmCommand : IGmCommandHandler
{
    readonly IRollbackGmQuerySource m_Source;

    public ActorListGmCommand(IRollbackGmQuerySource source) =>
        m_Source = source ?? throw new ArgumentNullException(nameof(source));

    public GmCommandDefinition Definition { get; } = new()
    {
        id = "actor.list",
        description = "列出预期角色、实际握手状态和输入前沿。",
        usage = "actor.list",
        resultContract = "thirdperson.rollback-gm.result.actor-list/1"
    };

    public async Task<GmCommandResult> ExecuteAsync(IReadOnlyList<string> arguments, CancellationToken cancellation)
    {
        IReadOnlyList<RollbackGmActorSnapshot> actors = await m_Source.CaptureActorsAsync(cancellation);
        var sections = new GmResultSection[actors.Count];
        for (int i = 0; i < actors.Count; i++)
        {
            RollbackGmActorSnapshot actor = actors[i];
            var fields = new List<GmResultField>
            {
                GmResultField.Text("peerId", "Peer", actor.PeerId),
                GmResultField.Text("playerId", "Player", actor.PlayerId),
                GmResultField.Boolean("expected", "配置中的预期成员", true),
                GmResultField.Boolean("handshakeAccepted", "实际握手已接受", actor.HandshakeAccepted),
                GmResultField.Boolean("rosterLocked", "完整名单已锁定", actor.RosterLocked),
                GmResultField.Boolean("hasInputFrontier", "已有输入前沿", actor.HasInputFrontier)
            };
            if (actor.HasInputFrontier)
                fields.Add(GmResultField.Unsigned("inputFrontier", "输入前沿", actor.InputFrontier));
            sections[i] = new GmResultSection { title = actor.ActorId, fields = fields.ToArray() };
        }
        return new GmCommandResult(GmResultCode.Success, "角色名单；握手状态不表示持续在线证明。", sections);
    }
}
