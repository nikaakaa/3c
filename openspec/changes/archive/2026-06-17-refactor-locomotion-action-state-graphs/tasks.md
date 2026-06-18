# Tasks

## 1. 规格与冲突收口

- [x] 1.1 读取 `formalize-character-frame-module-architecture` 的 proposal、design、tasks 和 deltas。
- [x] 1.2 读取 `refactor-locomotion-fullbody-ownership` 的剩余未完成项。
- [x] 1.3 搜索 active changes 中所有要求默认状态机包含 `Action.Dodge` 的表述。
- [x] 1.4 搜索 active changes 中所有把 Dodge 写成全局状态机叶子的任务。
- [x] 1.5 标记与本变更冲突的 `add-light-attack-combo-action` 状态图方案。
- [x] 1.6 确认本变更不新增 LightAttack、Jump、HitReact 或 UpperBody。

## 2. 当前实现审计

- [x] 2.1 扫描 `Assets/Configs/3C/StateMachine/CorinStateMachine.asset` 中的 `Action.*` 节点。
- [x] 2.2 扫描 `CorinStateMachine.asset` 中指向 `Action.*` 的 transition。
- [x] 2.3 扫描 `CorinStateMachine.asset` 中 Action motion module。
- [x] 2.4 扫描 `CorinStateMachine.asset` 中 Action animation module。
- [x] 2.5 扫描测试中断言默认状态机进入 `Action.Dodge` 的用例。
- [x] 2.6 扫描生产代码中读取 `stateFrame.ActionState` 作为 Action 权威的路径。
- [x] 2.7 扫描生产代码中读取 `stateFrame.ActionMotionSpec` 作为 Dodge motion 权威的路径。
- [x] 2.8 扫描 rollback 测试中把 active Dodge 存在 state machine snapshot 的用例。

## 3. Locomotion graph 资产迁移

- [x] 3.1 创建 `Assets/Configs/3C/StateMachine/Locomotion/` 目录和 `.meta`。
- [x] 3.2 创建 `Assets/Configs/3C/StateMachine/Locomotion/Corin/` 目录和 `.meta`。
- [x] 3.3 将 Corin 默认 graph 资产迁移或复制为 `CorinLocomotionStateGraph.asset`。
- [x] 3.4 保留或明确迁移原资产 `.meta` GUID 策略。
- [x] 3.5 删除 Locomotion graph 中 `Action.Dodge` state node。
- [x] 3.6 删除 Locomotion graph 中 `Locomotion.* -> Action.*` transition。
- [x] 3.7 删除 Locomotion graph 中 `Action.* -> Action.*` transition。
- [x] 3.8 删除 Locomotion graph 中 `Action.* -> Locomotion.*` transition。
- [x] 3.9 删除 Locomotion graph 中 Action motion module 数据。
- [x] 3.10 删除 Locomotion graph 中 Action animation module 数据。
- [x] 3.11 保留 `Locomotion.Idle` 初始状态。
- [x] 3.12 保留 `Locomotion.MoveStart`、`MoveLoop`、`MoveStop` transition。
- [x] 3.13 保留 `Locomotion.TurnBack` transition。
- [x] 3.14 确认 graph validator 对删除后的 graph 仍通过。

## 4. 配置根引用

- [x] 4.1 更新 `CorinCharacterConfig.asset` 正式引用 Locomotion graph 资产。
- [x] 4.2 确认 `CharacterConfigSO` 不从旧 `CorinStateMachine.asset` fallback。
- [x] 4.3 确认缺失 Locomotion graph 时正式路径报错。
- [x] 4.4 确认 `BodyClaimPolicySO` 仍从 Action 配置目录解析。
- [x] 4.5 确认 Dodge action config 仍从 Action 配置目录解析。
- [x] 4.6 确认 Action animation key 不再从 Locomotion graph 解析。

## 5. Action lifecycle 收口

- [x] 5.1 确认 `ActionLifecycleRuntime` 是 Dodge active state 的直接 runtime owner。
- [x] 5.2 确认 accepted Dodge request 创建 `ActionLifecycleFrame`。
- [x] 5.3 确认 Dodge lifecycle 持续帧不依赖状态机 active state。
- [x] 5.4 确认 Dodge lifecycle 完成后释放 active action。
- [x] 5.5 确认 lifecycle release 让后续帧释放 body claim。
- [x] 5.6 确认 Dodge action animation request 来自 Action lifecycle。
- [x] 5.7 确认 Dodge action motion spec 来自 Action resolver/lifecycle。
- [x] 5.8 确认 `FullBodyActionFrameSubmitter` 不读取 `stateFrame.ActionState`。
- [x] 5.9 确认 `FullBodyActionFrameSubmitter` 不读取 `stateFrame.ActionMotionSpec`。
- [x] 5.10 确认 `ActionMotionResolver` 仍只消费通用 `ActionMotionSpec`。
- [x] 5.11 确认 `BodyClaimPolicySO` 是 Dodge full-body claim 来源。
- [x] 5.12 确认 Dodge 默认不需要 Action 局部 graph。
- [x] 5.13 确认 Directional Dodge 的 resolved motion spec 允许在完成时写入 Run latch。
- [x] 5.14 确认 Backstep Dodge 的 resolved motion spec 完成时不写入 Run latch。
- [x] 5.15 确认 Action motion resolver 仅在 Directional 完成且本帧仍有移动输入时写入 Run latch。

## 6. Pipeline 与 runtime facts

- [x] 6.1 确认 Locomotion graph 输出只作为 Locomotion candidate/facts。
- [x] 6.2 确认 Action lifecycle 输出只作为 Action candidate/facts/claim。
- [x] 6.3 确认 `CharacterFramePlan` 负责采用或压制 Locomotion output。
- [x] 6.4 确认 action facts 从 `ActionMotionResolveResult` 或 action lifecycle 派生。
- [x] 6.5 确认 Locomotion facts 不通过 Action state path 派生。
- [x] 6.6 确认 input consume 来自 frame-level action output，而不是 state graph consume。
- [x] 6.7 确认 action animation clear 来自 lifecycle exit 或 frame action output。
- [x] 6.8 确认 Shift 正式输入同时绑定 Run 与 Dodge 请求，不新增替代输入路径。
- [x] 6.9 确认 Directional Dodge 完成的 Run latch 通过 Locomotion output runtime 写入正式 runtime state，而不是只写 Action facts。

## 7. 回滚与恢复

- [x] 7.1 确认 `CommittedActionRestoreState.Gameplay` 包含 Action lifecycle restore 数据。
- [x] 7.2 确认 restore active Dodge 不要求 state machine snapshot active state 为 `Action.Dodge`。
- [x] 7.3 更新 rollback 测试 expected snapshot 口径。
- [x] 7.4 覆盖 accepted Dodge 后 capture/restore lifecycle active state。
- [x] 7.5 覆盖 Dodge 完成后 capture/restore lifecycle inactive state。
- [x] 7.6 覆盖 replay 后 action facts 收敛。

## 8. 测试迁移

- [x] 8.1 更新状态图配置测试，默认 graph 不包含 `Action.*`。
- [x] 8.2 更新状态图配置测试，默认 graph 不包含指向 `Action.*` 的 transition。
- [x] 8.3 更新状态图配置测试，默认 graph 只包含批准的 `Locomotion.*` state。
- [x] 8.4 更新 Dodge 行为测试，断言 Action lifecycle active 而不是状态机 active state。
- [x] 8.5 更新 Dodge 动画测试，断言 action animation request 来自 lifecycle。
- [x] 8.6 更新 Dodge motion 测试，断言 motion spec 来自 resolver/lifecycle。
- [x] 8.7 更新 BodyClaim 测试，断言 `Action.Dodge` claim 压制 Locomotion 输出。
- [x] 8.8 更新 claim release 测试，断言 Dodge 完成后 Locomotion 输出恢复采用。
- [x] 8.9 更新 wildcard transition 测试，保留 wildcard 行为但不要求默认 graph 有 `Action.Dodge`。
- [x] 8.10 更新 rollback replay 测试，移除默认状态机 active Dodge 断言。
- [x] 8.11 更新输入配置测试，断言 Shift 同时绑定 Run 与 Dodge。
- [x] 8.12 更新 Dodge resolver 测试，断言 Directional motion seed 允许完成写 Run latch 且 Backstep 不允许。
- [x] 8.13 更新 Action motion resolver 测试，断言 Directional 完成时有移动输入才写 Run latch。
- [x] 8.14 更新输出边界测试，断言 Action output 的 Run latch 会落到 Locomotion runtime port。

## 9. 静态边界测试

- [x] 9.1 增加静态测试：`CorinLocomotionStateGraph.asset` 不包含 `stateId: Action.`。
- [x] 9.2 增加静态测试：Locomotion graph 不包含 `toStateId: Action.`。
- [x] 9.3 增加静态测试：Locomotion graph 不包含 `fromStateId: Action.`。
- [x] 9.4 增加静态测试：Locomotion graph 不包含 `Action.Dodge.Directional` animation binding。
- [x] 9.5 增加静态测试：Action submitter 不读取 `stateFrame.ActionState`。
- [x] 9.6 增加静态测试：Action submitter 不读取 `stateFrame.ActionMotionSpec`。
- [x] 9.7 增加静态测试：Dodge action config 和 BodyClaimPolicy 仍位于 Action 目录。
- [x] 9.8 增加静态测试：配置根没有引用旧全局 mixed graph 入口。

## 10. 验证

- [x] 10.1 运行 `openspec validate refactor-locomotion-action-state-graphs --strict --no-interactive`。
- [x] 10.2 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp.csproj /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 10.3 运行 `dotnet build .\3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 10.4 运行 Locomotion graph 配置 EditMode 测试。
- [x] 10.5 运行 Action lifecycle 与 Dodge claim EditMode 测试。
- [x] 10.6 运行 Character frame arbitration EditMode 测试。
- [x] 10.7 运行 FullBody rollback replay 相关 EditMode 测试。
- [x] 10.8 运行配置根与目录边界静态测试。
- [x] 10.9 补充 Shift 输入绑定与 Dodge run latch 定向 EditMode 测试覆盖；本轮按用户要求未通过 Unity MCP 执行测试。
- [x] 10.10 运行本轮 Run latch 输出端口修改后的 `Assembly-CSharp` 与 `Assembly-CSharp-Editor` 编译验证。

## 11. 收尾

- [x] 11.1 运行 GitNexus `detect_changes()` 检查影响范围。
- [x] 11.2 记录仍保留的 `FullBody*` 兼容命名和下一步意图。
- [x] 11.3 确认没有新增 fallback 配置。
- [x] 11.4 确认没有新增第二 gameplay tick 入口。
- [x] 11.5 完成后再勾选所有任务。

## 12. 规格全面同步

- [x] 12.1 更新 proposal，明确 Shift、Directional Dodge、Run latch 和旧 HFSM 规格退役口径。
- [x] 12.2 更新 design，补齐 Run latch 权威、无移动输入回 Idle、Backstep 不写 latch 和停止后清 latch 的设计决策。
- [x] 12.3 更新 `character-config-root` delta，约束 Shift 同时绑定 Run 与 Dodge 且不新增 fallback 输入路径。
- [x] 12.4 更新 `fullbody-action-framework` delta，约束 Dodge request、completion、Run latch output 和 Action facts 边界。
- [x] 12.5 更新 `locomotion-state-graph-config` delta，约束 Locomotion runtime 是 Run latch 权威。
- [x] 12.6 更新 `unified-character-state-machine` delta，约束 graph runtime 不表达 Dodge lifecycle 或 Run latch 副作用。
- [x] 12.7 更新 `fullbody-rollback-replay` delta，约束 Action lifecycle 与 Run latch capture/replay 收敛。
- [x] 12.8 增加 `action-interrupt-arbiter` delta，移除“accepted Dodge 生成状态机 Action transition fact”的旧口径。
- [x] 12.9 增加 `character-frame-pipeline` delta，约束 accepted action request 进入 Action lifecycle submission 而不是默认 Locomotion graph。
- [x] 12.10 增加 `fullbody-hfsm-state-tree` 与 `fullbody-hfsm-tree-data` delta，退役 `/FullBody/Action/Dodge` 默认状态权威。
- [x] 12.11 运行 `openspec validate refactor-locomotion-action-state-graphs --strict --no-interactive`。

## 13. Backstep 无输入完整动画退出修正

- [x] 13.1 排查 FullBody runtime 的 Action lifecycle completion 调用面。
- [x] 13.2 运行 GitNexus impact 分析并记录索引未命中新 runtime 符号。
- [x] 13.3 将 Action lifecycle completion 扩展为可等待匹配动作动画播放完成。
- [x] 13.4 将 FullBody action submitter 的无移动 Dodge completion 规则接入动画完成门槛。
- [x] 13.5 同步 Character frame runtime port 和 rollback recording port 签名。
- [x] 13.6 增加 Backstep 无输入未播完不退出的 FullBody runtime 回归测试。
- [x] 13.7 更新 proposal、design 和相关 delta，删除 motion duration 直接退出的误导口径。
- [x] 13.8 运行 `Assembly-CSharp` 编译验证。
- [x] 13.9 运行 `Assembly-CSharp-Editor` 编译验证。
- [x] 13.10 运行 `openspec validate refactor-locomotion-action-state-graphs --strict --no-interactive`。
