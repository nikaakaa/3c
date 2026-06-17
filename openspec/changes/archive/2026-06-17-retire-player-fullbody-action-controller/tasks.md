## 1. 准备
- [x] 1.1 读取本变更 `proposal.md`。
- [x] 1.2 读取本变更 `design.md`。
- [x] 1.3 读取本变更 `tasks.md`。
- [x] 1.4 读取 `character-frame-pipeline` 当前 spec。
- [x] 1.5 读取 `character-runtime-ports` 当前 spec。
- [x] 1.6 读取 `fullbody-action-framework` 当前 spec。
- [x] 1.7 读取 `fullbody-rollback-replay` 当前 spec。
- [x] 1.8 搜索 `PlayerFullBodyActionController` 在代码、测试、prefab、scene 和 active OpenSpec change 中的引用。
- [x] 1.9 搜索 `FullBodySubmissionBuilder`、`FullBodyIntegratedFrameAdapter` 和 `LegacyFullBodyIntegrated` 的生产引用。
- [x] 1.10 对计划修改的 runtime symbol 执行 GitNexus impact analysis，并记录 blast radius。
- [x] 1.11 若 impact 返回 HIGH 或 CRITICAL，先报告风险再继续实施。

## 2. 测试先行
- [x] 2.1 增加静态测试：生产代码不得定义 `PlayerFullBodyActionController`。
- [x] 2.2 增加静态测试：生产代码不得引用 `PlayerFullBodyActionController` 作为字段、属性、构造参数或端口依赖。
- [x] 2.3 增加静态测试：`CharacterFrameRuntimeController` 不通过 FullBody controller 解析状态机 runner。
- [x] 2.4 增加静态测试：`CharacterFrameRuntimePortAdapter` 不通过 FullBody controller 访问 runner、snapshot、Dodge config、policy 或 output runtime。
- [x] 2.5 增加静态测试：`LocomotionFrameSubmitter` 和 `FullBodyActionFrameSubmitter` 不共享 `FullBodySubmissionBuilder` 作为正式构建中心。
- [x] 2.6 增加静态测试：正式 `CharacterFramePlan` 或 frame output source 不再使用 `LegacyFullBodyIntegrated`。
- [x] 2.7 增加静态测试：生产图不引用 `FullBodyIntegratedFrameAdapter` 作为正式 adapter。
- [x] 2.8 增加静态测试：Corin prefab/scene 不挂载 `PlayerFullBodyActionController`。
- [x] 2.9 增加 EditMode 测试：角色级 runtime controller 可推进 Locomotion-only frame。
- [x] 2.10 增加 EditMode 测试：角色级 runtime controller 可推进 Dodge accepted frame。
- [x] 2.11 增加 EditMode 测试：Dodge active 时 `CharacterFramePlan` 压制 Locomotion motion/animation。
- [x] 2.12 增加 EditMode 测试：Dodge exit 后回到 Locomotion 输出。
- [x] 2.13 增加 rollback replay 测试：Move/Run/Dodge 从 restore tick 重放后 strict facts 收敛。

## 3. Submitter 边界拆分
- [x] 3.1 盘点 `FullBodySubmissionBuilder` 当前构建的 Locomotion、Action、state facts 和 output 字段。
- [x] 3.2 将 Locomotion request/facts 构建抽到 Locomotion submitter 专属构建职责。
- [x] 3.3 将 FullBody Action request/facts 构建抽到 FullBody Action submitter 专属构建职责。
- [x] 3.4 更新 `CharacterFrameSubmitterGraph`，让 Locomotion 与 FullBody Action 作为 sibling submitter 独立提交候选。
- [x] 3.5 移除正式路径对共享 `FullBodySubmissionBuilder` 的依赖。
- [x] 3.6 收口 frame output source，删除 `LegacyFullBodyIntegrated` 在正式路径中的身份。
- [x] 3.7 降级或删除 `FullBodyIntegratedFrameAdapter` 的生产图引用。
- [x] 3.8 确认 FullBody Action submitter 不读取 Locomotion controller 私有状态。
- [x] 3.9 确认 Locomotion submitter 不处理 Dodge/Attack/Jump 的 action policy。
- [x] 3.10 确认没有新增第二 submitter graph、第二 output composer 或第二仲裁入口。

## 4. 状态机运行时归属
- [x] 4.1 新增 `CharacterStateMachineRuntime` 或等价模块。
- [x] 4.2 将 `CharacterStateMachineRunner` 创建迁入状态机运行时模块。
- [x] 4.3 将 current snapshot、active path、pending transition view 迁入状态机运行时模块。
- [x] 4.4 将 capture/restore gameplay state 迁入状态机运行时模块。
- [x] 4.5 将 diagnostic restore state 与 gameplay restore state 保持分离。
- [x] 4.6 确认没有新增第二 runner。

## 5. FullBody Action Runtime 归属
- [x] 5.1 新增 `FullBodyActionRuntime`、`FullBodyActionRuntimePort` 或等价窄模块。
- [x] 5.2 将 Dodge config 解析迁入 FullBody Action runtime。
- [x] 5.3 将 interrupt policy 解析和缓存迁入 FullBody Action runtime。
- [x] 5.4 将 current action resistance 解析迁入 FullBody Action runtime。
- [x] 5.5 将 action request provider/resolver 所需 facts 通过窄端口暴露。
- [x] 5.6 确认 FullBody Action runtime 不读取 Locomotion controller 私有状态。

## 6. Output Runtime 归属
- [x] 6.1 将 `FullBodyOutputRuntimeHost` 从 controller 内部类迁出。
- [x] 6.2 为 output host 提供 input buffer、motion executor、animation presenter、Locomotion output、diagnostics 的显式依赖。
- [x] 6.3 更新 `FullBodyOutputRuntime` 调用端，使其不依赖 `PlayerFullBodyActionController`。
- [x] 6.4 确认 output modules 不重新做请求仲裁、状态切换或 motion resolve。
- [x] 6.5 确认缺失正式依赖时不创建 fallback executor、fallback presenter 或隐藏配置。

## 7. Runtime Port 和 Controller 收口
- [x] 7.1 更新 `CharacterFrameRuntimeController`，让它组合状态机 runtime、Locomotion runtime、FullBody Action runtime 和 output runtime。
- [x] 7.2 更新 `CharacterFrameRuntimePortAdapter`，移除 `PlayerFullBodyActionController` 依赖。
- [x] 7.3 删除或替换 `FullBodyRuntimePortAdapter` 中对 `PlayerFullBodyActionController` 的包装。
- [x] 7.4 删除 `PlayerFullBodyActionController.cs` 和对应 `.meta`。
- [x] 7.5 更新 `FullBodyActionTickAdapter` 或删除其旧 controller 字段。
- [x] 7.6 确认没有新增角色级第二 runtime controller。

## 8. Rollback / Snapshot 迁移
- [x] 8.1 更新 `FullBodyRollbackSimulation` 使用角色级 runtime controller 或 host。
- [x] 8.2 更新 `LocomotionSnapshotHistoryRecorder` 使用状态机 runtime / frame runtime 端口。
- [x] 8.3 迁移测试 fixture 中的 FullBody controller 构造。
- [x] 8.4 确认 replay 不直接调用 FullBody submitter 具体实现绕过 host。
- [x] 8.5 确认 snapshot capture/restore 不保存 Unity scene object、Animancer runtime object 或 input runtime object。

## 9. Prefab / Scene 迁移
- [x] 9.1 更新 `可琳.prefab`，移除 `PlayerFullBodyActionController` 组件。
- [x] 9.2 更新 `可琳_Humanoid.prefab`，移除 `PlayerFullBodyActionController` 组件。
- [x] 9.3 更新 Sandbox 和 CameraTest 相关 scene override，移除旧 controller 引用。
- [x] 9.4 确认 Character runtime controller 仍绑定正式 `CharacterConfigSO`。
- [x] 9.5 确认 input buffer、motion executor、facing provider、animation presenter 仍可解析。
- [x] 9.6 确认 prefab/scene 不新增第二 pipeline、第二 runner、第二 motion executor 或第二 presenter。

## 10. 验证
- [x] 10.1 运行 PlayerFullBodyActionController retirement 静态测试。
- [x] 10.2 运行 submitter boundary / legacy source 静态测试。
- [x] 10.3 运行 Character frame runtime controller 定向 EditMode 测试。
- [x] 10.4 运行 Character frame arbitration / plan 定向 EditMode 测试。
- [x] 10.5 运行 FullBody action framework 定向 EditMode 测试。
- [x] 10.6 运行 FullBody rollback replay 定向 EditMode 测试。
- [x] 10.7 运行 Corin prefab/scene binding 定向 EditMode 测试。
- [x] 10.8 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore`。
- [x] 10.9 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 10.10 运行 `openspec validate retire-player-fullbody-action-controller --strict --no-interactive`。
- [x] 10.11 运行 GitNexus `detect_changes()`。
- [x] 10.12 不运行 Unity batchmode。
