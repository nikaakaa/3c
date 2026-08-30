# 持续Goal第三轮：目标时序排除与背向速度候选

## 当前状态

Runtime候选2edf9bc仅移除清速度的Handoff前置。频率仍3Hz，目标、硬边界、全部Foot/Anchor/旋转/Bend和资产不变。033902正式Replay后确认局部靶点成立，但因R996→997膝盖翻侧后移及放大，不接纳为可用组合；恢复原门，不能作为已通过基线。

固定效果对照193957；直接前驱为已验证恢复的025450（3a909f2）。本轮只读模型消费025450原始CSV，不新增采样器、诊断规则、测试代码或正式facts。原62与全部旧包保留。

## 先排除的目标资格假设

Module同帧先由Resolved Pair创建Foot Goal，经过原ResolveWeightedGoalSole得到左右有效目标，连同原动画Sole放入PelvisFrame。ResolveIntent继续要求正式Swing/PrimarySupport/NextLanding与GroundPath；不满足时进入原Pelvis Releasing，ResolvePelvisRelease将首选目标设为0，再受统一硬Reach约束。

025450有455个实际共同高度求值帧、588个Releasing帧；后者134个MissingSwingLanding、454个SupportUnavailable。这些帧并非默认原Pose：源Pose和双脚Ready目标均有效、Foot PositionWeight均1，能只读重算共同高度参考。该参考在455个正式求值帧与已发布值最大差0.390微米，属于现有Goal编码/float精度边界。

588个Releasing参考中，382帧大于+5毫米，122帧小于-5毫米，中位+41.286毫米，范围-181.071至+335.352毫米。因此“此处普遍丢掉了下降需求”不成立；若直接继续采用，会经常提高骨盆目标。本轮不改变该生产资格。

## 冻结输入的有限模型

从Frame3已记录的Spring状态播种，对Frame4–1045按原dt、真实正式权重、目标、姿态偏好和硬区间递推。原3Hz公式Output最大误差5.04e-8米。模型中的World值由本帧已发布AnimatedPelvis加模型加权偏移构成，不冒充实际新Writer测量；没有模拟FBBIK或之后状态反馈。

| 只读变化 | 世界Y步超过50毫米 | Correction步超过50毫米 | 负偏移积分 | 结论 |
| --- | ---: | ---: | ---: | --- |
| 原3Hz | 33 | 30 | 0.34073 m·s | 复现控制 |
| Releasing也使用共同高度 | 87 | 47 | 0.29732 m·s | 不能以负偏移减少掩盖更多世界大步，未实施 |
| 删除目标的姿态偏好夹取 | 35 | 32 | 0.33613 m·s | 未改善关键窗口，未实施 |
| 硬夹后以实际位移反算速度 | 41 | 39 | 0.39441 m·s | 加重后续惯性和负位移，未实施 |
| 速度限制随当前/上帧硬边界移动 | 41 | 39 | 0.41043 m·s | 同样加重，未实施 |
| 所有帧加入线性目标速度反馈 | 148 | 118 | 0.23971 m·s | 明显放大，未实施 |
| 无Handoff帧才加入该目标速度反馈 | 54 | 14 | 0.36650 m·s | 相对修正变小但世界运动更差，未实施 |

所谓线性目标速度反馈是改变速度反馈目标的另一控制公式，并非证明现有Critical Spring数学算错。边界速度方案只用既有两帧边界的割线，不声称恢复了真实连续碰撞时刻。上述模型都没有完成清零分支；不把它们提升为已经运行的Replay结论或某一类算法普遍不可用。

另试过把Spring输出限制在上一输出与当前目标之间的只读模型，但它出现3个完成清零点，超出了冻结原生命周期的递推假设，未拿其全程数字作为有效候选排名，也未进入代码。

## 本步的直接反例与公式

265帧同支撑同事件、Handoff=None，target=-0.0142204762米、previousOutput=0.0319810323米、previousVelocity=+0.45881778米每秒。旧门因无Handoff不清速度，输出先上升至0.03571212米；266硬上界到-0.0144094229米，再发生约50.122毫米Correction下降。目标已经更新，旧速度仍使输出先向反方向移动。

新门只检查已按正式硬Reach夹取的target：

`direction = target - previousOutput`

`velocityReset = abs(direction) > GeometryEpsilon && previousVelocity * direction < 0`

`inputVelocity = velocityReset ? 0 : previousVelocity`

随后仍使用原频率3Hz的一次Critical Spring和原硬夹紧/朝外速度处理。Handoff事件判定与输出事实不变；VelocityReset不再意味着一定发生Handoff。没有重置位置、没有前视、没有目标范围夹紧或其它新参数。

只读预测Correction超过50毫米30→28，世界Y超过50毫米仍33，P90约42.947→42.719毫米；负偏移积分约0.34073→0.34056米秒，允许回正条件下的迟滞208→209。266预测Correction下降约44.536毫米而非50.122毫米；267、322、466、R826等不保证改善。这只是准入小步，不缩小持续Goal的完整质量目标。

业务取舍是优先跟随本帧有效目标，而非保留背向目标的旧速度；代价可能表现为速度/加速度转折和下游膝盖时点改变。最终世界骨盆还叠加原动画和Root移动，所以不承诺世界位置单调或硬边界永远无突变。

## 实现与验证边界

Runtime只改CharacterFootStrideHipsBuilder的一个条件，规定flags构建27既有警告0错误并shutdown，当前change strict与diff通过。Diagnostics由原指定任务迁移精确公式及版本，不增加CSV列，不改37个质量Target、eligible、阈值或七维评分。

必须由正式Replay核对：新无Handoff清速度帧是否真实发生、265/266及全包Input/Target/Reach不变、Foot保护、世界和Correction大步、负偏移与迟滞、实际Solved Knee及相对额外Knee变化。旧62在新门触发帧会与新公式冲突，不能离线改发为新版本伪造通过。

## 实际Replay与独立验收

Runtime 2edf9bc、Diagnostics 586c828。后一提交仅3个Diagnostics文件6行增删，facts63/Analyzer63/diagnosis32，CSV仍1221列；Editor规定flags构建57既有警告0错误并立即shutdown，全量strict95/95。没有资产或Program/Projection变化。

新包20260831-033902-719-ba011a31952443d996821aac829c05af，1043共享帧、2086脚行。034036 Proof对025625官方matched1044、aggregate空、Divergent0；Runtime identity及完整1044 frames、trace/start/input/body hash逐值相同，samples SHA匹配。采样与Finalizer结束、failure为空后退出自己启动的Play，Unity回Edit。

143个共同目标/硬Reach/Posture/Body/原动画输入列逐值相同，geometry50195行只有四身份列不同。全帧仍实际使用3Hz；新速度门、InputVelocity、Handoff三事件重算均无矛盾，一次Spring复算的未夹紧Output最大差0.025微米。

总清速度99→124，29帧新增true、4帧不再需要清；30帧在Handoff=None时清速度，其中148原本已经清速度，只是历史输出改变后Handoff从TargetCrossedOutput变None。因此30不是净新增数，也不说明Handoff事件公式被改。

265的新InputVelocity为0，输出30.126758毫米；旧输出35.712120毫米。本帧Correction步由+3.731变-1.854毫米，世界Y仍随Root/原动画上升，41.867→36.281毫米。266仍真实夹到-14.409423毫米，Correction下降50.122→44.536毫米，世界下降3.519变上升2.067毫米。789的Correction下降57.856→48.711毫米。

Foot State/Goal位置/权重无差异，最终Ankle/Heel/Toe最大变化0.580/0.660/0.580微米；ResolvedEffectiveSole逐值相同，实际加权Pelvis均在正式硬区间内。原525 Contact行、穿透34/90、持续Gap3/60保持。全部37项规则、eventKinds、计数、rate、scorePolicy、完整Health/Evidence及occurrence相同，总分61.9不变；measurements有真实腿姿态变化与微量舍入，不写成所有测量一致。

## 剩余问题和明确代价

与直接前驱比较，Correction超过50毫米30→28，仅移出266/789，无新增；P90 24.774→24.634毫米，P99和最大值不变。世界Y超过50毫米的33帧集合完全相同，P90 42.948→42.719毫米，但中位19.913→20.018毫米。231/267/303、322/466及420旧坏窗没有解决；675/711收益保留，但属于前序结构修改，不重复计功。

允许回正且目标非负的755帧资格中，实际offset低于-5毫米208→209，唯一新增155，时间3.467→3.483秒。负偏移积分0.3410367→0.3408695米秒几乎不变，没有2Hz那种明显延长，但也不宣称迟滞消失。

同侧2084对的Solved Knee actual超过5厘米686→687，超过10厘米160→157；extra超过5厘米431→432，超过10厘米仍89。actual P90 91.455→91.665毫米、P99 218.687→219.964毫米，最大R826仍626.929毫米，深压缩不变。总数减小不能覆盖下面的具体窗口。

R996本帧target59.539毫米高于previousOutput47.347毫米，但旧速度-0.63416米每秒；新门清零后输出47.836毫米，旧为40.116毫米。骨盆因此高7.720毫米，BendWeight从0.028473变0.215860，Foot目标未变。其实际Knee步168.357→48.483毫米；下一997两版BendWeight都为0，实际Knee步16.865→221.587毫米，extra19.653→186.438毫米。

这是一条已存在翻侧的时点后移和峰值放大，不是凭跨版本差值宣称新增问题类别或全包全面变差。994原228.396毫米峰未变，不能剪掉它制造局部总峰结论；固定193957在997原本231.752毫米，因此该代价是相对直接前驱的回归，不是比固定基线所有窗口都坏。它仍阻止把这条组合按局部骨盆收益交付。

没有修改现有Bend实现或权重，也没有通过规则或更换对照掩盖代价。当前Goal不自动授权另改Bend；已向用户请求后续独立处理方向/权重交接的许可，许可前只完成本次恢复，不实施该扩展。

## 保存与恢复状态

完整原包、旧62解释和全部历史样本不覆盖。033902的12文件ZIP与独立Proof保存在Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-6-opposed-velocity，逐文件SHA与原包相同。

- ZIP SHA256：351E448137EDCC754516B9C6943B4FD9262871B8ED2C101E1FB4B5B0B475A92B。
- Proof SHA256：9F9AAD6AEE80B952284562502FC3A9BFC0E68E57E298914D3208E229C9B6573B。
- samples SHA256：F7ACA2123C0688205CA7108BC6428DDD13C70C680BB130448061A0D991CCDD20。

Diagnostics通过663c5f942b96acc8c2c3812c3c5c06f587a81d5e独立恢复旧门的facts62/Analyzer62/diagnosis31；Runtime通过1c0a283c7df411796440342033f372f7ca2c666e恢复原Handoff前置与当前合同。未操作用户既有FinalIkFullBodySolver换行标记、proposal或project。Runtime、Diagnostics和全部Configs对3a909f2内容无变化。

完整Editor以规定flags构建57既有警告、0错误，结束立即shutdown；重新Refresh并等待编译/导入完成，Console0错误，再同Record回放。恢复新包20260831-035643-027-7f16a02676e741caa94d1f3bc3c6cbd9已完成Finalizer，035815 Proof对033902官方matched1044。因为Proof只验证输入/Body及产品身份，Foot/Pelvis恢复另外对025450逐列核对，没有拿官方matched代替表现恢复。

两包2086×1221列中1197个非运行身份列逐值相同，覆盖全部Foot/Pelvis/Knee/Reach/Spring/Body/Input和直接Writer World。24身份列逐列双向映射无冲突，geometry50195行只有四身份列不同。实际3Hz1043/1043帧、旧门reset99、None-Handoff reset0；37项规则、计数、完整score/occurrence/measurements逐值相同，总分61.9。Runtime identity、完整1044 Proof frames及trace/start/input/body hash也对025625相同，原始samples SHA与Proof匹配。根任务与独立诊断任务分别确认恢复。

恢复12文件ZIP和独立Proof位于本实验archive的restored-original子目录，原文件未覆盖：

- 恢复ZIP SHA256：49C07E601278360CB739D6C61B99443364EFC207E80A5B4AA82258F3922D6DEA。
- 恢复Proof SHA256：037833FC162FD8AEE7E99F51FD20CF76D53C96DF20FAE8637C5F48041F38143A。
- 恢复samples SHA256：C64FA835F925C7CDCBD0046F366DE6F61806AE2BB367F6A9F5F22CE19312CEDE。

Unity已退出本任务拥有的Play回到Edit。全量strict95/95和diff检查通过。该轮完成的是完整候选试验及安全恢复，骨盆/R826等质量要求仍未达到；后续所请求的Bend方向/权重交接修改尚未获批，不能借持续Goal自动实施。
