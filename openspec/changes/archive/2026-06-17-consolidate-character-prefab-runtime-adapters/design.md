## Context
当前 core 抽离后，正式 runtime state 已在 `CharacterRuntimeCore`、`LocomotionRuntimeModule` 和 `FullBodyActionRuntimeModule` 内，但 prefab 上仍有以下 runtime 相关 MonoBehaviour：

- `AnimancerComponent`
- `PresentationTransformInterpolator`
- `InputRequestBufferComponent`
- `UnityInputSystemRequestBufferAdapter`
- `CharacterAnimancerPresenter`
- `CharacterFrameRuntimeTickAdapter`
- `CharacterFrameRuntimeController`
- `UnityInputSystemLocomotionInputSource`
- `TransformFacingDirectionProvider`
- `CharacterMotionDriver`
- `PlayerLocomotionController`
- `FullBodyActionRuntime`

其中一部分是真正的 Unity-facing adapter，一部分是迁移期 facade。这个 change 的目标是提高 prefab 装配的 Locality：正式 gameplay 装配从“多个看起来都像 controller 的 Mono”收敛为“一个 runtime assembly adapter 组合多个窄 Unity adapters”。

## Goals
- 让正式 Corin prefab 的 runtime owner 视觉和实际架构一致。
- 让 `CharacterRuntimeCore` 是正式 runtime state 的唯一 owner。
- 让 `PlayerLocomotionController` 和 `FullBodyActionRuntime` 不再作为正式 prefab 组件表达 Locomotion/Action owner。
- 保留必要 Unity seam，不为了减少 Mono 数量而把 Unity 对象塞回 pure C# core。
- 用自动测试锁定 prefab allowlist 和唯一出口。

## Non-Goals
- 不删除 Animancer 或 motion driver 这类真实 Unity adapter。
- 不把 prefab 改成单 Mono 全部包办。
- 不改变现有 gameplay 行为。
- 不在本变更中处理 light attack combo、UpperBody、Aim 或网络同步。
- 不跑 Unity batchmode。

## Decisions
- Decision: 正式 prefab 允许多个 Unity-facing adapter，但只允许一个 gameplay runtime assembly adapter。
  - Rationale: 输入、Animancer、Transform、CharacterController 和 tick registration 都是不同 Unity seam；强行压成一个 Mono 会降低 Depth 和 Locality。
- Decision: `PlayerLocomotionController` 与 `FullBodyActionRuntime` 在正式 prefab 上退场。
  - Rationale: 它们现在已不应表达 state owner。继续挂在 prefab 上会让 Interface 暗示错误 ownership。
- Decision: 新 assembly adapter 只负责装配 dependencies 和调用 core，不重新实现 Locomotion 或 Action 逻辑。
  - Rationale: 保持 core/module 的 pure C# Implementation，不新增分裂路径。
- Decision: prefab 装配通过 allowlist 静态测试约束。
  - Rationale: prefab 是 Unity 序列化表面，最容易在后续手改中恢复旧组件，测试必须直接扫 YAML 或 SerializedObject。

## Proposed Final Runtime Adapter Roles
- `CharacterFrameRuntimeController` 或重命名后的等价 adapter：唯一 gameplay runtime assembly adapter，创建/绑定 `CharacterRuntimeCore`。
- `CharacterFrameRuntimeTickAdapter`：simulation tick phase registration adapter，只转发到同一个 runtime assembly adapter。
- `InputRequestBufferComponent`：输入请求缓冲 adapter。
- `UnityInputSystemRequestBufferAdapter`：Unity InputSystem 到请求缓冲的 adapter。
- `UnityInputSystemLocomotionInputSource`：移动输入 facts adapter。
- `CharacterMotionDriver`：唯一 motion executor adapter。
- `CharacterAnimancerPresenter` + `AnimancerComponent`：唯一 animation presenter / Animancer runtime adapter。
- `TransformFacingDirectionProvider`：facing/camera basis adapter。
- `PresentationTransformInterpolator`：表现插值 adapter。

`PlayerLocomotionController` 和 `FullBodyActionRuntime` 不再作为过渡代码类型保留；旧测试或旧调用必须迁移到 `CharacterFrameRuntimeController` 或 pure C# module seam。

## Risks / Trade-offs
- Risk: 直接从 prefab 删除 facade 可能暴露序列化引用缺口。
  - Mitigation: 先让 runtime assembly adapter 显式持有所需引用，再删 prefab 组件。
- Risk: 兼容 facade 代码还存在，后续开发者可能再次挂回 prefab。
  - Mitigation: 添加 prefab allowlist 测试和静态边界测试。
- Risk: 若一步重命名 `CharacterFrameRuntimeController`，可能造成 prefab GUID 或引用 churn。
  - Mitigation: 第一版优先保留类型名，只收敛职责和 prefab 组件；重命名另开 change。

## Migration Plan
1. 增加 prefab 装配 characterization tests，记录当前组件清单和预期退场组件。
2. 扩展或新增 runtime assembly adapter，让它直接绑定 core dependencies。
3. 迁移 `PlayerLocomotionController` / `FullBodyActionRuntime` 在 prefab 上承担的剩余 Unity adapter 职责。
4. 从两个 Corin prefab 移除迁移期 facade 组件。
5. 增加静态测试防止正式 scene 或 prefab 恢复旧 facade、debug tooling 或第二出口。
6. 运行 OpenSpec、编译和定向 EditMode 测试。

## Open Questions
- 第一版是否保留 `CharacterFrameRuntimeController` 名称作为 runtime assembly adapter，还是另开后续 change 做命名收敛。
