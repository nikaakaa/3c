# Design: 角色管线 Definition 配置边界

## 目标

把 `CharacterPipelineDefinition` 固定为角色管线的装配清单，而不是复合编辑器。作者打开 Definition 时应先看见“这个角色引用了哪些正式 Config”，只有进入具体 Config 才编辑对应业务数据。

目标链路：

```text
CharacterPipelineDefinition
  -> RootTreeAsset
  -> CharacterInputProfile
  -> CharacterGameplayEffectProfile
  -> ActionProfile[]
  -> GameplayBehaviorProfile[]
  -> CharacterAnimationPresentationProfile
  -> generated CharacterSimulationProgramAsset
  -> generated CharacterPresentationProjectionAsset

CharacterAnimationPresentationProfile
  -> Layer catalog
  -> Animancer TransitionLibraryAsset
  -> stable producer bindings

Compiler
  -> Definition references
  -> Semantic IR / Float32 Program
  -> Presentation Projection

Runtime
  -> Program + Projection
  -> 不读取 authoring Profile
```

## 数据所有权

### CharacterPipelineDefinition

Definition 只拥有装配关系和少量管线级标量：RootTree、SimulationTickRate、各 Config 引用和 generated artifact 引用。它不再内联保存 Animation Layer 或 producer binding。

Program/Projection 仍由 Definition 持有引用，因为 Host 必须从同一个装配根加载匹配产物；但它们属于 generated output，不属于默认 authoring 表单。

### CharacterAnimationPresentationProfile

Profile 是唯一动画表现 authoring 真相，保存：

- 有序 Layer catalog。
- 唯一 Animancer TransitionLibraryAsset 引用。
- `AnimationProducerId -> TransitionAssetBase + Easing` binding。

Profile 不保存 RootTree、Definition owner、Program、Projection、runtime lifecycle 或 StateMachine flow。这样不会形成反向依赖或第二份逻辑图。

### Projection

Compiler 从 Definition 引用的 Profile 读取表现配置，并把运行所需 Layer 与 producer binding 编入 `CharacterPresentationProjection`。Runtime 继续只读取 Program/Projection。Profile 修改必须改变 source revision 并触发正式重编译。

## Inspector 信息架构

### Definition Inspector

默认视图只显示：

1. Pipeline：RootTree、SimulationTickRate。
2. Config References：Input、GameplayEffect、Action、Behavior、Animation Presentation Profile。
3. Artifact Status：`Missing / Invalid / Ready` 与 Compile 命令。
4. Navigation：打开 RootTree、Profile、Agent Controller。

Program/Projection 对象引用与 identity 详情放在默认折叠的 Generated Artifacts 区域。Hash、capability 与 compiler report 只在显式展开或执行 diagnostics 后显示。Inspector Repaint 不运行 Compiler、不计算完整 source revision、不解码 Program，也不递归建立 producer 投影。

### Profile Inspector

Profile Inspector 是 Layer、TransitionLibrary 与 producer binding 的唯一写入口。它使用正式 Profile asset 作为 Undo/dirty owner。

Profile 本身不保存 Definition owner。Editor 通过 AssetDatabase 查找引用该 Profile 的 Definition：

- 只有一个引用者时，直接使用该 Definition 作为只读 producer context。
- 多个引用者时，作者必须在 Inspector 中显式选择 context。
- 没有引用者时，仍可编辑 Layer 与 TransitionLibrary；producer 来源投影与新增 binding 命令保持不可用，并显示明确配置错误。
- context 是 editor-only view state，不写入 Profile，不参与 source revision。

Profile Inspector 从选定 Definition 的已编译 Projection 读取稳定 producer identity、LayerId 和来源显示信息。它不重新编译 Graph，不读取 runtime state，也不保存 producer flow。Graph/Timeline 导航只解析来源，不建立第二写入口。

## 迁移

Corin 迁移顺序固定为：

1. 读取当前 Definition 内联 Layer、TransitionLibrary 与全部 producer binding。
2. 创建 `CorinAnimationPresentationProfile.asset` 并写入完全相同的数据。
3. 将 Corin Definition 改为 Profile 引用。
4. 修改 Compiler、Editor、Preview、Agent 与 Validator 调用链。
5. 重建 Program/Projection，确认 source revision 与 Profile dependency 一致。
6. 删除内联字段、旧类型名、旧 Definition Inspector presentation 区和旧 authoring service owner 逻辑。
7. 删除任何迁移辅助代码，不保留兼容读取。

当前内联 YAML 结构清晰且 Corin 是正式已知资产，可以直接进行一次性资产重写；不需要运行时 migrator 或 fallback。

## 决策与 Tradeoff

### 独立 Profile asset，而不是继续内联后折叠 UI

- 收益：Definition 真正成为装配清单；动画表现配置拥有明确文件、Inspector、Undo 与变更边界；以后新增角色时可以直接审查引用关系。
- 成本：每个角色多一个资产，切换编辑对象多一步。
- 选择原因：只折叠 UI 会保留错误的数据所有权，后续 Compiler、Agent 和 Inspector 仍会把 Definition 当作动画配置本体。

### 允许兼容 Definition 复用 Profile，而不是强制一对一 owner

- 收益：两个使用相同 Graph/producer identity 的角色可以共享表现配置，不需要复制资产。
- 成本：Profile Inspector 在多个引用者时需要显式 context；每个 consuming Definition 都必须独立通过 producer binding 校验。
- 选择原因：在 Profile 中保存反向 owner 会制造循环引用和第二装配根；强制一对一则无必要地禁止合法复用。

### 保留 generated artifact 引用，但从默认 authoring UI 分离

- 收益：Host 仍能从 Definition 严格装配 Program/Projection；作者不会把 generated output 当成手工 Config。
- 成本：Definition 数据模型并非字面上只有业务 Config 引用，仍含内部生成引用。
- 选择原因：彻底移除产物引用需要额外 registry/路径解析，会引入新的运行时装配路径，反而破坏当前唯一链路。

### 使用 Profile Inspector，而不是新增 Presentation EditorWindow

- 收益：沿用 Unity 资产编辑习惯，不增加第三个窗口，也不与 Graph/Timeline 双窗口工作流竞争。
- 成本：复杂 producer 列表的可视空间小于专用窗口。
- 选择原因：当前只有 Layer、Library 与少量 producer binding，不需要独立工作台；未来规模真实增长时再单独 proposal。

## 风险与控制

- 资产迁移遗漏 producer binding：迁移前后按 stable `AnimationProducerId` 集合与 transition GUID/fileID 对比，缺失即停止。
- Profile 共享但 producer topology 不兼容：Compiler 对每个 Definition/Profile/Projection 组合严格报告 orphan 或 missing binding，不按名称 fallback。
- Inspector 再次触发重编译卡顿：默认 Repaint 只读取轻量 metadata；完整 compiler diagnostics 和 source revision 必须由明确命令或正式资产变更调度触发。
- Agent 形成第二写入口：Snapshot 只读输出 Profile identity 与 binding；Patch schema 继续拒绝 Presentation 写操作。

