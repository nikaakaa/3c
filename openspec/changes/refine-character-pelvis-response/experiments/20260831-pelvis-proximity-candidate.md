# 骨盆几何候选的项目适配实验

## 当前状态与授权

用户批准从112611开始对齐输入、筛选ZZZ共同候选，并在候选有实际价值时进入独立实现与同输入回放。当前已完成只读输入/数学筛选及Runtime候选，Diagnostics迁移及Unity编译已完成，正在生成匹配产品；尚未运行新的Runtime Replay，不称可用修复。

固定Foot效果参照为193957，直接前驱d51e247／20260831-112611-018-2429691288e6434a8588a55de100efc2。原样保留3Hz、20毫米硬Reach、原Handoff和正速度清理、中性软姿态偏好、Support资格及Release首选0。此步暂不改末端硬投影是隔离目标变量，不是将Reach认定为永久正确。

模型已有取舍：完整选择器减少主要突降，但移动平地平均额外下压约增加1.85毫米。已明确告知用户；准入仅指进入真实回放检验，不意味忽略这个代价。未修改评分标准、Foot/Bend或扩大容差。

## ZZZ已闭合的有限普通分支

来源为D:/ZZZ_Dump/PIK分析包/disasm_exact/pik_core/function_171D2F40_171D31A0.asm与function_171D97B0_171D9AEE.asm；仅选择已观察Corin B0=0的2F40→1B8→97B0分支，不新增运行时B0模式开关。9AF0另一分支不混入本实验。

输入A_i为原Foot基准，X_i为处理后Foot位置，B为共同参考，U为Up，c_i为脚部处理后的scalar，r为半径。2F40只生成候选：

- |A_i−B|<r：OriginalWithinRadius，候选为c_i。
- 否则C=|B−X_i|²−r²；C>=0：TargetOutsideRadius，没有几何候选。
- C<0时令b=2dot(B−X_i,U)；b<=0候选为c_i，b>0候选为(−b+sqrt(b²−4C))/2。

97B0优先取OriginalWithinRadius候选的MAX；不存在该类且恰有一项几何候选时取该项；其余取MIN(cL,cR)。原−FLT_MAX只表示没有该几何候选；项目用typed Kind表示，不使用哨兵假位置。

它不是双腿合法区间求交，也不直接移动Foot或Pelvis。这里恢复的是条件候选数学，不是所有PIK模式或最终solver协调。

## 项目输入适配，不能冒称完全复刻

所有量使用本帧表现世界米制域与同一Component Up：

- A_i：CaptureAnimatedPose取得的原动画Ankle，不读取FBBIK已经施加骨盆平移后的OriginalAnkle。
- X_i：本帧Resolved经既有Foot Goal编码与作者PositionWeight得到的预Reach有效Ankle。零作者权重对应原动画Ankle，不使用旧目标。
- B：同帧原动画Pelvis，不使用上一最终Physical Pelvis。
- c_i=dot(X_i−A_i,U)：本项目实际加权脚踝修正。它不是ZZZ的原生post-g/k arr128，不额外给Pelvis单独乘g，不更改Foot原输出。
- r：正式Profile新增PelvisFootProximityRadius，Corin明确为0.2米。数值参考已观察Corin半径；它定义的是项目共同参考附近的候选选择范围，不叫真实腿长、不从20毫米余量推算，也不声明不同模型/缩放下已等价ZZZ。

ZZZ的c经过自身Foot高度缩放、基准及位移构造，本项目保留世界Anchor与现有Foot目标。这个差异是实验输入政策，不由相同数值或名字抹去。仅迁移共同候选选择器，未迁移ZZZ Foot响应、g/k/W、199或共同响应核。

## 输入核对

112611的2086脚行均源Pose可用、Resolved Ready、PositionWeight=1。2023条可用Pelvis Reach目标与ResolvedEffectiveAnkle逐值相同；左右公共Pelvis一致，Up全(0,1,0)。910个原HeightTarget脚目标Sole与Resolved Sole最大差5.715微米，来自原Goal编码/反解边界，不把它们叫逐字相等。正式实现继续消费编码后的有效Ankle，实际浮点及边界分支需新Replay验证。

本包没有非单位/变化坐标变换、作者零/极小权重的动态覆盖。世界域适配不宣称一般非均匀缩放下等价原ZZZ局部半径。

## 冻结输入筛选

从原Frame3已记录Spring状态播种，冻结原Foot、Body/Pose、Support资格、Posture几何、硬区间和dt，原3Hz复算Output最大误差5.043e−8米；没有新增完成/清理点。该模型没有求解新Knee或最终Physical Foot，不当正式facts或Replay。

共1043公共帧：995帧走BothOutsideCorrectionsMinimum，48帧走SingleGeometricCandidate。实际455个HeightTarget资格帧里分别412/43；OriginalWithinRadius与双几何候选分支没有动态覆盖。两种单脚几何候选分别42个正根、6个直接修正。不能把0覆盖分支称为已验证，也不能把本实现简写为单纯MIN。

| 模型指标 | 原目标 | 仅脚踝修正MIN控制 | 完整几何选择 |
| --- | ---: | ---: | ---: |
| 世界Y绝对步超过50毫米 | 33 | 31 | 24 |
| 向下超过50毫米 | 24 | 19 | 13 |
| Correction步超过50毫米 | 28 | 19 | 18 |
| 最大世界Y步 | 80.210 | 77.025 | 65.814毫米 |
| 负偏移积分 | 0.338134 | 0.361461 | 0.361447米秒 |
| 移动平地平均偏移 | −15.355 | −17.208 | −17.208毫米 |
| 移动低于−20毫米 | 32 | 33 | 33 |

完整几何与仅MIN的输出最大相差162.041毫米，说明不是对已失败MIN模型换名重试。337个正式请求改变超过0.1毫米；825帧候选从约−0.741变为+440.946毫米，但后面的原软偏好/硬边界仍限制它，不能只凭raw q变大就声称骨盆实际抬升同幅度。

- 278：请求+293.375→−5.659毫米，模型下压更深，必须保留。
- 414：模型输出107.213→59.135毫米；419为42.069→24.152毫米。420仍触同一−1.674毫米硬上界，世界下降80.210→62.292毫米，不是硬边界消失。
- 985：请求446.038→70.685毫米，输出327.372→126.103毫米；986仍按原资格退出转0，989输出210.258→76.252毫米。没有借此改Support准入。
- 322／466最终仍在原硬上界，主要几何冲突未解。
- 原774个“目标非负且硬区间允许0”帧中，新目标有56帧变负；不能将220→293全称为迟滞增加。双方仍非负的718帧独立复核，低于−5毫米为原199→候选243；其中45帧新增、1帧消失。这是同域模型下的真实额外下压风险，不能用大步次数下降掩盖，也不当作已运行的Knee/Foot结果。

## 实现及验收边界

唯一变化为HeightTarget从双Sole最低差改为上述正式几何候选。删除旧Sole/min字段与旧公式，不保留开关或并行生产者；Frame携带实际有效Ankle，Profile显式半径进入Revision，Corin Float32/Fixed/Projection通过正式构建同步。新公共事实直接发布A/X/B/U/r、双脚c、Kind/Value、Selection和请求值。

原Support/Release、3Hz/速度门、中性软偏好与20毫米Reach不动。Foot位置、旋转、世界Anchor、Capture、查询、Goal权重、Bend及Body不动。当前spec明确写旧最低高度差，因此必须先同步该条及公开Ankle输入边界，再实施，不能假称零行为重构。

真实Replay需要同时核对原始输入和候选每个分支、985退高、420及全包世界大步、移动/静止负偏移、脚部穿透/间隙/轨迹、Landing完成以及实际Solved Knee。总分不作为单独准入；若出现明确不可接受回归，精确撤销本候选及匹配产品/诊断，并同Record确认恢复。原始模型、失败、恢复证据都保留。

原始只读输入核对与模型结果见evidence/20260831-pelvis-proximity-inputs.json和evidence/20260831-pelvis-proximity-model.json。

## Runtime候选落地

CharacterFootPlacementModule在原Pre-Pelvis阶段从唯一Foot Goal解析有效Ankle，继续共用原PositionWeight与同一PoseRoot；原Committed Sole计算复用相同Ankle运算，未启用旧Visible读取门。CharacterFootStrideHipsBuilder只替换HeightTarget，公开每脚候选Kind、Value与最终Selection；没有新增持久历史或第二骨盆生产者。

Corin正式Profile新增PelvisFootProximityRadius=0.2，Schema迁移到character-foot-placement-profile/v37-pelvis-foot-candidates，其他参数保持原值。该字段经现有JsonUtility/Profile Revision进入产品身份，不手写生成产物。TrainingEnemy不在本次范围，未修改或构建。

Runtime通过规定flags构建：27个既有依赖/字段警告、0错误，用时52.79秒；finally立即执行build-server shutdown成功。此结果只证明Runtime编译，完整Editor、正式产品和新Replay仍待后续步骤。

独立ZZZ窗口已对c3166ff完成有界静态审查：选择优先级、typed unavailable与Module本帧输入符合声明，未发现阻止本次Replay的静态矛盾。Up沿用Body.VisibleRotation生成的既有ComponentUp，不是新采样PoseRoot轴。原始接近判断的平方距离比较与ZZZ开方比较在实数规则上一致，浮点恰贴边界时可能有ULP级分支差；本步不声称逐位复刻。静态通过不代表视觉效果通过。

676fd5c独立完成六文件Diagnostics迁移：facts67／Analyzer67／diagnosis36，CSV1224→1233，仅目标19→28标量，其余列和37项质量规则保持。旧112611由正式Analyzer在缺ReferencePointX时明确拒绝，没有补列或重发旧包。首次外部构建缺Temp的project.assets及31个Editor依赖DLL，正常restore和完整项目依赖构建解决；最终57个既有警告、0错误、112.67秒并shutdown，没有修改源码绕过。Unity显式Refresh经历一次域重载断开后自行恢复，随后Console零错误、Edit态；正式Corin产品构建继续进行。
