## MODIFIED Requirements

### Requirement: TEngine 启动流程不得替代 gameplay tick 权威

项目 MUST 保持 TEngine 作为启动、资源、热更和 frame source 底座。Gameplay tick 权威 MUST 位于 `GameplayTickSystem`。TEngine Procedure、TEngine FSM、TEngine TimerModule 和 TEngine UpdateDriver MUST NOT 直接 tick BTSMTL gameplay graph、单个 `CharacterPipeline`、ActionRuntime、MotionStage、网络 peer 或 Timeline runtime。

#### Scenario: 进入角色 runtime

- **WHEN** `ProcedureLoadAssembly` 完成
- **THEN** 系统进入 `GameApp.Entrance()`
- **AND** 项目 runtime MUST 初始化或取得正式 `GameplayTickSystem`
- **AND** TEngine frame source MUST 只驱动 `GameplayTickSystem`
- **AND** 角色本地逻辑 tick 和表现帧 MUST 由 `GameplayTickSystem` 调度

#### Scenario: TimerModule 不作为角色 tick

- **WHEN** 项目需要推进角色 gameplay
- **THEN** 系统 MUST NOT 使用 TEngine `TimerModule` callback 作为角色 logic tick
- **AND** 系统 MUST NOT 通过多个 timer 为不同角色分别 tick pipeline
