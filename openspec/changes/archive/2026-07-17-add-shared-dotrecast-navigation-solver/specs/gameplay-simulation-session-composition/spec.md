# gameplay-simulation-session-composition Specification

## MODIFIED Requirements

### Requirement: Actor Registration 必须在 Active 前形成不可变 roster

Character Actor Host MUST提供带显式ActorId、Program artifact、Projection、抽象Float32 World body binding、可选local input、Presentation/output port与diagnostics metadata的不可变registration。通用registration与Character Host MUST不暴露或要求`UnityCharacterControllerWorldBodyBinding`具体类型。每个具体WorldSolver Definition MUST在Active前校验binding实现与自己匹配；Unity CharacterController Solver MUST只接受CC binding，DotRecast Solver MUST只接受state-only DotRecast binding。Session preparation MUST在Active前校验ActorId唯一性、Program/Projection identity、ProgramCatalog binding、当前Pipeline/Source/Solver所需端口与initial state；Active后 MUST不增删Actor、不换Program、修改binding或切换Solver。

#### Scenario: DotRecast Composition注册Actor

- **WHEN** Composition收到显式state-only DotRecast binding
- **THEN** 同一Character Host MUST建立正式Actor registration
- **AND** registration MUST不要求CharacterController或第二Character Host

#### Scenario: Binding类型错误

- **WHEN** DotRecast Solver Definition收到CC binding
- **THEN** Composition MUST在创建World前失败
- **AND** MUST不搜索替代binding或切换Solver

## ADDED Requirements

### Requirement: Unity WorldBodyBinding必须只有抽象合同与显式实现

Unity Float32 composition层 MUST提供唯一抽象WorldBodyBinding合同，包含BindingId、ActorId、InitialBody和严格校验。CC binding与DotRecast state-only binding MUST作为独立实现。抽象合同 MUST不包含CharacterController、Rigidbody、Transform写入或DotRecast类型；其它Composer、Source、Pipeline、Character Host和Presentation MUST只依赖抽象合同。

#### Scenario: 保留CC环境

- **WHEN** Composition选择Unity CharacterController Solver
- **THEN** CC binding MUST仍由该Solver adapter使用
- **AND** DotRecast binding MUST不获得CC组件
