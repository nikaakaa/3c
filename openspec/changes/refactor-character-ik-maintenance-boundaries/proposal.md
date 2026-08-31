# Change: 收紧IK结果、历史状态与诊断映射的维护边界

## Why

当前IK已经形成唯一Foot Placement、Goal Assembler、FBBIK和Writer，Transition、Target、Interpolation与Pelvis也有明确实现入口。需要保留这条主链，不按文件行数重拆系统。

维护成本主要来自三处：

1. `CharacterFootLifecycle.Evaluate`先返回Resolved、过程Motion与完成凭据，Module在Pelvis裁决后再次完成Landing，保存Resolved后仍可能夹紧Goal。下游不能仅凭“最终结果”类型确定真正提交的目标。
2. Interpolation从过程`CorrectionResponseFact`读取上一帧方向；Solver清空根Bank的BendHistory后仍可能读取Vendor旧`bend.direction`。运行历史、过程证据和第三方工作字段的归属没有完全分开。
3. 响应字段在多份过程记录、公开Diagnostics、CSV列名、位置写入、Analyzer解析和必需列清单之间重复转抄。增加解释证据需要多处人工对齐。

本提案只处理上述维护边界。小楼梯与斜坡的Ground Path误判已单独定位，但用户已要求暂不讨论，本change不包含该修复。

## What Changes

- 把Foot内部的“脚需求”“裁决凭据”“最终脚结果”严格分型。Primary Support与Pelvis消费同帧typed脚需求；唯一Foot收口阶段在既有Pelvis和Reach处理完成后发布最终Resolved Pair，Goal只编码该结果。
- 保留已经收口的`ResolvePelvis -> AdvancePelvisResponse`、现有公式、权重、准入和配置。仅搬移既有Foot Reach夹紧的责任归属，不新增一次Pelvis响应或另一份Foot插值。
- 将下一帧真正使用的方向与数值放进最小typed运行状态，过程Fact只记录本帧变化。正式清空Solver历史时，从同一Rig参考姿态准备结果重新建立方向，不读取重置前的Vendor工作字段。
- 过程证据按响应、接触、支撑与可达职责分组，删除同义平铺副本；Editor为当前采样格式建立唯一typed列绑定，由同一绑定驱动列名、写值、读取和必需列校验。
- 继续使用唯一Sampler、Analyzer、Publisher及当前评分政策；删除被替换的类型、字段和手写映射，不新增旧格式reader、自动补列或并行实现。

## 范围

### 包含

- Corin正式Foot链内部的请求、结果、完成凭据及Goal编码关系。
- Interpolation运行历史与过程证据分责。
- FBBIK初始化及明确清空Solver历史后的方向所有权修正。
- Foot诊断记录分组和Editor采样读写映射统一。

### 不包含

- Ground Path、CapsuleCast、地面上侧轮廓算法、场景碰撞和Layer配置。
- 新的脚状态、Landing/Contact政策、位置basis、Weighted Goal Sole历史接管、Foot Height或作者曲线。
- 正在进行的Pelvis软姿态偏好、压缩余量、频率和清速度实验；不得把本次整理变成新的调参实验。
- 动画方向取反策略、PoseBone零位移Goal跳过规则、各Effector旋转能力等其它求解行为问题。
- Pose Program、外层Frame Transaction、PostCommit诊断异常政策、通用节点ABI或Final Writer架构重构。
- TrainingEnemy、装备内容接线、Gameplay、KCC、Network、Rollback及新的Solver。

## 行为变化与验证

结果分型、运行状态搬移、诊断映射迁移属于结构整理，必须保持相同正式输入下的Goal、Pelvis、状态、Bend与最终骨骼行为。最终Resolved的含义会收紧为Reach处理后的正式目标，旧的夹紧前值改为请求证据，不能伪装成同语义字段。

“清空Solver历史后仍继承Vendor方向”属于独立行为修正，只允许其明确初始化边界发生变化；不与普通帧膝盖算法修改混在同一个提交里。使用已有录制、正式回放和只读离线对账，不新增测试工程或临时运行链。提案阶段不编译、不Build、不运行Unity。

## 与现行规格对比

| 当前合同或active change | 对账结论 | 本提案处理 |
|---|---|---|
| current `character-foot-placement-presentation`：唯一深模块、唯一根事务 | 一致，继续保留 | 不新增外部阶段和独立提交；只在模块内部区分请求与结果 |
| current：`Resolved Foot必须形成紧凑下游合同`与`Pelvis必须只消费Resolved Foot Pair` | 名称和时序有冲突：Pelvis参与最终可达裁决，不能又要求它先读取裁决后的最终结果 | delta明确引入内部typed脚需求，并重命名Pelvis requirement；最终Resolved只在裁决后发布 |
| current Foot诊断文字写为从Committed事实“深冻结”，animation pipeline要求Writer前冻结Pending、Seal后发布 | 冻结时机表述不一致 | 收紧Foot requirement为计算阶段捕获证据、Pending冻结、Writer补写实际输出、Committed只读发布；不改外层Fault政策 |
| current `character-animation-pipeline`与Pose Graph：Vendor字段不能成为跨帧真相 | 一致，但Reset路径尚未落实 | 增补正式初始化方向与清空历史的可验证场景 |
| active `stabilize-character-foot-path-and-landing` | 同时修改Resolved、Pelvis合同，另有尚未完成的Goal Sole历史与位置basis任务 | 本提案只接管请求/结果语义收口；不实施那些行为任务，实施前必须合并重叠条款，不能两套定义并存 |
| active `refine-character-pelvis-response` | 唯一响应已落地，新的行为实验仍在进行 | 以用户接受的最终实现为输入，完整保留公式和参数；不重新实现、挑选或撤销其候选 |
| active `consolidate-foot-diagnostic-scoring` | 统一评分与本次统一字段映射不同 | 保留唯一评分Owner、维度、权重、分母、缺失语义和原始历史包 |
| active `refactor-character-pose-graph-architecture` | 已计划外层Module、Program、Publication与Diagnostics Projector迁移 | 本次仅处理Foot内部和采样映射，不创建第二套外层协调器；后续重构消费本次收口后的合同 |

本轮只新增独立提案，不修改current specs、`project.md`或其它active文件，不把设计写成已经安装的事实。实现与归档时须按`design.md`的所有权对账，把旧“Pelvis读取最终Resolved”的条款替换为新合同，防止其它delta后归档又覆盖回来。

## Impact

- 规格差量：`character-foot-placement-presentation`、`character-animation-pipeline`、`character-presentation-pose-graph`。
- Runtime：Foot内部合同、Lifecycle收口、Pelvis请求适配、Goal编码、Interpolation历史、FBBIK方向准备和重置。
- Editor：Foot过程诊断投影、Sampler列绑定、Analyzer解析与共享格式identity。
- 不改变Pose Graph端口、Goal Contribution/Goal Set ABI、Rig作者数据或Profile字段。若实现发现必须改变这些外部合同，应报告超出范围，不自行扩大迁移。
- 诊断语义或布局确实变化时升级唯一格式版本；历史原包保留，不建立兼容运行路径。
