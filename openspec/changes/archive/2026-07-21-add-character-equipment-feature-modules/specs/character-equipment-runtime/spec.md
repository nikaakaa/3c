## ADDED Requirements

### Requirement: Equipment Runtime 必须属于唯一Character Program和State

Equipment catalog、route entry、parameter constant与initial Loadout MUST编译进唯一Target Character Program；每个Actor的当前Slot、Equipment、Feature、revision、pending change、host generation、contribution handle与Feature local state MUST保存在唯一`CharacterSimulationState` typed aggregate。Runtime MUST不持有Unity Feature asset、Graph clone、MonoBehaviour module state或第二份equipment cache作为业务真相。

#### Scenario: 创建Corin Session

- **WHEN** Session从Corin Program创建Actor state
- **THEN** Equipment aggregate MUST按Program initial Loadout初始化
- **AND** Host MUST不读取Equipment Profile资产补齐状态

#### Scenario: Presentation查询装备

- **WHEN** Presentation需要显示当前MainWeapon
- **THEN** MUST消费committed Equipment projection
- **AND** MUST不把Renderer activeSelf作为gameplay真相

### Requirement: Equipment Host 必须只调度预编译入口

Persistent Feature Host与Action Route Host MUST按committed Slot/Feature/revision选择Program catalog中的唯一compiled entry，并通过现有compiled Runnable control lifecycle执行。Host MUST不tick Unity Graph、不调用Feature callback、不复制Sequence/Selector/StateMachine解释器。Unknown entry、重复entry或stale generation MUST明确失败。

#### Scenario: PrimaryAction路由到Sawblade

- **WHEN** MainWeapon committed Feature为Sawblade且PrimaryAction request到达
- **THEN** Route Host MUST进入Sawblade的预编译Route entry
- **AND** 后续StateMachine/Timeline MUST由现有operation runtime执行

#### Scenario: 装备更换后旧generation恢复运行

- **WHEN** outgoing Feature generation已被Equipment commit替换
- **THEN** 旧generation的Running节点 MUST保持aborted
- **AND** Host MUST拒绝stale continuation

### Requirement: Equipment Change 必须使用显式事务生命周期

系统 MUST使用`PendingEquipmentChange`表达Begin、Committed、Cancelled状态，并把ChangeId、Slot、from/to Equipment、source ActionInstance、begin/resolved tick保存进typed state。Begin MUST只创建active pending记录；Commit或Cancel MUST移除active pending，并在同一aggregate保留最后一条resolved记录及其最终状态；Timeline TreeClip MAY在明确帧提交Commit operation；Cancel/Action abort MUST显式撤销未提交记录。系统 MUST不按动画normalized time、Renderer状态或Timeline结束猜测commit。

#### Scenario: 换装前摇被打断

- **WHEN** Equip Action在commit TreeClip之前被Dodge取消
- **THEN** Active Pending change MUST从state移除且last resolved记录 MUST标记Cancelled
- **AND** 当前Equipment、Tag、Effect、Host与Visual selection MUST保持不变

#### Scenario: commit后动作被打断

- **WHEN** TreeClip已经提交新Equipment后Equip Action被取消
- **THEN** 新Equipment MUST保持committed
- **AND** 后续动作取消 MUST不回滚已提交换装

### Requirement: Equipment Commit 必须原子切换全部贡献

Commit MUST在同一Character state transaction中校验旧identity/revision和Action冲突，结束outgoing host generation，撤销旧Tag/Effect contribution，替换Slot identity/revision，重置Feature local state，安装新Tag/Effect contribution，启动incoming generation并追加equipment lifecycle/presentation output。任一步失败 MUST恢复savepoint且不发布部分变化。

#### Scenario: 新被动Effect应用失败

- **WHEN** incoming Feature Passive Effect未通过正式GE admission
- **THEN** Equipment commit MUST失败并恢复旧Slot、Tag、Effect、state和host generation
- **AND** MUST不输出新装备Visual selection

#### Scenario: 旧Feature Action仍活动

- **WHEN** commit时目标Slot仍有非Equip Active Feature ActionInstance
- **THEN** commit MUST返回结构化ActionConflict
- **AND** MUST不强杀动作或隐藏旧装备

### Requirement: Action admission 必须支持通用Required Tag Query

Action Profile admission MUST在创建ActionInstance前评估Required Tag Query，并与现有Block、Cancel、Target与request规则使用同一committed/candidate Tag view。Equipment Feature action MUST通过equipment source授予的Tag满足该条件；系统 MUST不按EquipmentId、Feature enum、节点名称或Graph owner硬编码准入。

#### Scenario: Sawblade Attack合法

- **WHEN** MainWeapon授予`Equipment.Feature.CorinSawblade`且Attack Required Query命中
- **THEN** Action admission MAY继续执行其它通用条件
- **AND** MUST不额外检查Corin武器枚举

#### Scenario: stale攻击请求

- **WHEN** Sawblade已卸下但旧Attack request仍在当前输入窗口
- **THEN** Required Tag Query MUST拒绝新Sawblade ActionInstance
- **AND** Route graph MUST不开始

### Requirement: Feature Action 必须捕获Equipment Context

Feature Route激活ActionInstance时 MUST捕获SlotId、EquipmentId、FeatureId、EquipmentRevision和RouteId；该context MUST进入Action state、codec、snapshot、hash与diagnostics。Feature参数读取、producer来源和lifecycle fact MUST使用该context。Core Action MUST显式使用None context，MUST不复制当前MainWeapon身份。

#### Scenario: 动作运行期间收到装备修正

- **WHEN** 一个已开始Action的当前EquipmentState因restore改变
- **THEN** 已恢复Action MUST使用snapshot中的Equipment Context
- **AND** MUST不重新绑定到当前Slot的另一Feature

#### Scenario: Core Dodge

- **WHEN** Dodge Action不属于任何Feature Route
- **THEN** ActionInstance Equipment Context MUST为None
- **AND** Dodge admission MUST不依赖MainWeapon存在

### Requirement: Equipment Parameter读取必须类型化且有上下文

`ReadEquipmentParameter` MUST通过Action Equipment Context或显式Slot/revision读取Program catalog中的Target typed constant，并验证Feature、ParameterId与value kind。缺少上下文、revision不匹配或类型错误 MUST使operation失败；系统 MUST不返回零、默认item值或当前Slot值作为fallback。

#### Scenario: 攻击读取MotionScale

- **WHEN** Sawblade Action通过自己的Equipment Context读取MotionScale
- **THEN** operation MUST返回该Equipment编译后的Target scalar
- **AND** MUST不访问Unity EquipmentDefinition资产

#### Scenario: Core节点读取Feature参数

- **WHEN** 没有Equipment Context的Core Action尝试读取Feature-local参数且未显式提供Slot/revision
- **THEN** operation MUST失败
- **AND** MUST不隐式使用MainWeapon

### Requirement: Feature local state必须按generation初始化和重置

Feature local state MUST由Program State Layout声明，并只通过当前Character transaction读写。装备commit进入新generation时 MUST从Program canonical default初始化incoming state，outgoing state MUST被重置或释放；第一版 MUST不保存未装备物品实例状态。Restore/replay MUST按snapshot中的generation恢复相同状态。

#### Scenario: 重新装备同一把武器

- **WHEN** Sawblade卸下后再次装备
- **THEN** 其Feature local state MUST从定义默认值初始化
- **AND** MUST不恢复上一次未声明持久化的计数

#### Scenario: State恢复到换装前

- **WHEN** Target snapshot恢复到Equipment commit之前
- **THEN** Slot revision、host generation与Feature local state MUST一起恢复
- **AND** MUST不存在新旧generation混合状态

### Requirement: Equipment Tag与Passive Effect必须复用正式聚合

装备授予Tag MUST使用稳定`equipment:<slot>:<revision>`source；Passive Effect MUST通过正式GE apply创建并把handle记录在Equipment contribution中。卸下 MUST按source/handle精确撤销。Equipment runtime MUST不拥有第二Tag container、Buff列表或自行实现GE stack/duration。

#### Scenario: 武器授予攻击Tag

- **WHEN** Sawblade commit成功
- **THEN** Gameplay Tag aggregate MUST记录对应equipment source
- **AND** Action Required Query MUST立即在同一transaction后续步骤读取到

#### Scenario: 卸下被动Effect武器

- **WHEN** outgoing contribution保存两个EffectHandle
- **THEN** commit MUST通过正式GE Remove精确撤销这两个实例
- **AND** MUST不按EffectId删除其它来源实例

### Requirement: Equipment状态必须进入Target snapshot和hash

Float32与Fixed Target MUST分别为Equipment aggregate、Action Equipment Context、Feature local state和pending change提供canonical codec、copy/restore、hash与layout identity。所有影响未来模拟的字段 MUST进入Character snapshot；Presentation-only Unity visual instance MUST不进入。两Target MUST保持相同业务语义但使用各自typed ABI。

#### Scenario: Target snapshot恢复

- **WHEN** Target codec恢复一份合法Character snapshot
- **THEN** Equipment identity、revision、pending state、Feature state和Action context MUST原子恢复
- **AND** 下一Tick Host选择 MUST与保存时状态一致

#### Scenario: Fixed Target缺少Equipment codec

- **WHEN** Program声明Equipment capability但Fixed Target未安装完整state codec
- **THEN** Fixed Program build MUST失败
- **AND** MUST不复用Float32 bytes或跳过字段

### Requirement: Equipment Runtime 必须拒绝未编译内容

Session Active后Equipment request MUST只引用Program catalog中的EquipmentId，且目标Feature、Route、parameter schema和capability必须与Program identity一致。Unknown item、运行时新Feature、asset热加载或layout变更 MUST失败。系统 MUST不按资源路径、显示名或默认Loadout替代。

#### Scenario: 请求DLC武器但Program未包含

- **WHEN** runtime收到未编译EquipmentId
- **THEN** request MUST返回UnknownEquipment
- **AND** MUST不加载Resources或Addressables补齐

#### Scenario: Snapshot来自不同装备catalog

- **WHEN** snapshot Program/Layout identity与当前装备catalog不匹配
- **THEN** restore MUST拒绝整份snapshot
- **AND** MUST不逐Slot近似迁移
