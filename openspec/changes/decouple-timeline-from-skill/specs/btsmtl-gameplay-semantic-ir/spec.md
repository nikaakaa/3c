## MODIFIED Requirements

### Requirement: Character Authoring 必须先编译为 Numeric-Neutral Semantic IR

Character 产品的 Compiler Frontend MUST以 CharacterPipelineDefinition 为唯一组合根，将正式 C# 控制合同、技能内容以及可达局部图、Timeline、TreeClip、Blackboard、Action、GameplayEffect 和 Motion 内容编译为不可变 Gameplay Semantic IR。Frontend MUST先完成稳定发现，再通过唯一语义发射生成经 canonical encode/decode、header 和 SemanticHash 校验的 artifact。Timeline 内容发射 MUST与 Character 组合和调用点来源分离，供明确的独立 Timeline 根复用。IR MUST表达稳定来源、operation、控制流、状态声明、数值字面量、producer 和能力要求，MUST不保存 Unity object、目标数值 runtime value、Network Model 或可变运行状态。Numeric Target MUST只消费 validated artifact，不重新遍历作者数据。

#### Scenario: 编译 Corin Semantic IR Artifact

- **WHEN** Frontend 构建 Corin CharacterPipelineDefinition
- **THEN** MUST生成与 Numeric Target 无关且可 canonical 读取的角色语义产物
- **AND** Float32 与 Fixed Target MUST消费相同语义，不能重写片段业务规则

#### Scenario: Frontend Discovery 失败

- **WHEN** 可达内容存在重复 identity、递归引用、缺失 owner 或缺失 emitter
- **THEN** 发布 MUST失败并报告精确来源
- **AND** MUST不忽略无效内容、读取旧缓存代替或继续 Target 构建

## ADDED Requirements

### Requirement: 独立 Timeline 语义根必须复用相同内容发射

系统 MUST允许精确 shared Timeline 作为独立内容语义根，包含片段、TreeClip/子图、外部输入签名与能力闭包。其正式 artifact MUST具有明确根类型、版本与来源，使用相同语义编码、校验和数值降低基础；MUST不构造假 Character Definition、Skill、Graph 节点或另一套 Timeline emitter。Graph/C# 调用点的实际绑定与来源 MUST在组合时单独链接。

#### Scenario: 同一 shared Timeline 经两种入口构建

- **WHEN** Character 组合和独立 Timeline 根引用相同内容
- **THEN** 内容操作、参数和局部状态声明 MUST来自同一领域发射规则
- **AND** 两种调用来源 MUST分别可追溯，不得伪造相同 Graph 路径
