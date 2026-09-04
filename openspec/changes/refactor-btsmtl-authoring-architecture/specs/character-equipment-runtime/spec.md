## RENAMED Requirements

- FROM: `### Requirement: Equipment Host 必须只调度预编译入口`
- TO: `### Requirement: Equipment路由必须由代码控制选择已发布技能`

## MODIFIED Requirements

### Requirement: Equipment Runtime 必须属于唯一Character Program和State

Equipment catalog、代码route binding、Skill entry、parameter constant与initial Loadout MUST编译进唯一Target Character Program；每个Actor的当前Slot、Equipment、Feature、revision、pending change、host generation、contribution handle与Feature local state MUST保存在唯一`CharacterSimulationState` typed aggregate。Runtime MUST不持有Unity Feature asset、Graph clone、MonoBehaviour module state或第二份equipment cache作为业务真相。

#### Scenario: 创建Corin Session

- **WHEN** Session从Corin Program创建Actor state
- **THEN** Equipment aggregate MUST按Program initial Loadout初始化
- **AND** Host MUST不读取Equipment Profile资产补齐状态

#### Scenario: Presentation查询装备

- **WHEN** Presentation需要显示当前MainWeapon
- **THEN** MUST消费committed Equipment projection
- **AND** MUST不把Renderer activeSelf作为gameplay真相


### Requirement: Equipment路由必须由代码控制选择已发布技能

装备角色级Host调度 MUST由代码控制模块按committed Slot／Feature／revision及锁定binding选择技能和持续规则。不得tick Feature Graph、克隆作者对象、运行旧Host opcode或建立第二解释器。未知binding、Skill entry或过期generation必须正式失败。

#### Scenario: 选择装备PrimaryAction

- **WHEN** 控制模块读取已装备Feature的Route
- **THEN** MUST通过预构建绑定找到精确Skill entry
- **AND** ActionInstance仍按唯一准入建立

#### Scenario: 装备binding无效

- **WHEN** Route引用未安装代码模块或缺失技能
- **THEN** 构建或Active前校验 MUST失败，不得选择其他Feature

#### Scenario: PrimaryAction路由到Sawblade

- **WHEN** MainWeapon committed Feature为Sawblade且PrimaryAction请求到达
- **THEN** 代码控制 MUST解析已发布binding并选择精确技能
- **AND** 后续技能局部StateMachine／Timeline MUST由原解释器在唯一事务执行

#### Scenario: 装备更换后旧generation恢复运行

- **WHEN** outgoing Feature generation已被Equipment commit替换
- **THEN** 旧generation继续运行的请求 MUST被拒绝
- **AND** 旧实例的停止／清理按唯一动作生命周期完成，不重新激活旧技能


### Requirement: Feature local state必须按generation初始化和重置

Feature控制状态 MUST由代码模块schema声明并由Program State Layout安装，并只通过当前Character transaction读写。装备commit进入新generation时 MUST从Program canonical default初始化incoming state，outgoing state MUST被重置或释放；第一版 MUST不保存未装备物品实例状态。Restore/replay MUST按snapshot中的generation恢复相同状态。

#### Scenario: 重新装备同一把武器

- **WHEN** Sawblade卸下后再次装备
- **THEN** 其Feature local state MUST从定义默认值初始化
- **AND** MUST不恢复上一次未声明持久化的计数

#### Scenario: State恢复到换装前

- **WHEN** Target snapshot恢复到Equipment commit之前
- **THEN** Slot revision、host generation与Feature local state MUST一起恢复
- **AND** MUST不存在新旧generation混合状态
