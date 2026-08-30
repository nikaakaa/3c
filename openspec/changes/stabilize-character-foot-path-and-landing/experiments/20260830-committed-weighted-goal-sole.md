# Committed Weighted Goal Sole读取实验

## 状态与范围

前驱为dd792d3记录的141256位置basis候选；该候选靶点有效，但出现Right404–412新下陷，未作为可用修复接纳。原质量基线仍为c519865/130545。本实验只接通已有但始终不可读的上一加权Goal脚底参考。93815d9完成Runtime，9979aa8完成Diagnostics；150516实际Replay与两份基线完成对账，结论为拒绝当前组合。读取合同确已运行，但接触离面和新增穿透不能作为修复交付。两轮候选源码、原始包、Proof与失败原因均保留；恢复原基线后的Replay另行记账。

`HasFrame`是Pending开放标志，SealFrame清它是正确行为。旧TryResolvePreviousVisibleOutput把它当Committed结果可读条件，导致085503、130545、141256的VisibleOutputTransferred均为0。新实现将用参考本身的Frame、Completion、Side、世界点、Position/Rotation Weight及Bank/Resolved Pair身份；没有历史明确Unavailable，已有历史却不合法直接拒绝。

保存的点是最终Foot Goal经Constraint/Reach与权重之后推算的Sole，不是最终物理骨骼点。零权重时仍是动画基准：130545 Left349/853的实际物理Sole分别比该参考高约25.607/27.755毫米，随后350/854发生Capture。因此必须分别测量参考连续与真实Physical跳变，不能称为零风险布尔修正。

## 唯一行为变量

正式Plant Target Capture时，原来不可达的合法上一Weighted Goal Sole输入首次参与完整XYZ Residual捕获和既有dual scalar重基。非Capture帧不重基；Residual仍同帧推进一次。位置basis、Support角历史、脚高、Target Height模式、半衰期、两档速率、Goal权重、Contact/Lock规则、查询几何均不改。

不把这叫作ZZZ直接复刻。保留项目WorldAnchor/Sole政策，不引入g/k/W/199、最终Pose低通、清Residual.Y、相对动画Capture、单scalar替代XYZ或第二输出路径。

## Runtime接口与清理

`CharacterFootWeightedGoalSoleReference`为唯一不可变参考，公开Available、FrameSequence、CompletionIdentity、Side、WorldSole、PositionWeight和RotationWeight。Bank仅保存左右两份该值，移除HasVisibleFootOutputs及左右VisibleSole；Begin、Discard和Reset清空Pending参考，SealFrame仍关闭HasFrame。读取会拒绝开放Bank、非有限参考、Frame/Completion/Side/Rig不匹配及未来帧；已发布Foot结果却缺参考也直接拒绝，不静默补值。

Frame消费上一参考，Lifecycle Fact保留该输入；最终Goal编码后将当前参考加入同一Foot Motion结果。Diagnostics公开PreviousWeightedGoalSole与CurrentWeightedGoalSole。原PreviousResponseOutputAvailable/Point诊断名改为ContinuityReferenceAvailable/Point，表示本次连续化实际使用的点；内部持久PreviousResponseOutput仍保留真实Response历史。原VisibleOutputTransferred改为CorrectionResponseWeightedGoalSoleTransferred，没有旧别名。

正式Capture把完整参考传入唯一ApplyCorrectionResponse，不再传拆开的bool与Vector3。普通Swing/Release传Unavailable，不激活重基。加权Goal Sole的原纯几何解析同时服务既有Pelvis输入与最终引用封装，未重复加权；实现期首次构建暴露2处前置消费者仍需该纯函数，已恢复统一复用并通过构建，未把编译失败当作Replay结果。

## 对账

同时比较本轮与141256、130545。先验证输入/Body/原动画与查询几何一致，再验证参考身份、上一/当前值、实际Capture来源与scalar/Residual公式。重点包括Right404–412下陷、Right476–484原有悬空、Left349→350及853→854零权重后Capture、12组稳定ABA、所有37Target、Solved Knee翻侧时点、Reach与Pelvis。

141256已产生的好处不能抵消接触新坏点；只比有回归的前驱提高分数也不能算通过。代码、Runtime/Diagnostics独立提交和原始Replay包都保留，失败后不覆盖样本。

## 150516正式证据与可比性

输入仍为`43357ff3cd384e5cba75d2c31175b116`，1044输入帧。原始包为`Diagnostics/FootPlacementRuns/20260830-150516-677-f57b917b10ca447ab2b5c580ee7fdc66`，1043输出帧、2086脚行、1166列、50195几何行，facts55/diagnosis24，quality-score/1和全部37项rules/scorePolicy不变。

| 文件 | SHA256 |
| --- | --- |
| samples.csv | 47f53f204e6589521e3695462cf96224acaa102fb5ab4ab96b0aa6ee44c53d1b |
| ground-path-geometry.csv | a6262d5c70585733f56e5848fbdcff5e3ab6ee31c0275ea87342af2be1787cb1 |
| facts.json | 5dc704e2900eb70d8e89ac1787b7f5e8afc1f55e6e9a17af419e6231bed0c81f |
| diagnoses/quality-score.json | 6a2787866447d497773079b90fcd83abd5052266f8867255b3a201f202cf7fcc |
| 持久candidate-proof.json | 9035fe35227fce97a12294f4a8bb2a6b7b4a0ecbdca00b418b39003d70799201 |

Proof副本位于`Diagnostics/FootPlacementReplayArchives/20260830-committed-weighted-goal-sole/candidate-proof.json`。原位置为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260830-150655-065-ed8d57a4ec0a46888071001c0d0f43d0.json`。官方对141256为matched1044，逐帧分歧0；直接对130545持久Proof的1044条frames也完全相同，但对原基线仍有A轮Program/Projection身份换代，不能改写官方比较对象。Body、正式输入、OriginalSole、时钟一致；geometry只变SampleIdentity、GroundPathInputIdentity、GroundContactSurfaceIdentity、GroundContactCandidateIdentity。

## 参考与数学的实际覆盖

Current与Previous参考均2086/2086可用，2084组相邻carry逐字段一致。首样本左右各引用未采到的Frame2，正式状态为PreviousSampleUnavailable，不伪造前帧。78次Plant Capture全部执行Transfer，其中69次active residual同帧衰减、9次没有active residual；无Capture却Transfer为0。所有Capture之前Response均已初始化；初始化及初始化加Transfer动态样本仍为0。

独立CSV重算最大误差：完整Capture约2.895微米，Decay约0.029微米，Desired约4.717微米，q约0.312微米，Previous scalar约0.144微米，Response约0.464微米。Desired误差保留约40米世界坐标float减加的舍入边界。以p为上一Goal参考，A为当前目标，a/h为位置basis，R=p-A、D=A+R'、c0=h·(p-B)，实际输出符合：

`O = p + (I - a h) (R' - R) + a δ`

没有丢掉完整XYZ，也没有恢复8bf2的相对动画Capture。78个参考与实际上一Response距离中位仅0.00023毫米、最大0.01039毫米，因此本次主要作用是激活scalar重基，不是突然取得更接近Physical的点。73个零Position Weight样本的Goal参考与Physical Sole距离中位20.389毫米、P90为120.427毫米、最大184.315毫米；2013个正权重样本最大约0.00503毫米。不能把正权重的相等推广到零权重。

## 三版质量对账

下表顺序固定为原基线130545、位置轴141256、接通参考150516，计数为matched/eligible。

| 指标 | 130545 | 141256 | 150516 |
| --- | --- | --- | --- |
| 最终接触平面穿透 | 19/78 | 20/78 | 21/78 |
| 持续接触未贴合 | 12/60 | 12/60 | 14/60 |
| 接触状态输出扰动 | 405/1036 | 405/1035 | 403/1035 |
| 接触首帧连续性 | 49/54 | 48/54 | 49/54 |
| Swing到Landing交接 | 15/53 | 15/53 | 8/53 |
| Plant输出扰动 | 315/523 | 315/523 | 314/523 |
| Locked垂直证据 | 1/25 | 2/25 | 0/25 |
| FullAnchor水平漂移 | 0/8 | 0/8 | 0/8 |
| Stable Swing输出扰动 | 145/347 | 147/348 | 147/348 |
| Path输出扰动 | 206/680 | 208/680 | 208/680 |
| Release内部Correction反向 | 2/59 | 2/59 | 3/59 |
| Landing退出扰动 | 49/60 | 49/60 | 49/60 |
| Landing腿伸展 | 0/2 | 0/2 | 0/2 |
| 参考总分 | 60.4 | 60.4 | 56.7 |

全部37项规则与评分政策一致，新增包相对141256的coverage一致。评分下降仅因Left341→342 Stable尾部从98.289毫米增到101.561毫米，跨10厘米尾档；不能把3.7分作为整体严重度差。Path放大Evidence与Path质量是同一批事件，不重复扣分。Action、同Event重入、初始化加Transfer、非单位dual等仍没有动态覆盖。

### 接触首步变小与离面变大是同时发生的代价

Right959上一参考p.y=2.49541759且Position Weight为1，不是零权重错源。新旧Selected Target、Desired与完整Residual相同：A.y=1.97999978，D.y=2.33068752，Captured R.y=0.5154178、AfterDecay R.y=0.3506877。前驱Previous scalar为0.11286068，增加30毫米后输出Y=2.145009；候选按p-B重基为0.4932692，减少25毫米后输出Y=2.4704175。

由此，真实Sole世界Y单步下降从350.409毫米缩到25.000毫米，完整3D位移从409.295毫米缩到212.981毫米；同时中心离接触平面从165.009毫米变为490.418毫米。Ankle单步318.602到98.840毫米是另一测量点，不能混作同一指标。Right959–960仅两帧，不是新增持续段命中；961后的Release继承高输出，964新Event再Capture，才形成新增持续离面。959–971约200毫秒跨度跨过Event换代，不能写成同Event连续。

该处Clip Source改变但SourceLineage读取的是PosePlanHash；Plan仍相同且InitializedBefore=1、InitializationReason=None。这是现行合同中的持久Response Capture，不是漏采初始化，也不授权借Clip切换重置历史。

### 其它必须保留的坏窗与改善

- Right404–407 Heel穿透峰值为7.299/24.733/13.679毫米，缓解前驱但未恢复原基线。FullAnchor沿Up下陷0.364/11.320/2.995毫米；随后Sliding下陷5.610/14.123/约0毫米。新407提前进入Locked，是相对前驱唯一State变化；407仍距Anchor约9.072毫米，不把低于1厘米阈值写成零漂移。
- Left572–574新增穿透4.554/4.554/11.854毫米；Right447–448已有穿透10.651/10.651/23.201毫米。另一方面Left638–642改善至66.712毫米、末尾Left1036–1045改善至177.666毫米。全包穿透最大187.069/187.071/177.666毫米，尾部下降不能抵消新增坏窗。
- Left782–790和Right964–970新增持续Gap，ScoredGapMax分别163.543与292.366毫米。WholeFootGap最大296.812/296.812/490.405毫米，P90为133.878/133.878/171.791毫米。
- Right478–480中心净空仍约11.706/7.964/5.419毫米，等于WorldResidual尾差且Response已到Desired；Right483/484仍11.557/75.076毫米，是Residual为0时相对动画scalar债。原毫米悬空未修复，不能统称两层实际拖延。
- Left349/853零权重参考与Physical相差26.460/27.386毫米。接着350首步81.977到52.350毫米，但351从38.526到69.081毫米；854首步74.185到63.159毫米，但855从32.348到41.775毫米。证据只支持首步少动、追赶后移，不支持Physical绝对连续。
- 新Release反向命中Left791–798中，Correction方向点积约-0.831，但Physical位移方向点积约+0.976；只称内部Correction反向，不能叫新增肉眼回弹。

### Swing、膝盖和下游

原91个严格稳定窗口仍可一一对齐，12组Direction ABA中原6组非零额外XZ全部归零，另6组原本就是0。物理附加二阶位置差中位1.507/0.351/0.351毫米，P90为18.753/13.355/13.356毫米，大于20毫米窗口8/6/6。位置轴靶点收益没有被撤销，也不代表全包通过。

Right933仍约493.7毫米SolvedKnee单步；原基线大翻侧在934，候选提前到933。新Right736/737的Knee单步由前驱76.654/244.708毫米变为303.700/16.890毫米，Right798/799也出现翻侧提前。全包SolvedKnee相对输入Knee的额外offset步大于5厘米为392/389/395、大于10厘米为78/76/77，最大约592.958毫米基本不变。CSV没有最终Physical Knee列，不能冒称最终骨骼实测。

三版FBBIK均2086/2086成功，Ankle残差最大0.715/0.715/0.995微米；Reach评估1945次且Unavailable/Clamp均0。Goal被准确执行不意味着膝盖稳定或目标不穿面。Pelvis相对前驱最大约-33.268毫米，相对原基线最大+45.970毫米；同持有期间Anchor几何与获取身份不变。

## 正式处置与失败经验

拒绝150516与141256作为无回归工作版本。Diagnostics分别用1a81927、8812436精确撤销，恢复facts52/diagnosis21，与b8ed3c8诊断目录一致，间隙诊断和七维评分保留。下一步只恢复本任务7个Runtime及4个Corin配置/生成产物到c519865，任务勾选同步撤销，再Edit Refresh和同输入Replay证明恢复；不动既有proposal/project修改，不删除任何失败包，不在候选上叠膝盖或新速率。

已证实的读取缺陷与位置轴职责并不因撤销而变成正确；本次恢复是恢复实验前行为以隔离下一变量。以后重做这两层必须同时证明Contact触地时间、完整世界连续性、相对动画修正速度与脚掌旋转时序，不能仅以公式无误、首步变小、局部ABA消失或总分变化宣布完成。
