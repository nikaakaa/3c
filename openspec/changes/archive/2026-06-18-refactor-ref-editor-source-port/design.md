## Context
`Ref/wly970123/taco-editor` 的 Timeline 与 TreeDesigner 编辑器把复杂交互拆成稳定组件：field 管时间轴坐标和 selection，clip 只委托 move / resize，drag manipulator 管 pointer capture，GraphView shell 管节点、端口、连线和 SearchWindow。当前项目已经迁移了部分资源和名称，但仍有大量自研交互逻辑，例如 root pointer mode 推断、局部 delta frame 计算、半移植 inspector 和独立静态 helper。这些实现容易和 Ref 的布局、鼠标命中、selection、resize cursor、move leader 行为产生偏差。

当前架构约束仍然不变：Ref 只能作为源码来源，不作为运行时依赖；正式数据权威是项目自己的 `CharacterActionDefinitionSO`、Committed Action branch authoring、TimelineNode authoring、seconds authoring 和 fixed tick compile；节点树与时间轴是两个窗口，节点树只选择/打开 TimelineNode，时间轴独立编辑该 node 的 timeline 数据。

## Goals
- 用 Ref/Taco editor 源码级结构替换当前半移植 editor 交互。
- 保留当前项目正式 adapter、serializer、compiler、validator 和 runtime evaluator。
- 删除当前重复、临时、半自研的 editor path，不保留 fallback。
- 让 clip 选择、拖拽、伸缩、locator、pan、zoom、rectangle selection、节点 SearchWindow、固定 root 和 node panel 行为可由自动测试验证。

## Non-Goals
- 不引入 Taco runtime asmdef、namespace 依赖、sample asset、`TimelinePlayer`、`PlayableGraph` runner、`BaseTree` 或 `RunnableTree` 作为正式 gameplay。
- 不把 timeline 嵌回 Character Behavior Editor 节点窗口。
- 不把 Ref frame authoring 作为正式保存字段；UI frame / tick 只作为 seconds authoring 的视图和量化辅助。
- 不修改 `CharacterFramePipeline`、motion executor、Animancer presenter、rollback 或 action lifecycle 权威。

## Decisions
- Decision: 采用源码级移植，而不是继续在现有类内补 bug。
  - Implementation MUST 为 Ref 的关键 editor 类提供项目命名的等价类或等价职责分层，例如 field view、track view、clip view、track handle、selection、drag manipulator、drag line manipulator、graph node view、search window 和 node panel。
  - 当前 `CommittedActionRefPortedTimelineView` 这类混合类 MAY 被拆分或替换，但最终 public editor path MUST 只指向新移植 shell。

- Decision: Field / Graph 持有交互权威，adapter 持有数据写回。
  - Timeline field MUST 管 frame position map、scale、offset、selection、move leader、apply move、resize clamp、invalid preview 和 locator。
  - Clip view MUST 只负责显示、选择和委托 drag manipulator，不直接维护第二套 timeline 数据。
  - GraphView MUST 通过 stable id 与 adapter 写回 node、edge、layout 和 panel payload。

- Decision: 以项目正式数据替换 Ref data model。
  - Ref `Timeline`、`Track`、`Clip`、`BaseTree`、`RunnableNode` 的状态访问 MUST 映射到项目 editor snapshot / serialized adapter。
  - 写回必须进入 `CharacterActionDefinitionSO` 内正式 branch / TimelineNode authoring 数据。
  - seconds authoring -> fixed tick compile -> runtime tick sampling 必须保持。

- Decision: 移植后删除旧路径。
  - 旧 card/list branch editor、手写 root pointer mode、临时 debug log、重复菜单入口、Dodge-only branch authoring 正式入口和半移植资源 helper MUST 删除或替换。
  - 若某个 Ref 能力因项目 runtime 尚无语义无法正式支持，例如 ease-in / ease-out 混合，UI MUST 隐藏或作为明确禁用状态，不得展示假编辑入口。

## Risks / Trade-offs
- Risk: 直接移植 Ref editor 代码量较大。
  - Mitigation: 先移植 Editor-only shell 和 manipulator，再逐步接 adapter；每一步用静态测试证明没有 Ref runtime 越界。
- Risk: Ref frame model 与项目 seconds authoring 不一致。
  - Mitigation: UI 内部可使用 frame / tick position map，但所有保存字段仍是 seconds，compiler 负责 fixed tick 量化。
- Risk: Graph 和 Timeline 两个窗口的入口关系混乱。
  - Mitigation: Character Behavior Editor 只显示节点树和 TimelineNode 打开入口，Timeline Editor 是唯一 field/track/clip 编辑面。

## Migration Plan
1. 盘点 Ref editor 类、资源和当前项目半移植类，建立一一对应迁移清单。
2. 导入或重写为项目命名的 Ref-equivalent editor-only 类和 UXML / USS 资源。
3. 用项目 adapter 替换 Ref runtime data model。
4. 替换窗口入口，使旧半移植 editor shell 不再可达。
5. 删除旧重复路径和临时 helper。
6. 跑 OpenSpec、EditMode 定向测试和静态边界测试。

## Open Questions
- Ref 的 ease / mix UI 是否在本阶段完全隐藏，还是作为 disabled visual state 保留，需要在实现前按当前 action timeline runtime 支持度最终确认。
