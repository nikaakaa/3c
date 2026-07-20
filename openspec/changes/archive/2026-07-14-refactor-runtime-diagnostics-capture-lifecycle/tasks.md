## 1. 现状与正式合同

- [x] 1.1 盘点 `RuntimeTraceBuffer`、`RuntimeDiagnosticsContext`、target registry、Session、Analyzer、snapshot 与全部调用方的 ownership 和生命周期。
- [x] 1.2 盘点 Graph、StateMachine、Timeline、Blackboard、Motion、Animation producer 的事件种类、调用频率和现有 channel。
- [x] 1.3 标记每种现有事件是当前状态、Boundary Capture、Evaluation Capture 还是 Continuous Capture，列出不应默认录制的高频事件。
- [x] 1.4 定义 target 级 diagnostics interest 的 identity、Live/Capture kind、channel 集合、Capture detail 与 acquire/release 语义。
- [x] 1.5 定义 effective interest 并集、无 interest 的 `None` 状态和 target 切换/注销时的清理语义。
- [x] 1.6 定义 source/runtime instance/domain/fact kind 组成的 Live State 稳定键。
- [x] 1.7 定义 Live State record、单调 revision、delta cursor、首次同步和落后重同步合同。
- [x] 1.8 定义 Capture session identity、detail、segment、单调 cursor、停止冻结和容量边界合同。
- [x] 1.9 定义 provider 输出的 source-mapped current state、Timeline playback summary、Host summary、capture history view 与版本化 change set。
- [x] 1.10 明确 strict source identity/content hash、显式 Host、唯一精确匹配、Follow/Pin、Ended 与 domain reload 不变的 API 边界。

## 2. Runtime target 与 interest 生命周期

- [x] 2.1 将 diagnostics target 注册改为默认没有启用的 Live/Capture interest。
- [x] 2.2 用正式 target diagnostics store 替换默认 `RuntimeTraceChannel.All` 的 `RuntimeTraceBuffer` 初始化。
- [x] 2.3 实现 target 内 interest acquire，并返回可精确释放的 handle。
- [x] 2.4 实现 target 内 interest release，并在重复释放、错误 target 或已终止 target 时明确失败或无效化。
- [x] 2.5 实现同一 target 多个 interest 的 channel 并集与 Capture detail 并集。
- [x] 2.6 实现最后一个 interest 释放后将 effective collection 关闭到 `None`。
- [x] 2.7 让 target 注销时失效全部 interest，并向 Editor 发布正式 target lifecycle。
- [x] 2.8 确保 target metadata、program revision 与 Source Map 在无 interest 时仍可用于严格 target 解析。
- [x] 2.9 删除 target 上由 Editor 直接设置全局 channel 的公开写入口。

## 3. Live State runtime 数据

- [x] 3.1 实现有界的 Live State store，按稳定键覆盖当前事实而不是追加历史。
- [x] 3.2 实现 Live State 的增量日志、cursor 读取和落后后的完整当前状态同步。
- [x] 3.3 让 `RuntimeDiagnosticsContext` 在构造 payload 或解析 source handle 前判断 Live/Capture interest。
- [x] 3.4 将 Graph 创建/销毁、Runnable 生命周期和当前 node status 接入 Live State。
- [x] 3.5 将 Composite child selected、State transition、State scope 与 exit barrier 接入 Live State。
- [x] 3.6 将 Timeline playback、logic/visual time、Track/Clip、TreeClip 与 terminal 接入 Live State。
- [x] 3.7 将 Blackboard 写入/清理、Motion contribution/resolved 接入 Live State。
- [x] 3.8 将 Animation selection、sample、lifecycle、fade 和 presentation interpolation 接入 Live State。
- [x] 3.9 为等价当前状态去重，避免未变化的 node/edge/lifecycle 反复产生 Live State mutation。
- [x] 3.10 确保 visual time、sample time、fade 等连续状态只覆盖当前 record，不写入默认 history。

## 4. Capture runtime 数据

- [x] 4.1 实现独立的有界 Capture segment store，不与 Live State store 双写同一容器。
- [x] 4.2 实现 Capture 开始、停止、冻结、释放和 target 终止时的生命周期。
- [x] 4.3 实现 Boundary detail 的 node/state/edge selected/TreeClip/Timeline/Animation lifecycle 记录。
- [x] 4.4 将 `EdgeEvaluated`、ConditionGraph 与 State transition evaluation 仅接入 Evaluation detail。
- [x] 4.5 将 Timeline logic/visual time、Animation sample/fade、presentation interpolation 仅接入 Continuous detail。
- [x] 4.6 让 Capture 满容量时按完整 segment 丢弃最旧数据，不产生半个 tick/frame segment。
- [x] 4.7 为 Capture 提供只读 cursor delta，不在每次读取时复制所有 event。
- [x] 4.8 在 Stop Capture 或 target 结束时生成不持有 runtime store 的不可变 Capture snapshot。
- [x] 4.9 删除“Pause Live 即持续 Buffer history”的旧语义与实现。

## 5. 共享增量 read provider

- [x] 5.1 定义 Editor-only target provider 的 attachment、source map cache、Live State cursor、Capture cursor 和 revision 字段。
- [x] 5.2 在 provider 初次附着时捕获严格 Source Map snapshot 一次。
- [x] 5.3 让 target revision/source map 变化时正式替换 provider cache，禁止按名称或路径迁移旧映射。
- [x] 5.4 实现 provider 对 Live State delta 的一次性消费与 source-mapped current state 更新。
- [x] 5.5 实现 provider 对 Capture delta 的一次性消费与录制中摘要更新。
- [x] 5.6 实现 provider 的 Graph instance 索引和 Timeline playback 摘要增量维护。
- [x] 5.7 实现 provider 的 Host channel summary 增量维护。
- [x] 5.8 实现 provider change set，精确表示变更 source、instance、菜单数据和 capture position。
- [x] 5.9 确保一个 target 在一次 Editor update 中最多消费一次 runtime delta。
- [x] 5.10 在 revision 未变化时避免 event list、Source Map、LINQ 结果和 view model 的分配。
- [x] 5.11 实现 Capture 停止后的冻结 history view，并只在作者改变 history position 时计算对应视图。
- [x] 5.12 实现 target Ended 时的 current state/capture snapshot 冻结与 runtime 引用释放。

## 6. RuntimeDebugSession 与窗口 binding

- [x] 6.1 将 RuntimeDebugSession 收敛为 target resolver、interest coordinator、shared provider 与 capture position owner。
- [x] 6.2 删除 Session 的全局 SetChannels/Buffer 控制和 full snapshot refresh API。
- [x] 6.3 让 Graph binding 在进入/退出 Live Debug、切换 source、关闭窗口时声明和释放所需 Live interest。
- [x] 6.4 让 Timeline binding 在进入/退出 Live Debug、切换 source、关闭窗口时声明和释放所需 Live interest。
- [x] 6.5 让 Host Inspector 只在实际显示诊断时声明和释放所需 Live interest。
- [x] 6.6 实现共享 Capture 开始/停止命令及其 detail 选择，不让窗口各自创建 Capture。
- [x] 6.7 保持 Graph/Timeline 各自 Follow/Pin，只让它们从共享 provider 解析实例。
- [x] 6.8 保持显式 Host、唯一精确 source/hash target 匹配和 mismatch 状态，不增加 fallback。
- [x] 6.9 让 Frozen Live、Capture history、Ended、Detached 状态有互斥且明确的 Session 表达。
- [x] 6.10 让 domain reload 后重建的窗口 binding 重新获取 interest，不序列化 runtime handle、target 或 instance。

## 7. Graph 与 Host UI 增量消费

- [x] 7.1 将 Graph target request/fingerprint 计算移到页面绑定、authoring 改动、Undo/Redo 和 locator 恢复边界。
- [x] 7.2 删除 BaseTreeWindow `Update` 中的全量 Live Debug refresh。
- [x] 7.3 删除 Graph overlay 刷新时对全部 Node/Edge 的无条件 clear 和重绘。
- [x] 7.4 让 Graph 只按 provider change set 更新当前 binding 命中的 Node、Edge、StateMachine 状态。
- [x] 7.5 让 Graph target/instance 菜单只在对应 menu revision 变化时重建。
- [x] 7.6 将 Graph 的 Live、Frozen、Capture、Ended 与 source mismatch 状态显示为明确产品状态。
- [x] 7.7 让 CharacterPipelineHostEditor 改读 provider current summary，不再调用全量 `Latest(channel)`。
- [x] 7.8 让 Host Inspector 的 repaint 只由相关 provider revision 驱动。

## 8. Timeline UI 增量消费

- [x] 8.1 将 Timeline target request/fingerprint 计算移到 Timeline locator 或 authoring 内容变化边界。
- [x] 8.2 删除 TimelineEditorWindow `Update` 中的全量 Live Debug refresh。
- [x] 8.3 删除 Timeline Live Debug 对完整 event list 的 LINQ 过滤、时间扫描和摘要重建。
- [x] 8.4 让 Timeline 从 provider current playback summary 读取 logic/visual time、cycle、terminal 和 provenance。
- [x] 8.5 让 Timeline 从 provider change set 增量更新 active Track、Clip、TreeClip 和 animation lifecycle overlay。
- [x] 8.6 让 Timeline playback menu 只在本 Timeline playback summary revision 变化时刷新。
- [x] 8.7 让 Timeline Capture history 使用 shared capture position，不调用 preview evaluator 或重新采样 authoring Timeline。
- [x] 8.8 将 Timeline 的 Live、Frozen、Capture Recording、Capture History、Ended 与 mismatch 状态显示为明确产品状态。

## 9. 删除旧链路与文档

- [x] 9.1 删除 `RuntimeTraceBuffer.Snapshot()` 全量复制路径和依赖它的 full-history live API。
- [x] 9.2 删除 `RuntimeDebugAnalyzer` 全量重建 Source Map/state/instance/events 的路径。
- [x] 9.3 删除 `RuntimeDebugTraceSnapshot.Capture` 与 `RuntimeDebugViewModel.Events/Latest` 作为 Live 数据入口。
- [x] 9.4 删除窗口双刷新、每帧菜单重建、每帧 source fingerprint 和与它们对应的旧字段。
- [x] 9.5 搜索确认不存在默认 `RuntimeTraceChannel.All`、第二个 provider、第二个 capture store 或旧 Buffer compatibility API。
- [x] 9.6 更新 `openspec/project.md` 的 diagnostics 架构口径。
- [x] 9.7 更新 `btsmtl-runtime-diagnostics`、`btsmtl-timeline-editor-preview`、`btsmtl-tree-inspector-information-architecture` 与 `character-pipeline-runtime` current spec。
- [x] 9.8 使用项目既有受影响程序集静态编译入口完成编译；若调用 `dotnet build` 或 `msbuild`，使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`。
- [x] 9.9 若执行 `dotnet build` 或 `msbuild`，构建结束后立即执行 `dotnet build-server shutdown`。
- [x] 9.10 运行 `openspec validate refactor-runtime-diagnostics-capture-lifecycle --strict --no-interactive`。
