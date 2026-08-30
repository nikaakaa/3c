# Locked Sliding世界误差响应实验

## 范围

以e6ca016/155326恢复版为对照；该版仍有踏空，不称质量合格或迁移前版本。用户批准仅修改Locked内部Sliding响应，五种顶层状态、转场、Anchor、Sliding目标生成、Landing首次接触、Swing、查询、旋转权重与膝盖不改。61615d4的膝盖实验已停止，临时Runtime改动撤销且未进入Unity Replay，本次不恢复它。

R482–484的Selected Target高度不变、Plant Residual为0，动画Sole上升36.557/88.519毫米，旧scalar每帧仅下降25毫米，最终中心间隙0/11.557/75.076毫米。目标正确而动画相对修正速度不能保持到位，是本轮直接靶点。

## 唯一响应域与数学

同一Correction Response Owner每帧只运行一个位置域：`AnimationRelativeScalar`或`SlidingWorldError`。只在本帧实际State Target Kind为`LockedSliding`时采用后者，不按Post结束后的State猜测。Support Direction角历史仍每帧推进并提供旋转，不借它旋转世界误差。

D仍由既有`SelectedWorldTarget + PlantWorldResidualAfterDecay`产生。Sliding的唯一持久位置历史为完整Vector E：进入域或发生已有正式Plant Capture时，E取上一实际Interpolation Response Output减本帧D；普通Sliding帧沿用E，不从新动画B、Support Direction或目标的正常水平滑动重捕。每帧正dt且E非零时，恰好一次`E = MoveTowards(E, zero, speed * dt)`；输出为`O = D + E`。推进不越过零，余量小于步长时直接到零，不另增完成容差。不能要求O的单帧位移受这个预算约束，因为D仍可移动。

速度数值保持既有1.8/1.5米每秒；依据待消除误差`-E`在本帧ComponentUp上的正负分别选择Increase/Decrease预算。纯横向选择两档较小值并发布Tangential事实；这是本项目明确政策，不是默认配置或ZZZ参数业务名复原。World域不再运行scalar公式，旧scalar数值字段只在AnimationRelativeScalar域有意义，World域为typed未执行占位；World误差不得冒充scalar零响应。

## 进入、退出与重置

进入/正式Target Capture先取得完整上一O，E_beforeAdvance=O_previous-D；同帧继续推进，不复活capture冻结，也不读已拒绝的WeightedGoal新接口。旧Visible读取门保持原样，本轮不启用它；World域使用既有持久Interpolation Output。

正常Sliding退出只到另一Plant响应或Releasing。前者已有TargetKind/LockResponse/State边沿捕获完整Plant Residual；后者已有StateEntered捕获完整Release Residual。退出本帧先由该原有残差链承接完整O并推进一次，再将scalar同步为本帧q，使O=D，不额外执行第二次scalar步长。这是带DomainTransferred事实的历史移交，不是首次初始化或Source reset；无dt时完整输出守恒。若退出却没有完整目标残差捕获，必须typed拒绝，不用只投scalar补齐。Hard Ownership Loss继续原Suppressed/Reset政策。

未初始化的首次合法Sliding输入按原首次合法输入政策同步D，E=0；不构造不存在的前帧。Source/Profile/World失效仍经原Reset，清活动域及其历史。根Bank、ApplyPostTransition、ClearCorrectionResponse必须携带或清除新域，不能留下失效E。

## 事实与验收

Runtime公开当前/上一Response Domain、DomainTransferred、WorldError CaptureReason、BeforeTransfer、BeforeAdvance、AfterAdvance、Advanced与MaximumStep。原Direction History、Plant残差、Desired点、Response点和实际Physical测量保留。唯一Diagnostics由指定任务迁移到facts56/diagnosis25，避免复用历史53–55；七维评分及37个质量Target的原规则不改。

完整同输入Replay核对R483–484是否消除再离地，所有Sliding入口/退出的完整XYZ、同Event Anchor、水平轨迹、Heel/Toe穿透、Contact间隙、Release、Reach、Pelvis与Solved Knee是否回归。R478–480的首次Landing尾差不在本轮修复承诺内。旋转权重低时，中心更靠地可能加深Heel穿透，必须看真实两端，不以中心归零或总分上涨接纳。

实施前用户Unity处于Play；只离线写代码/构建，不在Play Refresh，不擅自停止用户运行。Runtime及正式DTO已实现；规定flags构建1个既有警告、0错误，build server已shutdown。尚未加载或Replay，不声明视觉效果。

## Runtime事实接口

公开Motion DTO新增`CorrectionResponseDomain`、`CorrectionResponsePreviousDomain`、`CorrectionResponseDomainTransferred`；域为None、AnimationRelativeScalar、SlidingWorldError。新增`SlidingResponseErrorCaptureReason`（None/Initialized/DomainEntered/TargetCaptured，可组合）、`SlidingResponseErrorBeforeTransfer`、`SlidingResponseErrorBeforeAdvance`、`SlidingResponseErrorAfterAdvance`、`SlidingResponseErrorAdvanced`与`SlidingResponseMaximumStep`，共15个CSV标量出口。World域旧scalar Desired/BeforeRebase/Previous/Current/AppliedDelta全为未执行0；SelectedSpeed与DeltaDirection仍发布本帧选速，新增Tangential只用于纯水平世界误差。

World域的CorrectionResponseEvaluated仍为true，表示整层完成；Support Direction始终按既有10度合同推进，SelectedSupport旋转方向仍取这一输出。Parent Continuity Owner用World Error实际推进/未归零事实识别响应所有权，不能由未执行scalar的0反推无响应。退出域保留WorldErrorBeforeTransfer并清活动E，本帧scalar Current/Previous同步Desired，DomainTransferred=true且InitializedThisFrame=false。

静态复核确认正常Sliding退出没有绕过Release或Suppressed直接进入Swing的路径；后处理RotationProjectionUnavailable仍可能在Sliding内部产生Unavailable结果，但不改变实际响应域。本包原数据没有该Unavailable分支或Slide到新Plant/FullAnchor的动态覆盖，不能提前宣称它们已验证。

## 173423封口结果：未通过，位置域由后续Contact实验替换

Runtime=`9a24148`，Diagnostics=`9f5b539`。采样`20260830-173423-446-c78a7881826143bc84bd4f27d39ee169`为facts56/d25，1043采样帧、2086脚行；32个World域样本、16次进入及16次退出，退出全部Release，12次实际推进。所有原始输入/动画Sole/时钟与155326相同，50195行查询几何只变四项实例身份。官方Proof为baseline-created，独立比较155326的持久Proof则1044条frames与所有Runtime/输入/Body哈希完全一致，不改写官方结果。

原37规则与七维分数均未变，总分仍60.4。Contact Gap为12/60→10/60，但接触面穿透19/78→21/78，Contact额外输出405/1036→411/1038。R483/484中心11.557/75.076毫米→约0，Toe却由面上0.905/51.875毫米变为面下10.652/23.201毫米；L573/574同型，原RotationWeight=0未变。R485→486的物理Sole步111.017→144.812毫米，L575→576为114.929→145.883毫米。测量只证明对已验证ContactPlane的距离，不宣称实体Collider交集。

该候选没有解决Landing原残差尾差，不能以两个Gap事件消失或总分不变通过。用户要求优先修接触位置历史，未授权把这些退化隐藏为评分变化。候选原包不覆盖，持久Proof在`Diagnostics/FootPlacementReplayArchives/20260830-sliding-world-response/candidate-proof.json`，SHA256=`B2BF78FA9E97941C40716AC25AAB10BBA8A8D66766E685972612D3F2CB3BB9E7`。后续单一Contact残差实验删除第二Sliding世界误差，仍必须同时对照本失败前驱与155326，不声称本版本已验收。
