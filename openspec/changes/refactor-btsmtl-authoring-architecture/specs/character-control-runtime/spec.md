## Purpose

定义C#显式StateMachine／State／Transition的Gameplay控制职责，以及控制状态转换、技能请求和发布恢复的独立边界，使Locomotion与技能在同一模拟事务推进，不产生统管两者的角色总状态机或重复的技能执行状态。

## ADDED Requirements

### Requirement: 角色级控制必须由显式代码模块承担

Gameplay Locomotion控制 MUST使用C#显式StateMachine／State／Transition。StateMachine MUST协调当前控制State及转换；State MUST表达当前移动模式的Enter／Tick／Exit；Transition MUST声明稳定身份、来源、目标、纯条件、优先级和稳定评估顺序，并按既有同Tick语义执行选中转换。条件求值 MUST不消费输入、激活技能或执行退出效果。输入消费、技能候选选择和装备路由 MUST由同一控制代码组织，通过正式输入、角色／世界观察及合法当前决策事实产生类型化控制变化、动作请求和运动意图。控制 MUST不通过角色RootTree或Equipment Host图执行，也不得直接修改场景、播放动画或发送网络消息。

#### Scenario: 输入驱动攻击选择

- **WHEN** 玩家或AI提交已声明的攻击请求
- **THEN** 控制模块 MUST通过同一准入规则选择动作并记录代码来源
- **AND** 技能图 MUST不再承担另一份角色级状态选择

#### Scenario: 多个控制Transition同时成立

- **WHEN** 同一控制State的多个Transition条件在本Tick成立
- **THEN** StateMachine MUST按已声明优先级和稳定评估顺序选择转换，并明确来源与目标State
- **AND** 未选中条件 MUST不消费输入、启动技能或产生Exit效果


### Requirement: 控制状态转换与技能激活必须独立

控制State MAY在保持active时形成技能请求，实际激活 MUST经过唯一准入及Action事务。技能激活 MUST不强制控制Transition，控制State离开也 MUST不隐式结束技能；需要同时切换或停止时 MUST由同一正式决策阶段明确组织相应操作。技能的前摇、攻击窗口、后摇和局部状态 MUST只由ActionInstance内的执行状态维护。控制 MUST不新增统管Locomotion与技能的角色总状态机或复制这些事实的并行逻辑状态机。Presentation PoseStateMachine MUST继续只负责姿态选择与混合，不决定Gameplay控制或技能生命周期。

#### Scenario: 移动状态中激活攻击

- **WHEN** 当前移动状态允许攻击，技能准入通过且Gameplay移动模式未改变
- **THEN** MUST创建独立ActionInstance并保持当前控制State，不重复调用该State的Exit／Enter
- **AND** 技能与Locomotion的运动占用及位移贡献 MUST通过现有Motion规则统一处理

#### Scenario: 控制转换不要求结束技能

- **WHEN** 控制Transition成立且正式规则未请求结束当前技能
- **THEN** MUST执行控制状态转换并保留该ActionInstance的有效执行状态
- **AND** 技能后续推进 MUST不依赖已离开的控制State对象

#### Scenario: 技能进入后摇

- **WHEN** 技能从攻击窗口进入后摇且Gameplay移动模式未改变
- **THEN** 后摇流程 MUST由所属ActionInstance内的技能执行状态推进
- **AND** MUST不在控制层同步维护另一份Attack阶段或移动加攻击组合State


### Requirement: 控制模块状态必须参与唯一角色事务

所有影响后续模拟的控制状态 MUST由模块显式声明类型、默认值、状态版本与所有权，并进入角色的正式状态、codec、snapshot和hash。字段 MUST覆盖当前State identity、业务需要的进入Tick、确实跨Tick的转换进度及输入缓存；已有Body／Tag／Action事实 MUST直接读取，不保存镜像。模块、State和Transition对象 MUST不私藏计时器或选择游标作为第二业务真相。恢复 MUST只还原正式数据，不重新触发Enter／Exit、状态进入效果或技能请求；后续正常推进与重算 MUST使用同一控制实现。

#### Scenario: 恢复控制模式

- **WHEN** 恢复到角色处于移动模式切换过程中的Tick
- **THEN** 后续重算 MUST从已恢复控制状态继续
- **AND** MUST不读取恢复前代码对象保留的游标

#### Scenario: 恢复已激活技能的控制状态

- **WHEN** snapshot包含active控制State及已建立的ActionInstance
- **THEN** 恢复 MUST直接还原各自状态，不执行State的Enter／Exit
- **AND** MUST不因恢复再次消费输入或创建第二次技能释放


### Requirement: 动作选择必须复用唯一准入与释放生命周期

控制模块 MUST通过唯一动作准入、目标快照和生命周期入口建立或停止ActionInstance。替换动作 MUST显式停止旧实例，按既有graceful或force语义处理退出；停止未完成的实例不得继续正常输出。技能请求 MUST由正式控制决策阶段处理，不得递归启动第二个角色控制循环。

#### Scenario: 技能请求后续动作

- **WHEN** 技能产生声明的后续动作候选
- **THEN** 候选 MUST在下一次正式角色决策阶段接受或拒绝
- **AND** MUST不由Timeline或TreeClip直接创建另一套释放生命周期

#### Scenario: 持续普通移动

- **WHEN** 输入只要求普通移动且没有动作释放
- **THEN** 控制模块 MUST能产生正式运动意图
- **AND** MUST不为了取得身份创建空技能


### Requirement: 代码控制合同必须进入构建与Session身份

模块 MUST声明稳定身份、语义版本、参数／状态schema、输入合同、能力和可用技能／producer；控制State及Transition MUST具有可关联代码来源的稳定身份，其定义及顺序受模块版本约束。构建 MUST将其与角色技能闭包、Numeric Target及完整状态布局共同锁定；缺少实现、依赖、能力或版本不匹配 MUST在Active前失败。不同Target MUST复用相同业务规则，不得按网络模型选择另一套控制算法。

#### Scenario: 控制代码版本不匹配

- **WHEN** 客户端或authority安装的控制模块与角色运行包声明不同
- **THEN** Session MUST拒绝启动并指出模块身份
- **AND** MUST不选择旧模块或任意实现继续运行


### Requirement: 规则更新必须通过明确版本和正式加载边界

可更新规则 MUST通过正式发布清单、依赖加载和合同装配提供。共享执行核心 MUST只依赖规则合同，不得依赖可替换实现。Session Active后 MUST固定规则与技能版本；新版本在新的Session创建时采用，旧状态格式不得自动迁移。

#### Scenario: 运行中发布新规则

- **WHEN** 资源或代码发布了新的角色规则版本
- **THEN** 已有Session MUST保持其锁定版本或明确结束
- **AND** 新的Session MUST核对完整新组合，MUST不混用新规则与旧状态布局
