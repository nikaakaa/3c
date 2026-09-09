## RENAMED Requirements

- FROM: `### Requirement: Document v4必须原子替代v3`
- TO: `### Requirement: Document v7必须原子替代v6`
- FROM: `### Requirement: Document v4失败恢复必须同时覆盖Unity owner与正式package`
- TO: `### Requirement: Document v7失败恢复必须同时覆盖Unity owner与正式package`

## MODIFIED Requirements

### Requirement: Agent Authoring Document必须是按需生成的持久化目录包

系统 MUST通过唯一正式Document目录包表达精确Definition的作者目标，Unity资产保持正式真相。当前实现v6是本次输入基线，v7统一表达现有技能及新的动画作者组织。包 MUST在Assets之外按需生成，保持manifest、可编辑正文、只读context及service基线分离；不得成为第二运行资源、自动apply目录或独立动画数据库。

#### Scenario: 作者请求当前文档
- **WHEN** 调用方对精确Definition执行checkout
- **THEN** service MUST输出当前唯一版本的规范包与同步身份，不修改Unity作者资产或Build

#### Scenario: AI首次编辑现有Character Controller

- **WHEN** Agent对已有合法Character root显式checkout
- **THEN** 系统 MUST从当前正式Graph、StateMachine、Timeline、Presentation与可达Clip Curve生成规范目录包
- **AND** response MUST返回唯一文档包绝对路径
- **AND** 系统 MUST不修改或保存Unity资产

#### Scenario: 普通人工编辑期间没有AI会话

- **WHEN** 作者修改Graph、Timeline或AnimationClip但没有显式checkout
- **THEN** 系统 MUST不创建或刷新文档包
- **AND** MUST不触发reconcile、compile、build或publish

### Requirement: Graph逻辑与layout必须使用独立分片

各作者图 MUST以独立逻辑与layout分片保存稳定节点／连接／接口及位置，覆盖AnimGraph、Layer、State Pose、Rule和Control Rig。State Machine布局继续与状态／转换正文分离；纯布局变化不得改变动作、混合、Rig或播放语义。窗口缩放、选中和观察目标不能进入作者分片。

#### Scenario: 移动一个Rig节点
- **WHEN** 作者只修改节点位置
- **THEN** 计划 MUST只包含对应owner的布局修改，不能重写动画资源或求解参数

#### Scenario: AI只增加Node而不编辑layout

- **WHEN** 新Node在graph.json中存在且layout.json没有对应位置
- **THEN** apply MUST按Graph kind、拓扑层级与identity稳定排序生成位置
- **AND** MUST不移动未受影响的已有Node

### Requirement: Presentation分片必须保持整包同步与稳定owner

v7 MUST在现有整包语义下表达Profile装配、各角色图、层接口／Implementation、Slot／Group、Mask／Blend Profile、Rig控制以及既有Action Timeline动画设置。Timeline的Slot轨道、Sections和Blend设置 MUST扩展原timeline.json／curves.json及其owner，不新增并行Montage分片或资产。

动画字段、资源目录及局部作者约束 MUST来自动画模块与Animation Input Contract，不能以Gameplay编译成功为前提。整包apply继续处理所有实际受影响owner及跨域引用，不引入第二动画Document服务或跳过约束开关；角色集成错误与动画局部错误必须分开归属。

Player与Timeline Segment的AnimationClip引用必须为结构化真实对象引用，包含GUID、合法local file id及一致路径；新增可拥有的图、声明或子资产使用正式local identity分配，既有Clip不能伪造local资源。Profile不重复保存Player资源来源，generated binding不可编辑。

所有分片 MUST服从整包hash、五生命周期、Conflict与反向导出，不提供文件级apply、名称猜测或第二套Presentation同步服务。

#### Scenario: 修改Player资源
- **WHEN** 调用方将一个Player改为引用另一原生Clip
- **THEN** 计划 MUST修改该Player的唯一资源引用与实际相关owner，不创建Source Slot／Profile binding副本

#### Scenario: 修改动作的Slot
- **WHEN** 调用方修改Action Timeline动画轨道的Slot引用
- **THEN** 该Timeline与Rig Slot目录引用 MUST进入同一事务语义，原动画片段和玩法窗口不复制

#### Scenario: AI只修改一个Player资源
- **WHEN** 调用方改变一个Player使用的资源
- **THEN** v7 MUST写入Player唯一结构化资源引用并锁定整包及真实owner
- **AND** 不保留Source Slot／Profile双重binding

#### Scenario: AI提交退役Profile binding
- **WHEN** v7目标请求创建退役Source Slot或重复Profile source binding
- **THEN** parser MUST拒绝旧创建语义并要求直接资源或typed资源参数
- **AND** 不得生成兼容子资产

### Requirement: Presentation JSON必须由共享Capability生成稀疏typed字段

图角色、节点、接口、端口、状态／Alias／Rule、Slot、Mask与Rig目标的可编辑字段 MUST来自唯一Capability及资产合同。直接资源Player只保存资源或显式资源参数，普通混合保存Curve／Mask策略，Timeline保存自己的动画设置。内部Source binding、Action Playback Input、参数汇总、Goal Assembler、C#类型、SerializedProperty路径及运行时offset MUST不进入editable。

#### Scenario: 文档提交旧内部节点
- **WHEN** v7正文仍包含退役的内部作者kind或旧Source Slot字段
- **THEN** strict parser MUST拒绝并定位字段，不忽略、转换为默认配置或调用旧reader

#### Scenario: 提交资源参数
- **WHEN** Layer调用提供typed AnimationClip参数
- **THEN** 参数与接口 MUST精确匹配，不同时保存另一套Profile资源映射

#### Scenario: Clip Player JSON包含Source Id字符串
- **WHEN** Player正文包含任意Source Id、Provider Id字符串或旧Source Slot字段
- **THEN** strict parser MUST在Reconciler前拒绝，要求唯一结构化资源或参数引用

#### Scenario: Clip Player JSON包含Sequence字段
- **WHEN** Player请求旧Sequence包装资产或复制素材Curve
- **THEN** parser MUST拒绝；Sequence Player只能使用原生AnimationClip或合法typed参数

### Requirement: Pose Transition JSON必须保存可解析混合资产引用

转换 MUST保存规则、混合方式、duration、Curve及Blend Profile的正式typed设置，并通过统一Rig关联资产引用解析。有限Action Timeline的动画设置 MUST使用同一Blend合同表达自己的进入／退出设置；Slot节点不复制这些时间策略。条件式Curve字段只在对应模式合法时存在，未知资产、错误Rig或自由文本identity不得进入Mutation。

#### Scenario: 修改转换与动作Profile
- **WHEN** 目标正文为转换和某个Timeline分别指定Blend Profile
- **THEN** Reconciler MUST分别写入各自时间设置owner，不将二者合并为Slot私有配置

#### Scenario: Document修改一条Custom Transition
- **WHEN** 目标为转换设置Custom与合法Curve／Profile
- **THEN** Reconciler MUST生成与Details相同的typed Mutation并处理条件字段
- **AND** apply只保存作者，不自动Build

### Requirement: Presentation Reconciler必须调用唯一Presentation Mutation

Reconciler MUST只计算完整typed计划，人工编辑与Document共用相同Capability、字段／接口约束、Mutation及真实owner。各图、Layer、Slot目录、Mask／Profile、Rig控制及Timeline动画设置 MUST参与现有跨领域资产事务；不能直接改YAML、私有路径、generated Projection或另建Presentation mutation service。

新增owner、符号引用分配、引用切换、旧作者对象删除和反向导出必须按依赖安排。新组织迁移包含真实作者变化，但无业务变化的普通导出往返仍必须为零修改；不得为了完成清单制造apply或重写未变资产。

#### Scenario: 新建层和控制图
- **WHEN** 目标创建Layer接口、实现图和Control Rig引用
- **THEN** 计划 MUST先建立实际owner及稳定identity，再绑定调用与接口，最后删除被替代对象
- **AND** 任何失败都不能留下半个新目录或另一份可编辑图

#### Scenario: 修改Slot和Timeline设置
- **WHEN** 目标同时调整Rig Slot目录与Action Timeline轨道
- **THEN** 修改 MUST进入同一事务并在成功后从Unity资产规范反向导出

#### Scenario: 正文没有变化
- **WHEN** 当前版本包checkout后原样dry-run
- **THEN** 计划 MUST为空，不因显示名、可选字段或布局格式产生假变更

#### Scenario: apply提交退役Source Slot与binding
- **WHEN** v7 apply目标要求新增旧Source Slot与重复binding
- **THEN** 计划 MUST拒绝退役作者模型且不写资产
- **AND** 新的Player资源和Layer接口仍进入同一正式Mutation

#### Scenario: apply修改Locomotion Sync Group

- **WHEN** 文档目标状态调整Group中的原生AnimationClip成员
- **THEN** Reconciler MUST使用结构化Clip引用生成Profile Mutation并校验成员唯一性
- **AND** MUST不修改Clip Curve或自动Build Projection

### Requirement: Document v7必须原子替代v6

v7 MUST完整承接当前v6技能Macro、owner、接口及其它领域正式数据，再安装新的动画作者合同。所有reader、writer、manifest和MCP说明同时切换到唯一v7；v6及更早工作包必须拒绝并要求显式checkout，不常驻双版本reader或静默文件升级。

已有旧Unity作者对象只能由显式一次性Editor迁移流程转换，该流程与Runtime加载和日常编辑严格分开；迁移成功后移除旧类型与旧读写路径，不能成为fallback。

#### Scenario: 读取旧工作包
- **WHEN** 调用方使用v6或更早包请求dry-run／apply
- **THEN** service MUST拒绝旧包并要求重新checkout，不能修改资产或猜测新字段

#### Scenario: 升级后仍编辑技能
- **WHEN** 同一v7包包含此前v6的技能Macro和接口
- **THEN** 技能正文及其正式语义 MUST完整保留，不能为了Pose升级删除或改写技能配置

#### Scenario: 读取v3文档包

- **WHEN** service发现schema为`btsmtl-agent-authoring-document.v3`
- **THEN** dry-run与apply MUST拒绝该文档且不修改资产
- **AND** 调用方 MUST显式重新checkout

### Requirement: Document v7失败恢复必须同时覆盖Unity owner与正式package

apply MUST锁定dry-run返回的精确整包hash，并对所有受影响的图、Rig关联设置、Timeline、Profile、子资产及工作包拥有一个完整事务。成功必须保存作者并规范反向导出；保存、删除或发布包失败必须恢复原owner和包，返回明确失败状态。generated Program／Projection不属于作者事务，必须在其成功后由精确Build另行发布。

#### Scenario: 控制图保存失败
- **WHEN** 迁移中任一控制图或共享设置owner保存失败
- **THEN** 该批图、Timeline设置、引用及包 MUST一起恢复，不留下半迁移作者内容

#### Scenario: 发生Conflict
- **WHEN** Unity作者资产与Document正文均已变化
- **THEN** apply MUST拒绝，继续使用既有显式rebase规则，不自动合并或覆盖作者修改

#### Scenario: Clip Curve Validator失败

- **WHEN** Gameplay和Timeline mutation已经执行，但Clip Curve Validator发现Phase非单调
- **THEN** Application Service MUST回滚同一事务内全部Gameplay、Timeline、Clip与Presentation owner
- **AND** 正式Document package MUST保持apply前内容且响应不得报告`Clean`
