## 1. 边界确认

- [x] 1.1 盘点 Timeline Preview Simulation 的唯一消费者。
- [x] 1.2 盘点 PreviewComposition 的场景、Prefab 与配置引用。
- [x] 1.3 盘点 Preview Simulation 在 Core、Float32、Unity 与 Character 程序集中的依赖。
- [x] 1.4 确认 Timeline/Track/Clip authoring 与 Agent Patch 合同不受影响。

## 2. Authoring Preview 单链路

- [x] 2.1 将 `CharacterPipelinePreviewController` 收敛为唯一 `PreviewPlaybackEngine`。
- [x] 2.2 从 `PreviewSession` 删除 Simulation 分支和 reset API。
- [x] 2.3 保持连续播放复用同一 playback generation。
- [x] 2.4 让连续播放与手动 seek 复用同一 playback generation，seek 只更新 sample time。
- [x] 2.5 让含 TreeClip、MotionCurve 或 MotionWarp 的 Timeline 仍可采样 AnimationTrack。
- [x] 2.6 在量化帧未变化时跳过重复 preview evaluation。
- [x] 2.7 将 Timeline 拖动位移统一为固定面板坐标，确保游标布局更新后仍可连续拖动采样。
- [x] 2.8 隔离游标拖动与轨道框选的 Pointer/Mouse 手势所有权。
- [x] 2.9 允许共享 Timeline 的多个正式 operation 映射到同一 Authoring Preview 动画 producer。
- [x] 2.10 仅在 Timeline、Target 或 authoring 内容切换时 retire 旧 animation lifecycle。
- [x] 2.11 为正式运行与直接采样预览配置显式 Timed/Immediate transition evaluation mode。

## 3. Preview UI 与 Host 清理

- [x] 3.1 从 `TimelinePreviewSession` 删除 Action target snapshot state。
- [x] 3.2 从 `TimelineEditorView` 删除 Action Target 按钮与 Popup。
- [x] 3.3 从 `TimelinePreviewTarget` 删除 Action target snapshot API 和缓存。
- [x] 3.4 从 `CharacterPipelineHost` 删除 `PreviewComposition` 字段与属性。
- [x] 3.5 收窄 `CanPreviewTimeline` 到正式动画表现依赖。
- [x] 3.6 从 `CharacterPipelinePreviewProgram` 删除 Gameplay activation 解析，只保留 Timeline operation 解析。

## 4. Preview Simulation 代码删除

- [x] 4.1 删除 `PreviewSimulationExecution`。
- [x] 4.2 删除 `PreviewSimulationActorRegistration`。
- [x] 4.3 删除 `IPreviewSimulationActorRegistration`。
- [x] 4.4 删除 `SimulationPreviewActionActivation` 与 Kernel preview activation 输入。
- [x] 4.5 删除 `Float32PreviewInputSourcePort`。
- [x] 4.6 删除 Preview Float32 Pipeline pass contracts 与 runtimes。
- [x] 4.7 删除 Unity Preview Session Source、Pipeline 与 pass definitions。
- [x] 4.8 删除 `SimulationTickSourceKind.Preview`。
- [x] 4.9 删除 Float32 与 Fixed 的 `EntryOperation` Preview override。

## 5. Preview Simulation 资产删除

- [x] 5.1 删除 Corin Preview Session Composition 资产。
- [x] 5.2 删除 Preview Session Source 资产。
- [x] 5.3 删除 Preview Pipeline 与三项 pass 资产。
- [x] 5.4 删除 Prefab 中 `m_PreviewComposition` 引用。
- [x] 5.5 删除 ServerAuthoritative Client scene 中 `m_PreviewComposition` 引用。
- [x] 5.6 删除 DotRecast Authority Client scene 中 `m_PreviewComposition` 引用。

## 6. 文档与验证

- [x] 6.1 更新 `openspec/project.md` 的 Preview 当前口径。
- [x] 6.2 确认 current spec 冲突由本 change delta 完整替换。
- [x] 6.3 搜索确认没有 Preview Simulation、PreviewComposition 或 Action target preview 残留。
- [x] 6.4 编译受影响的 Core、Float32、Simulation Unity、Character Runtime 与 Timeline Editor 程序集。
- [x] 6.5 关闭全部 dotnet build server。
- [x] 6.6 严格校验本 OpenSpec change。

## 7. MotionCurve 只读表现投影

- [x] 7.1 复用 MotionCurve 正式区间采样 API，按 Timeline 帧率从零绝对求值当前时间。
- [x] 7.2 对单一 contribution 应用权重、Local/World 空间、累计位移与累计朝向。
- [x] 7.3 对同一区间多个 Motion 来源显式报错，不复制正式 Motion arbitration。
- [x] 7.4 将求值结果只应用到 visual root，不修改 logic root、CharacterController 或 Simulation body。
- [x] 7.5 在退出预览、切换 Timeline 或 Target 时恢复 visual root 原始姿态。
- [x] 7.6 更新 Preview 状态文案并确认 Agent authoring 合同不受影响。
- [x] 7.7 编译 Character Runtime 与 Timeline Editor 程序集并关闭 build server。
- [x] 7.8 严格校验本 OpenSpec change。
