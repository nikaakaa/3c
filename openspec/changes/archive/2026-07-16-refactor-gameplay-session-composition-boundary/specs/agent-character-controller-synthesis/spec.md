## MODIFIED Requirements

### Requirement: Agent 生成链路必须是 editor-only authoring 编译链路

系统 MUST将 Agent生成角色动作控制器实现为 editor-only authoring编译链路。Agent JSON、Intent、Macro和 Patch IR MUST只服务编辑期生成、修复和评估。运行时 MUST只执行由正式 BTSMTL asset编译得到的 `CharacterSimulationProgram`，并由 Session Pipeline的 Program Evaluate/Finalize Pass推进 Action operation、Timeline operation与 GameplayFacts。系统 MUST NOT在 Gameplay Runtime、Pipeline Pass、服务端或网络同步路径中执行 Agent JSON或调用 LLM。

#### Scenario: 运行时加载角色

- **WHEN** CharacterPipelineHost向 Session Host注册角色
- **THEN** runtime MUST只读取已发布 ProgramAsset、Projection和 Session composition
- **AND** MUST不读取 Agent Intent、Patch IR、LLM输出文件或运行时 authoring Graph

