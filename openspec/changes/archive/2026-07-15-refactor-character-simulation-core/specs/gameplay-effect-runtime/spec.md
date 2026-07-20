# gameplay-effect-runtime Specification

## MODIFIED Requirements

### Requirement: Gameplay Effect Runtime 必须形成独立编译模块

通用 Gameplay Effect contracts、catalog compiler、operation evaluator 与 state layout/transaction logic MUST位于独立 ThirdPersonGameplay portable assembly。该程序集 MUST不引用 Character、BTSMTL authoring object、Networking、Presentation、Diagnostics 或 UnityEngine。Character Compiler MAY将其 catalog编入 CharacterSimulationProgram，Kernel MAY通过正式 operation contract执行；模块 MUST不创建独立 Tick、Manager 或隐藏 mutable runtime object。

#### Scenario: Character 编译 GE

- **WHEN** Character Compiler引用通用 Effect compiler contracts
- **THEN** MUST生成 portable catalog/state layout
- **AND** Gameplay assembly MUST不引用 CharacterPipeline或 GraphContext

#### Scenario: 普通 DotNet 执行 GE

- **WHEN** portable Kernel执行 GE operation
- **THEN** MUST只读写 CharacterSimulationState提供的 GE slots
