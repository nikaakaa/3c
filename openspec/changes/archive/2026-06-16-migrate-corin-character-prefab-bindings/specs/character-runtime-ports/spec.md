## ADDED Requirements
### Requirement: Prefab 绑定保持唯一角色帧管线
系统 MUST 确保 Corin prefab 和正式场景装配后仍只有一个角色级 `CharacterFramePipelineHost` 推进正式帧管线。Prefab 迁移 MUST NOT 通过新增 MonoBehaviour、runner、motion executor 或 presenter 绕过当前角色帧管线。

#### Scenario: Prefab 不新增第二管线
- **WHEN** 自动校验 Corin prefab 组件绑定
- **THEN** 生产路径 MUST 仍通过 `PlayerFullBodyActionController -> CharacterFramePipelineHost -> CharacterFramePipeline`
- **AND** prefab MUST NOT 挂载新的正式 pipeline runner
- **AND** FullBody、Locomotion、Action MUST 仍只是 request 或 frame output 的提交者或 runtime adapter

#### Scenario: Scene 不覆盖出分裂路径
- **WHEN** 自动校验正式场景中的 Corin 实例
- **THEN** scene override MUST NOT 启用独立 Locomotion tick driver 作为正式 FullBody 并行路径
- **AND** scene override MUST NOT 新增第二个 action motion executor 或第二个 animation presenter 作为正式出口

#### Scenario: Runtime 引用迁移不改变管线持有关系
- **WHEN** prefab 迁移完成后检查生产代码和序列化组件
- **THEN** `CharacterFramePipeline` MUST 仍只由 `CharacterFramePipelineHost` 持有
- **AND** prefab MUST NOT 序列化或挂载新的 pipeline owner 组件
- **AND** FullBody tick adapter MUST 仍复用 controller 的同一个 host
