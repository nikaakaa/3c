# Foot Placement原子数据链

## 目标

每个Presentation帧只允许一笔Foot Placement事务：同一帧Original Animated Pose、Step/Body/World事实进入，左右脚与Pelvis完成候选求值和唯一所有权选择后，一次提交Final Goal Set，再由唯一FinalIK FBBIK执行。

```text
Original Animated Pose + Step/Body/World Facts
-> Stance Candidate
-> Active/Revision Geometry Candidate（各一次）
-> Pelvis Result
-> Active/Revision Reach Candidate（复用同一Plan Sequence）
-> Transition Origin
-> Pre-Continuity Goal
-> Frozen Landing Handoff Origin + Committed Anchor Goal
-> Final Goal + Goal Owner
-> FinalIK FBBIK Result
```

Stance Candidate不是Unlocked Swing的基线。Swing从同一份Original Animated Pose合成；Stance只在Locked、Sliding、真实Contact/Anchor或Idle所有权成立时成为Final Goal。

## 已完成的数据合同

- `CharacterPredictiveFootFrameEvaluation`封存本帧唯一Original Pose，Goal阶段不得再次读取Rig。
- `CharacterFootPlacementFootGoalInput`原子携带Side、Original Pose、Feature、当前事件权重、Stance Candidate和Stance diagnostics。
- `CharacterPredictiveFootFrameEvaluation`同时封存左右Active/Revision Geometry Candidate；Stance观察、Landing候选、Pelvis输入和Goal阶段复用这些值，不再在Pelvis前后重复求值Plan。
- Geometry Candidate只负责当前Plan、Ground Path、鞋底净空和动画轨迹合成；Pelvis完成后，Reach阶段只对该不可变候选施加腿长约束，并保持同一个Plan Sequence和typed reject reason。
- `CharacterFootCompletedOutput`原子保存上一完成帧Original、Final、Sole、Ground Path、Support、Plan identity和Body Path；不得再用分散字段拼接跨帧历史。
- `CharacterFootCompletedOutput`同时保存Final Goal Owner；跨帧Output Continuity以`Plan Sequence + Goal Owner`作为完整身份，不能把同Plan下的Landing、Stance和Plan输出视为同一owner。
- Transition Origin只在所有权换代帧，用上一完成Final相对当前帧Original捕获；禁止混用旧事件Original与新事件Original。
- schema v102为每脚发布Original、Stance、Active/Revision Geometry、Active/Revision Reach、typed reject reason、Transition Origin、Pre-Continuity、Final和唯一Goal Owner，共1483列，Header唯一且逐行等宽。
- Transition合成后的Goal不再执行第二次完整Reach改写；Pelvis候选在Event Successor与待替换事务期间继续与Foot旧侧属于同一个完成输出。

## Event Successor唯一交接

候选Successor只保存不可变geometry与时钟，不参与Goal。事件晋升后，唯一Transition从上一完成输出以权重0开始；Stance观察、Ground Path、Ankle与Final Goal消费同一个权重。Blend只能在新Active首次进入`Executing`后的下一帧推进，不能在候选预建或`Planned`阶段提前累计。

## 数据证据

run `3ad732e4437f4df8a5eff8acf23f7059`共1393行、1463列。FBBIK位置残差通常接近零；最大Y跳变最早出现在`Planned -> Executing`的新Active/Path及`EventSuccessor/PredictiveExit`的Transition合成，证明Solver只是执行错误输入。

首次接入Successor Transition后的run `ee532097a6ad4330ac25dcdf3a52ebb9`共1302行、1463列。左脚大于5cm的Goal Y变化从474次降到31次，95分位从20.9cm降到2.2cm，最大物理下陷从53.8cm降到1.7cm，证明唯一交接方向有效。

同一run右脚frame `369 -> 371`显示：新Plan仍为`Planned`时Blend已从0推进，首个Active Candidate出现时Transition权重已到0.58，产生44.6cm跳变。该证据要求Blend起跑绑定新侧首次可执行，而不是候选创建或事件晋升时刻。

该run还显示部分旧Active在身体已上升时Ground Path仍停留低层踏面，造成晚期Swing下陷；这是Ground Path/Plan身份或支撑链问题，必须在Transition闭环后独立定位，禁止用Blend或Current Grounding掩盖。

run `628412c3a53e421b9a45ef3210231875`的左脚frame 385在Pelvis前仍有Executable Active Geometry，Pelvis后却得到`ReachExceeded`并立即进入`PredictiveExit`；frame 386至387仅由Render Delta推进退出权重，Final Goal Y连续下降约70.9cm和48.3cm。右脚同一事件换代也在候选`FutureLandingNoCandidate`后以相同方式下降约47.0cm。旧链同帧重复求值Plan，使“几何是否有效”和“Pelvis后是否可达”混成一个布尔值；v102先拆开该因果链，后续4A.20再修正错误的退出所有权。

v102回归run `6712da2be2d740d18158ad34f44d08dd`共1190行、1483列、Header唯一且所有分块逐行等宽。左右脚分别有22/23帧`Geometry Candidate有效、Reach Candidate无效`，与22/23帧`PredictiveExit`完全一一对应；最大Final Goal Y单帧下降为`77.6cm/96.6cm`。这把当前最大跳变确定在Pelvis后的Reach裁决到Transition所有权之间，而不是Geometry Path或FBBIK。

修正“FadeOut状态短路Reach诊断”后，run `89996fbc87fb4dd5bc02116537ecea11`共814行、7个流式压缩分块、1483列，Header唯一且每行等宽，Unity Console为0 Error/0 Warning。右脚frame 250至254中Geometry Candidate持续有效且Y约为`2.42m`，Reach Candidate连续以`ReachExceeded`拒绝；Transition仍以`PredictiveExit`拥有Final Goal，并在frame 254单帧下降`1.078m`。左脚frame 725至726的Geometry与Reach Candidate均有效，但`PredictiveExit`仍把Final Goal单帧下降`44.2cm`。因此v102已经能区分两种独立错误：Reach拒绝触发错误退出，以及候选仍有效但Transition主动退出；二者都位于Transition所有权，不得再归责Ground Path或FBBIK。

待替换事务修复后的run `fec7a6b685c449ae9446b9181da2ad06`共881行、8个流式分块、1483列且逐行等宽。Unsupported Swing中的Active求值失败已经进入`AwaitingReplacement`并保留上一完成输出，但同帧仍暴露两处数据链分裂：Transition之后存在第二次Reach改写，`Pre-Continuity -> Final`最大额外变化约`34.89cm`；Event Successor保留Foot旧侧时，Pelvis候选会暂时消失。

移除第二Reach并统一交接期Pelvis候选后的run `79dfec002859417790ba827bf7a2872f`共1561行、12个流式分块、1483列，Header唯一且逐行等宽。`Pre-Continuity -> Final`最大额外变化降至左`2.192cm`、右`0.982cm`，主要Event Successor跳变帧的Pelvis候选保持有效；权威Unsupported Swing中不再出现向Original启动的`PredictiveExit`，因此4A.20闭环。

该run把下一处错误确定为Landing部分接管：右脚frame `246 -> 250`与`837 -> 840`中，Anchor Blend从约`0.303/0.046`上升到`0.593/0.706`时，Final Goal分别下降约`82.48cm/63.59cm`，最大物理下陷达到约`1.330m`。代码在Anchor部分权重时先以`1 - AnchorBlend`衰减Predictive，再把剩余权重隐式交给Original动画；它没有把旧预测完成输出直接混合到Committed Stance/Anchor。该异常已经位于Goal Owner合成层，FBBIK位置残差多数约为`1e-7m`，不是Solver放大。

首版Landing互补交接run `fe5f181673874bcea6525954d29a9a22`共1774行、1483列且逐行等宽。它消除了Original动画空档，但右脚frame `229 -> 234`暴露了更精确的事务错误：frame 231在旧事件和Plan 31上捕获Surface `-481332`，Committed Anchor Y为`2.423243m`且Blend为0；同帧Final已经降到`1.981060m`。frame 232事件换代，但Active仍是Plan 31，Anchor Blend仅`0.0929362`，Pre-Continuity重新求值到`1.504806m`，Final为`1.590162m`并产生约`84.48cm`物理下陷。Committed Anchor端点正确，错误发生在混合左端点：实现用了事件换代帧重新计算的Transition，而不是紧邻上一完成Final。

正式Landing事务必须满足：首次Committed Anchor出现时冻结`PreviousCompletedFinal`；后续同一`Plan Sequence + Landing Event identity`只更新Committed Target与Blend，不改Origin；`AnchorBlend=0`时Final等于Origin。schema v103将以1503列新增逐脚Handoff可用性、Plan/Event identity、Blend、Origin和Target，验证`Previous Final -> Frozen Origin -> Target -> Final`。该实现尚需新Unity run证明，4A.21与4A.22在证据通过前保持未完成。

v103回归run `0bf203fb9dad45fb97eb4619b063ebb6`共16个压缩分块、2189行、1503列，Header无重复且逐行等宽。左右脚每笔连续Handoff的Origin都与紧邻上一完成Final精确一致，连续事务内Origin漂移为0；Final对`Lerp(Origin, Target, Blend)`的最大误差分别约`0.0099mm/0.0099mm`。因此冻结左端点、唯一互补公式、Owner identity与v103诊断链已经生效，4A.22闭环。

同一run同时证明4A.21尚未闭环：同identity的Handoff可用性出现一帧消失再重现，左脚30次、右脚92次；连续事务内Committed Target基本不动，但单帧Blend最大增加左`0.663`、右`0.658`。左脚frame `794 -> 795`中旧Handoff仍是Plan 152/Event `9381358328273184905`，Current已换成Plan 156/Event `10629688855033292255`，Support从`Unlocked/ApproachingContact`切到`Locked/Supporting`，Blend却从`0.3542521`增至`0.91882`，Final Goal三维跳变约`1.205m`、Y跳变约`13.87cm`；下一帧同一新事件又回到`Unlocked/Unsupported`并开始释放。现在错误已定位到Stance Anchor事务与Step换代边界，不在Landing Lerp、Ground Path或FBBIK。

代码对账确认直接原因：捕获后的Anchor在`ApproachingContact`仍把逐帧Predictive Contact Target当作Contact Surface前提；目标短暂缺失时`PlantContact`被清除，而捕获首帧raw Blend为0，因此同帧`ClearAnchor`，后续目标恢复又重新Capture。修复口径是Committed Anchor在`Landing/Locked`状态下用自身Surface与local anchor维持Contact；Predictive Target只参与首次捕获。schema v104扩展为1527列，增加逐脚Stance/Anchor事务状态、Anchor Plan/Event、动画Constraint identity与权重、raw/target Anchor Blend、raw/target Pelvis Support及Committed Goal可用性；数据通过前4A.23保持未完成。

首个v104 run `20a2e819963f4fa1a7a8df90307cf4c2`共21个压缩分块、2972行、1527列且Header唯一、逐行等宽。右脚同identity一帧Handoff闪断从92次降到1次，证明Committed Anchor不再依赖逐帧Predictive Target有效；左脚仍有30次。frame `2310 -> 2314`中Anchor Plan 455持续存在且Committed Goal持续有效，但Handoff只在2310至2311可用，2312起又退回ActivePlan。原因是Modifier每帧重新用`LastOutput.PlanSequence == AnchorPlanSequence`判定Handoff资格，Handoff首帧后Completed Output却优先记录底层Active Plan，导致已开始事务自我失效。正式修复必须让`Plan/Event`已冻结的Handoff持续到Anchor事务结束，并在Handoff期间把Completed Output identity记为Anchor Plan。

首次持续身份实现的Unity run在frame 20以`Foot Landing handoff origin is unavailable`失败：调用方已确认同一Handoff存在，但`BeginOrContinueLandingHandoff`仍先执行“新事务必须有完整上一输出”的门禁，之后才检查同identity。正确顺序是同一事务直接继续；只有创建新Handoff时才要求上一完成Final。

同一run还证明下一层是动画权重边界：同一Anchor的Animation Constraint target可单帧完整翻转`0↔1`，raw Blend最大步进约`0.62`，SmoothStep后最大约`0.82`；左脚frame `2314 -> 2316`发生`Capturing -> Holding -> Releasing`，Current Event换代后Constraint从1变0，最大Y跳变与穿透仍超过1m。必须先消除Handoff事务自我失效，再用下一run区分剩余异常属于Constraint事实边界、Anchor目标距离还是低帧步进。

本run停止后Unity Console另有一笔自动Input Action锁存失败；它属于Foot Placement之前的测试入口，不能把该run描述为Console干净或完整效果回归，但不影响上述已封存帧对Landing代数关系的只读对账。

Landing事务闭环后，下一owner才是新Plan自身Ground Path/Clock：左脚frame `1338 -> 1341`的`Pre-Continuity`已经随Path连续两帧下降约`50.78cm/49.03cm`。该问题必须按Ground Path与权威Phase对账，不能再由Landing Blend、Current Grounding或FBBIK掩盖。

## 固定诊断顺序

1. Frame、Build Family、Completion identity。
2. Original Pose与Step/Body/World事实是否同帧。
3. Stance、Active、Revision各候选首次出现异常的位置。
4. Geometry Candidate在Pelvis前是否有效，Reach Candidate在Pelvis后为何拒绝。
5. Transition Origin和Transition Blend是否在唯一边界推进。
6. Pre-Continuity与Final是否新增跳变。
7. Landing Handoff的Plan/Event identity、冻结Origin、Committed Target与Blend是否满足同一事务。
8. FinalIK Result/Residual是否只是执行已跳变Goal。

上游候选或所有权尚未一致时，不调阈值、不增加第二查询、不让响应式Grounding接管Swing、不修改FBBIK。

## 精炼经验

- 几何Path连续不等于Goal连续；所有权换代本身必须有可观测候选和唯一权重。
- 候选存在不等于候选可执行；没有新侧目标时推进Blend，会把首帧权重偷跑成硬切。
- 同一个Plan一帧内只能产生一次Geometry Candidate；Pelvis后只允许追加Reach裁决，不能重跑Path和鞋底合成制造互相矛盾的候选。
- Landing Handoff不得把同一Plan额外采样到phase=1生成第二份Ankle/Support；Approaching Contact必须复用本帧Geometry Candidate，Successor无Committed Landing时只能使用该Plan已验证的Projected Landing。
- 跨事件连续性必须使用“上一完成Final - 换代当前帧Original”，不能使用上一事件Original。
- Completed Output必须原子保存；分散的Last字段会允许不同帧、不同Plan和不同Support被拼成不存在的历史。
- Landing互补混合的公式正确仍不够；若左端点来自当前帧重算Transition，事件换代会在Blend=0前先跳。左端点必须冻结自紧邻上一完成Final。
- FinalIK残差接近零而Goal先跳时，首因不在Solver。
