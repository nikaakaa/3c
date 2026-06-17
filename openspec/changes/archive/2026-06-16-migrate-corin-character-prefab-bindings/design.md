## Context
只读检查显示两个角色 prefab 的 `PlayerLocomotionController` 和 `PlayerFullBodyActionController` 都能读到 `CorinCharacterConfig.asset`，但 prefab YAML 仍保留旧平铺字段。`Sandbox.unity` 和 CameraTest 场景也存在直接组件或旧配置引用。

## Goals
- 让 Corin 正式角色 prefab 只通过角色配置根和正式 runtime 组件装配。
- 清理 scene override 中的旧配置入口。
- 保持唯一 Character frame pipeline、唯一 runner、唯一 motion executor 和正式 animation presenter 路径。

## Non-Goals
- 不修改 Corin 配置资产内部引用。
- 不设计新配置格式。
- 不合并 Animancer Presenter；该职责由 `refactor-unified-animancer-presenter` 拥有。
- 不运行 Unity batchmode。

## Decisions
- Prefab 迁移必须优先通过 Unity 序列化系统完成，并用 YAML 静态测试验证结果。
- 清理旧字段只发生在对应代码已不再声明或已明确标记为 legacy 的字段上。
- Scene override 必须和 prefab 一起验证，避免 prefab 正确但场景实例仍覆盖旧引用。

## Binding Model
```text
可琳.prefab / 可琳_Humanoid.prefab
  -> PlayerLocomotionController
      -> CharacterConfigSO
      -> input source
      -> motion executor
      -> facing provider
      -> locomotion presenter or approved unified presenter
  -> PlayerFullBodyActionController
      -> CharacterConfigSO
      -> InputRequestBufferComponent
      -> PlayerLocomotionController
      -> action presenter or approved unified presenter
  -> CharacterFramePipelineHost
      -> created by runtime host, not serialized as second component
```

## Editing Protocol
- 优先使用 Unity SerializedObject、PrefabUtility 或等价 Unity 序列化 API 修改 prefab/scene。
- 写文件仍通过系统工具约束执行，不使用 MCP 写 C# 脚本。
- 每次修改 prefab/scene 后必须通过 Unity 只读检查确认组件引用可解析。
- YAML 静态测试只作为验证，不作为唯一编辑手段。

## Validation Matrix
| Check | Evidence |
| --- | --- |
| Prefab root config | SerializedObject test reads both controllers and compares root asset path. |
| Legacy field cleanup | YAML/static test rejects retired config fields as formal values. |
| Runtime chain intact | SerializedObject test confirms input buffer, locomotion controller, motion executor and presenter references. |
| Scene override cleanup | Scene YAML/static test confirms no old config override remains. |
| Pipeline uniqueness | Static test confirms no serialized second pipeline/runner/executor path is introduced. |

## Risks / Mitigations
- 风险：直接文本编辑 prefab 破坏 Unity 序列化。
  - 缓解：实施时优先用 Unity Editor/SerializedObject 或明确可验证的序列化流程，写文件仍遵守系统工具要求。
- 风险：统一 Animancer Presenter 尚未实施，过早删除旧 Presenter 会破坏播放。
  - 缓解：本变更只清理配置绑定；Presenter 组件结构变更由 `refactor-unified-animancer-presenter` 或其后续任务负责。
- 风险：`PlayerLocomotionController` impact 为 HIGH。
  - 缓解：实施前必须报告 blast radius，并运行 rollback/synctest 相关验证。
- 风险：Prefab 正确但 scene override 继续覆盖旧引用。
  - 缓解：scene override 与 prefab 绑定同批验证，不把场景留到人工检查。

## Validation
- 运行 prefab/scene binding 静态测试。
- 运行相关 EditMode 行为测试。
- 运行 C# build。
- 运行 `openspec validate migrate-corin-character-prefab-bindings --strict --no-interactive`。
