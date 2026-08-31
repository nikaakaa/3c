# Change: 收口IK权威数据流与控制权

## Why

当前IK已经形成唯一Foot Placement、Goal Assembler、FBBIK和Writer，Transition、Target、Interpolation与Pelvis也有明确实现入口。需要保留这条主链，不按文件行数重拆系统。

本change的主目标是完善权威数据流与控制权：每个业务事实有明确来源，每个阶段只消费被授权的结果，每个可变状态和最终输出有唯一修改者。诊断分组、字段映射和代码整理只为这条主线服务，不以文件变小、类型增多或校验增多作为完成标准。

维护成本主要来自三处：

1. `CharacterFootLifecycle.Evaluate`仍同时返回初步Resolved、过程Motion与完成凭据，Module在Pelvis响应和可达观察后再次完成Landing，初步结果和最终结果仍使用相同类型。此前“保存Resolved后再夹紧Goal”的问题已随`9da24a5`删除末端夹脚而消失，不再作为本次重构依据或待迁移逻辑。
2. Interpolation从过程`CorrectionResponseFact`读取上一帧方向；Solver清空根Bank的BendHistory后仍可能读取Vendor旧`bend.direction`。运行历史、过程证据和第三方工作字段的归属没有完全分开。
3. Module仍从SwingMotionResult的State、Step和Resolved一起重判Reach准入，Pelvis前又将临时Goal反解为有效脚底，Stride准备直接混用Step、Landing和Path字段。即使这些值当帧一致，决定和阶段权威仍分散在多个入口；它们随后又在过程记录、Diagnostics和CSV中重复转抄。

本提案只处理上述维护边界。Ground Path修复已经由其它工作进入当前代码，本change不重新实现或撤销；已否决的“Current Support替代Swing包络高度”也不恢复。

## 2026-09-01开工基线

- 用户指定的唯一源码与行为语义基线为 `ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。不能用当前HEAD、最新提交、其它候选或仅凭相近效果替换；233436是对应运行证据，205014是交叉对照。获取基线只做只读Git对比或独立检出，不回退当前工作区的后续正确改动。

- 行为对照采用`20260831-233436-894-d1564c7fa0b442f6aef02bb470ca0b1b`恢复包，保留`205014`已接受的膝向运输及Foot/Pelvis行为。恢复代码为`a6eceac`，恢复确认记录为`ad3527e`。
- 直接比较`205014`与`233436`：两包均2086脚行、1215列；1191列逐值相同，24列仅采样/实例/Surface/Path身份变化且映射无冲突。`233436`同目录正式Proof对前次`232702`匹配1044输入帧，不能把它改称官方直接对`205014`的A/B。
- 当前保留“Reach只作逐腿/交集观察及原Landing完成资格”，不再硬夹骨盆、不在末端夹脚、不以Primary为硬执行例外；观察结果仍可能阻止原Landing完成，不能借“只作观察”删除该资格。
- `compact-foot-diagnostic-publication`的小报告、唯一明细存储、索引与只读查询已经完成；本次只统一记录和列映射，不重做存储。
- 该基线用于保护现有行为，不代表全身IK已无缺陷。R825–827深折叠、部分接触间隙和穿透保留为已知问题，不在结构迁移中顺手调参修复。
- `stabilize`尚未勾选的任务须区分有效后续工作、已否决候选与过期条款；不能将所有历史任务完成或所有change归档作为本次机械前置。实施前只需完成直接重叠合同对账，并停止同一Foot/Pelvis/Bend文件上的并行行为实验。

## What Changes

- 按“正式输入/世界观察 -> Foot目标与连续输出 -> 初步脚请求 -> Primary/Pelvis与可达观察 -> Landing完成 -> 最终Resolved -> GoalSet -> Solved Pose -> Physical写入”固定生产者、只读消费者和修改权限。
- Foot请求生产者一次发布Support、Reach观察准入和加权脚几何；删除下游借过程Motion或原始Step/Path再作同义判定的路径，不通过临时Goal回算Pelvis输入。保留原条件、空间变换与数值顺序。

- 把Foot内部的“初步脚需求”“完成凭据”“最终脚结果”严格分型。Primary Support与Pelvis消费同帧typed脚需求；既有Pelvis响应、逐腿可达观察及Landing完成判断之后发布最终Resolved Pair，Goal只编码该结果。
- 保留`ResolvePelvis -> AdvancePelvisResponse`、共同目标、软姿态偏好、现有速度/Handoff规则、权重和配置。Reach不取得骨盆或脚目标的硬修改权，不恢复已经删除的夹紧API、公共执行边界或恒值兼容字段。
- 将下一帧真正使用的方向与数值放进最小typed运行状态，过程Fact只记录本帧变化。正式清空Solver历史时，从同一Rig参考姿态准备结果重新建立方向，不读取重置前的Vendor工作字段。
- 过程证据按响应、接触、支撑与可达职责分组，删除同义平铺副本；Editor为当前采样格式建立唯一typed列绑定，由同一绑定驱动列名、写值、读取和必需列校验。
- 继续使用唯一Sampler、Analyzer、Publisher、紧凑明细存储和当前评分政策；删除被替换的类型、字段和手写映射，不新增旧格式reader、自动补列或并行实现。复用现有输入/发布边界校验，不在每个内部方法重复检查同一组身份和数值。

## 范围

### 包含

- 每类输入、Observation、目标、状态、权重、支撑选择、Pelvis、Goal和骨骼结果的权威来源、合法消费者与唯一修改者，以及实际读取路径迁移清单。

- Corin正式Foot链内部的请求、结果、完成凭据及Goal编码关系。
- Interpolation运行历史与过程证据分责。
- FBBIK初始化及明确清空Solver历史后的方向所有权修正。
- Foot诊断记录分组和Editor采样读写映射统一。

### 不包含

- Ground Path、CapsuleCast、地面上侧轮廓算法、场景碰撞和Layer配置。
- 新的脚状态、Landing/Contact政策、位置basis、Weighted Goal Sole历史接管、Foot Height或作者曲线。
- Pelvis软姿态偏好、压缩余量、频率和清速度的新实验；不得把本次整理变成调参、恢复Reach硬执行或重做已完成工作。
- 已保留的有符号膝向运输及退化分支政策、PoseBone零位移Goal跳过规则、各Effector旋转能力等其它求解行为变化；不恢复SmoothKnee失败后处理。
- Pose Program、外层Frame Transaction、PostCommit诊断异常政策、通用节点ABI或Final Writer架构重构。
- TrainingEnemy、装备内容接线、Gameplay、KCC、Network、Rollback及新的Solver。

## 行为变化与验证

结果分型、运行状态搬移、诊断映射迁移属于结构整理，必须保持相同正式输入下的Goal、Pelvis、状态、Bend与最终骨骼行为。最终Resolved仅表示原有Landing完成处理之后的Goal输入，初步结果改为内部请求证据；不能为强化“最终”含义重新加入Reach硬执行，也不承诺FBBIK一定达到全部目标。

“清空Solver历史后仍继承Vendor方向”属于独立行为修正，只允许其明确初始化边界发生变化；不与普通帧膝盖算法修改混在同一个提交里。使用已有录制、正式回放和只读离线对账，不新增测试工程或临时运行链。提案阶段不编译、不Build、不运行Unity。

## 与现行规格对比

| 当前合同或active change | 对账结论 | 本提案处理 |
|---|---|---|
| current `character-foot-placement-presentation`：唯一深模块、唯一根事务 | 一致，继续保留 | 不新增外部阶段和独立提交；只在模块内部区分请求与结果 |
| current：字段只有一个写入Owner，下游不读Foot内部状态 | 原则一致，但Module仍读过程Motion.State重判准入，部分Pelvis输入通过临时Goal反算 | 决定和有效几何收进唯一请求生产者，Root只调度，下游只读被授权视图 |
| current：`Resolved Foot必须形成紧凑下游合同`与`Pelvis必须只消费Resolved Foot Pair` | 名称和时序仍需分清：Pelvis读取初步需求，其输出上的本腿可达观察又参与原Landing完成判断 | delta把Pelvis输入改为内部typed脚需求，并重命名requirement；最终Resolved在原完成阶段后发布 |
| current Foot诊断文字写为从Committed事实“深冻结”，animation pipeline要求Writer前冻结Pending、Seal后发布 | 冻结时机表述不一致 | 收紧Foot requirement为计算阶段捕获证据、Pending冻结、Writer补写实际输出、Committed只读发布；不改外层Fault政策 |
| current `character-animation-pipeline`与Pose Graph：Vendor字段不能成为跨帧真相 | 一致，但Reset路径尚未落实 | 增补正式初始化方向与清空历史的可验证场景 |
| active `stabilize-character-foot-path-and-landing` | Resolved/Pelvis条款仍保留双腿硬执行、Goal夹紧等过期要求；位置basis、Goal Sole任务混有未完成和已否决候选 | 以用户已接受的Reach撤除和233436行为为准，对账直接重叠合同；不为任务勾选复活旧实验 |
| active `refine-character-pelvis-response` | 任务已完成，第16步已删除全部Reach硬执行和末端夹脚，只保留观察及原Landing完成资格 | 本提案删除旧夹紧迁移目标，严格保留当前共同目标、软偏好、一次Spring及其参数 |
| active `add-animation-relative-knee-response` | 明确为已否决、已撤销的SmoothKnee实验 | 不作为实施依赖，不恢复其配置、尾段或角差历史；保护stabilize中已保留的膝向运输 |
| active `compact-foot-diagnostic-publication` | 小报告、analysis清单、details与索引已完成 | 保留单次解析、内存事实交接和只读查询；本change只统一字段搬运，不复建大型facts.json |
| active `consolidate-foot-diagnostic-scoring` | 统一评分与本次统一字段映射不同 | 保留唯一评分Owner、维度、权重、分母、缺失语义和原始历史包 |
| active `refactor-character-pose-graph-architecture` | 已计划外层Module、Program、Publication与Diagnostics Projector迁移 | 本次仅处理Foot内部和采样映射，不创建第二套外层协调器；后续重构消费本次收口后的合同 |

本轮只更新本提案六份文档，不修改current specs、`project.md`或其它active文件，不把尚未实施的结构设计写成已安装事实。`project.md`与stabilize仍有旧硬Reach保证，已与用户后续决定及当前源码不一致；实施/归档时必须沿唯一合同清除这些过期要求，不能据此恢复硬夹紧。旧“Pelvis读取最终Resolved”的输入阶段表述也须统一替换，防止后归档的delta覆盖当前决定。

## Impact

- 规格差量：`character-foot-placement-presentation`、`character-animation-pipeline`、`character-presentation-pose-graph`。
- Runtime：Foot内部合同、Lifecycle收口、Pelvis请求适配、Goal编码、Interpolation历史、FBBIK方向准备和重置。
- Editor：Foot过程诊断投影、Sampler列绑定、Analyzer解析与共享格式identity。
- 不改变Pose Graph端口、Goal Contribution/Goal Set ABI、Rig作者数据或Profile字段。若实现发现必须改变这些外部合同，应报告超出范围，不自行扩大迁移。
- 诊断语义或布局确实变化时升级唯一格式版本；历史原包保留，不建立兼容运行路径。
