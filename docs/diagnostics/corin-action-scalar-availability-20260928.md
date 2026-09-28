# 攻击混合栈参数可用性修复

## 现场

用户日志：`FullBodyAction`、`corin.full-body-action.slot`、Completion 3211、`SourceIncomplete`。单一攻击来源在 0.16 秒 Crossfade 的 0.0553377 秒处报错，权重 0.2761153。编辑器 Console 回读存在同一条记录。

## 代码原因

`CharacterPoseNativeActionSlotSource.Materialize` 对 Vector3、Quaternion 参数保留标量页的 availability=0，实际值由类型化参数通道承载。例如可琳 EventGraph 的 `animation.lean.rotation` 是 Quaternion。

请求契约和 `AnimationBlendSourcePoseWorkspace.PrepareCapture` 均允许 availability 为 0 或 1；但 `AnimationSlotBlendJob.IsValidSource` 要求每个参数都是 1，将合法的未提供标量误报为 `SourceIncomplete`。此外，混合栈历史和存储姿态此前没有保留 availability，输出又无条件设为 1。此前只处理动作源读取类型，未完整处理下游契约。

## 修改

- 源参数检查接受契约规定的 0/1，非法标记、非有限值、采样未完成等检查继续保留。
- 混合每个参数时，只累计提供该参数的来源，并按这些来源的权重归一化；无贡献则输出 availability=0。
- Scratch、History、Stored 三层增加原生字节缓冲，将 availability 随值一起复制、发布、清空和释放。打断后保存旧姿态时也保留缺值信息。
- 使用现有混合栈路径，初始化时分配原生缓冲，逐帧不新增托管分配。未修改动画、同步组、攻击时间轴或 IK，不需要重烘。

## 检查边界

源代码与请求契约已核对，`git diff --check` 通过。Unity Bee 的 `ThirdPersonClient.Runtime` Csc、ILPostProcess、ScriptAssemblies CopyFiles 均返回 exitcode=0，程序集于 2026-09-28 15:40:56 更新，晚于最后源码修改时间 15:40:45。重载后 MCP 未恢复正常响应，未确认新类型已加载或 Console 清空后的状态。

未新增测试，未进入 Play 或执行 replay。运行中的攻击是否还出现其他来源不完整问题，需要用户手测确认；本修复不把所有 `SourceIncomplete` 都解释为同一种原因。
