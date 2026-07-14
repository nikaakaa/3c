## 1. 语义盘点

- [x] 1.1 搜索正式 runtime 中所有 `MotionProposal` 引用。
- [x] 1.2 搜索 OpenSpec 中所有 `MotionProposal` 引用。
- [x] 1.3 搜索正式 runtime 中是否引用 `BBBNexus.MotionClipData`。
- [x] 1.4 搜索正式 runtime 中是否引用 `BBBNexus.WarpedMotionData`。
- [x] 1.5 列出当前 `StrictGameplayOutput` 中 motion 字段。

## 2. Motion 命名清理

- [x] 2.1 将 `MotionProposal` 类型重命名为 `MotionIntent`。
- [x] 2.2 将 `MotionProposal.cs` 文件重命名为 `MotionIntent.cs`。
- [x] 2.3 将 `StrictGameplayOutput.MotionProposal` 重命名为 `MotionIntent`。
- [x] 2.4 将 `MotionResolver.Resolve` 的 `baseProposal` 参数重命名为 `baseIntent`。
- [x] 2.5 将 `MotionResult.RequestedDisplacement` 的语义确认指向最终 intent displacement。
- [x] 2.6 清理所有 `Proposal` 命名残留。
- [x] 2.7 不保留 `MotionProposal` 兼容别名。

## 3. Motion Modifier 链路

- [x] 3.1 定义 `MotionModifier` 的运行时输入和输出边界。
- [x] 3.2 定义 `MotionModifierContext` 需要读取的 actor pose、deltaTime、authority mode 和 runtime facts。
- [x] 3.3 在 `CharacterMotionStage` 中明确 resolver 后、Move 前的 modifier 执行点。
- [x] 3.4 第一阶段固定 modifier 顺序，不做动态插件注册。
- [x] 3.5 保持 `MotionContribution` 只表达来源贡献。
- [x] 3.6 保持 `MotionIntent` 只表达 Move 前最终意图。
- [x] 3.7 保持 `MotionResult` 只表达 Move 后实际结果。

## 4. Motion Warp 数据入口

- [x] 4.1 定义 Timeline 输出 motion warp window 的数据结构。
- [x] 4.2 定义 motion warp target key 的读取规则。
- [x] 4.3 定义 target 缺失时的正式行为。
- [x] 4.4 定义 position correction 权重。
- [x] 4.5 定义 yaw correction 权重。
- [x] 4.6 定义 max correction 限制。
- [x] 4.7 让 TimelinePlaybackScheduler 采样 motion warp window 时只写 pipeline output。
- [x] 4.8 让 MotionStage 在 Move 前应用 motion warp。
- [x] 4.9 禁止 Timeline clip、Graph 节点或 Animator 直接应用 motion warp。

## 5. 文档和规格同步

- [x] 5.1 更新相关 OpenSpec 文档中的 `MotionProposal` 口径。
- [x] 5.2 明确 Action/Ability 身份不由本变更定义。
- [x] 5.3 记录为什么不恢复 BBB SO/config 链路。
- [x] 5.4 运行 `openspec validate refactor-action-motion-semantics --strict --no-interactive`。
- [x] 5.5 使用 `rg` 检查正式 runtime 不再残留 `MotionProposal`。
- [x] 5.6 使用 `rg` 检查正式 runtime 不引用 `BBBNexus.MotionClipData` 或 `BBBNexus.WarpedMotionData`。
