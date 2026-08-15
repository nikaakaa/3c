## 1. 唯一执行链

- [x] 1.1 收敛为`FootGrounding -> optional PredictiveFootPlacementModifier -> FinalIK FBBIK`。
- [x] 1.2 删除独立Predictive作者节点、第二Grounding、LegIK/TwoBoneIK、第二Pelvis和旧兼容ABI。
- [x] 1.3 保持一个World Query、一个Stance/Anchor、一个Pelvis Spring和一次FBBIK。
- [x] 1.4 FinalIK只执行统一Goal；Goal或Path先跳且solver residual接近零时归责Foot Placement。

## 2. 动画与动作事实

- [x] 2.1 Corin Locomotion保持in-place；Simulation唯一拥有作者Move Speed，Foot Analysis不再发布Action Root位移。
- [x] 2.2 Foot Analysis发布同脚Landing时钟、25点Foot/Ankle/Hip路线、Clearance、Constraint、Support与Orientation。
- [x] 2.3 Locomotion Sequence、Pose和Foot Fact共同消费Simulation Movement Clock，不保留Plan私有累计时钟。
- [x] 2.4 左右脚Landing使用稳定Marker身份并按同脚前后Landing定义线性Action Phase。
- [ ] 2.5 让当前StateMachine目标唯一拥有离散Foot Action Fact；Stored Pose、退出源和Inertial History不得复活旧事件。
- [ ] 2.6 Start、Loop、Stop和MovingTurn的Marker Epoch与Occurrence必须连续，状态切换不得让Cycle或Phase倒退。
- [ ] 2.7 Start、Loop、Stop和MovingTurn必须让当前同脚事件在LiftOff前成为权威PreSwing事实；Foot Placement不得按下一动作预造世界计划、运行时补造事件或逐帧重规划。

## 3. 冻结预测路径

- [x] 3.1 删除Action Root、输入幅值二次缩放、Terrain三维弧长重定时和`FootRouteWorldAlignment`。
- [x] 3.2 Ground Probe按`本脚Swing起点 -> 权威对侧Landing -> 本脚Landing`冻结为唯一分段直线；转弯顶点由同一KCC未来位置与Body旋转还原。
- [x] 3.3 冻结Animation Foot Route只负责投影出单调Foot Rate；对侧Landing塑造查询路线，但不得按对侧事件Phase强制本脚经过拐点。
- [x] 3.4 逐点Sphere与相邻Capsule Sweep取得踏面；先过滤Slope、Step、Edge、Center和Reach，再构造一次Upper Envelope。
- [x] 3.5 Plan冻结单调`Action Phase -> Path Fraction`，运行时不从当前Pose、Root或最近线段反推进度。
- [x] 3.6 Action Phase到达Landing后以`ActionCompleted`结束旧Plan。
- [x] 3.7 恢复Current Grounding合法支撑面作为最终只向上安全下界；删除该下界的run出现约左`7.3cm`、右`19.3cm`穿透。

## 4. Stance与Pelvis

- [x] 4.1 PreSwing由Stance拥有，LiftOff后Predictive接管，ApproachingContact通过同一个Anchor Blend交接。
- [x] 4.2 Current Query只提供当前支撑和接触事实，不重建Future Path或移动Landing。
- [x] 4.3 Predictive Foot Ground Envelope保持feet-only；Current与Predictive Body候选只进入现有唯一Pelvis owner。
- [x] 4.4 Pelvis保持一个连续Spring；旧的Pose Root单向向上换基已由第13节数据证伪并删除。
- [ ] 4.5 用新run继续定位楼梯结束后的Pelvis上移、Anchor释放循环和踏空交接，不通过新增Spring或调参掩盖。

## 5. 诊断闭环

- [x] 5.1 Scene、Game与CSV消费同一完成快照，保存Route、Probe、Envelope、Clearance、Landing、最大转向能力、Trajectory Curvature、Stance、Pelvis和FBBIK因果链。
- [x] 5.2 CSV采用流式gzip分块与manifest；当前Writer合同为1199列，并显式记录每脚鞋底支撑半径。
- [x] 5.3 Gizmo不显示文字；Executable画完整冻结几何，Rejected只画真实查询与拒绝几何。
- [x] 5.4 自动往返入口与普通Play分离，通过真实Gameplay输入在`teststart`和`testend`之间运行。
- [x] 5.5 自动run `666c8155d1604914bc1cd0db4fb502b5`证明Landing附近实际Root到冻结Root的平面误差中位约左`10.0cm`、右`21.8cm`，P95约左`39.6cm`、右`46.6cm`。

## 6. 当前路径修复

- [x] 6.1 将当前段冻结切线从碰撞前请求速度改为创建帧committed `Body.TargetVelocity`；禁止使用包含Presentation纠错的`VisibleVelocity`。
- [x] 6.2 保留Simulation Motion Timeline描述Timed段剩余时间与确定Continuation，Plan创建后不再读取Body或输入。
- [x] 6.3 将Movement最大转向能力与实际轨迹曲率拆开：前者只验证速度方向变化是否连续，后者同时驱动KCC未来圆弧和root-local动画几何旋转。
- [x] 6.4 Runtime C#构建通过，0 error，并关闭.NET build server。
- [x] 6.5 完成精确Float32与Fixed Character Build、OpenSpec strict validate和单一路径静态搜索。
- [x] 6.6 运行新的自动双向短run，验证Header与每行均为1207列、Console 0 Error、Plan几何冻结。
- [x] 6.7 对账新旧run的Foot Rate、Ground Probe拐点、Goal Y、Final XZ、Heel/Toe物理残差和FBBIK residual。
- [x] 6.8 复用不污染普通Play的确定A/D连续转向段，证明实际轨迹曲率约`42–54°/s`，Movement节点`720°/s`只是能力上限；直行曲率保持零。
- [x] 6.9 Simulation/KCC发布一次冻结的碰撞求解后未来Body XYZ轨迹，并以同一Trajectory Curvature积分圆弧；不得改用逐帧重规划、当前Pose投影、阈值调参或响应式兜底。
- [ ] 6.10 数据指标显著优于失败基线后，再进行30分钟回归与长期耐久；编译、Build和无报错不得替代效果验收。

## 7. Ground Probe与Swing起点修复

- [x] 7.1 将对侧Landing从“本脚同相位位置上的高度样本”改为Ground Probe精确空间顶点；固定run中旧平面错位约`0.59–0.81m`，新顶点坐标误差为0。
- [x] 7.2 将查询路线采样与Foot Rate映射拆开；查询沿三点折线按空间长度采样，Foot Rate由本脚冻结动画路线对整条折线做最近投影并单调化。
- [x] 7.3 删除按对侧事件Phase强制本脚穿过折线顶点的映射；该方案会产生约20%的离散进度跳变。
- [x] 7.4 计划早于LiftOff创建时，以Stance锁脚点重基Swing Foot路线，排除生成到LiftOff期间的Body位移；固定双向run首步最大Foot Rate跳变降至左约`8.1%`、右约`7.4%`。
- [x] 7.5 Predictive Modifier保留当前动画XZ和Sole-to-Ankle几何，Swing Y直接消费`Ground Envelope + Animation Clearance`；不得把冻结Query Route XYZ写入Goal。
- [x] 7.6 将设计与经验文档压缩为现行所有权、公式、数据约束和已否决方案，不再追加逐轮修复流水账。
- [x] 7.7 用不污染普通Play的连续A/D圆周段验证KCC位置圆弧、轨迹切线旋转、三点Ground Probe和Animation Foot Route消费同一冻结Trajectory Curvature。
- [ ] 7.8 继续收口Start、Loop、Stop和MovingTurn的当前Landing身份；不得保存、晋升incoming世界计划，不得在状态切换后补造当前事件或重查同一Landing。

## 8. 台阶边缘旧计划修复

- [x] 8.1 用v87固定run证明边缘跳变不是Surface A-B-A：旧incoming计划在Landing附近让实际鞋底与冻结Path错位约`28.8cm`，同帧Current安全下界补高约`22.7cm`。
- [x] 8.2 删除左右脚incoming计划存储、Future Query、晋升、交换和对应End Reason；当前权威PreSwing成为每脚唯一计划创建边界。
- [x] 8.3 v88固定双向run保持1207列且无宽度错误；台阶段鞋底/Path错位P95降到左约`5.15cm`、右约`7.53cm`，额外Current补高最大降到左约`2.17cm`、右约`0.001cm`，同计划Surface A-B-A与Progress回退均为0。

## 9. 唯一Swing高度

- [x] 9.1 用v88证明`max(CurrentAnimatedSoleY, PredictedSoleY)`在楼梯段左右脚分别切换59和58次，并出现26和16次往返；该分支是两个高度owner竞争，不是GDC Ground Path合成。
- [x] 9.2 删除Native Y与Predicted Y的逐帧`max`；最终Swing Y唯一等于`Ground Envelope + Animation Clearance`，Current Grounding只保留最终向上物理安全下界。
- [x] 9.3 v90固定双向run共768行、1207列且无宽度错误；Heel/Toe物理穿透均小于`0.001mm`，左/右Current额外补高最大约`2.15cm/0.96cm`，FBBIK residual保持近零。

## 10. 鞋底配置空间边缘

- [x] 10.1 用v89定位两个约`12.6–13.1cm`Current补高帧：鞋底/Path XZ只差约`0.1–0.6cm`，但Envelope仍在墙面之后插值低高踏面，错误发生在Edge Fraction而非时钟、Surface切换或FBBIK。
- [x] 10.2 Future Capsule的近竖直命中沿平面外法线扩张现有`SwingCapsuleRadius`，以胶囊中心接触位置生成Edge Fraction；不新增查询、参数或高度补偿。
- [x] 10.3 v90固定双向run验证边缘斜段已按胶囊中心接触位置前移；大于`5cm`的Current追高帧由v89的2帧降为0，左/右最大追高由约`13.15cm/2.81cm`降为`2.15cm/0.96cm`，未新增超过`1mm`的Heel/Toe穿透。

## 11. 诊断合同收口

- [x] 11.1 删除已不存在执行计划的Incoming Plan生命周期、速度、CSV和Inspector字段；保留Incoming动画事件事实用于状态切换诊断。
- [x] 11.2 v92短run c0cf76a5036741c2a4235c86524c381f 共640行；Header与每行均为1189列、列名唯一、左右脚各566个字段完全对称、Console 0 Error，Heel/Toe穿透、Lift与FBBIK residual相对v90未回退；v91因删除旧字段后序列偏移仍沿用旧宽度而作废。

## 12. 所有权连续性回归

- [x] 12.1 用v92证明当前回归不在FBBIK：正常PreSwing计划的LiftOff连续偏移几乎始终为0，左右脚首帧预测/基线高度最大错位约`19.72cm/26.37cm`；提前退出的20个计划均以`AnchorBlend=0`交接；上楼有效Plan只有约`1.1%`可提供预测Pelvis。
- [x] 12.2 Swing Foot Route从`PathStartPhase`锁定鞋底同时减去Body与局部脚基值；Plan无论在PreSwing还是Swing创建，都在提交时建立LiftOff高度连续偏移。
- [x] 12.3 Plan提交后只用同源committed Body速度与Trajectory Curvature计算剩余Landing平面误差，并与现有鞋底查询半径比较；删除Desired Input零值、转向布尔和方向符号中断。
- [x] 12.4 Predictive Body Support Path不再要求同一摆脚仍处于Supporting/Releasing；下一Executable Landing可独立进入现有唯一Pelvis owner。
- [x] 12.5 Anchor在当前安全Goal处原子取得完整世界所有权；只有`PlantContact + 有效Anchor + 完整Blend`报告Anchored，释放Blend期间报告Contact。
- [x] 12.6 删除以in-place脚相对Root全速度推断锁定的错误烘焙；从同一Plant区间提取精确`Release / LiftOff / ApproachContact`边界，并提升Artifact算法身份使旧产物失效。
- [x] 12.7 用当前Calibration重建的Heel/Toe计算鞋底平面支撑半径，台阶立面沿外法线按`max(SwingCapsuleRadius, SoleSupportRadius)`扩张后再生成Edge Fraction；`MaximumEdgeGap`仍只表达几何间隙，未新增查询、参数或固定高度。
- [x] 12.8 删除25点`Constraint / Support / Orientation / Body Pivot`离散路线；Runtime只按同一权威Action Phase和精确事件边界解析`Locked / Sliding / Unlocked`与`Supporting / Releasing / Unsupported / ApproachingContact`，不允许采样格把真实LiftOff推迟到相邻帧。
- [ ] 12.9 完成Runtime与Editor编译、精确Float32/Fixed Character Build、OpenSpec strict validate、单一路径静态搜索和新双向CSV对账后，才判定本节实现是否改善。

## 13. 动作所有权、Landing交接与上坡骨盆回归

- [x] 13.1 用固定双向run `c19024e53faf4c7eafa78ad8a696d67e`证明恒定输入下仍有26次假`ActionInterrupted`；上坡Root逐帧抬升时Pelvis Target已回到0，Current却长期停在约`-0.195m`，FBBIK仍准确执行Goal。
- [x] 13.2 Plan失效检查改为在真实LiftOff后对账当前Root位置/朝向与同相位冻结KCC状态；不得用Action Phase猜Simulation段切换，也不得用台阶碰撞后的瞬时Target Velocity误判输入变化。
- [x] 13.3 删除Pelvis Spring对Pose Root向上位移的单向反向换基；权威Root位移直接移动身体，唯一Spring只输出支撑腿附加位移。
- [x] 13.4 ApproachingContact把同一Executing Plan的Ankle、旋转与冻结Contact Surface原子交给现有Stance/Anchor；权威事件接触不得再被in-place鞋底局部速度否决，也不得混用Current Query的另一踏面或静默切换支撑面。
- [x] 13.5 双向run `d15cb7b7a2f0463c864a73605f8dfef4`证明上坡零Target区间Pelvis Current中位由约`-0.193m`改善为约`+0.0005m`；同时证明主路线Root仍与冻结KCC轨迹重合时，速度比较仍产生33次假中断，全部ApproachingContact仍因`4–6.7m/s`局部脚速无法捕获Anchor。
- [ ] 13.6 用下一新run验证主路线假`ActionInterrupted`归零、ApproachingContact捕获完整Anchor、上坡Pelvis不回归，并继续定位同Plan Path/Goal Y剩余跳变。

## 14. 身体支撑坡线与同采样地形残差

- [x] 14.1 用自动双向run `df93b1fbbf0240a181a835cacd105e2f`证明Landing交接与旧Anchor问题改善后，同Plan上坡仍有约`35–43cm`的Component Goal逐采样跳变；实现中的`Predictive Body Support Path`只有布尔有效位并直接转发离散KCC Root/Hip，未实现Spec要求的身体支撑坡线。
- [x] 14.2 Future Query改为只应用`合法支撑高度 - 同相位Ground Probe高度`残差；碰撞求解后的Future Body XYZ不得再叠加相对计划起点的整段地形高度。
- [x] 14.3 每个Executable Plan冻结`当前合法支撑 -> 可选对侧Landing -> 本脚Landing`的Root/Hip支撑锚点；运行时按同一Action Step Phase分段插值Component Up高度，平面位置仍消费同一冻结KCC轨迹。
- [ ] 14.4 用新双向run验证Body Support Path不再等于离散KCC Y、上坡Pelvis Target连续且同Plan Goal跳变显著下降，并对账Anchor、Heel/Toe、Current安全下界和FBBIK residual无回退。
- [ ] 14.5 完成Runtime与Editor编译、精确Float32/Fixed Character Build、OpenSpec strict validate和单一路径静态搜索。

## 15. 回退基线后的路线对齐单变量修复

- [x] 15.1 将代码、资产、场景与既有OpenSpec实现恢复到提交`bfb571868a58edf1b9d3c1b19844a57e4d022491`，只保留压缩后的失败经验；删除会继续参与Unity编译的未跟踪自动路线脚本。
- [x] 15.2 区分创建帧原动画鞋底与Ground Probe支撑起点：Root Trajectory以`EventPhaseAtGeneration`的Native Sole对齐同相位未对齐动画路线，生成一次平面刚性偏移并整步冻结；删除从`PathStartPhase`开始向Landing衰减为零的路线偏移。Ground Probe继续只拥有地面查询起点，不取得动画路线坐标所有权。
- [ ] 15.3 用同一版本新run对账创建帧Native Sole、冻结Animation Foot Route、Landing XZ、Foot Rate、Goal连续性、上楼/下楼Heel/Toe距离与FBBIK residual；未获得数据与观感改善前，不再修改Anchor、Pelvis、查询阈值或有符号高度。

## 16. 静止回原动画的单一Stance修复

- [x] 16.1 明确`GroundedStationary + 无权威Step`只保留Current Grounding接触与坡面安全，不再把静止接触定义为新Anchor捕获。
- [x] 16.2 在现有Stance owner中让已有Anchor通过原有Blend连续释放到Current Grounding，并从旧Anchor鞋底面与Pelvis Reach中同步退场。
- [x] 16.3 完成单一路径静态搜索、Runtime与Editor编译、OpenSpec strict validate，并在Unity效果验证前提交该单变量版本。
- [ ] 16.4 在已打开GameplayLab验证平地停步回原动画、斜坡静止不穿透、重新起步恢复正式Stance规则；运行生命周期错误若阻断验证，作为独立根因处理。
