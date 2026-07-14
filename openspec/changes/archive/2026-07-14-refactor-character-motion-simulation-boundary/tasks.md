## 1. 固定现有运动链路和迁移范围

- [x] 1.1 盘点 `CharacterPipelineHost` 的 `CharacterController`、visual root、input source 和 motion authority 序列化字段。
- [x] 1.2 盘点 `CharacterPipeline` 构造链中 concrete `CharacterController` 的传递位置。
- [x] 1.3 盘点 `CharacterMotionStage` 对 `CharacterController.Move`、`transform` 和 `isGrounded` 的全部访问。
- [x] 1.4 盘点 LocalSolver、ExternalPose 和 None 三种 motion authority 的现有分支。
- [x] 1.5 盘点 partial correction、full correction、ExternalPose 和初始化时的所有逻辑位姿写入。
- [x] 1.6 盘点 `MotionResult`、`ResolvedCharacterMotionFact`、logic sample 和 motion diagnostics 的生成字段。
- [x] 1.7 使用 `rg` 确认所有 `CharacterPipelineHost` 场景、prefab 和 asset 装配点。
- [x] 1.8 记录 Sandbox Corin Host、`CharacterController`、logic root 和 visual root 的 scene fileID。
- [x] 1.9 明确本 change 不修改 MotionContribution 仲裁、Timeline MotionCurve、MotionWarp 或动作手感数值。
- [x] 1.10 明确本 change 不实现 Unity 服务端、纯 C# KCC、DotRecast、确定性 KCC 或新 Network Model。

## 2. 建立无 Unity 类型的运动执行合同

- [x] 2.1 定义包含 position、rotation、velocity 和 grounded 的逻辑体状态值类型。
- [x] 2.2 定义包含 logic tick、delta time、当前体状态和最终 `MotionIntent` 的执行输入。
- [x] 2.3 定义包含 requested/actual displacement、position、rotation、velocity、grounded 和碰撞摘要的执行结果。
- [x] 2.4 定义只负责执行一次世界约束运动步骤的 `ICharacterMotionExecutor` 合同。
- [x] 2.5 让 executor 合同不引用 `CharacterController`、`Transform`、`CollisionFlags`、Graph、Timeline、Action 或网络 packet。
- [x] 2.6 让 executor 合同不读取 correction sequence、server tick、ack 或 Network Model policy。
- [x] 2.7 明确 executor 对非法输入和不可执行状态返回正式错误，不生成默认位姿。
- [x] 2.8 保持 `MotionIntent` 为 gameplay motion 语义，不新增 backend id、physics enum 或 executor type 字段。

## 3. 建立逻辑位姿端口

- [x] 3.1 定义读取当前逻辑体状态的 `ICharacterLogicPosePort` 合同。
- [x] 3.2 定义通过该端口应用 ExternalPose 的正式操作。
- [x] 3.3 定义通过该端口执行完整 correction 重定位的正式操作。
- [x] 3.4 让 pose port 不访问 visual root 或 Animancer transform。
- [x] 3.5 让 pose port 不执行 motion contribution 仲裁、modifier 或网络策略。
- [x] 3.6 定义 executor result 与 pose port 当前状态不一致时的明确错误。
- [x] 3.7 删除通过 Host transform、Stage 缓存 transform 或场景搜索补齐逻辑位姿的设计入口。

## 4. 实现唯一 Unity CharacterController adapter

- [x] 4.1 新增正式 Unity `CharacterController` motion executor component。
- [x] 4.2 让该 component 显式引用唯一 `CharacterController` 和 logic root。
- [x] 4.3 将当前 LocalSolver 的旋转应用顺序迁入 Unity executor。
- [x] 4.4 将当前 `CharacterController.Move` 调用迁入 Unity executor。
- [x] 4.5 将当前 actual displacement 计算迁入 Unity executor result。
- [x] 4.6 将当前 grounded 和碰撞摘要读取迁入 Unity executor result。
- [x] 4.7 让 Unity executor 同时提供正式 logic pose port 能力或引用唯一 pose adapter。
- [x] 4.8 让 Unity adapter 对缺失、禁用或绑定错误的 `CharacterController` 明确失败。
- [x] 4.9 确认项目中只有该 adapter 可以为 CharacterPipeline 主线调用 `CharacterController.Move`。
- [x] 4.10 删除任何自动 `GetComponent<CharacterController>`、子节点搜索或默认组件创建。

## 5. 迁移 CharacterMotionStage

- [x] 5.1 将 `CharacterMotionStage` 构造参数从 concrete `CharacterController` 改为正式 pose port 和可选 executor。
- [x] 5.2 保持 contribution 收集和 `MotionResolver` 运行顺序不变。
- [x] 5.3 保持固定 `MotionModifier` 顺序不变。
- [x] 5.4 让 LocalSolver 在 modifier 和 correction plan 后构造唯一 execution input。
- [x] 5.5 让 LocalSolver 只调用一次正式 motion executor。
- [x] 5.6 让 Stage 从 execution result 构造既有 `MotionResult`。
- [x] 5.7 让 Stage 从 execution result 构造既有 `ResolvedCharacterMotionFact`。
- [x] 5.8 让 Stage 从 pose port/execution result 推送唯一 logic sample。
- [x] 5.9 删除 Stage 中 direct `CharacterController.Move` 调用。
- [x] 5.10 删除 Stage 中 direct `CharacterController.transform` 和 `isGrounded` 读取。
- [x] 5.11 删除 Stage 中 direct logic Transform 写入。
- [x] 5.12 让 executor 缺失、执行失败或 result 非法时停止该次 motion 结算并报告 provenance。

## 6. 闭合 correction 与 ExternalPose

- [x] 6.1 保持 correction plan 在 gameplay intent 和 motion modifier 之后生成。
- [x] 6.2 将可参与碰撞的 correction delta 合入唯一 execution intent。
- [x] 6.3 从 execution result 计算 correction 实际应用 delta 和 application extent。
- [x] 6.4 让完整 correction 的显式重定位只通过 logic pose port。
- [x] 6.5 保持 correction acknowledgement 的 input sequence 和 server tick 来源不变。
- [x] 6.6 防止同一 correction 同时由 executor 和 pose port 重复应用。
- [x] 6.7 让 ExternalPose 只通过 logic pose port 应用外部样本。
- [x] 6.8 让 ExternalPose 分支完全不调用 motion executor。
- [x] 6.9 让 ExternalPose 分支继续生成正式 MotionResult 和 logic sample。
- [x] 6.10 让 None 分支不执行 executor 或写入 gameplay motion。

## 7. 迁移 Pipeline 与 Host 装配

- [x] 7.1 从 `CharacterPipeline` 构造函数删除 concrete `CharacterController` 参数。
- [x] 7.2 让 `CharacterPipeline` 接收正式 logic pose port 和按模式可选 executor。
- [x] 7.3 从 `CharacterPipelineHost` 删除旧 `m_CharacterController` 字段。
- [x] 7.4 为 Host 增加显式 logic pose adapter 引用。
- [x] 7.5 为 Host 增加显式 motion executor adapter 引用。
- [x] 7.6 让 LocalSolver 缺少 pose port 或 executor 时配置失败。
- [x] 7.7 让 ExternalPose 缺少 pose port 时配置失败。
- [x] 7.8 让 ExternalPose 不要求 executor 或 `CharacterController`。
- [x] 7.9 让 None 不因缺少 executor 而创建默认实现。
- [x] 7.10 保持 visual root、Animancer、input profile 和 camera 装配边界不变。
- [x] 7.11 删除旧构造重载、兼容字段、自动转换和双重装配入口。

## 8. 迁移 Sandbox 与 Corin 资产

- [x] 8.1 在 Sandbox Corin logic root 上配置正式 Unity motion executor/logic pose adapter。
- [x] 8.2 将现有 `CharacterController` 显式绑定到该 adapter。
- [x] 8.3 将 Corin Host 的 logic pose adapter 引用绑定到正式 component。
- [x] 8.4 将 Corin Host 的 LocalSolver executor 引用绑定到正式 component。
- [x] 8.5 保持 Corin visual root 引用指向现有 model root。
- [x] 8.6 保持 Corin input source、motion authority、PipelineDefinition 和相机引用不变。
- [x] 8.7 从 Sandbox YAML 删除旧 Host `m_CharacterController` 序列化数据。
- [x] 8.8 使用 `rg` 确认没有其它 Host asset 遗留旧字段或缺失正式 adapter。
- [x] 8.9 不创建 runtime migrator、Editor fallback、`FormerlySerializedAs` 或一次性兼容 component。

## 9. 收口 diagnostics 与依赖方向

- [x] 9.1 让 motion diagnostics 继续显示 contribution、resolved intent、modifier 和 correction delta。
- [x] 9.2 增加 executor identity、requested/actual execution result 和碰撞摘要的只读诊断字段。
- [x] 9.3 让 diagnostics 从正式 Stage/result 读取，不反射 Unity adapter 私有状态。
- [x] 9.4 让 `ResolvedCharacterMotionFact` 保持 model-neutral，不保存 executor component 或 Unity identity。
- [x] 9.5 使用 `rg` 确认 BTSMTL、Timeline、ActionProfile 和 PipelineDefinition 不引用 executor implementation type。
- [x] 9.6 使用 `rg` 确认 Presentation 不调用 motion executor 或 logic pose 写入。
- [x] 9.7 使用 `rg` 确认 Network Model 不直接调用 Unity `CharacterController`。
- [x] 9.8 使用 `rg` 确认 Character runtime 不出现 backend enum、DotRecast、Fantasy 或 deterministic mode switch。

## 10. 纠正网络模型和后继 change 文档

- [x] 10.1 更新 `openspec/project.md` 的 Current State，记录已归档 network model boundary 和新的 motion execution boundary。
- [x] 10.2 更新 `openspec/project.md` 的 Network Boundary，区分预测结果、权威输入和独立服务端模拟。
- [x] 10.3 在 `add-local-two-client-gameplay-network-closure/proposal.md` 移除“resolved motion 限幅等于服务端权威运动”的表述。
- [x] 10.4 在该 change 的 `design.md` 把 Owner outgoing 改为 canonical input/action request 到独立服务端模拟。
- [x] 10.5 在该 change 的 `design.md` 明确 Unity authoritative backend 与纯 C# KCC backend 只能选择一个作为当次正式实现。
- [x] 10.6 从该 change 的服务端 motion 设计删除直接累加客户端 applied displacement 生成 canonical pose 的路径。
- [x] 10.7 重写该 change 的 motion tasks，使服务端从 canonical input/action state 推进并调用正式 backend。
- [x] 10.8 更新该 change 的 spec deltas，保持 resolved client motion 仅用于 prediction comparison/diagnostics。
- [x] 10.9 保持确定性 KCC/rollback 为独立 future Network Model，不添加未实现选项。
- [x] 10.10 运行 `openspec validate add-local-two-client-gameplay-network-closure --strict --no-interactive` 并修正全部文档问题。

## 11. 删除旧路径并检查统一性

- [x] 11.1 删除 Host、Pipeline 和 MotionStage 上全部 concrete `CharacterController` 依赖。
- [x] 11.2 删除除正式 Unity executor 外的 CharacterPipeline 主线 `CharacterController.Move` 调用。
- [x] 11.3 删除 ExternalPose 的 direct Transform 写入路径。
- [x] 11.4 删除 correction 的 direct Transform 写入路径。
- [x] 11.5 删除旧 motion executor 命名、旧 serialized field 和无引用 adapter。
- [x] 11.6 使用 `rg` 确认没有 fallback、兼容构造函数、自动组件搜索或双写位姿路径。
- [x] 11.7 使用 `rg` 确认只有 MotionStage 生成最终 `MotionResult` 和 resolved motion fact。
- [x] 11.8 使用 `rg` 确认 logic root 与 visual root 仍是两条明确且单向的数据链。

## 12. 编译与严格校验

- [x] 12.1 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 `Assembly-CSharp`。
- [x] 12.2 客户端 runtime 编译后立即执行 `dotnet build-server shutdown`。
- [x] 12.3 使用相同参数编译 `Assembly-CSharp-Editor`。
- [x] 12.4 Editor 编译后立即执行 `dotnet build-server shutdown`。
- [x] 12.5 运行 `openspec validate refactor-character-motion-simulation-boundary --strict --no-interactive` 并修正全部问题。
