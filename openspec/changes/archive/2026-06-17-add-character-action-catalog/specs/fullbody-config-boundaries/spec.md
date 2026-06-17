## MODIFIED Requirements

### Requirement: 配置资产目录表达职责归属
系统 MUST 将状态机配置、动作逻辑配置和动画表现配置放在可区分的目录中。目录结构 MUST 帮助设计者判断一个资产是否参与状态拓扑、动作逻辑或动画表现。Action Catalog 和动作定义 MUST 位于 Action 领域目录，FullBody MUST NOT 作为新 Action 逻辑配置的正式目录根。

#### Scenario: 状态机目录不混入动画资产
- **WHEN** 检查 `Assets/Configs/3C/StateMachine/`
- **THEN** 该目录 MAY 包含 Locomotion 局部状态图或批准的状态机拓扑资产
- **AND** MUST NOT 包含 `ActionAnimationProfileSO` 资产
- **AND** MUST NOT 包含基础移动动画配置资产
- **AND** MUST NOT 作为 Action Catalog 的正式目录

#### Scenario: 动作目录承载动作逻辑配置
- **WHEN** 检查 `Assets/Configs/3C/Action/Corin/`
- **THEN** 该目录 MUST 能定位 Character Action Catalog 或等价动作逻辑入口
- **AND** `Actions/Dodge/` 子目录 MUST 能定位 Dodge action definition 或等价 Dodge 逻辑配置
- **AND** `InterruptPolicy/` 子目录 MUST 能定位动作中断策略
- **AND** `BodyClaim/` 子目录 MUST 能定位 `BodyClaimPolicySO`
- **AND** 该目录 MUST NOT 要求保存角色具体 AnimationClip

#### Scenario: 动画目录承载角色动画绑定
- **WHEN** 检查 `Assets/Configs/3C/Animation`
- **THEN** Action 动作动画绑定和动作动画 Profile MUST 归属该目录或其角色子目录
- **AND** Locomotion 动画 alias、exit policy 和 motion profile 配置 MUST 归属该目录或其角色子目录
- **AND** 这些动画配置 MUST NOT 定义状态树拓扑、Action Catalog 或动作进入条件

#### Scenario: 旧 FullBody Action 目录不作为正式入口
- **WHEN** 检查默认 Corin 动作配置闭环
- **THEN** `Assets/Configs/3C/Action/FullBody/` MUST NOT 作为正式 Action Catalog、Dodge definition、request policy 或 body claim 的解析来源
- **AND** 若旧目录短期存在，里面的资产 MUST 只能作为迁移残留，并 MUST 被静态校验报告
