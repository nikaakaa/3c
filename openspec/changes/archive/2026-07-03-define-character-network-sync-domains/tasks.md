# Tasks

- [x] 1. 梳理现有 `CharacterPipelineOutput.Network` 字段，标注每个字段属于 MotionSyncDomain、ActionSyncDomain、GameplayResultSyncDomain、StateEffectSyncDomain 或 PresentationSyncDomain。
- [x] 2. 定义 SyncDomain contract 的最小运行时类型或接口命名，确保它们不进入 Graph 结构身份。
- [x] 3. 定义 `ActionInstanceHandle` 或等价显式 action context 的字段和生命周期。
- [x] 4. 调整 action activation 输出链路，使 activation 成功后能返回显式 action context。
- [x] 5. 调整 Timeline playback request 合同，使动作 Timeline 可显式携带 action context。
- [x] 6. 调整 action window、motion、cue、gameplay result 输出提交 API，使它们不依赖 ambient current active action。
- [x] 7. 定义 MotionSyncDomain 的连续同步输出：input sequence、tick、motion snapshot、correction acknowledgement。
- [x] 8. 定义 ActionSyncDomain 的离散事务输出：activation、end、window digest、action decision ack。
- [x] 9. 定义 GameplayResultSyncDomain 的结果输出：gameplay result id、source actor、target actor、result type、可选 action instance id。
- [x] 10. 定义 StateEffectSyncDomain 的最小输出占位：state id、effect instance id、tick、payload digest。
- [x] 11. 定义 PresentationSyncDomain 的最小输出占位：cue event id、cue type、可选 action instance id、replication policy。
- [x] 12. 调整 `NetworkSendStage` 设计，使其按 SyncDomain + policy 聚合 outgoing packet。
- [x] 13. 调整 `NetworkReceiveStage` 设计，使其按 SyncDomain 注入 incoming decision、snapshot、correction 或 event。
- [x] 14. 保持 `Graph/BTSMTL` 不读取 transport、Fantasy session 或 peer 实现。
- [x] 15. 清理 action 输出中依赖 current active action 作为默认归属的路径。
- [x] 16. 更新 Runtime Debug 设计，使其按 SyncDomain 和稳定 id 展示输出链路。
- [x] 17. 对照 `add-gameplay-sync-runtime-character-adapter`，确认 GameplaySyncRuntime、Character adapter 和 loopback peer 只消费 SyncDomain packet 合同，不新增第二套 graph/action 语义。
- [x] 18. 运行 `openspec validate define-character-network-sync-domains --strict --no-interactive`。
