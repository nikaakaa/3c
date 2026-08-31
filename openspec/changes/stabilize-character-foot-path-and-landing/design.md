# Design: 在唯一Foot事务内稳定Path并保证Landing可达

## Context

### 当前单步：Locked Sliding世界误差响应

当前用户批准继续反弯独立实验，保留160901认可行为及192218恢复结果；203023仅加载新诊断规则，原始Foot、Pelvis和Solver数值与恢复包一致。本轮只修改唯一FBBIK内可靠动画膝向的有符号腿轴运输，保留退化历史、全部权重、Foot/Pelvis目标和唯一Writer，不恢复已拒绝的SmoothKnee后处理或Reach强压。以下Sliding及高度响应条款保留各阶段历史，不能据其早期草案改动当前已认可脚链。具体实验和同输入验收见[有符号动画Bend实验](experiments/20260830-signed-animation-bend.md)。

### 2026-08-30位置响应坐标候选

当前处置：141256位置轴候选与150516加权Goal参考组合均未通过完整质量约束。34c9974将代码、Profile及Corin生成产物精确恢复c519865内容，Diagnostics经1a81927、8812436恢复facts52/diagnosis21。155326恢复Replay已封口：2086行1140列中1116列与130545逐值相同，24列只为运行/Surface/Path身份；全部物理输出、状态、37个Target及60.4分恢复。官方Proof对上一候选保留7个版本身份差异、帧分歧0，直接对130545则Runtime身份和1044条frames一致。下述位置basis和Committed参考是已验证部分机制、但尚未接纳的设计方向，不代表当前运行实现。原13:05:45基线仍有踏空与反弯，恢复不等于修复，更不是认定它优于20:43历史版本。

本轮以c519865与130545恢复Replay为基线，只替换Correction Response的位置坐标合同。纯稳定Left412–414中，Applied Support Direction为Up→10°斜向→Up，标量欠账被旧`O=D+N(c-q)`变成0→-10.315毫米→0的额外Z；三帧无Path Revision、Target Tracking或Rotation Goal Weight。新ZZZ exact证据表明位置在PIK组件所属坐标系执行`F+LocalY*c`，N另参与目标几何与旋转；原先由Up特例推导位置沿N的结论撤销。

3C的目标是Sole Center，不是ZZZ Foot pivot。本轮不复制其`desiredRaw`、g/k/W、幅度折返或owner-Y选速率。PoseRig从实际`Binding.Animator.transform`的同帧矩阵捕获位置basis；这个PoseRoot与CaptureFoot、最终Goal编码相同，不借Body Up、GroundPath Up或默认轴。令L为owner到world线性部分、G为world到owner，s为`|L*LocalY|`，a为`L*LocalY/s`，h为`rowY(G).xyz*s`。有限可逆矩阵、s>0、a单位长与h·a≈1为必需合同；h不是单位方向，禁止归一化。

令B为本帧动画Sole、D为已选Target加完整WorldResidual后的Desired：`q=dot(D-B,h)`，沿用以世界米为单位的c历史与1.8/1.5m/s增减速率，`O=D+a*(c-q)`。正式VisibleOutputTransfer也必须用h重基scalar；WorldResidual仍从完整上一世界输出捕获并在同帧Advance，不删切向分量、不改Capture起点。公式等价于`G(O).xz=G(D).xz`及`G(O).y=G(B).y+c/s`。它只消除Support Normal直接旋转位置欠账，不承诺Owner本身换Up/尺度时绝对世界连续；非均匀矩阵的scalar数学成立也不代表整套脚掌旋转已经覆盖非均匀父级。

Support Direction保留原10°历史与正式lineage，只生成现有Foot Rotation；配置激进改名为`SupportDirectionMaximumChangeDegrees`且保留10，Profile升至v36，旧名不兼容。Ground/Target Height仍沿原Component Up。Lifecycle继续按正式Position/Rotation Weight混合并反解Ankle，使有效Heel/Toe中点保持目标Sole；不把Contact改成旋转满权，不改变FullAnchor/Sliding/Release或Reach准入。Profile身份变化必须显式重建Corin产物并原样记录Proof身份差异，TrainingEnemy不修改。

Runtime已构建通过的候选不等于视觉通过。实验必须同时核对稳定Swing额外XZ、垂直跳变、全部接触/穿透、膝盖、Pelvis、Reach和世界Anchor；不能只凭12个靶窗口消失接受。基线和后续采样记录见[位置响应坐标实验](experiments/20260830-position-response-basis.md)。本轮不修改原有proposal/project未提交内容；其中旧沿Support Normal的位置推断以本段新证据及delta spec为准，候选接受后再精确同步，不能混入他人的整文件改动。

当前重构已经把Landing Lifecycle、Ground Path、Anchor、Support、Pelvis与Goal收进唯一`CharacterFootPlacementModule -> Resolved Foot Pair`根事务，并完成纯Transition Resolver、唯一Transition Runtime、纯State Target Resolver、统一Interpolation Runtime与Post-Interpolation Constraint阶段拆分。旧`CharacterFootStateMachine`、分散Residual/Progress和兼容入口已经删除；这个唯一生命周期和Owner边界继续保留。Prediction已经以零诊断回归接管唯一KCC Future Translation，当前剩余重点是Landing/Lock垂直接管仍可绕过Interpolation，以及正式Foot Motion、Support、Reach和Contact/Lock旧语义尚未全部迁移。

诊断显示三类问题具有不同边界。Path侧已经证明identity单独触发Residual重置不合理，但删除该触发后用户观察到的整体抖动基本不变；`20260828-184607-138-d23494c9824a42a89c9973d567305442`进一步证明同Event、同RootLocalLanding在720度/秒转向下因瞬时世界速度变化令Future Body Translation单帧移动约36厘米，查询切换Surface，Next Landing移动约39.7厘米并由Post Constraint产生约17.5厘米Correction跳变。`20260829-084258-427-2e96ab5155fd4730a74be4732c90493f`已经让Prediction以零诊断回归进入唯一运行链，因此当前首要断点转为Landing/Lock垂直接管：Acquire进入帧与Ground Constraint都能绕过Interpolation同帧抬脚。Landing直腿则仍是Foot Goal在Pelvis无有效Reach协调时超过腿长，FBBIK只能把膝盖夹到伸展极限。设计必须分别处理Prediction时间连续、Landing提交、垂直视觉连续、允许的Ground穿透预算和腿链可达，不能用一个最终Pose平滑器互相交换问题。

## ZZZ PIK参考口径

`D:\ZZZ_Dump\PIK分析包`作为本change的行为基准。运行证据入口固定为`CSV全量证据复盘.md`与`CSV全量证据审计.json`，证据等级与静态边界固定为`证据审计与勘误.md`、`代码边界与完整性清单.md`、`PIK精确函数审计表.md`与`平滑层审计.md`；旧重构C++、固定窗口ASM和“完成报告”只作历史材料，与上述入口冲突时不得使用。CSV是字段级轮询Trace，Raw是单帧快照，均不得伪装成函数入口调用栈或最终Playable Pose证据。

本change对P0/P1已经确认的控制结构采取“默认照搬”原则：实现顺序、跨帧状态分离和边沿处理不得另造同类算法；只有项目输入合同确实不同，或同一1044帧Replay证明照搬会回归时，才允许在现有Owner内调整，并把差异、业务取舍和证据写回proposal。照搬的是行为，不是地址、混淆字段名、对象内存布局、构造默认值或PIK单体组件。

项目正式Foot Motion lineage、根Bank双页事务、canonical Observation、完整Ground Path/Envelope、分型Transition/Interpolation/Post Constraint、Resolved Foot Pair、唯一Goal Set/FBBIK/Writer和封口诊断继续作为装配外壳。ZZZ行为必须进入这些既有Owner，不得形成第二Prediction、第二Interpolation、第二Goal链、Legacy路径或全局缓存。

### 证据等级与采用规则

ZZZ资料必须按分析包的正式证据等级使用：

| 等级 | 可采信内容 | 本项目使用方式 |
|---|---|---|
| P0 原始证据 | 当前DLL精确函数边界、指令、字段访问、分支、数组步长和直接调用 | 可定位实现和验证数学；不得从地址直接推导业务名 |
| P1 直接结构 | Foot/Toe脚掌几何到单一位置+法线、当前态到目标态线性混合、边沿/驻留/一次性事件、两份独立速率限制、权重基准混合、固定容量与有限值保护 | 本change默认照搬控制顺序和状态职责，并翻译为typed Event、Profile与根Bank合同 |
| P2 结构推断 | 候选位置、接触状态、旋转参考、组件过滤等中性职责 | 只能帮助决定现有Owner，不直接产生业务字段、状态或默认值 |
| P3 业务猜测 | Plant、Lock、Grounded、Pelvis进阶参数、Stride、Moving Platform等具体名字；Foot/Toe对象绑定已经由静态实例升级为P0/P1 | 不作为本change实现合同；必须由项目正式输入和Replay重新定义 |
| X 错误或冲突 | 固定步到渲染帧相位、世界速度EMA精确公式、候选稳定tie-break、PIK直连tbik/通用Solver等旧结论 | 从Change删除，不得继续作为采用理由 |

### 成熟结论逐项对账

| ZZZ精确结论 | 本项目采用结论 | 当前归属 |
|---|---|---|
| `f`来自当前/下一状态identity和过渡量，执行`current + (target-current) × clamp01(f)` | Formal Approach Progress、Contact、Lock、Support、dominant Source Weight与`0x274`均被排除。当前Final Component Pose已经由Pose Graph完成动画混合；本change不在Foot Placement内补造第二个`f`。未来只有Pose Graph同时发布同一StandardBlend的source/target Foot骨骼实际贡献与双Support lineage时才可迁移该层 | Pose Graph上游边界、State Target |
| `0x171D6920`为每脚目标高度保存历史并执行`delta=clamp(target-history, ±rate×dt)`，同时由历史到目标距离、事件位与Profile模式控制直接采用、限速、冻结与强制刷新 | 每脚唯一Target Height历史保存Accepted Landing沿Up高度；Swing输出为`正式Raw Height + Filtered Landing Height - Current Landing Height`，正常Phase直接通过；Profile显式选择`Direct`时立即采用Landing高度，选择`RateLimited`时中等变化按`MaximumVerticalTargetSpeed`推进，大变化和正式Force刷新内部目标并由后级连续化 | Interpolation Runtime Target Height Stage |
| `0x171D7910`先限制`arr130`支撑方向并独立推进`arr128`；`81DF–8424`由所属GameObject点变换、Foot pivot与N生成局部目标，`85B2–85E2`明确用`F + LocalY × c`产生位置。`arr230=arr228+arr130×arr128`只是Owner Up与N重合等条件下的特例，不证明位置沿N | 本项目保留Sole目标而非复制匿名pivot；从同一PoseRoot有限可逆矩阵生成WorldAxis与dual HeightProjection，用它们处理位置scalar，Support Direction角历史只服务旋转。Profile两档增减速率保持项目政策，本轮不迁移ZZZ的owner-Y选速率、g/k/W或幅度折返 | PoseRig/StateFrame PositionResponseBasis、Interpolation位置响应与支撑朝向 |
| `0x171D8A00`把修正结果按`k=clamp01((1-weight)×globalWeight)`与基准混合，同时让缓存量回归；运行Trace中`0x278`有12个目标窗口、`0x274`有10个响应窗口，138/138次变化精确匹配`5×dt`，扩展字段`0xFC`全部为1 | 项目让`SelectedWorldTarget + PlantWorldResidual`先形成Desired Output，再由PoseRoot位置轴的Correction Response形成唯一Response Output，最后通过现有typed Foot Goal Position/Rotation Weight Owner与动画基线混合一次；可参考Target/Current Weight有界追踪数学，但不新增匿名模式或Goal后处理器 | Interpolation Runtime、Resolved Foot、Goal Contribution |
| B边沿、D组合位、驻留计时和一次性事件分开；全量有393/393个上升/下降窗口，19个快速重入窗口全部满足`0x64>0x60`；`0x60`按dt累加并在边沿复位，`0x64`主要递减重装；`f54`的146个非零行全部从属于下降沿 | 使用正式Contact Event表达Rising/Falling与Same-Event Reentry Refresh；保留边沿Context与Retained Anchor强制刷新，但不复制匿名B/D、`0x54`、`0x58`、`0x64`、`0xC4`或固定时间阈值，顶层五态不增加匿名状态 | Contact Transition Context、Resolver/Runtime |
| `0x252`读取后立即清零，只旁路主标量慢响应并回到共同输出尾段；全量CSV中它始终为0。`0x274`向`0x278`有界追踪已经动态闭合，但外部typed触发动作仍未知 | 一次性Force只保留为未激活静态参考；Target/Current Weight有界追踪只可映射到项目现有正式Weight Owner。Action Pose occupancy不是Goal Owner，不能借匿名Force或权重字段触发Hard Ownership Loss、重置历史或建立fallback | typed Policy、Interpolation ownership、Goal visibility |
| Foot与Toe世界姿态生成脚掌多点查询，先解析唯一`FootTargetPosition + SupportNormal`；只有FootL/FootR与Pelvis writer，没有Toe writer | 使用Final Animation Pose与Rig Calibration的Heel/Toe接触几何，在现有World Query Backend内生成固定容量Current Support Observation，解析唯一Position+Normal并只写现有Foot Goal；不照搬六次调用、半径、宽度或无稳定tie-break的候选顺序 | Current Support Observation、State Target、Resolved Foot |
| 无有效候选只跳过本次新候选调整，通用输出尾段仍继续 | Rejected Observation不提交新Landing，但不得把整帧Foot状态冻结；既有Accepted Landing、Interpolation和输出仍按正式合同推进 | Observation、Landing Context、Interpolation |
| 候选分支严格选择更高Y，但等高候选没有稳定tie-break证据 | 项目继续使用canonical最近合法Surface和稳定identity；不复制“最高Y”候选政策 | World Query Adapter、Observation |
| 主求解存在标量死区、有界响应和带符号速率限制，但输入不是已证明的世界速度Vector | Prediction Motion继续采用本项目Replay已经证明的`Body Current + Timeline Continuation`合同；不得再声称它是照搬ZZZ世界速度EMA | shared Prediction Motion State |
| 热路径固定缓冲、数组边界和NaN/Inf保护 | 现有根Bank、固定容量Observation/Workspace继续作为更强等价实现；新增Prediction、Landing和Pelvis状态也必须固定布局、无每帧托管分配、入口有限值校验、容量溢出typed失败 | Runtime storage、validator |

本change的ZZZ迁移主链因此固定为：

```text
Accepted Candidate / Verified Anchor
-> typed事件与正式Support Target选择
-> 每脚目标高度采用政策
-> current/target状态混合
-> 换代时捕获并衰减完整Plant World Residual
-> 每脚Support Direction按显式最大角变化推进
-> 每脚米制Correction Response标量沿PoseRoot局部Y对应世界轴双向限速
-> 既有Foot Goal权重混回动画基线
-> 唯一Goal Assembler与FBBIK
```

当前正式实现已经删除raw Contact累计PlantBlend并接入Heel/Toe Current Support、独立Correction Response与Target Height显式采用模式，但`20260830-022607`证明两Probe的`纯Component Up最大位移 + 同一selected raw Normal`不是ZZZ的24B几何等价物；单帧斜Normal又被项目自创`BasisTransferred`投影放大为24厘米水平反切、1.218腿伸长比与12.5厘米FBBIK残差。Direction History与标量Correction Response必须按上述P0/P1顺序闭合，`BasisTransferred`整条删除；Current Support Position+Direction的多点几何仍须继续恢复，不能把现有临时双Probe公式升为最终架构真相。正式Owner收敛为`TargetHeightHistory / PlantWorldResidual / SupportDirectionHistory / CorrectionResponseHistory / PlantTarget`。Prediction、Pelvis进阶字段、Knee、Moving Platform和PIK到tbik/通用Solver的连接仍不属于已确认主链；除本change已有项目证据支持的能力外，不用未闭合推断扩大范围。

### 实施就绪边界

| 结论 | 证据 | 实施裁决 |
|---|---|---|
| Foot/Toe世界姿态生成脚掌查询，解析单一Position+SupportNormal，只写Foot与Pelvis | P0/P1静态闭环 | 可以开始；翻译进现有Pose Input、World Query、Resolved Foot和唯一Writer |
| 可琳Target Height可直接换代；`arr130`限角与`arr128`限速是不同历史，位置沿Owner局部Y，Raw的Up等式只属有限特例 | P1 exact汇编、已闭合T/G/F与writer reader、既有Trace | 本轮以项目Sole合同实施PositionResponseBasis候选；Direction角配置改为SupportDirectionMaximumChangeDegrees，数值10°与1.8/1.5m/s均不变。完整Replay前不声明质量完成 |
| 当前态/目标态Position+Normal按Transition Weight混合，随后Correction与基准权重混合 | P0/P1静态闭环 | 可以开始；使用现有typed Transition、Interpolation与Goal Weight，不创建通用Tween |
| Heel/Toe精确Probe形状、数量、半径、宽度与等高顺序 | 外部查询调用P0，typed方法和稳定tie-break未知 | 不能照抄常量；使用项目正式Support Probe/Profile和稳定identity政策 |
| `0x199`采样对象业务名、D/B/`0x54`/`0x58`具体业务含义 | 双档与边沿数学、全量活数据流P1，业务映射P2/P3 | 不能照抄字段/状态；项目只使用明确Desired Response增减、Contact Event、Retained Anchor和Profile政策 |
| `0x252`外部触发、`0x274/0x278`外部业务 | `0x252`只有setter/数学且全量为0；`0x274/0x278`已有12/10个窗口与138/138次`5×dt`动态闭环，typed触发未知 | 不实现匿名Force/模式；只用已闭合数学核对现有Foot Goal/Position Weight Owner，Action occupancy不得成为第二Goal Owner或Hard Ownership Loss |
| Foot Direction角限制 | `0x171D7FE6-0x171D81DA`读取旧`arr130`、比较实例`+0x70`并改写本次Direction；可琳活体`+0x70=10°`，P1 | 直接进入唯一Interpolation Direction History；不是Rotation Writer、世界输出投影或Final Pose低通 |
| Plant冻结、移动平台、Knee和最终Animator/Playable权重 | P2/P3或调用尾未闭合 | 不阻塞当前主链；不得借此增加第二Rotation Writer、第二Solver或Final Pose低通 |

## Decision 1: 唯一正式Foot Motion Runtime Frame

Projection Compiler在`build-character-foot-motion-data-foundation`归档后，从原生AnimationClip Catalog和匹配Foot Analysis lineage降低唯一Foot Motion payload。Source Runtime按与Component Pose相同的Live Contribution、Source、Cycle、Normalized Time与Completion生成左右脚typed Sample；离散Lock Mode不得跨Source混合。

Foot Placement Pose Input只接受这一个Frame。缺失完整Curve、Event table、Source lineage或Contribution归属时整帧typed invalid，不读取旧Artifact字段、旧隐藏Feature、默认值或另一动画Source补全。

Step Event table由正式Step Time边界、Step Distance、匹配Artifact中的RootLocalLanding与稳定source/cycle/side ordinal共同编译。Step Distance只与同脚相邻Motion-space Landing的水平距离对账：循环首个Event展开上一周期，有限Clip首个Event使用素材起点。RootLocalLanding来自同一Event ordinal/sample的Target VisualRoot-local落点，不能跨时刻直接相减代替素材步长。编译器以0.1毫米几何与0.1毫秒Landing边界容差验证Artifact重建，不作为作者运行调参。Runtime不读取Library Artifact；Editor Build只把已经严格对账的结果发布进Projection，不再保存未消费的单值时距曲线副本。

## Decision 2: 消费者按依赖顺序迁移

正式顺序固定为：

```text
Path逐阶段归因
-> 首个不连续阶段修复
-> 拆分State / Transition / Interpolation / Post Constraint
-> Step Time/Distance接入Prediction
-> 共享Prediction Motion稳定
-> Approach Plant目标持续准备 / Contact Verification / Anchor冻结
-> Foot Height接入Swing
-> Support接入Primary Support/Pelvis与非对称速度边界
-> Landing Reach闭合
-> Contact/Lock接入Transition、State Target与Interpolation
```

每一步只切换一个业务定义。对应旧字段在同一步删除；未轮到的正式字段可以存在于不可变Frame，但不得影响行为。架构拆分已经完成并继续保持单一Owner。Step Time先成为Prediction唯一时域，再驱动已经连续的Residual截止；共享Prediction Motion只稳定committed Body Target当前速度与移动计划Continuation，不修改正式Step Time、RootLocalLanding、Visible Rotation或KCC世界碰撞。随后让Approach Contact继续Tracking并在唯一Interpolation中准备Plant目标，首次正式Contact Rising再以一次Plant Verification建立Anchor；稳定Plant冻结Anchor。只有该边界准确后才消除Acquire与Ground Constraint绕过Interpolation的同帧抬升并安装穿透/Lock门控，再迁移Foot Height。Contact/Lock最后清理旧语义，因为延长Landing前必须先让Swing接近Verified Anchor并让Pelvis拥有有效Support/Reach输入。

## Decision 3: State、Transition、Interpolation与Post Constraint分层

每只脚继续由一个根事务处理，但帧内顺序固定为：

```text
Immutable Foot Input / Observation
-> Pre-Interpolation Transition
-> State Target
-> Unified Interpolation
-> Post-Interpolation Transition
-> Post-Interpolation Constraint
-> Resolved Foot
```

`CharacterFootDiscreteState`只表达当前业务归属：`Swing / UnlockedSupport / Landing / Locked / Releasing`。Lock的`Sliding / FullAnchor`继续是Locked内部的typed响应模式，不扩展成第二套顶层状态。状态本身不得拥有`Enter/Exit`副作用、计时器、Residual或World Query。

顶层五态保持不变，不增加`Rebound`、`Blocked`或另一套Grounded状态。根Bank内部新增分型`Contact Transition Context`，只保存上一帧正式Lock请求、距最近Contact边沿的秒数、最近Contact Event identity和最近释放的Contact Event identity。`Contact Rising / Contact Falling / Same-Event Reentry Refresh`只属于本帧不可变Transition事实或Reason，不成为持久顶层State。相比PIK依赖匿名输入与驻留阈值，本项目还拥有正式Event和Anchor lineage：同Event在Releasing期间重新请求Lock时必须强制刷新Transition与Target，但只可复用仍保留的Verified Anchor；Release完成、Anchor已清除后不得复活旧Event。紧接着到来的新Event必须执行自己的首次Plant Verification，不受上一Event回弹事实影响。

`CharacterFootTransitionResolver`是纯决策器，只读取不可变Frame、Observation与上一Committed typed Context，返回固定`CharacterFootTransitionDecision`。Decision至少包含Source、Target、Reason、Event lineage、执行Phase、Anchor Command与Interpolation Policy identity。允许边固定为：

```text
Swing -> Landing | UnlockedSupport
UnlockedSupport -> Landing | Swing
Landing -> Locked | Releasing
Locked -> Releasing
Releasing -> Landing | Swing
```

输入驱动的边在Pre-Interpolation阶段执行；只有依赖本帧插值完成事实的边在Post-Interpolation阶段执行。`Releasing -> Landing`只允许同Event Contact Rising、原Verified Anchor仍保留且Reach/Lock准入继续合法时在Pre阶段执行；它必须Retain原Anchor并从当前Effective Correction连续重入，不得Create Anchor、重查Landing或把Interpolation清零。`Releasing -> Swing`属于Post阶段，完成后必须用Swing输出分类执行同帧Post Constraint。系统不得循环求Transition直到稳定，也不得让状态目标或插值器暗中改State。唯一Transition Runtime负责应用Decision、改写离散State、执行Anchor Create/Retain/Release命令并记录原因；其他模块不得写这些字段。

Contact边沿事实 MUST由同一Resolver按上一Committed Contact Transition Context与本帧正式Contact、Lock Mode和Event identity纯计算。唯一Transition Runtime在应用Decision时同时更新该Context；State Target、Interpolation、Post Constraint与Diagnostics不得写回边沿历史。Reset、Retarget、Source lineage变化和根Bank Discard分别按现有事务语义清空或保留Committed Context，不得在失败帧推进边沿秒数或消费Event。

`CharacterFootStateTargetResolver`按已经确定的State纯计算本帧目标，输出目标Correction、Reference、Contact/Support/Reach意图和固定typed Interpolation Request。它不得读取或推进Delta Time、Residual、Progress与上一输出，也不得查询世界。Swing/UnlockedSupport目标只来自Ground Path、Envelope与正式Foot Height；Landing/Locked目标来自唯一Anchor和正式Contact/Lock；Releasing只回到原始动画Swing目标。

`CharacterFootInterpolationRuntime`是Foot连续链的唯一所有者。它只接受`Previous Interpolation State + State Target + typed Policy + Delta Time`，持有一份统一Interpolation State并发布Output、Residual和Completion。现有Swing Residual、Acquire Residual、Release Residual与散落的HalfLife推进必须迁入这里；政策固定为Swing跟随、typed Support Target换代、完整Vector换代连续和Residual Half-Life等有业务含义的typed策略，不提供string key、字典注册、任意曲线回调或项目级通用Tween。Approach Prediction只准备Target，不建立可见Takeover Tracking。

Interpolation State内部必须把每脚唯一`Target Height History`、完整Vector `Plant World Residual`、`Correction Response Direction History`与标量`Correction Response History`分开。Target Height History保存Accepted Landing沿Component Up的世界高度，不保存包含动画Phase的完整Swing Raw Height。Swing Raw Height仍由`Ground Envelope + Formal Foot Height`产生，过滤后输出固定为`Raw Height + Filtered Landing Height - Current Landing Height`；因此同一Ground Path只因动画Phase推进时直接通过正式曲线。Requested Direction来自State Target选择的`CharacterFootSupportTarget.SupportNormal`，Direction History先从上一Applied Direction朝Requested Direction限制一次角变化，Applied Direction只服务Foot Rotation，位置响应使用独立PoseRoot basis。Position与Requested Normal必须保留各自正式lineage。没有Pose Graph source/target双Support事实时不得再次混合Final Pose或从Foot Event推导权重。

Foot Motion Profile必须显式序列化`TargetHeightAdoptionMode`、`MaximumVerticalTargetSpeed`与`TargetHeightForceRefreshDistance`并纳入Revision。`Direct`模式在合法Landing Height换代时立即更新History，由后级World Residual和Correction Response保持可见连续；`RateLimited`模式下，同Event累计高度差不超过`PathRevisionDistance`时发布`HeldWithinRevisionDistance`并保持Applied Delta为0，超过该距离且未达到Force Refresh Distance时才以`MaximumVerticalTargetSpeed × Delta Time`追赶，累计差达到Force Refresh Distance或Event正式换代时立即更新History并由后级接管。Corin迁移首先使用可琳激活实例已经实测的`Direct`模式；不得把匿名D位、`0x54`或`0x58`复制成顶层状态。Approach/Plant取得Interpolation所有权后，Swing Target Height更新必须发布typed Held，Held期间Next Swing Event只能提供本帧Raw Swing Target，不得改写或解释Current Plant拥有的Target Height identity/value；Plant Target沿同Event继续这份Landing高度历史。

Formal `ApproachContactToLandingProgress`只记录Prediction准备进度，不进入Position、Direction、Residual、Correction或Goal权重。正式Contact Curve只提供接触证据与边沿；旧运行时累计`max(previous, Contact)`和Prepared Plant可见混合整体删除。Approach期间普通Swing继续消费202551动画XZ、Ground Path Envelope与Formal Foot Height，Prediction只更新同Event Prepared Target及lineage。旧Current Contact Event的Prepared Target与下一Swing Event已经同帧并存时，下一Event仍可更新Prediction、Observation、Landing Context与Ground Path，但不得成为当前State Target、Interpolation、Rotation、Reach Goal或Ground硬最低约束；当前输出改为本帧Current Support完整Target，Prepared Target只拥有Post Constraint的当前接触测量。两者都不得把未验证Prepared Landing变成可见Plant目标。Contact Rising完成Current Contact Verification后，State Target才一次换为Verified Anchor的Position+RequestedDirection；Runtime以持久上一实际Response Output和新Target捕获一次完整Vector Residual，并在同帧继续Advance。Verification失败不得消费未验证Prediction，Lock Weight只负责Contact后的Rotation可见响应、Full Lock完成资格与Release边界，不驱动Position Target。Target Event、Target Kind、Lock Response、Verification、Direct Follow、State/Response、目标点或强制高度刷新发生正式换代时允许Residual Capture。`DesiredOutputPoint = SelectedWorldTarget + PlantWorldResidualAfterDecay`只表示本帧内部期望，不再直接当成最终Foot输出。

Correction Response Stage每个合法可见帧先读取Requested Support Direction与上一Applied Direction。首次合法输入直接采用Requested Direction；其后计算夹角并用Profile显式`SupportDirectionMaximumChangeDegrees`把本次Applied Direction限制为最多朝目标转该角度。Corin值采用可琳活体实例实测的每次10度。Runtime随后以`DesiredResponse = dot(DesiredOutputPoint - OriginalSole, PositionResponseHeightProjection)`计算标量目标，并持久保存上一Committed Response、初始化事实和typed增减方向。它在Swing、UnlockedSupport、Landing、Locked与Releasing每帧恰好执行一次；Applied Direction同时服务本帧Foot Rotation，不得以Component Up、Animated Up、上一法线或默认Up补全无效Current Support。Direction变化只推进Direction History并保持原Correction scalar，严禁恢复`BasisTransferred = dot(previousWorldOutput - currentOriginalSole, newDirection)`；该投影会丢失切向世界差，已由022607 Replay否决。只有正式Position Target Capture且根Bank已有上一Post Constraint/Post Reach的Weighted Goal Sole参考时，完整Vector Residual与标量Response才执行一次`WeightedGoalSoleTransferred`。该参考在Solver之前由最终Goal权重推算，不是最终物理Sole，零权重时尤其不能混同。未初始化、Reset、Retarget、Source/Profile/World lineage失效后的首次合法输入直接同步；普通动画目标变化、同Event Prediction换点、Contact Verification、Action Pose Contribution、攻击、Lock Response换代、Release完成和Same-Event Reentry不得清零。已初始化时固定执行：

```text
delta = DesiredResponse - PreviousResponse
rate = delta >= 0 ? CorrectionResponseIncreaseSpeed : CorrectionResponseDecreaseSpeed
CurrentResponse = PreviousResponse + clamp(delta, -rate * PresentationDelta, rate * PresentationDelta)
ResponseOutputPoint = DesiredOutputPoint + PositionResponseWorldAxis * (CurrentResponse - DesiredResponse)
```

最大Direction角变化与两档标量速度必须由Profile显式序列化、有限、为正并进入Revision；Corin采用活体实测的每次`10°`及`1.8m/s`、`1.5m/s`。以Desired Response增减选择速率是项目typed映射，只继承`0x199`已证明的双档结构，不声称复原其匿名采样对象业务身份。旧`MaximumVerticalCorrectionSpeed`、`BasisTransferred`世界投影、稳定帧逐帧上一世界输出重表达和对全部Plant帧使用单档`0.6m/s`的链继续保持删除。`20260829-144901`否决的是错误输入、参数与作用域，不得再用它否定一手Trace已经证明的Direction与标量双历史。`0x252`在全量运行样本中保持0，不能产生项目Force开关；`0x274/0x278`的Target/Current Weight追踪虽已动态闭合，但typed触发未知，只用于核对现有Foot Goal/Position Weight Owner。Action Pose Contribution是动画基线，不是竞争Goal Owner；它不得触发Hard Ownership Loss、Anchor释放或Interpolation reset。`animation.foot-placement-weight`只在Response Output之后控制既有Goal可见权重，即使为0也不得清除连续历史。

最后的动画基准混合只由既有Foot Goal/Position Weight对`ResponseOutputPoint`执行一次，不再建立有历史的第三平滑层，也不在FBBIK或Final Pose之后低通。三份连续状态只可由各自明确的Reset、Retarget、lineage失效或Policy退出规则清理；同Event业务更新不得用一个无类型Reset同时清空。Swing/UnlockedSupport继续以Accepted Ground Envelope作为不可延迟的硬下界，Release继续使用统一Residual。`AcquireByWeight`进入帧不得对Contact Anchor调用`RaiseToMinimum`，Lock Weight达到1也不得把未收敛Residual或Correction Response清零。

Foot Hard Ownership Loss只由`!Grounded || !CurrentStep.IsAuthoritative`形成。Action Slot的Live Pose Contribution、`SourceActionInstanceId`与左右脚Pose Weight都只描述动画基线来源；它们不是Foot Goal ownership token。Action开始、结束或持续占用不得释放Anchor、进入Suppressed特殊路径、清空Interpolation或阻断仍有非零Goal Weight的Landing Reach。作者`animation.foot-placement-weight`可以把最终Goal可见权重降到0，但同一有效Foot Motion/World lineage下连续状态仍推进，Action退出时不得从default历史重新启动。Stride/Pelvis若要在全身Action中停用，必须由独立显式作者Policy表达，不得继续借用Goal Ownership命名或影响Foot Reach安全。

根`CharacterFootStateContext`收敛为一组分型数据块：`Discrete State Context`只存当前State与最近Transition，`Contact Context`只存Anchor和Lock响应，`Contact Transition Context`只存边沿与已消费Event历史，`Interpolation State`只存上一目标、Target Height、换代Residual、Correction Response、Effective Correction与完成事实，Landing与Observation继续使用各自typed Page。所有数据块仍由同一个Pending/Committed根事务一次Seal或Discard，不建立独立生命周期。

Post Constraint只在插值后消费结果，但按状态承担两种明确责任。Swing/UnlockedSupport必须继续把Accepted Ground Path Envelope作为硬最低约束，防止一个仍可到达的Swing因为Residual或目标采用政策落到地形下；这项约束不参与Landing/Lock垂直接管。Landing/Locked的Verified Anchor部分只测量连续输出的穿透深度、判断是否位于`GroundPenetrationTolerance`内并发布`GroundCatchup`与Full Lock门控，不得调用`RaiseToMinimum`、修改Effective Correction或写回Interpolation历史。Contact接管允许不超过容差的轻微穿透；若状态交接继承超预算Contact误差，输出必须继续由同一Correction Response向Verified Anchor收敛，期间不得Full Lock。Landing Reach和有限值边界仍可硬夹紧不可达Goal。Ground测量与Reach夹紧都不得回写State Target、Residual或Transition。

## Decision 4: 先定位Path同帧放大，再分离连续目标与Envelope安全

FutureLanding世界事实固定拆成`Committed Body Target Current + 移动计划Continuation -> shared Prediction Motion State -> KCC Future Body Translation -> Raw Landing Candidate -> Query Admission -> canonical Landing Observation -> Landing Tracking -> Approach Plant Target Preparation -> Contact Verification`。Prediction Motion State属于Foot根Bank且左右脚共享一份；它不得进入Gameplay、World State、rollback或网络packet。状态至少保存初始化标志、稳定当前速度、稳定Continuation速度、移动计划Generation、Body Reset Sequence与Prediction Source identity，并随根事务Seal或Discard。

Prediction使用本项目Replay已经证明的正式控制律，不把ZZZ主求解的未命名标量响应解释成世界速度算法。当前目标取committed Body Target世界速度，Continuation目标取committed移动计划下一段世界速度；两者分别计算`TargetVelocity - StableVelocity`。差值不超过Profile显式`PredictionVelocityDeltaThreshold`时保持稳定速度，超过时按`PredictionVelocitySmoothSpeed * PresentationDelta`执行有界EMA响应，再把结果限制到`PredictionMaximumSpeed`。三个配置必须为有限正值、纳入Profile Revision且由Corin正式序列化；缺失或非法时整项typed unavailable，不提供默认值。首次合法输入直接以对应正式速度初始化，避免从零产生启动滞后；Body Reset、Retarget、移动计划Generation或Prediction Source变化清空状态，普通Landing Event、Animation Source和左右脚Step换代不得重置角色级Prediction Motion。移动计划Current Velocity只作为诊断对照，不得替换KCC当前运动起点。停止边界缺失移动计划时保持上一Committed Prediction状态且不生成本帧Future Translation；显式静止计划的生产侧闭合不在本change实现，Foot Placement不得补零。

唯一KCC Future Body Translation继续负责真实世界碰撞，只是请求中的当前与Continuation平面速度改为稳定速度。左右脚按各自正式Step Time读取同一Pending Workspace；RootLocalLanding仍只乘本帧Visible Rotation，不预测Future Yaw。Prediction不得复制KCC、创建低速普通路径或在KCC结果后另做位置低通。

Raw Landing在`Tracking`阶段仍从每帧不可变Frame Input重新投影；Committed Observation Page保存上次真实查询使用的Side、Landing Event、Source Sample、Source Cycle、按1毫米量化的Raw Landing、按`1e-4`量化的Component Up、Profile Revision与World Revision。当前Candidate与该查询快照的世界位移累计不超过`PredictionInputAccumulationDistance`且Up夹角不超过`ComponentUpChangeAngleDegrees`时复用同一Committed Observation Page，不更新累计基准也不查询。

Corin显式使用5厘米累计位移和1度Up角度。距离配置必须为正且不得超过Landing Sphere半径；因此本change不采用10厘米。Event、Source Sample、Source Cycle、Profile Revision或World Revision变化不受阈值限制，必须执行一次新查询。正式Foot Lock Mode处于`Sliding`、正在准备接触准入时，只要canonical预测输入identity变化也必须刷新Observation，避免缓存落点误差与动画脚底残差相加后越过8厘米Lock准入；输入identity未变时仍复用，不重复查询。超过任一阈值时同样恰好查询一次；SphereCast使用当前Candidate生成的新canonical Key反量化几何，并只选择canonical最近合法Surface。Accepted与Rejected查询结果都属于不可变Observation；Pending根事务失败时不得提交新Page。

上一Committed Surface、Frame、Authority Tick、Trajectory Generation、Future Translation Source、Foot State、Residual与查询输出不得进入Query Admission、Observation Key或候选选择。上一Surface不得传入World Query或改变候选选择。`Tracking`阶段新查询命中不同Surface时只换代NextSwingLanding，不得覆盖Current Anchor；同Surface才通过独立`LandingAcceptanceDistance`决定是否替换点位。Accepted与Rejected Observation必须保持各自Key和结果；新Rejected Observation不得冒充上一Accepted结果，但同Event Landing Tracking可以继续持有此前已经Accepted的NextSwingLanding，诊断必须同时记录当前Rejected Observation和被保留Landing的原始lineage。查询前累计阈值、1毫米Key量化和查询后Landing接受距离是三个独立定义，不得合并。

Landing Context在现有唯一生命周期内保存三个可并存的typed槽位：`NextSwing Empty / Tracking`、`Plant Target Tracking / Verified`与`Verified LastLanding`。NextSwing属于Prediction Event，Plant Target是项目对ZZZ独立目标历史结构的typed映射，LastLanding属于已经真实接触的Plant事实；三者不是互斥状态，也不构成第二状态机。PreSwing、Swing和Approach Contact都保持NextSwing Tracking；首次Accepted Observation建立NextSwingLanding，后续可信Observation继续按Query Admission和Landing接受规则换代并重建Ground Path。`ApproachContactToLanding`只声明Plant目标准备区：每次Accepted Prediction可以更新Plant Target的Desired Point与诊断Ground Path，但不得直接把Path Revision写入可见Correction。

唯一Interpolation按`Target Height Adoption -> Selected Position+RequestedDirection Target -> Plant World Residual -> Direction History -> PoseRoot-Y Correction Response scalar -> Existing Goal Baseline Mix`顺序执行。PreSwing/Swing/Approach用正式Envelope与Foot Height生成Raw Height，以Accepted Landing沿Up高度作为唯一Target Height历史，并按`Raw Height + Filtered Landing Height - Current Landing Height`形成Swing目标；正常Phase不积分历史，动画XZ保持202551轨迹。`TargetHeightAdoptionMode=Direct`时合法Landing换代直接进入History，`RateLimited`时中等变化才使用`MaximumVerticalTargetSpeed`，大变化和正式Force Refresh直接更新History。Approach只准备Plant Target，不改变可见目标。Contact Verification成功后才选择Verified Position+RequestedDirection并继续同一历史；正式Position换代捕获完整Vector `PlantWorldResidual`并同帧衰减。Direction History从上一Applied Direction以每次10度上限朝Requested推进且不重投影标量，Correction Response scalar随后按Profile两档速率产生Response Output。Contact只提供接触证据，Lock Weight只提供Contact后Rotation可见响应、Release和完成资格。raw Contact、累计`max`、Formal Approach Progress与`0x274`均不得驱动Position Target。稳定且高度delta为0的Locked帧必须发布`TargetHeightUpdateReason=None`。Owner诊断必须并列发布`TargetHeightHistory / PlantWorldResidual / SupportDirectionHistory / CorrectionResponseHistory / PlantTarget`，不得把任一历史伪装成另一层，也不得恢复旧单档`MaximumVerticalCorrectionSpeed`或`BasisTransferred`。Resolved Foot之后只通过既有Foot Goal Position/Rotation Weight把Response Output与动画基线混合一次。普通PreSwing/Swing继续由Ground Envelope硬保护；Landing和Locked只测量过滤目标或Anchor穿透，不用Post Constraint抬升可见输出。

该Event首次产生正式Contact Rising且Lock Mode请求Sliding或Locked时，Runtime必须执行一次typed Current Contact Plant Verification。Verification使用当前Animated Sole生成唯一Current Contact查询输入，但查询结果只可在同一Transition事务中建立一次LastLanding、Promoted Contact Landing与Anchor；它不得作为第二Prediction、第二Anchor或逐帧Ground路径。稳定Plant期间Anchor冻结，普通Contact、速度、Surface候选和Prediction变化不得再次查询或重定位。Verification缺失、拒绝、与Event lineage不一致或超过Lock准入时保持没有Anchor并发布typed unavailable，不使用早期Prediction点冒充真实Plant。

Approach Contact到达时没有同Event Accepted Landing必须发布typed准备不可用，但仍保持Tracking并等待后续合法Observation；不得使用Animated Sole、默认地面、另一Event或旧Surface建立预测目标。Reset、Retarget、World Revision变化或Backend重建必须使Tracking与Verified Contact失效。正式`Sliding`接触准入刷新在Tracking中仍按canonical输入执行；实际Contact Rising只执行上述一次Plant Verification，稳定Plant不得以误差修正为由重复查询移动Anchor。

当前FootPlacementSurface在World Query Backend生命周期内视为静态，Backend发布固定非零World Revision；Reset、Retarget或Backend重建必须清空每脚Observation Page。移动平台和运行时Surface变更不在本change范围。

Ground Path Input identity只表示查询输入lineage，不单独触发Residual重置。Path Revision只由Event、Path可用性或Accepted Landing端点变化产生；同一Event、同一Landing与同一Envelope内的Phase目标变化不得发布Path Revision。正式Swing目标变化超过独立`PathRevisionDistance`时，Interpolation Runtime可以发布分型`TargetTrackingApplied`并捕获`PreviousOutput - NewTarget`，但不得把它记录为Path Residual重建。原始Builder目标与State Target继续分列诊断，不得互相改名覆盖。`PathRevisionDistance`不得控制Landing接受、Residual截止或Release完成；后二者分别只读取`SwingResidualTolerance`与`ReleaseCompletionTolerance`。

Accepted Swing Motion必须携带与同一Ground Path Event匹配的typed Swing Path Landing Reference。Verified Plant Landing只属于Contact/Anchor准入，不得门控Swing Path可用性或提供Swing Residual的Landing Point。同帧旧Event完成Plant Verification、下一Swing Event已经Accepted时，Foot根事务必须同时保留旧Contact Landing和新Swing Path Landing，不得把Path发布为一帧不可用。

Path诊断必须先在同Frame、Side与Event lineage下记录`Raw Landing/Path Target -> Swing Target -> Captured Residual -> State Output -> Vertical Rate Limit -> Ground Penetration/Post Constraint -> Encoded Goal`。任一后继阶段的单帧Correction变化明显大于直接输入变化时，必须先修复第一个产生不连续或放大的阶段；不得通过Goal低通或Step Time截止把该跳变藏到无Owner的后处理器。统一Interpolation的正式限速属于目标接管政策，必须同时记录追赶欠账和穿透代价。

在上述Correction链已经连续后，普通Swing目标使用统一Interpolation State中的Residual。基础半衰期仍来自Profile；当Residual大于`SwingResidualTolerance`时，Interpolation Runtime按剩余Step Time计算保证在Landing前收敛到容差所需的半衰期，并取它与基础半衰期的较小值。没有有效Step Time时不得猜测截止时间，只能发布明确输入不可用。Step Time只解决Landing前仍有Residual欠账，不负责改变Raw Target、重选State Output或修正同帧放大。Releasing完成只使用独立`ReleaseCompletionTolerance`，不得因调整Swing截止精度而改变Release退出时机。

Swing的Ground Path Envelope同时服务连续轨迹目标和插值后的Ground安全约束。Post Constraint MUST消费本帧Accepted Swing Motion已经采样的同一Envelope Point和Path identity，不得重新Raycast、SphereCast或读取另一Surface。Envelope随Swing Progress连续采样；只有正式Path Revision才能改变其几何。没有仍活跃Current Contact Prepared Target时，Swing/UnlockedSupport的Interpolation Output低于Envelope必须立即执行硬最低约束并记录Clamp事实，但不得把Clamp写回Interpolation历史；存在该Prepared Target时必须改由其世界点只测量当前接触，不得让Next Landing Envelope越权抬脚。Landing/Locked只以冻结Contact Anchor测量穿透深度、容差内/外、竖直限速和预计追赶时间，不得立即抬升；Full Lock只有在正式Weight完成、位置残差不超过`LandingLockCompletionTolerance`且穿透不超过`GroundPenetrationTolerance`时成立。

`Releasing -> Swing`完成必须由Post-Interpolation Transition先更新顶层State，再按新State执行Ground Floor和最终输出分类，避免同一帧发布Swing却跳过Swing Envelope保护。

## Decision 5: Foot Height只定义Swing动画高度

Swing的世界目标沿Component Up固定为：

```text
DesiredSoleHeight = RuntimeGroundEnvelopeHeight + FormalFootHeight
DesiredCorrection = DesiredSoleHeight - AnimatedSoleHeight
```

Formal Foot Height只表达动画脚高于动画Foot Path的高度。它不包含Runtime Landing、Anchor、Pelvis或世界修正。Runtime Ground Envelope只表达地面下界。两者组合后删除由`LandingConstraintWeight`乘`BaselineHeightError`或`FormalTargetCorrection`的旧高度/目标政策；Foot XZ继续来自动画骨骼，不创建Foot Forward曲线或空间位置双写。

## Decision 6: Support与Lock解耦并先进入Pelvis

Resolved Foot把正式Support写入`SupportIntentWeight`。Primary Support的Acquire/Retain资格由Support Intent、稳定Event lineage和有效`PelvisReachReference`共同决定，不要求Lock Mode为Locked，也不把Support反写为Foot Goal约束。

`ContactReference`继续只属于脚锁；`PelvisReachReference`可以来自同Event已经Accepted的Landing/Ground事实。这样Sliding或暂时不可锁的承重脚可以协调Pelvis，但不能因此把脚固定到世界Anchor。

Primary Support只消费Resolved字段，不读取Foot State、Lock Mode或Context。Support的Pelvis Reach Reference按同Event冻结Anchor、Verified Landing、Prepared Plant Landing、Accepted Swing Landing的正式优先级取得，Contact Anchor不可用时不得把Formal Support强制清零。Support曲线为0时不得由相对大小归一成1；双脚都无正式Support时Pelvis进入现有typed Release，而不是猜一只脚承重。

### Current Support由Foot/Toe脚掌几何解析唯一位置与法线

ZZZ静态链已经P0/P1闭合：真实Foot与Toe世界姿态生成脚掌多点空间查询，查询结果先解析为单一`FootTargetPosition + SupportDirection`，随后按状态权重混合、归一化Direction、执行Direction History与标量Correction History并只写FootL/FootR；Toe没有独立Goal、Writer或Pose低通。成功路径至少由六次查询记录中的完整XYZ几何组合Position，并从另一记录取得Direction，明确否决`OriginalSole + ComponentUp × max displacement`与“同一selected raw hit同时提供Position+Normal”的伪等价。外部typed查询重载、精确记录语义、宽度和半径仍未知，不能反向猜测。

每个表现帧从同一`FinalAnimationPoseFrame`和Rig Calibration取得Heel、Toe接触点、Foot Rotation、Component Up与Foot尺寸。Current Support Builder在现有World Query Backend内为Heel与Toe生成固定容量Observation，使用同一Profile坡度、距离、Layer、自身Collider排除、有限值与World Revision合同；不得从Foot Motion Toe曲线、另一Pose Source或LateUpdate骨骼读取建立第二输入。每个Probe分别保留Accepted/Rejected、命中位置、法线、距离、Surface identity和拒绝原因，再按固定Probe顺序、合法Surface identity与Component Up几何规则解析一个`CharacterFootSupportTarget`：

```text
CharacterFootSupportTarget
  Position
  SupportNormal
  SurfaceIdentity
  ObservationLineage
```

当前双SphereCast以最近命中、纯Up最大位移和selected raw Normal组成Target的实现只属于022607失败经验，不得成为最终设计：球半径会把台阶边缘接触偏移写入命中点，纯Up Position又与斜Direction不在同一几何域。正式Current Support必须在同一固定容量事务内从Foot/Toe多点记录解析一个完整XYZ Position与一个Direction，并为各自来源保留明确记录lineage；不得调低Slope阈值、偏好Up、平均多法线或把SphereCast hit point直接当Foot Target。任一必需记录无效或容量溢出时发布typed unavailable，不以旧Support、Animated Up、单点降级或默认地面冒充成功。准确查询形状与Position组合仍属待闭合项，实施前必须由现有Rig几何与项目Backend形成明确typed合同，不复制匿名六次调用常量。

Current Support Target进入State Target选择，但当前Final Component Pose已经完成Pose Graph混合，Foot Placement不再补造第二状态权重。Position Source与Requested Direction Source必须分别发布Event、Path、Frame、Completion和World lineage，不能伪装成同一Observation。Verified/Retained Anchor同时拥有冻结Position与Direction，实时Current Support拒绝不得反向释放Anchor。Requested Direction归一化后进入唯一Direction History，Applied Direction与Rig Sole Forward只生成Foot Rotation，位置响应使用独立PoseRoot basis；Target Height仍独立沿Component Up计算。位置、旋转、Applied Direction、分型lineage、Goal Weight和Writer汇入同一Resolved Foot，Toe不生成第二Goal。Rotation与Ankle Position必须按FBBIK本帧实际Position/Rotation Weight联合反解，零Rotation Weight不得改变Swing动画XZ。Direction变化只由Interpolation内每次10度历史推进，不执行世界投影、不在FBBIK或Final Pose之后增加Rotation低通。Pelvis仍只消费Resolved Foot Pair，不读取原始多点记录。

## Decision 7: Landing Reach先协调Pelvis，再限制Foot Goal

### 有效动画Bend方向的独立实验

2026-08-31以160901认可行为、192218恢复包和同值203023新诊断基线重新实验，不沿用130545的327行旧计数。当前2082个可靠动画脚行中223行请求被Applied历史倒置；方向d与-d会选到不同膝盖侧，不是等价平面表示。可靠动画直接更新运输前Stable方向，并用本帧原腿轴到加权Target腿轴的FromToRotation生成实际请求，不再由Stable或Applied历史翻号。退化时继续使用原Stable方向、Target平面投影与Applied保留政策，既有四个退化样本不得改义；Stable不能保存已运输方向，亦不新增sourceAxis历史。全部权重、Foot/Pelvis目标、Vendor和Writer保持不变。此项目修正不冒称ZZZ SmoothKnee，零权重深折叠及Vendor内部ReadPose/LimitBend边界仍由真实回放裁决。现行current spec要求唯一Goal/Solver与根Bank归属，没有要求有效方向与历史dot非负，与本轮合同不冲突。具体假设、业务取舍与Replay门见[有符号动画Bend实验](experiments/20260830-signed-animation-bend.md)。

Foot Motion Profile新增必须显式序列化的米制`MinimumLandingLegCompressionReserve`并纳入Profile Revision。缺失、非有限或越界时整项typed invalid，不提供代码默认值或旧配置补全。State Target Resolver与Resolved Foot为预测Landing脚，以及仍持有同Event Contact Goal的Landing、Locked、Releasing脚发布typed Reach Request：Hip、目标Ankle、Leg Length、最小压缩余量、Landing Event和有效世界Reference。Releasing必须继续参与直到其Goal权重归零，避免Pelvis在释放期间单独上提并把接触腿拉到近伸直奇异区。它不是第二Support、第二Anchor或第二状态机。

Pelvis Builder同时计算Primary Support腿和正式Foot Reach允许的Pelvis沿Up硬区间；两者都严格使用真实腿长减正式最小安全余量。原动画额外弯曲余量独立生成PosturePreference，仅影响目标：

```text
HardPelvisInterval = LeftRequestedLegHardInterval ∩ RightRequestedLegHardInterval
```

交集存在时，preferred target先受完整硬区间约束，原Critical Spring按现有频率求值一次，积分后的Output仅对硬区间夹紧一次并清除朝外速度；Module不再调用后置ApplyLandingReach改写输出。原动画姿态余量不得作为第二份输出区间。未加权Spring区间按正式Pelvis Goal权重换算，Reach资格最终核对实际加权位移。只要非零位移是安全余量所必需，即使小于5毫米也必须写出；不得一边发布Reach Available一边把实际Goal权重清零。Support换代、坡度变化和Target跨越Output保留显式Handoff与Velocity Reset。本次用户批准的三步路线保持既有频率，不采用旧草案PelvisMaximumUpVelocity/DownVelocity，也不加另一平滑器。

交集不存在时，系统先保持Primary Support安全，再把Landing Foot Goal夹紧到保留最小压缩余量的最大可达点，发布`LandingReachUnavailable`，并禁止该脚进入Full Lock。它可以保持Landing、Sliding或进入Releasing，但不得把超长目标交给FBBIK后仅靠腿伸直夹紧。

该政策的业务取舍是：不可同时满足双腿时显式保Primary安全，并用既有Foot Goal可达保护处理其余目标。必要的硬边界调整仍可能突变，不能保证不变脚目标、Body和腿长时骨盆也绝对连续；这不是允许默默撤销有效脚约束。

## Decision 8: 正式Contact与Lock驱动Transition与统一插值

正式Contact有效且同Event Lock Mode首次从Unlocked进入Sliding或Locked时，Pre-Interpolation阶段先执行一次Plant Verification；只有Verified Landing合法且该Event尚未消费，Transition Resolver才发布`Swing/UnlockedSupport -> Landing`与Create Anchor命令。Transition Runtime只建立一次Anchor，并把本次Contact Rising与Event写入同一Contact Transition Context；State Target Resolver以该Anchor生成唯一Position+SupportNormal目标，Interpolation Runtime以当前Output到Anchor的Residual继续既有响应。正式Lock Weight只负责Contact后的Rotation可见响应、Release与完成资格。Mode、Weight、Event或Verification不一致时发布typed invalid，不按早期Prediction或旧PlantConfidence继续。

正式Locked Mode和完成的Lock Weight触发`Landing -> Locked`，并使用`FullAnchor Response`目标。已锁脚回到Sliding Mode时保持同一顶层Locked生命周期和同一Anchor，只切换内部Sliding Response目标。Mode回到Unlocked或Contact正式退出时触发`Landing/Locked -> Releasing`，记录Contact Falling和最近释放Event；Release仍由Interpolation Runtime这个唯一Effective Correction Owner处理。

Releasing期间同Event再次出现Sliding或Locked请求，且原Verified Anchor仍保留、Lock距离与Reach仍合法时，Resolver必须发布typed `SameEventContactReentryRefresh`并执行`Releasing -> Landing`。Transition Runtime只Retain原Anchor；State Target立即重新计算同Anchor目标；Interpolation Runtime从当前Effective Correction连续接管，不得重置为零或重新查询世界。若Release已经完成、Anchor已清除，旧Event不得复活；新Event即使紧接上一边沿也必须执行自己的首次Plant Verification。任何State Target都不得直接写Anchor、State、Contact Transition Context或插值进度。

迁移完成后删除旧`PlantCycleConsumed`布尔、旧PlantConfidence状态准入、旧Constraint Weight接触政策及相应Projection字段；重入资格必须由明确Contact Event、Releasing生命周期和Retained Verified Anchor共同表达。Foot Placement Weight继续只表达整个Foot IK作者权重，不替代Contact、Lock或Support。

## Decision 9: 诊断证明阶段责任，不决定行为

封口诊断必须继续按同Frame、Completion、Program、Projection、Rig、Event和Surface lineage组合Source、Path、Context、Goal、Solved和Physical结果，并至少发布：

```text
Path Revision原因与前后目标
Raw Landing/Path Target、Swing Target、Captured Residual、State Output、Target Height、Plant Mixed World Target、Plant World Residual、Ground Penetration/Post Constraint与Encoded Goal的逐阶段Correction
Transition Decision、State Target、Interpolation Request/Output/Completion与Post Constraint前后值
Residual基础/截止半衰期与剩余距离
Ground Path identity、Envelope/Anchor穿透、容差、Ground Catchup与Full Lock门控
Formal Step/Foot Height/Contact/Lock/Support输入
Support与Landing Reach区间及交集
Foot Goal夹紧量与LandingReachUnavailable
Target/Solved Extension Ratio与Compression Reserve
```

Diagnostics不得创建Anchor、选择Support、改变Reach、Clamp Goal或执行第二次Query。

诊断Publisher统一为可判定质量Target发布`Health Score`与`Evidence Score`；原因、合同和候选比较只发布Evidence。Health Score由明确eligible总体、互斥严重度档位、发生率和严重尾部计算；Evidence只表达样本量与必需事实覆盖率，不得相乘或让低证据提高Health。eligible为0或必需可见事实缺失时发布typed Unavailable，不用100或0冒充结论；可见输出完整但原因阶段缺失时，质量与原因可用性分开。按用户后续授权，`consolidate-foot-diagnostic-scoring`以固定7维20/20/15/15/15/10/5替换旧文件平均分和禁止总分约定；摘要只作浅层参考，保留分项、贡献、次数、分母、阈值、代表帧、最差项与弱证据，不名为Pass/Fail，不替代视觉验收。

现有facts中已经生成但尚未进入正式Target的Swing到Landing Floor交接、实际脚位置Envelope反事实与Plant Interpolation必须进入同一Analyzer/Publisher链。Plant诊断必须分别记录raw Contact、正式`ApproachContactToLandingProgress`、Approach Target Preparation、Lock Weight、Target Kind、Lock Response、Target Height Component Up、Selected Support Position/Normal、归一化Support Direction、Position Response WorldAxis与未归一化HeightProjection、Target Height模式/前后与Update Reason、Previous/Current World Target、Previous/Current实际Response Output Point、Residual Capture Reason、World Residual捕获前/后/衰减后、Desired/Previous/Current Correction Response、Response Direction、Selected Rate、Applied Delta、初始化/重置原因、Continuity Owner及Effective Correction前后；Analyzer必须强制Approach Progress变化不改变Position/Normal/Residual/Goal权重，并证明首次Contact Verification换代才捕获完整Vector Residual。Ground Path Component Up、Target Height Component Up与Support Direction及Position Response Basis必须分列，Analyzer不得用任一缺失轴代替另一轴。稳定Swing诊断还必须在同Event、同Source/Cycle和稳定Ground Path三帧域分离动画Source、最终物理脚与Foot Placement新增速度和加速度，并保留202551的AdvanceToHold失败事实作为旧链反例。旧raw Contact累计max、`PlantBlend`、`TakeoverWeightAdvanced`、`WeightStarted`、`WeightCompleted`、`WeightChanged`、`PlantBlendedCorrection`、旧单档`MaximumVerticalCorrectionSpeed`及“World Residual已取代Correction历史”的Disposition列不得保留。Current Support诊断必须记录Heel/Toe Pose输入、各自Observation、Surface关系、唯一Position/SupportNormal、归一化与Foot Rotation Goal，不得由Sampler重查世界。可见Swing跳变必须同时按Presentation Delta、跨Body Tick数与米每秒分类为正常采样、低表现采样或速度异常；Actual Foot Envelope反事实只在有限Path走廊内且交点唯一时形成候选Correction，歧义、走廊外与无交点只发布事实。FutureLanding候选仍只消费正式World Query已经发布的canonical选择事实，不得由Sampler重查世界或复制QueryAll。Observation诊断必须同时记录Query Purpose与Refresh Mode；首次`ForcedPlantVerification + CurrentContactVerification`允许同Key正式查询一次且不得记为duplicate，同Event后续重复Verification必须记为typed不一致。

Prediction诊断必须补齐`Raw Body Target Current + Raw移动计划Continuation -> Stable Prediction Velocity -> KCC Future Translation -> Raw Landing -> Observation -> Tracking -> Approach Plant Target Preparation -> Contact Verification`，并把移动计划Current作为对照列记录，连同速度差、阈值、EMA响应、最大速度Clamp、状态初始化/重置原因、Tracking状态、Verification Frame/Reason和稳定Plant候选忽略事实。这样实现阶段必须先证明Prediction稳定，再判断Interpolation或Post Constraint，不得把所有抖动归到最终Pose。

采样包固定由同一Recorder发布`每Frame/Side一行的samples.csv + 只保存Ground Contact/Envelope数组项的ground-path-geometry.csv`。几何表必须按Sample、Frame、Completion、Side与Ground Path identity连接主表，不得为每个几何项重复整套Source、State、Goal和Solver列。

每次采样固定写入项目本地持久目录`Diagnostics/FootPlacementRuns/<run-id>/`，不得写入Unity会清理的`Temp`。该目录只承载本地原始诊断，不自动复制、晋升或加入版本控制；需要对账的基线由作者明确选择后再单独归档。

停止录制必须进入唯一`Finalizing`生命周期。Unity主线程只停止捕获并冻结最后一批不可变Frame；后台Finalizer继续排空同一Writer、先封存几何表再以`samples.csv`作为包完成标志、运行同一C# Analyzer与Publisher，最后把Completed或Failed状态发布回Editor。不得增加Python Reporter、同步停止分析路径或仅扩大队列掩盖持续吞吐不足。

## 后续能力的ZZZ补证边界

本change不实施下列能力。后续正式change必须先按上述证据等级确认能采信到什么程度，再把同类责任翻译进项目现有Owner：

| 后续能力 | ZZZ当前证据状态 | 项目唯一归属与限制 |
|---|---|---|
| Foot Normal进阶角度与冻结政策 | Foot/Toe输入、位置+法线中间量和单一Foot writer已经P0/P1闭合；具体角度限制、Plant冻结、角速度与移动平台政策仍是P2/P3 | 本change只实现Current Support Normal驱动的唯一Foot Rotation Goal；进阶限制补证后仍扩展同一State Target与Goal，不增加第二Rotation Writer |
| Knee方向稳定 | tbik批处理、通道和权重组合存在，但没有直接call证明PIK当前帧调用它 | 若后续补出运行连接，只进入现有FBBIK Bend History和唯一Solver，不做Solver后骨骼修正 |
| Pelvis进阶响应与旋转 | 存在Pelvis词汇与主标量响应，字段级PD、上下速度和旋转参数映射未闭合 | 继续保留项目自己的Pelvis Reach与Critical Spring；除非动态证据闭合，不以ZZZ字段改写现有控制器 |
| Stride Wrapping | min/max变体和若干缓存存在，Stride业务映射仍为P3 | 后续先补证，再决定是否进入正式Foot Motion/Stride意图与Pelvis |
| 楼梯专用表现 | DownStair、Stair与Knee速度的具体调用政策未闭合 | 后续只能由Ground Path坡度与阶沿事实驱动同一Foot/Knee Owner，不建立楼梯IK旁路 |
| Moving Platform | 存在位置差和缓存结构，平台进入/离开业务语义未闭合 | 后续由World Query Backend发布稳定Surface Frame/Delta与World Revision，Foot不直接读Rigidbody |
| Time Scale同步 | `TrySync`复制配置与状态成立，完整Time Scale恢复政策未证明 | 继续使用项目既有Presentation事务；不复制对象内存布局或推断的恢复时序 |
| 状态过渡混合时钟 | 只确认`f`来自当前/下一状态和过渡量，ManualWait实际调用频率、暂停与掉帧行为未闭合 | 继续使用项目既有Presentation Frame、统一Foot Interpolation和Evaluate Barrier，不增加第二更新循环 |
| 动画后Bone Adjust | 相关方法簇存在，具体业务调用链和最终Pose贡献未闭合 | 补证后归Pose Graph正式节点或Constraint Operation，不塞进Foot Runtime或LateUpdate直写骨骼 |

以下内容明确不照搬：预测/普通fallback、Legacy/Advance/Current多套Pelvis并存、全局Footprint缓存、PIK硬编码`cos30°`、固定“每帧两次查询”预算、半径内最高点候选、混淆bitfield布局、多个IK组件和常驻最终Pose低通。项目坡度、查询数量与候选政策由正式Profile、完整Ground Path和World Query合同决定，不能把另一个产品的常量当成普适结论。

## Rejected Alternatives

- 恢复旧Goal Transition或在FBBIK之后加全局平滑。
- 只把传统State Machine拆成多个State类，但继续让State自己执行Enter/Exit、改Anchor和推进插值；文件变多但责任没有分开，新Transition仍需改多个State。
- 建立项目级通用Tween Manager、string channel或字典注册插值；Foot的Event lineage、Anchor接管、Landing截止和Release完成无法由无业务语义的Tween可靠表达。
- 复用Animation Pose Graph的Transition Routing；Pose Source权重换代与Foot接触所有权是两种业务事务，共用路由会让Foot状态依赖动画图内部生命周期。
- 保留旧`CharacterFootStateMachine`作为新模块外的转发或兼容入口；这会让离散状态和Effective Correction继续存在两个可写位置。
- Path identity每帧变化就无条件重置Residual。
- 在KCC Future Body Translation之后平滑世界位置，或在Final Writer前增加常驻全Pose低通；前者会把碰撞结果揉成不存在的轨迹，后者会破坏Contact、Reach与动作响应。
- 把Action Pose Contribution当作Foot Goal Owner并在Action开始时释放Anchor、清空Interpolation或跳过Landing Reach；Action只改变动画基线与作者Goal可见权重。
- 为了模仿PIK新增独立Predictive Modifier、普通/预测双路径、全局Footprint缓存、GrounderIK、LegIK或第二骨骼写入。
- 把每帧CurrentSwingFloor命中接进Swing State Target并以`ContinuousTargetChanged`重建Residual；这会让预测输入稳定时悬空脚仍追逐实时地面查询。
- 把完整Swing目标当作Ground Floor，或允许没有Profile预算、没有收敛事实、仍可进入Full Lock的无界穿透。
- 把当前脚水平投影到一维Ground Path上包络并取同距离最高点；该做法无法区分竖直边两侧的真实Surface。
- 直接给膝盖设置最小角度而不处理Foot Goal与Pelvis可达。
- 只降低Foot Goal Weight掩盖超长目标。
- 在Support和Foot Height未迁移前单独延长Landing或接入Lock Weight。
- 让Primary Support/Pelvis读取Foot State、Lock Mode或可变Context。
- 同时保留旧PlantConfidence和正式Contact/Lock并择优输出。
- 增加第二Landing状态、第二Anchor、第二Goal Set或第二FBBIK。

## Migration

1. Foot Motion数据change已经完成用户验收并归档，记录当前Curve/Event identity。
2. 保留已经完成的Releasing到Swing顺序修正和identity触发清理，记录它们没有明显改善整体Path抖动。
3. 对最新代表事件逐阶段发布Raw Target、Residual、State Output、Floor与Goal事实，修复第一个已证明的不连续阶段。
4. 固定当前State、Transition边、目标、Residual和Post Constraint映射；建立分型Context、纯Transition Resolver、State Target Resolver与唯一Interpolation Runtime，在根事务内逐项迁移等价行为。
5. 切换固定帧内管线并删除旧`CharacterFootStateMachine`、三套Residual、旧Contact Progress、分散HalfLife推进与所有兼容入口；从此只有Transition Runtime写离散State/Anchor，只有Interpolation Runtime写Effective Correction。
6. 发布唯一Foot Motion Runtime Frame，用正式Step Time/Distance替换旧Prediction时域并删除旧Step消费者。
7. 在同一根Bank增加左右脚共享Prediction Motion State，以本项目Replay证明的阈值、EMA和最大速度分别稳定committed Body Target当前速度与移动计划Continuation；只把稳定速度交给唯一KCC Future Body Translation，并补齐Raw/Stable/Translation及移动计划Current对照诊断。
8. 让PreSwing、Swing与Approach Contact保持Landing Tracking，并由统一Interpolation持续准备Plant目标；首次正式Contact Rising执行一次Plant Verification并建立冻结Anchor。
9. 保留已经完成的旧`MaximumVerticalCorrectionSpeed`单档后置链删除，在同一Interpolation中分离Plant Target Height、完整Vector Plant World Residual与新的标量Correction Response History；Current/Target Position与Support Normal按同一状态权重混合并归一化，Correction Response沿PoseRoot局部Y对应世界轴使用双档速率，Target Height仍沿Component Up独立处理；安装初始化/重置和Action/攻击/同Event不清零政策，使`Desired Output -> Response Output -> Existing Goal Baseline Mix`只有一条正式链。
10. 从Final Animation Pose与Rig Calibration取得Heel/Toe脚掌几何，在现有World Query Backend内生成固定容量Current Support Observation，解析唯一Position+SupportNormal并生成同一Foot Goal Rotation；删除任何单点降级、Toe Goal、第二Writer或Pose后Rotation低通。
11. 让Support进入Resolved Foot、Primary Support和Pelvis，保持Lock生命周期不变；原动画弯曲余量只影响目标，完整硬Reach交给一次既有Pelvis Spring，不增非对称速度配置。
12. 增加双腿Reach交集、最小Landing压缩余量、Goal夹紧与typed拒绝。
13. 用Contact、Lock Mode与Lock Weight替换旧PlantConfidence生命周期并删除旧字段。
14. 显式重建Corin Projection、Float32与Fixed产品，完成编译、诊断重放和严格OpenSpec校验。

## 已否决的接触交接正式脚高候选（2026-08-30）

`9bce6c2`只研究085503中Right 475→476的输入变化：正式Foot Height从28.555毫米变为0，实验尝试在Contact捕获前继续这个高度变化。478–480的Correction Response已经追上Desired，不得把它称为两级响应重复拖延；483是独立的Response限速欠账，候选实际没有改善。

`da438fa`接入精确诊断后，124922同输入Replay成功完成并与085623 Proof逐1044帧匹配；46个准入和Capture公式全部成立。但穿透异常段19/78变为27/81，接触跳变405/1036变为428/1038，接触未贴合异常12/60变为13/60，故拒绝候选。Right476中心间隙25.286降到5.857毫米的同时Heel已由面上11.335变为面下8.094毫米。Left746的负Swing Residual已经抵消20毫米正式脚高，再扣完整37.673毫米直接形成面下目标。原始Foot Height相对作者落点路径定义，不是一份可从实际输出中无条件减去的独立位移。

本实验没有修改任何查询或Profile，但实际出现20条State变化及约2.17厘米骨盆输出差，说明首帧只沿Up改变捕获不能推出后续XZ、锁定时间和腿姿态不变。七维总分60.4升到61.9全部来自Stable Swing子项74升到84，不能替代恶化项的直接数据。恢复原完整世界输出捕获，删除候选快照、DTO及其诊断字段，不保留零值兼容路径。

这不是ZZZ一一对应迁移。新ZZZ证据已经区分Owner Transform、Foot pivot与Sole，并证明位置标量沿Owner局部Up推进、末端W在当前原生分支参与旋转；不能再用旧`arr230=arr228+arr130×arr128`特例证明Normal就是位移轴。基线、完整37项对比、失败数据及恢复结果见[本轮实验记录](experiments/20260830-contact-height-advance.md)。既有评分系统、原始失败样本和中文提交历史保留，用户proposal/project修改不纳入撤销。

`811dacb`撤销专属Diagnostics，`4be1f51`恢复Runtime。130545恢复Replay的2086行、1140列中1116列与085503逐值完全相同，24列只为身份；50195行geometry只有4身份列变化。全部原始物理数值与状态恢复，37个Target规则/计数及七维分数回到基线，总分60.4。1044条Proof帧及原基线输入/Body身份独立只读对账一致；恢复已验证，不等于原有IK质量问题已经完成。

## 决策补充：Committed Weighted Goal Sole参考

141256证明位置basis消除了稳定Swing的N×debt轴外摆动，但单独候选仍新增Right404–412接触穿透与下陷，不作为无回归版本交付。新的独立实验仅接通已有根Bank目标参考，不改位置basis、Target Height、Residual推进、速率、权重或查询。

现行`SealFrame`会清除Foot Bank的`HasFrame`开放标志；旧读取门却要求该标记为true，导致旧包的`VisibleOutputTransferred`始终为0。关闭事务与已有输出必须分开：删除三个松散的HasVisible/左右Visible字段，保存每脚带Frame、Completion、Side、WorldSole和Goal权重的不可变Weighted Goal Sole参考；读取时核对Committed Bank与Resolved Pair身份，不保留开放标志来绕过事务。

参考的唯一生产者仍是Foot Module的最终Goal编码后阶段，它按已有Goal权重和动画Foot Pose推算Sole，不读取最终骨骼，也不是新Physical反馈路径。旧`Visible`名称改为`WeightedGoalSole`；连续性参考点区分于实际上一物理输出。零权重时参考可能与Physical Sole相差数厘米甚至更多，该差异必须保留在原始对账中，不能以非零Goal样本的微米级残差推断二者恒等。

正式Capture采用合法上一参考时，以完整XYZ捕获Residual、同帧衰减，并以同一dual重基scalar；稳定帧保持原scalar，不增加新的历史或滤波。这个实验首次激活既有但不可达的重基分支，不是恢复曾经动态验证过的路径；必须同时对照141256和130545，尤其覆盖Right404–412及Left349→350、853→854的零权重后Capture。

150516已经完成上述双基线Replay：78/78 Capture实际采用参考，完整XYZ与dual数学一致，稳定Swing的6个原非零ABA额外XZ保持消除；但Right959世界下降350.409毫米缩成25毫米的同时，中心离面165.009毫米增到490.418毫米，后续新Event形成持续离面，另新增Left572–574穿透。Right404–412只部分修复前驱，原478–484毫米悬空及Sliding再离面未解决。因此组合拒绝，不在其上叠加新膝盖实验，先恢复原基线并同输入对账。具体提交、SHA256、37项同规则边界和失败机制见[加权Goal参考实验](experiments/20260830-committed-weighted-goal-sole.md)。

## 已实施切片对账

- `5c0922c`与`f47e35a`把Contact Transition与Hard Ownership的只读事实接入唯一Runtime/Diagnostics链。`20260830-074303-966-872089e73a3e4c138fb6fc1924e7e3e2`使用既有1044帧Record完成Replay并发布facts49/diagnosis18；Contact Transition Context为2086 eligible、0失败。与064530相比，744个共同正式行为列只有Surface/Path运行identity换号，数值差均不超过1微米；原有34个Target的次数、比率、Health与Evidence完全一致。该Record没有Action脚贡献，`action-hard-ownership`为0 eligible与typed Unavailable，不作为Action动态证明。
- `9425d32`删除未消费的`ObservedTimeToLandingSeconds`、`ObservedDistance`及Projection单值时距曲线副本；正式Time、Distance与RootLocalLanding继续由Current/Next Event Frame发布，作者Clip Catalog完整曲线组校验保留。`0ed8fb7`经正式Editor入口重建Corin Float32与Fixed产品，两个Program的全部行为hash不变；Projection删除69360行旧曲线块，剩余文本只变更10个派生identity/hash。该切片不等于全部Biomechanical Step旧投影与reader已经清理，5.4其余消费者仍须逐项删除。
- 对应`20260830-080619-740-9c9e3f8afa8c4cc0ab803457aa544eeb`保留了Replay proof的身份失败经验：唯一Aggregate差异是`projection_revision`，DivergentFrameCount为0。与074303相比，2086行的778个共同正式行为列仍只有14个Surface/Path identity列不同，全部行为数值差不超过1微米；36个Target、coverage与10927个事件总数完全一致。不得把身份门失败改写成完整proof通过，也不得修改比较器放行；原始包继续保存在本地`Diagnostics/FootPlacementRuns`，没有自动归档或加入版本控制。
- `c71d5a2`安装发布前Step一致性校验后，Corin Float32与Fixed正式Build均通过且产物无文本diff。`20260830-082232-916-b82991ce27cb4d3293d691f537f24153`完成1044帧matched Replay；与080619相比，1118个共同CSV列中的1096列逐值一致，22列差异只属于运行元数据和Surface/Path identity，50195行几何除identity外逐值一致；36个Target、coverage与事件总数完全不变。
- facts49的独立静态复核发现9.10仍有未覆盖出口：Suppress帧的Pre/Post Decision仍可能落在未完成的PathContinuity默认值；Retained Anchor只发布Event而缺少Point/Normal/Surface与获取Frame/Completion；正式FootPlacementWeight输入未独立发布。当前Record的Hard Ownership、Action与Same-Event Reentry样本均为0，不能以Contact Context的2086/0推断这些分支已经动态通过。9.10因此重新打开，后续只补Runtime事实出口与诊断，不改已验证运行行为。
- `b820757`与`f736ece`完成上述9.10出口迁移：Lifecycle独立发布真实Pre/Post Decision，删除Path中的转场副本；Anchor前后完整快照包含Event、Point、Normal、Surface、World与获取Frame/Completion，正式FootPlacementWeight独立发布。旧`ContinuousReentryTakeover`删除，唯一`ReentryInterpolationHistoryRetained`只表示转场未Suppress/Reset且Anchor保留；世界几何连续性由独立信息型Target观察，不再由名称暗示成功。
- 本版交付样本为`20260830-085503-819-259090e6db3f45dc9ab4f24f0511458b`，原始文件继续保存在`Diagnostics/FootPlacementRuns`，唯一格式为facts50/diagnosis19。原1044帧Record的Proof对082345基线`matched:1044`；主表1043个采样帧、2086脚行、1140列，无Frame Gap。与082232相比，1117个共同列中1095列逐值一致，22列差异仅为5个运行元数据与17个Surface/Path身份；50195行几何除身份字段外逐值一致。原36个Target的eligible、matched、Health/Evidence与全部共同数值分布不变；新增Formal Goal Weight Target使事件总数从10927增至13013，不代表行为事件增加。
- 新Contact Transition与Formal Goal Weight合同分别为2086 eligible、0不一致；927行Retained Anchor的前后Point、Normal、Surface、World、Event及获取Frame/Completion全部一致。Action、Hard Ownership、Same-Event Reentry与未执行Post分支仍为0样本，不能称这些分支已动态验收。9.10仅按事实发布与Analyzer接入完成勾选，最终综合行为验收仍属于未完成的第10节。
- 本版Runtime与Editor分别以规定flags构建0错误、27/30个既有警告，每次构建结束立即关闭build server；Unity仅在Edit Mode执行Refresh，首次资产时间戳导入错误经第二次Edit Refresh消失，Replay期间未Refresh、未重启Unity，结束后回到Edit Mode。当前交付不追加Foot行为改动，也不恢复已回退候选；Current Support完整XYZ几何与方向来源、Pelvis速度边界、旧Step消费者清理仍未完成。稳定Swing输出扰动145/347、Landing退出扰动49/60及Foot Placement新增穿透7/78仍是本版已有问题，不把本次零新增回归写成整体视觉质量通过。
