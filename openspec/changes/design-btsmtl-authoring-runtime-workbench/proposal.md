# BTSMTL Authoring Runtime Workbench

## Why

当前文档把“ScenePlay 场景运行”“Timeline 作者编辑”“RuntimeDebug 观察”混在了一个 Preview 名称下，导致产品形态不清楚：作者编辑时不知道自己是在改资产、看真实运行，还是看只读诊断；实现也容易重新做出窗口播放器、独立时钟和第二套 Runtime。

本 change 改为定义一个统一的 **Authoring Runtime Workbench**。它不是一个新的运行时，也不是一个单独的 Timeline 播放器，而是围绕同一个正式 ScenePlay Session 提供三种用户工作形态。Preview 只是其中一种形态：

1. **Authoring**：编辑正式 FlowCanvas、RootTree、子图、Timeline、Track、Clip、曲线和参数。
2. **Preview**：由 ScenePlay 驱动真实 Scene、Actor、Ability、RootTree、Timeline、Pose、Motion 和 Camera；作者可以继续修改内容，并在导出、准备和采用后继续观察结果。
3. **RuntimeDebug**：只读观察同一 Session 的真实调用路径和提交事实，按当前调用栈在 FlowCanvas 子图与 Slate Timeline 之间切换。

ScenePlay 是 Preview 的唯一正式运行底座，不是第四种产品形态。RuntimeDebug 也不创建第二个 Session、第二个 Actor 或第二个执行器。

## What Changes

- **重新命名产品 change**：从“rebuild Preview with ScenePlay”改为“BTSMTL Authoring Runtime Workbench”，主语从实现迁移改为三形态产品工作台。
- **明确三种产品形态**：Authoring、Preview、RuntimeDebug 的用户目的、可写边界、数据来源和显示行为分开定义。
- **统一运行归属**：一个 Workbench 使用一个正式 ScenePlay Session；窗口、页签、FlowCanvas、Slate 和 RuntimeDebug 都只绑定这个 Session，不按 Timeline 或页面创建运行实例。
- **重新定义 Preview**：Preview 是真实 ScenePlay 运行中的作者工作面，不是作者窗口本地播放。作者修改通过正式 Mutation、导出、Prepare 和 Adopt 进入当前 Session；Session、Scene、Actor 和 RuntimeDebug 绑定保持不变。
- **明确轻量更新边界**：参数、曲线、Clip 时间和可兼容内容修改可以在同一 Session 内后台准备并在安全边界采用；代码、状态布局、Composition 或不兼容运行结构不能承诺无感，必须明确显示需要重建或新 Session。
- **建立 RuntimeDebug 观察形态**：RuntimeDebug 只消费正式 RuntimeDebugSession、SourceMap、Trace、Playback 和提交快照；RootTree 进入子图时显示对应 FlowCanvas，进入 Timeline 时显示对应 Slate Timeline，离开后回到父调用路径。
- **统一工具表面**：复用现有 Graph Shell、FlowCanvas 和 Slate CutsceneEditor 的绘制与交互基础；不新建 Dashboard、第二曲线编辑器、第二 Timeline Renderer、事件中心或独立播放器。
- **固定唯一 UI 归属**：三种形态直接位于现有 `TimelineEditorWindow`，不得新增独立 Workbench、Session Dashboard 或预览设置面板；工具栏只保留 Profile、形态菜单、Session 菜单和一行状态。
- **固定唯一预览配置**：使用一个 `BtsmtlScenePlayProfile` ScriptableObject 保存 Scene、ContextId 和默认 ActorId。Timeline 只选择 Profile；详细配置只在 SO Inspector，不保存 Session、运行 identity、revision、播放时间或调试状态。
- **收紧 Session 操作入口**：Start、Pause、Resume、Stop，以及内容 Prepare、Publish、Adopt 都归入同一个 Session 菜单；不把流程按钮铺在 Timeline 顶栏上。
- **保留 CMC 的正确经验**：可以参考 CMC 的预热、手动刷新和局部缓存重建体验，但不得使用 CMC MontagePlayer 或其独立 PlayableGraph 作为 BTSMTL Preview 真相。
- **清理旧口径**：文档不再把 Preview 等同于用户必须显式点击 Unity Play，也不再把 RuntimeDebug overlay 当成完整 Runtime Preview；Unity Play Mode 是承载实现细节，产品入口由 Workbench 管理。

## Capabilities

### New Capabilities

- `btsmtl-authoring-runtime-workbench`：统一工作台、三种产品形态、唯一 ScenePlay Session、Preview 内容采用和 RuntimeDebug 调用栈观察。

### Modified Capabilities

- `btsmtl-timeline-editor-preview`：Timeline 作者面、Preview 运行面和 RuntimeDebug 只读面分离；Slate 作为作者和运行观察的共同视觉表面，但不拥有 Runtime。
- `graph-authoring-editor-shell`：Graph Shell 承载三种工作形态和共享 Session 状态，不为每个领域复制场景控制器。
- `btsmtl-runtime-diagnostics`：RuntimeDebug 通过统一 Session 提供当前调用栈、SourceMap、Capture/History 和只读视图。
- `gameplay-simulation-session-composition`：Preview 只能连接正式 Session composition，不能创建 Preview 专用 Kernel、Actor 或 Pipeline。
- 现有角色、Pose、Animation、Timeline、Document 和程序集所有权 delta：继续保留各领域边界，但统一消费 Workbench 的 Authoring/Preview/RuntimeDebug 语义。

## Impact

- 文档与规范：主 change 目录、能力名、相关 delta 引用统一改名；历史实施审计保留，但明确不等于当前产品形态已实现。
- 编辑器：现有 TimelineEditorWindow、GraphEditor、Slate projection 和 RuntimeDebugSession 成为工作台的接入表面；不新增平行作者模型或运行模型。
- 运行时：ScenePlay Session、正式 Simulation/Presentation/Timeline owner 继续是真相；Workbench 只管理入口、绑定、作者发布状态和只读视图。
- 作者修改：作者资产仍由唯一 Mutation、Validator、Undo、Export/Prepare/Adopt 链拥有。Preview 不能把运行时状态写回作者数据，也不能把未采用版本画成已生效。
- 进入成本：产品不要求用户手动操作 Unity Play 按钮；首次准备的等待、失败和采用阶段必须可见。是否由 Play Mode 或其它 Editor host 承载属于实现决策，不改变三种产品形态和唯一 Session 约束。
- CMC：仅作为交互体验参考，不进入 BTSMTL 正式运行链，不形成兼容播放器或 fallback 路径。
- 本 change 不新增测试任务，不把手动验收写入 tasks；实现接通与用户端到端验收分开记录。

## Product Boundary

```text
TimelineEditorWindow
├─ Authoring：原 Slate 作者面
├─ Preview：原 Slate 作者面 + ScenePlay
└─ RuntimeDebug：同一位置的只读运行观察

BtsmtlScenePlayProfile
└─ Scene / ContextId / DefaultActorId

唯一运行底座：ScenePlay Session
```

产品必须能让作者回答三件事：

- 我正在修改哪份正式作者数据？
- 当前 Preview Session 实际采用的是哪个版本？
- RuntimeDebug 当前真实执行到了哪个 Graph、Timeline、Track 或 Clip？

任何不能直接服务这三个问题的独立面板、播放器、状态副本或专用时间轴都不属于本 change。
