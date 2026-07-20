# Change: 重构角色管线 Definition 配置边界

## Why

`CharacterPipelineDefinition` 的业务定位是角色管线装配根，但当前同时内联保存 Animation Layer、TransitionLibrary 与 producer binding，并在同一个 Inspector 平铺编译产物、Hash、capability、producer 投影和导航操作。作者只是检查角色使用了哪些 Config 时，也必须面对动画表现编辑器和编译诊断内容；Definition 因而既不是纯装配清单，也没有清楚区分 authoring 输入与 generated artifact。

现行 `character-animation-presentation-authoring`、`character-animation-layer-runtime`、`character-state-timeline-authoring-loop`、`agent-character-controller-synthesis`、`btsmtl-timeline-editor-preview` 和 `btsmtl-tree-inspector-information-architecture` 明确要求 Animation Presentation 内联在 Definition 且由 Definition Inspector 编辑。这些要求与新的纯引用边界直接矛盾，必须一起修改，不能只隐藏 UI。

## What Changes

- 新增角色级 `CharacterAnimationPresentationProfile` ScriptableObject，唯一保存 Animation Layer catalog、Animancer TransitionLibrary 引用和稳定 producer-to-transition bindings。
- `CharacterPipelineDefinition` 删除内联 `CharacterAnimationPresentationDefinition`，改为只引用唯一 `CharacterAnimationPresentationProfile`；Compiler、Preview 和 Agent Snapshot 只能沿该引用读取。
- `CharacterPipelineDefinition` Inspector 收敛为配置装配清单：RootTree、Input、GameplayEffect、Action、Behavior 和 Animation Presentation Profile 引用，以及紧凑的编译状态与明确命令。
- Program 与 PresentationProjection 继续作为 Definition 的正式生成产物引用，但默认不平铺 Hash、capability、compile report 或 producer binding；重型诊断只在作者显式请求时执行。
- `refactor-character-simulation-core` 已加入 Definition Inspector 的 Program/Projection 诊断能力继续保留，但迁入默认折叠的 Generated Artifacts/Diagnostics 区域，不再成为选中 Definition 时的常驻表单与重计算入口。
- 新增 `CharacterAnimationPresentationProfile` Custom Inspector，作为 Layer、TransitionLibrary 和 producer binding 的唯一写入口；不恢复独立 Animation Presentation EditorWindow。
- Profile Inspector 通过 editor-only Definition context 显示 producer 来源和 Graph/Timeline 导航。一个 Profile 被多个 Definition 引用时必须显式选择 context，不保存反向 owner 引用，也不按名称猜测。
- Profile 修改继续进入正式 source revision 与 Program/Projection build；Runtime 仍只加载匹配 ProgramHash/source revision 的 Projection，不读取 Profile。
- 一次性迁移 Corin 当前内联 Layer、TransitionLibrary 和 11 个 producer bindings 到正式 Profile asset，并更新 Definition 引用。
- 删除旧内联类型、字段、Definition Inspector presentation 编辑区及旧 Undo/dirty owner 路径，不保留 `FormerlySerializedAs`、lazy migration、兼容 reader、双写或一次性 migrator。

## Capabilities

### New Capabilities

- `character-pipeline-definition-authoring`：定义 CharacterPipelineDefinition 的纯装配边界、紧凑 Inspector 与 Animation Presentation Profile 唯一资产边界。

### Modified Capabilities

- `character-animation-presentation-authoring`：把 Presentation 数据所有权和唯一写入口从 Definition 内联对象迁到 Profile asset。
- `character-animation-layer-runtime`：把 Layer 与 binding 的 authoring 来源改为 Definition 引用的 Profile，运行时仍只读取 Projection。
- `btsmtl-timeline-editor-preview`：预览目标沿 Definition/Profile/Projection 正式链路取得动画表现配置。
- `character-state-timeline-authoring-loop`：Corin Base layer 与 producer binding 改由 Profile 配置。
- `agent-character-controller-synthesis`：Agent Snapshot 只读 Profile，人工微调入口迁到 Profile Inspector。
- `btsmtl-tree-inspector-information-architecture`：Tree Inspector 的动画配置导航目标迁到 Profile Inspector。

## Impact

- Affected code:
  - `CharacterPipelineDefinition` 与其 Custom Inspector
  - `CharacterAnimationPresentationDefinition`、authoring service 与 Projection builder
  - Character Simulation Compiler、BuildService 与 source revision
  - Timeline Preview target、Agent Snapshot/Validator
  - Corin Definition、生成 Program/Projection 与新增 Profile asset
- Breaking changes:
  - 删除 `CharacterPipelineDefinition.AnimationPresentation` 内联对象
  - 所有调用者改为 `AnimationPresentationProfile`
  - Animation Presentation 的 Undo/dirty owner 从 Definition 变为 Profile asset

## Out of Scope

- 不改变 Program operation、动画 selection、Timeline sampling、AnimationPlaybackLifecycle 或 Animancer playback 语义。
- 不新增独立 Presentation EditorWindow，不把 Layer/binding 写回 Graph、StateMachine 或 Timeline。
- 不改变 ActionProfile、GameplayBehaviorProfile、InputProfile 或 GameplayEffectProfile 的业务数据模型。
- 不新增测试；使用静态编译、资产链路检查和 OpenSpec strict validation 收口。
