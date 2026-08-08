## RENAMED Requirements

- FROM: `### Requirement: 连续楼梯必须显式分离Gameplay Traversal与Foot Placement Surface`
- TO: `### Requirement: Ramp楼梯必须显式分离Gameplay Traversal与Foot Placement Surface`

## MODIFIED Requirements

### Requirement: Ramp楼梯必须显式分离Gameplay Traversal与Foot Placement Surface

只有明确选择连续Ramp作为Gameplay真相的楼梯梯段 MUST通过唯一`StairTraversalSurfaceAuthoring`声明一个Gameplay Traversal Ramp和一个Foot Placement Surface根。包含上行、顶平台和下行的Ramp往返路线 MUST把上行与下行拆成两个绑定，顶平台 MUST继续作为普通`Ground`；Gameplay Traversal Ramp MUST是对应绑定进入Deterministic Collision Artifact的唯一连续通行表面。该Ramp楼梯的真实踏面Collider MUST只供Foot Placement查询，不得进入该Artifact。同一Ramp绑定 MUST不保留逐级Gameplay Collider、Ramp失败后的Step fallback或按运行状态切换表面的模式。

普通`Ground`离散楼梯 MUST不注册`StairTraversalSurfaceAuthoring`，其合法性由显式Deterministic Surface所有权和普通Ground表面合同表达；系统 MUST不因为场景存在连续真实台阶而强迫其创建Ramp绑定。

#### Scenario: 角色跑上Ramp楼梯

- **WHEN** Fixed KCC沿注册`StairTraversalSurfaceAuthoring`的楼梯移动且Foot Placement查询当前与未来落面
- **THEN** Fixed KCC MUST只接触Gameplay Traversal Ramp
- **AND** Foot Placement MUST只从真实踏面与普通共享地面获得support

#### Scenario: 场景包含普通Ground离散楼梯

- **WHEN** 一组真实阶梯Collider由唯一Deterministic Surface作者拥有且没有`StairTraversalSurfaceAuthoring`
- **THEN** 该楼梯 MUST沿普通Ground规则进入Collision Artifact
- **AND** Ramp楼梯validator MUST不要求它创建Traversal Ramp

### Requirement: 楼梯表面角色必须通过Layer与作者所有权双重隔离

普通共享地面与离散楼梯阶梯代理 MUST使用`Ground`层，并 MAY同时被Deterministic Surface作者拥有和Foot Placement查询。Ramp楼梯的Traversal Ramp MUST使用`CharacterTraversal`层、被且只被一个`DeterministicCollisionSurfaceAuthoring`拥有，并 MUST被Foot Placement查询Mask排除。Ramp楼梯的真实踏面Collider MUST使用`FootPlacementSurface`层、被Foot Placement查询Mask包含，并 MUST不被任何`DeterministicCollisionSurfaceAuthoring`拥有。离散楼梯 MUST不复制`FootPlacementSurface`踏面，其同一组`Ground`阶梯Collider MUST同时承担Gameplay Bake与Foot Placement support。Layer MUST不替代Deterministic Surface作者所有权，作者所有权 MUST不替代Foot Placement LayerMask。

#### Scenario: Ramp楼梯真实踏面仍位于Deterministic Surface作者子树

- **WHEN** 楼梯validator发现Ramp楼梯Foot Surface Collider会被Collision Baker收集
- **THEN** validator MUST阻止Bake并报告Collider与冲突作者
- **AND** MUST不通过临时禁用Collider或Layer过滤继续生成Artifact

#### Scenario: Traversal Ramp被Foot Placement Mask包含

- **WHEN** Corin Foot Placement配置会查询`CharacterTraversal`
- **THEN** 楼梯配置校验 MUST失败并报告Profile与Ramp
- **AND** Foot Placement MUST不在Runtime按命中优先级选择真实踏面

#### Scenario: 离散楼梯使用Presentation专用踏面副本

- **WHEN** 同一离散楼梯同时存在进入Artifact的`Ground`阶梯代理和重叠`FootPlacementSurface`踏面Collider
- **THEN** 内容校验 MUST拒绝该重复表面
- **AND** Runtime MUST不按Layer或命中顺序选择其中一套

## ADDED Requirements

### Requirement: 离散楼梯必须作为普通Ground世界几何作者

离散楼梯 MUST使用持久化、启用、非Trigger且受Deterministic Collision Baker支持的阶梯形Collider代理。全部代理 MUST位于`Ground`层并被恰好一个`DeterministicCollisionSurfaceAuthoring`拥有；其可站立上表面 MUST与可见踏面的rise、run、宽度及首尾平台一致。同一组Collider MUST同时供Collision Artifact与Foot Placement Physics查询。离散楼梯 MUST不挂`StairTraversalSurfaceAuthoring`、不包含`CharacterTraversal` Ramp、不复制`FootPlacementSurface`踏面，也 MUST不由Builder、Bake、Inspector、OnValidate或Runtime生成。

#### Scenario: Fixed KCC连续通过真实台阶

- **WHEN** 离散楼梯Ground Collider经唯一Surface owner进入Collision Artifact
- **THEN** Fixed KCC MUST使用现有Step与Ground Probe语义处理该几何
- **AND** Foot Placement MUST从同一组Ground Collider获得Lyra current Sphere hit与预测路径support

#### Scenario: 离散楼梯误挂Ramp作者组件

- **WHEN** 离散楼梯根同时存在`StairTraversalSurfaceAuthoring`或`CharacterTraversal` Ramp
- **THEN** 内容校验或Bake MUST失败并报告冲突对象与表面角色
- **AND** 系统 MUST不自动选择真实台阶或Ramp

### Requirement: Gameplay Lab必须同时包含Ramp与连续离散楼梯内容

Gameplay Lab共享环境 MUST保留现有Low、High与OverLimit Ramp楼梯及其六个稳定Ramp绑定，并 MUST增加一段持久化`DiscreteStairs_Rise0.14_Run0.45`连续离散楼梯。离散楼梯 MUST与LowStairs使用相同单级rise、run和可行走宽度，但 MUST拥有独立Ground Collider、Deterministic Surface identity与非重叠通行路线。现有`StepCapabilityCourse` MUST继续独立表达0.14m、0.24m与0.40m的Step准入和拒绝边界，不得被连续离散楼梯替换或注册为Ramp楼梯。

#### Scenario: 同场景比较两种楼梯

- **WHEN** Local Fixed或Rollback组合加载唯一Gameplay Lab Collision Artifact
- **THEN** Artifact MUST同时包含LowStairs Traversal Ramp与DiscreteStairs真实Ground阶梯
- **AND** 两者 MUST由同一个Fixed KCC配置处理而不切换Motor或角色参数
