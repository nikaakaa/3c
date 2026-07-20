# character-motion-simulation-boundary Specification

## ADDED Requirements

### Requirement: WorldCapability与WorldFeature必须表达不同层级

WorldCapability MUST表达Program/Pipeline依赖的通用结果合同，WorldFeature MUST表达Solver具体世界机制。BodyMotion、Grounding、Collision与Reconstructible MUST保持跨Solver；NavigationSurface、Ground、Slope、Step、WallSlide、DynamicObstacle与ActorCollision MUST作为feature。Composer MUST分别校验两者。

#### Scenario: Composition要求NavigationSurface

- **WHEN** Program capability满足但Solver没有NavigationSurface feature
- **THEN** Composition MUST失败
- **AND** MUST不把通用Collision当作NavigationSurface

### Requirement: 同一WorldSolver实现必须可服务不同Session Source

WorldSolver MUST只消费portable WorldState与CharacterMotionRequest并返回portable batch result，MUST不读取Session Source、Network Model、packet、ack、history或Presentation。同一Solver实现 MAY装配到Local、Prediction或Authority Session，但每个Session MUST拥有独立runtime实例与WorldState。

#### Scenario: 两个Session使用DotRecast

- **WHEN** 两个不同Source的Float32 Session选择相同DotRecast Solver Definition
- **THEN** 两者 MUST执行同一Solver语义
- **AND** MUST不共享mutable query或WorldState
