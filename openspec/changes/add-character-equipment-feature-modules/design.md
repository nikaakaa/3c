# Design: 角色装备功能核心

## Context

Character当前正式链路是：

```text
CharacterPipelineDefinition
  -> Authoring Discovery
  -> numeric-neutral Semantic IR
  -> Float32 / Fixed Program
  -> Session / CharacterSimulationState
  -> Presentation Projection
```

ActionProfile/ActionInstance只拥有动作身份、准入和生命周期；Graph、Timeline、Motion、Gameplay Effect与Animation Presentation各自处理自己的事实。这个分层已经能运行固定角色，但装备事实仍散落在RootTree、ActionProfile、Prefab与Animation Profile中。新增武器时缺少一个能被Compiler完整发现、被Program静态链接、被State保存并由Presentation投影的正式输入单元。

本change只安装Equipment Core，不迁移任何具体角色，也不更新Agent工具或网络模型。它必须可以在当前代码基座上独立apply，并为后续Corin迁移提供唯一正式入口。

## Target Shape

```text
CharacterPipelineDefinition
  -> RootTree
  -> CharacterEquipmentProfile
       -> Slot catalog
       -> Route catalog
       -> Equipment catalog
       -> Feature definitions
       -> Initial loadout
  -> CharacterAnimationPresentationProfile
  -> CharacterEquipmentPresentationProfile

Compiler
  -> Character Root
  -> Feature Persistent Roots
  -> Feature Route Roots
  -> one Semantic IR
  -> Float32 Program + State Layout
  -> Fixed Program + State Layout
  -> Presentation Projection

Runtime
  -> one CharacterSimulationState
       -> Equipment aggregate
       -> Action aggregate
       -> Tag/GE aggregate
       -> Graph/Timeline/Motion state
  -> compiled Equipment Hosts
  -> existing Action/Timeline/GE/Motion runtimes

Presentation
  -> committed Equipment visual selection
  -> Equipment Visual Runtime
  -> existing Animation Playback Lifecycle
```

## Decision 1: Equipment Core是静态链接能力

`CharacterEquipmentProfile`列出当前Character Program允许使用的全部Equipment与Feature。Compiler在构建Program时发现全部Feature graph与state declaration；Session Active后只能在这个不可变catalog中切换。

收益：

- ProgramHash、LayoutHash、Snapshot与Target capability在启动前确定。
- Float32与Fixed从同一Semantic IR生成，不需要运行时反射或asset加载。
- 装备切换只修改已存在的typed state，不改变Program布局。
- 后续新增装备只要求修改authoring并重新发布对应Character Program。

代价：

- Program包含允许但当前未装备的Feature代码与state schema。
- 新增装备后必须重新生成Program，不能热下载进入活动Session。

结论：采用。当前业务要的是可审查、可替换的动作角色能力，不是DLC热加载框架。

## Decision 2: Gameplay与Unity外观由两个Profile分工

`CharacterEquipmentProfile`只保存Gameplay真相：

```text
Slot
Route
Equipment
Feature
Parameter
InitialLoadout
VisualBindingId
```

`CharacterEquipmentPresentationProfile`只保存Unity外观真相：

```text
VisualBindingId
RigBinding / RendererSet
PrefabReference / SocketBinding
LocalPose / LifecyclePolicy
```

`CharacterAnimationPresentationProfile`继续唯一拥有动画Layer、AvatarMask、Transition与producer binding。Feature只能声明Layer/producer需求，不能内嵌Animation Profile副本。

收益：Gameplay Program不依赖Prefab、Renderer或Transform；同一Feature可以更换外观实现；动画层配置仍只有一份。

代价：一个装备需要同时配置Gameplay Profile、Animation Profile和Equipment Presentation Profile。这个重复操作是明确的职责装配，不是重复数据源。

## Decision 3: Feature是authoring/link单元，不是运行时插件

`CharacterEquipmentFeatureDefinition`包含：

```text
FeatureId
FeatureRevision
ParameterSchema[]
LocalStateDeclarations[]
GrantedTags[]
PassiveEffects[]
PersistentGraph?
ActionRoutes[]
PresentationRequirements
RequiredOperationCapabilities
RequiredWorldCapabilities
```

每个Route实现包含：

```text
RouteId
ActionProfile
InlineRouteGraph
RequiredParameterIds
RequiredProducerIds
```

Feature graph是Feature serialized owner中的inline普通BTSMTL graph。Compiler将它作为composition root静态发现；Runtime只看到compiled entry index和state address。

禁止的形状：

```text
ActionModule
AbilityAsset -> BodyGraph
IAbilityBody
Equipment callback registry
runtime Graph clone
Feature MonoBehaviour Tick
```

Feature不会成为Action身份。Action runtime仍只接收ActionProfile与ActionInstance；FeatureId只存在于编译source map、route entry和可选Equipment Context。

## Decision 4: Slot与Route必须分离

Slot回答“当前装了什么”：

```text
MainWeapon
OffHand
Utility
```

Route回答“Character主流程向装备请求什么业务”：

```text
PrimaryAction
SecondaryAction
Reload
PersistentFeature
```

Route定义保存稳定RouteId、OwnerSlotId、现有InputRequestId、消费策略与missing implementation policy。第一版missing policy只允许：

- `ReturnFailure`：当前装备不实现Route时不产生Action。
- `RejectComposition`：Profile中允许的全部Equipment都必须实现Route。

不提供Priority竞争、选择第一个实现或默认武器fallback。同一Loadout中一个Route只能由Owner Slot当前Feature提供一个实现；重复或跨Slot争用在编译时失败。

这样RootTree只需要通用PrimaryAction Host。Sawblade与Gun都实现同一个Route时，换装备不要求修改RootTree拓扑。

## Decision 5: Feature graph使用显式composition roots

Authoring Discovery输入从一个RootTree扩展为有角色的root catalog：

```text
CharacterRoot
EquipmentPersistentRoot(FeatureId)
EquipmentRouteRoot(FeatureId, RouteId)
```

每个root保存稳定owner identity、role、source path和entry identity。Discovery必须从Definition/Profile引用闭包得到roots，不扫描目录、不按命名约定查资产，也不只编译Initial Loadout。

全部root复用同一Graph identity、Value Port、control topology、Timeline引用和Operation Set校验。Semantic IR只增加root role和Equipment catalog，不增加第二种Flow IR。

代价是Compiler从单根模型升级为多composition root模型；收益是RootTree不再承担所有可选业务分支，同时Runtime仍只有一个Program。

## Decision 6: 参数与Feature局部状态必须类型化

Feature声明稳定ParameterId和value kind；EquipmentDefinition为每个必需参数提供恰好一个值。第一版只接受现有portable value kind的明确子集：

```text
Bool
Int32
Scalar
Vector2
Vector3
Yaw
GameplayTagId
EffectId
ProducerId
```

Compiler将参数降低为Target typed constants。Graph通过`ReadEquipmentParameter`按Action Equipment Context或显式Slot/revision读取。Runtime不使用Dictionary、字符串名称、SerializedProperty或Unity asset查找。

Feature local state复用Character State Layout的typed slot contract。地址由FeatureId与state declaration identity组成，并只通过当前Character transaction读写。

第一版不引入EquipmentInstance持久化：Slot revision变化时，outgoing local state重置，incoming local state从Program默认值初始化。背包中未装备武器的弹匣、耐久或随机词条不在本change范围。

## Decision 7: 两类Host只调度compiled entry

Persistent Feature Host管理当前Slot Feature的持续入口：

```text
Equip revision N
  -> activate persistent entry generation N

Replace / Unequip
  -> abort outgoing generation
  -> activate incoming generation after commit
```

Equipment Action Route Host按Slot、Route和committed revision解析唯一compiled route entry。进入entry后，Sequence、StateMachine、Timeline、Motion与GE都由现有operation runtime执行。

Host不读取Feature asset、不回调C# handler、不复制控制流解释器。Unknown entry、重复entry或stale generation必须结构化失败。

## Decision 8: 装备切换是显式事务

正式生命周期：

```text
typed equipment change request
  -> BeginEquipmentChange
       validates slot/item/revision/action conflicts
       creates PendingEquipmentChange
  -> optional Equip Timeline
  -> TreeClip submits CommitEquipmentChange at explicit frame
  -> outgoing host abort
  -> outgoing Tag/Effect removal
  -> Slot identity/revision replacement
  -> local state reset/init
  -> incoming Tag/Effect install
  -> incoming host activation
  -> durable visual selection output
```

Pending记录至少包含ChangeId、SlotId、from/to Equipment、source ActionInstance、begin tick与commit state。

Commit前被打断：显式Cancel，现有装备不变。Commit后动作被打断：新装备保持，后续Timeline只结束自己的生命周期。系统不根据normalized time、Renderer状态或Timeline结束猜测是否已经换装。

Commit在同一Character transaction savepoint内原子完成。任一步失败时，Equipment、Action、Tag、GE、host generation和presentation output全部恢复旧值。

## Decision 9: Required Tag是通用Action准入

ActionProfile增加`RequiredTagQuery`，并与现有Block、Target、Cancel/Transition、request consumption形成稳定评估顺序。空Required Query显式表示Always。

Feature装备成功后按稳定source授予Tag：

```text
Equipment.Feature.CorinSawblade
source = actor + slot + equipment revision
```

Feature Action通过Required Query判断当前事实，不检查EquipmentId、WeaponType、Graph owner或节点名称。Tag只负责准入事实；Route仍由Owner Slot + RouteId明确选择。

这让移动、闪避、攻击、技能都继续使用同一个Action admission合同，也避免为每种装备新增专用If节点。

## Decision 10: Feature Action捕获不可变Equipment Context

Feature Route创建ActionInstance时捕获：

```text
SlotId
EquipmentId
FeatureId
EquipmentRevision
RouteId
```

该Context进入Action state、codec、snapshot、hash、fact与diagnostics，并在实例生命周期内不可变。参数读取、producer来源与lifecycle trace使用该Context，不重新读取“当前装备”猜测动作来源。

Core Action如Dodge、Jump与Equip自身使用`None`。恢复时若Context引用当前Program catalog中不存在的identity，整份state恢复失败，不将Context降级为None。

## Decision 11: Tag与Passive Effect复用现有聚合

装备Tag使用唯一Gameplay Tag aggregate的source计数。Passive Effect通过正式GE apply创建，并将EffectHandle保存在Equipment slot contribution中；卸下时按handle精确Remove。

Equipment runtime不拥有第二个Tag容器、Buff列表、GE堆叠器或duration clock。这样装备、Action与Gameplay Effect在同一Character transaction中看到同一Tag/Effect候选视图。

## Decision 12: Animation需求与Equipment外观分开投影

Feature PresentationRequirements只声明：

```text
RequiredLayerId
ExpectedBlendMode
ExpectedOutputPolicy
RequiredProducerIds
VisualBindingId
```

Projection build验证Animation Profile中的Layer、Mask、output policy、producer binding与transition资源。Feature graph中的动画仍由Timeline AnimationTrack提交producer：

```text
Feature compiled graph
  -> Timeline command
  -> Presentation Queue
  -> Animation Playback Lifecycle
  -> Animancer presenter
```

Equipment Visual Runtime只消费committed visual selection并管理Renderer/Prefab/socket。它不调用Animancer/Animator，也不将visual状态反写Gameplay。

第一版支持：

- `ExistingRigObject`：显式RigBindingId与RendererSet。
- `SpawnedVisualAsset`：显式Prefab、SocketBindingId、LocalPose与LifecyclePolicy。

不使用Hierarchy名称、模糊Transform路径、Tag或第一个子物体fallback。Actor Presentation重新创建时可从最新committed EquipmentState重建visual；远端Observed Actor同步不在本change范围。

## Decision 13: Float32与Fixed共享业务语义

Semantic IR增加numeric-neutral Equipment schema与operation contract，至少包括：

```text
ReadEquipmentIdentity
ReadEquipmentParameter
RequestEquipmentChange
BeginEquipmentChange
CommitEquipmentChange
CancelEquipmentChange
EnterEquipmentFeatureHost
ExitEquipmentFeatureHost
ResolveEquipmentActionRoute
```

Portable control定义identity、route resolution和事务顺序；Float32/Fixed分别实现typed constants、state partition、codec与operation evaluator。Scalar等数值由Target lowering生成具体类型，公共IR不保存Float32或Fixed runtime值。

两Target的ProgramHash/LayoutHash可以不同，但SemanticHash必须对应同一业务源。任一Target缺少operation、state codec或capability时，该Target Program整体编译失败；不允许删除Feature、跳过字段或复用另一Target bytes。

## Decision 14: Network、Agent与Corin迁移是后续消费者

本change到`committed EquipmentState + Presentation Projection`为止。它不修改：

```text
Agent schema / Snapshot / Patch / MCP bridge
Corin RootTree / Timeline / Prefab / Animation Profile
ServerAuthoritative input / checkpoint / ObservationFrame
DeterministicRollback canonical input / remote presentation
```

业务取舍：

- 收益：Equipment Core可以独立实现和审查，不与尚未实施的AI Agent change争夺schema，也不在核心未稳定前批量迁移Corin资产。
- 代价：本change完成后Corin不会自动变成装备化角色，联网换装也不会立即可测。

后续顺序应为：

```text
Equipment Core
  -> Corin Sawblade authoring migration
  -> Agent Equipment authoring support
  -> Network Equipment input/observation integration
```

这些后续change必须消费这里的正式Profile、Program、State和Projection，不得复制Equipment catalog或增加旁路。

## Capability Boundary

Feature必须声明并由Compiler推导实际operation/world capability。当前项目没有正式Hitscan、Projectile或跨角色Combat Result，因此本change不创建假的枪械operation。

示例：

```text
Sawblade Feature
  -> Action
  -> Timeline
  -> MotionCurve
  -> GameplayEffect

Future Gun Feature
  -> Action
  -> Timeline
  -> RangedAim
  -> HitscanQuery or ProjectileSpawn
  -> GameplayResult
```

如果Feature引用未安装能力，Compiler报告Feature、Route、Node与缺失capability，并拒绝整个Target Program。

## Failure Policy

以下情况必须在Authoring validation、Semantic compile、Target compile、Session preparation或State restore的最早正式边界失败：

- Profile capability与Profile引用不一致。
- SlotId、RouteId、EquipmentId、FeatureId、ParameterId或state declaration identity重复。
- Equipment引用错误Slot/Feature，或参数缺失、额外、类型不匹配。
- Initial Loadout引用未编译Equipment。
- 两个active Slot争用同一Route。
- Feature graph owner、entry或composition root不闭合。
- Feature引用未声明Action、Tag、Effect、Layer、Producer或capability。
- Equipment commit时旧Feature Action仍active。
- ExistingRigObject或SpawnedVisualAsset binding不完整。
- Action Equipment Context或state snapshot与Program catalog不兼容。

系统不选择默认装备、不搜索名称、不回退RootTree旧逻辑、不吞掉Target未实现operation。

## Risks And Mitigations

### Program体积增长

静态链接允许catalog中的全部Feature会增加Program与Projection。Compiler可以对SourceMap路径、重复参数schema、producer引用和Graph topology做canonical去重，但不能把Feature改回运行时Unity asset解释。

### 多composition root扩大Compiler职责

单Root discovery会变成显式root catalog。必须让所有root复用同一Frontend、Operation Set和identity规则；Equipment不得拥有第二个compiler或第二种Flow IR。

### Feature扩张成任意插件

Feature只能使用版本化Operation Set、声明的Route和typed state。新增领域能力必须先增加正式operation/capability，不能注册任意C# callback。

### 表现配置分散感

Feature、Animation Profile和Equipment Presentation Profile需要分别配置需求、动画实现与物体实现。Inspector和diagnostics必须提供identity跳转与闭包摘要，降低作者查找成本，但不能把三类所有权重新合并为一个大资产。

## Rejected Naming

不使用：

- `ActionModule`
- `AbilityModule`
- `AbilityAsset`
- `AbilityBodyGraph`
- `EquipmentAbilityRunner`
- `WeaponAnimatorController`

正式名称使用`CharacterEquipmentFeatureDefinition`，强调它是Character Compiler输入，不是独立运行时。
