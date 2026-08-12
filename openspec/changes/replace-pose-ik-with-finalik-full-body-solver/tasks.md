## 1. 当前真相与证据

- [x] 1.1 删除冗长逐轮诊断文档，把proposal、design、tasks和delta spec压缩为当前GDC唯一设计。
- [x] 1.2 对账动画事务：Simulation事实、Presentation时钟、Pose Contribution、Component Pose、world-aware Foot Placement、FBBIK与Final Writer。
- [x] 1.3 用自动run `01396a864cc04a4ebcfd5935e9b4577a`证明FBBIK准确执行错误Goal、Rejected并非无查询、预测Path XZ未进入最终Goal。
- [x] 1.4 用run `00214d2c4c164be991ae4b2a382318ca`对账冻结路线、预测Root、实际Root与Simulation/Presentation时钟，撤销“冻结世界Foot Route完整XYZ驱动Swing”的错误合同。

## 2. 统一执行链

- [x] 2.1 将独立响应式Goal前置与Predictive Modifier后处理收敛为一个world-aware Foot Placement owner和一个最终Goal Set。
- [x] 2.2 删除旧Predictive Modifier作者节点、descriptor、operation、workspace与generated产品合同，不保留兼容路径。
- [x] 2.3 保持唯一World Query backend、Stance/Anchor、Pelvis和FBBIK owner。

## 3. 权威动作计划

- [x] 3.1 让Locomotion Sequence、原动画Pose、Action Step Fact与Clearance统一消费Simulation `LocomotionMotionElapsedTicks`，删除Presentation与Plan私有累计时钟。
- [x] 3.2 通过正式Document把Corin Walk/Run恢复为git基线Move Speed与`ConstantSpeed`，删除Gameplay Locomotion中的Action Motion Curve引用。
- [x] 3.3 删除Foot Analysis的Action Motion Clip/Curve绑定与Action Root产物，确保它只发布root-local脚、净空、接触与步时钟事实。
- [x] 3.4 在事件替换、Phase回退、动作中断和Stance捕获时结束旧Plan。
- [x] 3.5 保证同一事件只创建一次计划，提交后Route、Ground Path、Landing和Query快照不变。
- [x] 3.6 动画表现Delta直接由同帧已呈现Body Sample Cursor计算，删除`LocalLogicTick + InterpolationAlpha`第二时钟，保证Realtime、Rate Playback、Pause与Step切换不让动作相位倒退。
- [x] 3.6A Committed Body Sample Cursor未前进时保持上一帧已提交Pose，只更新VisualRoot、Equipment与Camera，不向Animation、Foot Grounding或预测计划传入伪造Delta。
- [ ] 3.7 修复同一Landing Event在多Contribution混合中选中新时刻样本却只按身份判定更新的问题，保证选中贡献的Phase、TimeToLanding与完整Foot Action Fact每帧同步进入唯一Plan时钟。
- [ ] 3.8 禁止Stored Pose、StateMachine退出状态和Inertial History提交被冻结的Landing Event与Action Step Clock；它们只参与姿势、速度、高度与Plant混合，预测动作事实由当前StateMachine目标、当前Inertial输入与最新Live Slot目标离散拥有。
- [ ] 3.9 让Simulation Locomotion Sequence在每次状态入口以`InitialTime`建立本状态的时钟原点，之后只消费同一`LocomotionMotionGeneration`内的Elapsed差值；Start、Loop、End与MovingTurn共享同一`Locomotion.Gait`左右脚Marker段，过渡同步后的相位必须重建目标状态时钟基准，证明状态切换后不会重新从0或整段移动累计时间取模。
- [ ] 3.10 Foot Analysis在同一次烘焙中把下一次对侧Landing的Delay、Event Ordinal与Cycle Offset原子写入本脚事件，所有正式Locomotion有限片段与循环片段都验证左右Landing严格交替；Virtual Ground只能消费本脚事件携带的原子配对，不得在运行时独立选择、混合或比较另一只脚的当前事实。
- [ ] 3.11 Start到同速Loop的Marker同步过渡必须在有限片段最后完整左右脚Marker段开始：Walk提前`0.30s`、Run提前`0.25s`，视觉Blend仍为`0.15s`。StateMachine/Slot的最新Live目标必须独立于逐脚Pose Blend Weight持续提交Landing Event、Phase与Route；Pose权重只混合Sole速度、高度和Plant。用新run证明两脚下一Landing事实均在LiftOff前连续可用，Start到Loop不再出现`LandingEventUnavailable`、晚建Plan或同一物理事件被`EventReplaced`。

## 4. Foot Route与Ground Path

- [x] 4.1 实现并证伪“烘焙Action Root X/Z + root-local Foot”的路线，确认它改变in-place移动真相并可产生约两倍未来距离，不再作为现行合同。
- [x] 4.2 实现并证伪当前原动画鞋底空间投影进度，确认它仍让冻结计划依赖逐帧Pose，不再作为现行执行合同。
- [x] 4.3 先收集支撑高度并平移预测Root/Hip，再完成坡度、支撑中心、Step、Edge和Ankle Reach过滤。
- [x] 4.4 用计划创建帧冻结的committed Locomotion Intent请求平面速度与同一in-place Pose脚骨局部姿态差生成未来Query Route，删除碰撞后Body速度、Action Root、动画位移曲线与`FootRouteWorldAlignment`作为第二运动模型。
- [x] 4.5 只用Simulation Action Step Phase确定性采样Ground Path，禁止当前脚世界位置、Render Frame或私有Elapsed改变执行进度。
- [x] 4.6 把末端无命中与具体拒绝原因保留到Plan、Gizmo和CSV。
- [ ] 4.7 证明PreSwing提前生成的Plan只查询`Constraint Release -> Landing`；`Constraint Release`取离散约束从`Locked`切到非`Locked`的精确最近采样交界且不晚于LiftOff。预测起点从生成相位正确累计Simulation位移与脚骨局部姿态差，并与同帧Native Sole连续；首个Executing Goal连续且不包含Locked阶段的预先抬升。
- [ ] 4.8 按同一动作时钟中的下一次对侧脚Landing把本脚Ground Path切成两个连续Upper Hull区间；Virtual Ground分割点必须来自权威对侧接触Phase与同一唯一Future Query结果，随Plan一次冻结。证明`6m/s * 0.6s = 3.6m`是同脚完整步幅而非速度重复，并证明执行不再把跨越两次左右脚接触的楼梯高度压成一条长斜线。
- [ ] 4.9 把每段Future Query改为“先选前后唯一正式支撑，再接纳同时连通两端的Sweep候选”；近竖直命中只作为Edge Plane，Ground高度只来自地形接触而不被无IK Query Route高度钳制。用新run证明下楼末端不再出现高平台保持到最后一段后瞬降，且上楼不再由无关高台分支污染Upper Hull。
- [ ] 4.10 在同一Plan创建事务内用发现Upper Envelope三维弧长把请求移动路程预算反解为不可变水平Route Progress，再生成唯一正式Route与Landing。用新run证明坡面/楼梯旧Plan不再比实际Root提前约39.56cm，平地映射保持恒等，执行期同一Plan的Route Hash与Landing不变。

## 5. 完整Foot Motion与Stance

- [x] 5.1 Swing最终鞋底使用当前原动画Pose的XZ与基础旋转，只用Ground Path Y加同相位Animation Clearance决定高度。
- [x] 5.2 用当前动画Sole-to-Ankle几何重建Ankle，禁止冻结Route覆盖世界XZ；Heel/Toe仅做同支撑面的最小Up安全修正。
- [x] 5.3 无Executable Plan时Swing保持上游动画；Stance只在真实接触后接管锁脚。
- [x] 5.4 预测Landing、Support Phase和Hip只输入现有Stance/Pelvis owner。
- [x] 5.5 用精确Action Step Clock消除离散Constraint/Support在LiftOff后的滞后；run `3cd52acdba8e41f6adabb7f51675a043`与`6e75aa54978e451d8370ec3baa8724ff`均证明Executing Swing在Approaching Contact之前的StanceCaptured为0。
- [ ] 5.6 证明约束开始释放时现有Anchor不会被立即清除，最终Goal按同一个Anchor Blend从Stance连续交接到预测Swing；交接期间Plan不提前完成，首段Goal没有水平跳变或重新下陷。
- [ ] 5.7 证明`Constraint Release -> LiftOff`区间不存在`Plan=Planned && AnchorBlend=0`的所有权空窗，Current Grounding不会在该区间接管Swing，且新采样的鞋底下陷明显低于run `6e75aa54978e451d8370ec3baa8724ff`基线。
- [ ] 5.8 证明Predictive Swing在冻结Current Path上完成最终混合后鞋底重检，Executing Swing左右Heel/Toe均无未解释穿透；Reach只验证真实锁定Anchor与唯一Pelvis Spring当前值，不再修改Spring输出、立即清Anchor或单帧归零Foot Goal。

## 6. 诊断与产品闭环

- [ ] 6.1 Gizmo区分Foot Route、Ground Path与Clearance Path；只画当前Planned/Executing计划，不显示文字或旧Completed路径。
- [ ] 6.2 CSV继续保持Header与每行同宽，并能区分末端NoHit、Reach、Step和Edge失败。
- [x] 6.3 完成Runtime与Editor编译、单一路径静态搜索、OpenSpec strict validate并关闭.NET build server。
- [x] 6.3A 将`character.build_float32_products`与`character.build_fixed_products`改为后台可轮询的正式MCP作业；业务执行仍只调用现有唯一Build Orchestrator，Play Mode必须拒绝，Domain Reload丢失作业必须显式失败且不得自动重放。
- [x] 6.3B 修复关闭Scene Reload时GameplayLab重复Play复用已释放`SimulationSessionHost`的问题；Session Host先于Actor Host开始新生命周期，连续两次停止后重启自动往返均保持项目Console 0 Error。
- [x] 6.4 通过Unity Editor发布精确Float32与Fixed Character Build产品。
- [ ] 6.5 自动分析平地、上楼和下楼的Plan覆盖、Route/Goal连续性、Heel/Toe下陷及FBBIK residual。
- [ ] 6.6 短测优于失败基线后完成至少30分钟回归，再执行8小时耐久门禁。
- [ ] 6.7 用干净双向短run证明`Pelvis Resolved == Pelvis Current`、活跃权重FBBIK residual接近零，并把剩余Current Grounding穿透按Plan Completed、Anchor Fading与Current Surface交接分别归因；`assembly-reload`中断run不得计入效果结果。
