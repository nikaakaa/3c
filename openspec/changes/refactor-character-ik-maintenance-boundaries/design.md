# IK可维护性边界设计

## Context

维护目标是让“修改一种行为”主要落在该行为的Owner；增加解释证据不改变运行状态，改变评分不改变运行行为。

当前外层已经只调用唯一Foot Module，并通过根Bank提交Foot、Goal与Bend。内部`EvaluateFrame`仍需要理解脚需求、初步Resolved、Pelvis选择、Landing完成、最终Goal夹紧及多份诊断投影。问题是类型不能充分说明阶段含义，不是缺少更多可替换接口。

核对入口：

- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootPlacementModule.cs`：Evaluate、Pelvis调用、Finalization、Reach夹紧与Goal编码。
- 同目录`CharacterFootLifecycle.cs`、`CharacterFootSwingMotionBuilder.cs`、`CharacterFootLifecycleContracts.cs`：初步结果、凭据、最终结果和过程证据。
- 同目录`CharacterFootInterpolationRuntime.cs`：从过程Fact读取方向历史。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseConstraints/CharacterFinalIkFullBodySolver.cs`：参考姿态准备、BendHistory消费、Vendor方向读写及Reset。
- `Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/`：Sampler、Analyzer、Publisher和当前版本合同。

以上路径均相对Unity项目根`3cDemo/Client/3C_Client`。实施前重新读取源码与资源identity；不能把提案编写期间仍在变化的工作区自动认作已验收基线。

## Goals / Non-Goals

### 目标

- 明确请求和最终结果，消除“结果发布后再由Goal层修改业务位置”。
- 每个持久值只有一个写入Owner和明确Reset语义。
- 保留诊断证据，减少同一字段重复定义和转抄。
- 保持同一Module、根Bank、Interpolation、Pelvis Response、Goal Set、FBBIK和Writer。

### 非目标

- 不解决本轮已发现的Path上下表面混排问题。
- 不改变Foot/Pelvis正常帧公式、作者权重、状态准入、Ground/Reach安全政策或配置。
- 不借拆分建立通用IK框架、插件式状态机、全局Blackboard或第二缓存生命周期。
- 不修改TrainingEnemy、外层Pose Program架构和PostCommit诊断错误处理。

## Decisions

### 1. 模块内部采用请求、裁决和结果三个明确阶段

拟定内部术语为`CharacterFootPlacementRequest`、`CharacterFootPlacementRequestPair`与`CharacterFootReachOutcome`；最终输出继续使用`CharacterResolvedFootResult/Pair`。这些都是数据合同，不新增Pose Graph节点、可独立调用的运行链或对外提交步骤。

| 阶段 | 输入 | 输出 | 唯一责任 |
|---|---|---|---|
| Foot求值 | 同帧动画、正式Foot Motion、世界Observation、上一Committed历史 | typed脚需求与内部完成凭据 | Transition、Target、唯一Interpolation各自推进既有职责 |
| Primary Support与Pelvis | 请求中的Support/Reach视图、当前动画与Body、现有设置 | Primary结果、Pelvis结果和每脚Reach Outcome | 原有支撑选择、统一双腿边界和一次骨盆响应 |
| Foot收口 | 原请求、完成凭据、Pelvis及Reach Outcome | 最终Resolved Pair与过程证据 | 完成既有Landing准入与必要Foot目标夹紧 |
| Goal编码 | 最终Resolved Pair、Pelvis Result、正式空间绑定 | 三个Goal Contribution值 | 只编码位置、旋转和权重，不再决定Reach或修改业务目标 |

请求必须携带Frame、Completion、Rig、Side与Event身份，明确未加权目标和按作者权重得到的有效目标。Primary/Pelvis只读取所需的Support Eligibility、Support Intent、误差、Event、有效脚位置和typed Reach，不读取Foot State、Lock Mode、Anchor历史、Interpolation或Diagnostics。

内部完成凭据可以保留Finalization所需的本帧计算事实，但只能由Foot Lifecycle消费；不得被Pelvis、Goal、根Runtime或诊断评分器当成第二份可变状态。

保留当前求值先后与数学：先处理Foot，再进行一次`ResolvePelvis`，按既有Reach结果完成Landing，必要时按既有权重和空间转换顺序限制脚目标。不能借搬移职责改成另一种腿长算法、增加响应、重算Support或迭代多次。

Reach Outcome必须区分“原落脚请求得到满足”和“原请求不可达但已把输出夹紧到合法位置”。夹紧成功不能把原请求重新标为可达，也不能因此允许Full Lock。不可达时的Primary Support保护、Anchor保持、状态与作者权重均沿用原业务政策。

Reach Outcome只作为Foot Transition准入的typed输入；Hard Constraint与Pelvis不能直接写离散State。已有Transition Resolver/Runtime仍是判定和写入Owner，不新增另一份状态选择。

最终Resolved中的Final Ankle、Final Rotation、Effective Sole/Ankle与Correction必须来自最终输出，使用既有Foot/Heel/Toe几何推导；夹紧前的请求只能进入请求证据，不保留同名“最终值”。最终Resolved是Goal输入，不是FBBIK或Physical Writer已经达到的骨骼位置。

### 2. 运行状态与本帧证据分责

| 状态类别 | 写入Owner | 后续读取者 | 重置来源 |
|---|---|---|---|
| 离散状态与Contact边沿 | Transition Runtime | Target与后续Transition | 既有Foot Reset合同 |
| Verified Landing与预测跟踪 | Landing Runtime | 请求准备与Foot Lifecycle | 既有Landing身份变化和根Reset |
| 残差、响应标量、已应用方向 | Interpolation Runtime | 下一帧Interpolation | 既有分域切换和明确Reset |
| 骨盆输出与速度 | 既有Pelvis Response | 下一帧同一Response | 既有Pelvis释放与Reset |
| 腿方向历史 | FBBIK Adapter的typed BendHistory写入 | 下一帧同一Solver | 明确的Solver Reset及正式准备结果 |

`CorrectionResponseFact.ResponseDirection`中下一帧会读取的方向迁入显式运行历史。Fact只保存本帧的前值、目标、采用值、原因与结果，不承担下一帧状态。该迁移必须保持当前方向限制、数值推进、ContactWorldResidual、Release移交和所有重置理由不变。

不因为字段名字包含Fact就全部删除；必须逐字段检查运行消费者。已经没有消费者且也不属于其它active正式待实现合同的旧字段、构造参数和转抄链直接删除，不保留占位值。

`PreviousVisibleOutput`及Weighted Goal Sole正式接管仍由`stabilize-character-foot-path-and-landing`拥有。本change不启用、否决或复制该行为；其合法Committed引用可作为输入，但不能用本change重新解释其连续性语义。

根Bank继续唯一决定Prepare、Seal、Discard。业务Owner只更新Pending记录，不新增独立Committed identity；持久结果可用性与Pending事务开放状态分开解释。

### 3. Solver Reset单独按行为修正处理

目前普通帧已经有typed BendHistory，但历史为空且动画方向不可靠时会读取Vendor的可变`bend.direction`。正式清空Bank后若该字段未恢复，就会继承Reset之前的方向。

本提案建议沿用“清空Solver历史”的原始含义：初始化与完全Reset均从同一正式Rig参考姿态、Profile准备过程建立typed方向初值。参考方向必须由现有准备过程精确取得并保存在项目拥有的只读准备结果中，带有匹配的Rig/准备身份；不得新增默认世界轴、近似方向或运行时Transform搜索。准备几何无法建立合法方向时显式失败。

正式参考方向与已经发生过的运行历史必须分型。存在准备初值不代表`HasStableDirection`或`HasAppliedDirection`已经成立；初始化后遇到可靠动画方向时，必须保持原首次帧选择，不因参考初值提前启用历史半球限制。只有本次正式求值实际写入的历史才能成为下一帧Committed历史。

Solver每次求解只从当前Pose、Goal、Profile、正式准备结果与根Bank历史设置Vendor工作字段，之后才调用Vendor Update。Vendor对象可以复用，但它上次遗留的值不能决定本帧输入。

不扩大Reset范围：Foot Reset、Body流重置和Solver Reset目前各有调用责任，本change不能把所有Foot Reset都变成额外的Solver Reset。现有明确要求清空Solver历史的路径、初始化和调参清历史路径必须统一重建方向；需要跨Reset保留历史的其它业务必须显式提出，不能暗中保留Vendor字段。

该项与active的动画方向符号/半球翻转政策不同。本change不修改该政策。普通已建立历史的帧应保持原行为；完全Reset后退化输入不再继承旧Vendor方向是本次允许且必须单列的行为变化。

### 4. 诊断按业务分组，采样列绑定只声明一次

运行Owner在计算时产生不可变证据，按接触、插值响应、支撑、Reach/Pelvis组织。不再把同一组响应字段先平铺进一个过程记录，再逐项平铺到第二个过程记录和公开Diagnostics。

Root Pending Diagnostics页继续固定容量冻结同帧证据，Writer只补入它实际拥有的骨骼写入事实。Seal后的Gizmo、CSV、Trace与Watch读取Committed页，不重算业务。这不改变外层Frame Transaction及PostCommit Fault政策；相关外层审查风险不因本change完成而被宣称已修复。

Editor建立一份当前版本的typed采样列绑定。每个绑定声明稳定列名、数据类型、单位、所属业务组、有效性条件、typed写入与读取映射。同一有序绑定产生Header、单行写值、Analyzer读取及必需列校验。绑定在明确初始化时检查重复名称、类型和覆盖，不在OnInspectorGUI做扫描、代码生成或全量重建。

Runtime不读取列名、Dictionary或反射。Editor可以缓存解析索引与typed委托；解析仍沿唯一Sampler/Analyzer/Publisher，没有另一个“通用导出器”。大几何页保持独立紧凑表，不摊进每脚主行；没有语义变化的几何列不借机改格式。

列绑定只统一搬运，不统一或复制业务数学。运行公式仍由Runtime拥有，质量判断仍由既有Analyzer/Diagnosis拥有。普通新增证据字段不要求修改评分规则或Publisher；格式identity由一个正式定义提供给当前写入器、读取器和Publisher。

只有布局或语义真实变化时升级当前格式版本。最终Resolved从夹紧前转为夹紧后的语义变化必须显式改名或升级版本；原请求值需要保留时使用明确请求字段。旧reader、旧别名和自动补零删除，历史原包及原诊断结果仍作为不可变证据保存，不用新规则静默解释旧包。

### 5. 方案取舍

下表比较实现组织方式，不替用户判定实施优先级。提案建议项仅在用户批准本change后生效。

| 决策 | 同级可行方案 | 业务收益与代价 | 本提案建议 |
|---|---|---|---|
| Foot/Pelvis协作 | 一个内部联合求解方法；或typed请求/裁决/结果协作 | 联合方法让调用更少，但更换骨盆策略需要阅读更大实现；typed协作保留独立负责人，但需维护明确阶段合同 | typed协作，全部留在现有Foot深模块内部 |
| Solver完全Reset | 恢复正式参考方向；或保留明确声明的连续方向状态 | 前者利于重生、重载与可复现初始化；后者利于不间断动作，但Reset必须带保留语义且状态不能真正清空 | 现有清历史路径采用前者，不新增连续Reset模式 |
| 采样映射 | Editor静态typed列绑定；或由唯一schema生成固定Header/Writer/Reader源码 | 前者无需生成器和第二编译步骤，改列集中但有Editor委托分发；后者产物可直接调用，需额外维护生成器、生成时机与产物检查 | 静态typed列绑定，仅影响Editor采样，不进入角色热路径 |
| 正式诊断布局 | 内部按业务组保存、导出时展开；或各阶段独立快照再合并 | 前者减少重复字段，跨阶段共享一帧证据；后者便于单独订阅但增加快照关联与冻结成本 | 同一帧内分组记录，保留唯一冻结/发布链 |

## 所有权与接入顺序

本提案与现有active变更保持独立文件目录，不接管它们未完成的行为工作。

1. `refine-character-pelvis-response`继续拥有Pelvis行为、参数实验与结果裁决。实施本change前读取其用户接受的最终源码和资源，不能重新指定频率、余量或效果基线。
2. `stabilize-character-foot-path-and-landing`继续拥有Foot行为、位置basis与Goal Sole历史接管。请求/最终结果的重叠条款须在实施开始前完成唯一合同对账；本change实现期间，同一Foot/Goal/Bend文件不得并行进行行为实验。
3. `consolidate-foot-diagnostic-scoring`继续拥有评分。字段映射迁移保持其七维权重、分母、Evidence/Health区分和Unavailable规则。
4. `refactor-character-pose-graph-architecture`继续拥有外层Constraint调用、Program和Publication边界。本change不引入其拟建外层类型；它实施时消费本次结果合同，不恢复旧字段。若该change先实施，应重新定位本change适配点，不能保留旧外层旁路。

current与active现有“Pelvis只读取Resolved Pair”必须在最终安装时统一替换为“Pelvis读取typed脚需求，最终Resolved在Foot收口后产生”。不是新增第二种同名Resolved供Pelvis使用，也不是将旧类型做兼容包装。

`stabilize`的Pelvis delta标题还带有“并保持双腿可达”，与current标题不完全相同。实施/归档对账必须将两种旧标题及其Support/Reach条款归入本次唯一新requirement；本轮不擅改该active文件。其它仍在变化的用户文件出现合同冲突时停止相关实施并提交具体差异供用户决策，不回退已正确改动。

## 验证与交付

- 提案仅执行OpenSpec严格校验和文本差异检查，不运行Unity、Character Build或.NET构建。
- 实施采用独立小步中文提交：最终结果收口、Interpolation历史搬移、Solver Reset修正、诊断分组、采样映射迁移分别可追溯，不将行为实验夹在机械迁移中。
- 结构迁移使用已有正式录制及相同输入/Body/时序对比Goal、Pelvis、状态、Bend、实际骨骼与质量规则；不以总分上升代替对账。
- Solver Reset以既有可执行的初始化/Reset入口核对相同正式输入与准备结果，不用普通行走回放冒充已覆盖Reset退化输入。没有对应运行证据时明确标注未覆盖，不新增测试框架或临时替代入口。
- 采样映射迁移在独立分析输出目录对原始数据做字段级比较，保留原包；同类型字段顺序、非有限值、缺列和版本差异必须显式拒绝，不补值掩盖。
- 只有实施任务真实完成后才能勾选；构建时遵守项目的禁用build server参数并立即shutdown，禁止Unity batchmode。

## Open Questions

- 本提案无需新增业务参数。实施时若发现某项正常帧行为无法在保留数值顺序的前提下完成责任搬移，应作为明确行为决策交给用户，不能以“重构误差”自行接受。
- 初始化与Reset的正式方向若不能由现有参考姿态准备过程精确重建，必须报告缺失状态；不能用近似初值绕过当前Vendor状态所有权要求。
