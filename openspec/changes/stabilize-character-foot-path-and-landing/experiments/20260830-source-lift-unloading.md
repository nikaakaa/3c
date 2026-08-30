# 满锁后Source抬脚滑动的卸载交接实验

## 授权与对照

用户已授权把组合规则作为可撤销的小步实验处理骨盆下陷。固定193957（Runtime eb5fb05、Diagnostics 5d858bc、归档7f7b66d）作为对照，全部本地采样不删、不覆盖。不是ZZZ卸载开关复刻，不把新规则称为现有LoadEnded事实。

## 输入边界与业务取舍

正式LiftOff位于323/467，现版同帧已Release，不能通过接通LiftOff提前修复322/466。PreSwing从上一Landing开始，包含承重阶段。CompletedLockWeight只证明同Event曾满锁，Sliding可由位移或角度误差触发；FootHeight来自Source Sole相对作者Landing基线的非负高度，不是世界输出的一份独立位移。

本实验明确采用新的项目政策：Landing/Locked持有同Event Verified Anchor且请求仍有效、同Event曾完成满锁，正式Motion处于同一Source/Contribution的下一次Predictive Landing之前PreSwing，当前Contact与Anchor相同、下一Occurrence时序更晚，Mode为Sliding，FootHeight超过现有GeometryEpsilon（0.0001米，过滤零值浮点尾差），且本Event没有卸载重入保护时，进入已有Releasing。组合在193957命中40帧/26事件，第一次命中均比请求Falling早1或2帧；未发现Falling前回到Locked、脚高归零或Source换代，但这不保证所有滑动仍承重的场景正确。

政策代价是卸载段离地时机改变，不以FootHeight数值扣除输出，不修改Contact曲线或请求、边沿、满锁资格，不清Y、不增加参数。承重阶段的ContactWorldResidual、Swing目标、旋转权重、膝盖、两腿Reach与Pelvis弹簧全部不改。

## 唯一状态交接

组合由唯一Transition Resolver消费同一FormalFootMotion Runtime Sample，发布SourceLiftUnloading原因。Pre阶段Landing/Locked→Releasing，Retain原Anchor、无Suppress/Reset，原Release入口承接完整上一O并按现有半衰期推进。当前RequestsLock仍为true时ContactEdge仍None，计时正常递增，不伪造Contact Falling。

旧HardOwnership、请求/Event换代及超过SlideDistance的释放优先级保持；组合不得把原本已经超滑动范围的Release改名并额外延迟完成。

ContactTransition仅增加本Event的UnloadingEventIdentity和UnloadingReentryProtectedEventIdentity。前者阻止同一原请求仍有效时过早ReleaseCompleted后又立即Acquire；它不停止Release逐帧推进，不重复StateEntered或重复Capture。真实请求结束（包括Mode变Unlocked）后恢复原完成逻辑。

真正SameEvent Rising仍按原Reentry准入恢复Landing；若该Event曾因本政策卸载，清卸载标记并保护本Event不再因组合卸载。没有Falling却恢复Locked请求时，仅在原CanAcquire及LockDistance合法且本卸载标记匹配的范围内，用独立UnloadingLockRestored原因恢复Landing/Retain并保护本Event。恢复不冒用Contact Rising，不更改CompletedLockWeight。新Event/Create、Anchor Release及整体Reset清除卸载记录；旧标记不得阻止新Event正常Verification。

## 规格与诊断

这是一条明确新增的卸载政策，补充现有请求丢失或超滑动范围的Release原因；不是放宽HardOwnership、WorldAnchor或唯一Interpolation合同。FootHeight仍只由正式Motion Frame生产，Transition只将其用作本政策离散准入，不建立第二高度插值。

Runtime发布两项卸载历史的Before/After共4个标量；现有InputFormal、Pre/Post Reason、Anchor、请求、边沿和输出直接复用。唯一Diagnostics核对组合准入、清除、重入保护及跨帧carry；原37项质量规则和阈值不改。提前Release会缩短原Contact诊断域，必须另对原193957固定525个Contact Frame/Side比较真实Heel/Toe/世界输出，不用分母缩水声称改善。

## 验证目标

固定320–323、464–467及全包：骨盆目标/自由弹簧/两次硬夹紧/最终物理输出、Foot目标与最终Heel/Toe、Release交接、膝盖外溢。保留193957 Landing/Swing收益；不只比较总分，也不把Source抬脚组合准入成立当作视觉通过。同Event重入、无Falling恢复Locked等本包无动态覆盖的分支单列。

Runtime候选已实现并通过规定flags构建（27个既有依赖/项目警告、0错误，随后shutdown成功）。公开字段为Previous/Current UnloadingEventIdentity和Previous/Current UnloadingReentryProtectedEventIdentity，共4标量；事实直接来自同一ContactTransition Context。正常Requested/Edge/计时/CompletedWeight及所有Anchor几何不改。随后已完成Unity加载和212054正式Replay，结果见下文，不能再以“待采样”描述本候选。

诊断闭合追加4个纯事实：本帧实际LockDistance、SlideDistance、CurrentContact.NormalizedTime、NextLanding.NormalizedTime。前两项来自Settings，后两项来自同一FormalFootMotion，适用性使用现有Current/Next Available事实；不得由ordinal猜时间或硬编码Corin距离。连同4项历史共8个新标量，不改变运行行为或配置。

## 212054正式Replay封口

### 版本、输入与保留

- Runtime：`f8170e4`实现卸载；`0d40ba0`及`e5b8fd3`仅补实际距离、Occurrence时间事实。Diagnostics：`d8da442`，facts58、diagnosis27。评分规则与原37个Target的rules/eventKinds/scorePolicy不变。
- Record：`43357ff3cd384e5cba75d2c31175b116`，文件SHA256为`24D97232F35246C0B85A003B5980AC8F199D6FF63E9F74A0001B082F57EB89A6`。候选run为`20260830-212054-483-ae52b0b5bd714454adf3e69f15f29526`，1043个采样帧、2086脚行、1151列。
- 对193957，218个共同输入、动画源、时钟、程序及Profile字段逐值一致；50195行geometry仅4个实例身份列不同。没有批量忽略Event/Revision或给旧CSV补新字段。
- 官方212302 Proof匹配173549的1044帧、分歧0；193957未找到绑定官方Proof，不能称官方直接匹配193957。对193957的表现比较来自上述原始事实。
- 原包、12文件ZIP和官方Proof均已保存；持久目录为`3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-source-lift-unloading`。ZIP每个文件逐项SHA256核对，Proof复制哈希核对。193957原包/归档及全部110个原始run目录均未删除或覆盖。

### 实现合同与实际覆盖

8个新字段与facts逐行一致，26 eligible对应26次SourceLiftUnloading（左14、右12）；2084个连续历史carry无差。卸载标记共持有142脚帧，其中40帧原Contact请求仍有效；3帧Release插值虽已完成，Post仍按正式请求保持Releasing，没有暂停插值或重捕历史。26个标记最后全部由ReleaseCompleted清除。

26次完整Release交接Desired复算最大误差2.71微米；退出Contact域当帧Response=Desired、没有再执行scalar步长。当前包UnloadingLockRestored、真实同Event卸载重入保护均0动态覆盖。Action及重入几何同样没有eligible，不能用本包补称这些分支验证完成。

### 骨盆结果

以下均为相同采样调度，骨盆指标取每帧一次的1042个连续帧对，不把左右脚重复计数。

| 指标 | 193957 | 212054 |
| --- | ---: | ---: |
| 骨盆修正单步超过20毫米 | 162 | 120 |
| 骨盆修正单步超过50毫米 | 30 | 15 |
| 骨盆修正最大单步 | 89.362毫米 | 82.010毫米 |
| L321→322骨盆修正下降 | 89.362毫米 | 1.626毫米 |
| L465→466骨盆修正下降 | 79.742毫米 | 0.833毫米 |

L321提前进入原Release，L322脚中心由3.960000升至4.072426米，骨盆修正由-131.074变为-32.707毫米；L465提前Release，L466脚中心由0.719999升至0.845569米，骨盆修正由-156.112变为-53.517毫米。原两次Reach约束和弹簧均未改；卸载脚不再继续被旧高度拉住，确实减少了腿长冲突。不是减弱Reach或给骨盆追加低通所得。

用户随后反馈当前骨盆仍不合格。补核候选全部15次超过50毫米的骨盆修正步，15次都是下降；由本帧实际输入、3Hz弹簧频率、第一层目标夹紧及dt重算，自由弹簧单步均只下降约1.36–4.78毫米，余下大位移来自其后的Reach输出夹紧。10次发生在SourceLiftUnloading当帧，3次仍是Locked，1次Landing，1次原ContactOutOfSlideRange释放。不能因322/466的后续帧改善，就称骨盆大幅下拉已解决。

| 剩余窗口 | 上帧修正 | 自由弹簧本帧输出 | 第一次Reach后 | 最终两腿Reach后 |
| --- | ---: | ---: | ---: | ---: |
| L285，SourceLiftUnloading | +123.029毫米 | +120.425毫米 | +58.151毫米 | +41.019毫米 |
| L675，Landing | -3.477毫米 | -6.506毫米 | -78.963毫米 | -78.963毫米 |

285帧弹簧只下降2.604毫米，第一层额外压下62.274毫米、第二层再压下17.132毫米，合计82.010毫米。新Release刚开始，仍通过原完整世界残差连续交接；并不在入口把脚目标立即移到动画脚。该帧新旧脚目标只改变了有限位置，Reach冲突仍在，不能把状态改成Releasing本身解释为腿长已经安全。

675帧不是本组合的卸载范围。它也不能简单说成“原始腿长一定够不到”：实际Hip到TargetAnkle约689.253毫米，校准腿长695.434毫米，但第一层本帧保留68.058毫米弯曲余量，可用腿长627.377毫米，故其可达区间仍强制降低骨盆。这个余量不是本候选新增或调大。精确问题是脚目标、身体高度与保留腿部余量要求之间的冲突由弹簧之后的硬夹紧解决；本实验仅减轻其中部分卸载时段，没有闭合骨盆整条目标/响应/可达链。

### 固定Contact帧与业务代价

保持193957原525个Contact Frame/Side，使用各帧原Anchor Event/Point/Normal对账；新旧Anchor几何及身份相同。新版本提前进入Release的40帧仍全部计入，不按新状态缩小比较域。

| 固定525帧指标 | 193957 | 212054 |
| --- | ---: | ---: |
| 整脚间隙超过1/2/5/10厘米 | 178/118/44/11 | 192/129/49/11 |
| Heel/Toe端点平面负距超过1/2/5/10厘米 | 77/41/6/1 | 48/15/6/1 |
| 整脚间隙P90 | 42.681毫米 | 46.433毫米 |
| 整脚间隙最大值 | 349.378毫米 | 353.388毫米 |
| 端点平面负距P90 | 12.468毫米 | 6.615毫米 |
| 端点平面负距最大值 | 142.815毫米 | 142.815毫米 |

40个提前Release帧中，端点负距超过1厘米38→9，但整脚间隙超过1厘米0→13、超过5厘米0→5；L322为0→86.584毫米、L466为0→99.727毫米。这是允许更早卸载的真实代价，不是字段错误，也不能把平面负距离直接称作对有限Collider的穿模。

正式持续Gap仍3/60，但R440与R584两段各被提前Release截短一帧；不能仅报总数未变。Landing未闭合2/60、Landing exit53/60、floor-handoff17/53保持。FullAnchor横漂0/15、最大约3.932微米保持。

### 新增Swing回归的最早差异

原344个Stable帧对全部仍在新域，其中命中144→147；另外新增12个eligible中有3个命中，总计144/344→150/356。以下三个主要尖峰在两版都属于Swing、同Source/Event、当前NoRevision，不是Contact迁出后重新分类产生的统计假象。

| 帧对 | 原额外输出步 | 新额外输出步 | 新修正长度 | 新骨盆平移 |
| --- | ---: | ---: | ---: | ---: |
| L338→339 | 0.182毫米 | 103.360毫米 | 0.073750毫米 | 103.305毫米 |
| L514→515 | 0.732毫米 | 60.217毫米 | 0.088104毫米 | 60.261毫米 |
| R610→611 | 0.084毫米 | 71.519毫米 | 0.077480毫米 | 71.470毫米 |

链路已由代码与最终Physical数据闭合：

1. `CharacterFootLifecycle.BuildOutput`按`outputCorrection`长度大于GeometryEpsilon（0.1毫米），或旋转权重大于该门，决定是否发布正式PositionWeight。三帧FormalFootPlacementWeight均1、RotationWeight均0，但新修正长度跨到门下，PositionWeight由1变0。
2. `CharacterFootPlacementModule.CreateFootGoal`仍编码Goal记录、Resolved仍Ready，不是Suppress或Unavailable；位置约束权重为0。
3. `CharacterFinalIkFullBodySolver.ApplyGoals`先施加PelvisPreSolveTranslation；`CharacterFinalIkPoseBufferBackend.SetComponentPosition`同时平移其全部后代，腿和脚也被带动。
4. 此后ResetEffectorsToPose使用的脚基准已包含骨盆平移；Foot位置权重0，不再拉回原Foot目标。
5. 三窗完整XYZ均满足`PhysicalSole ≈ OriginalSole + ComponentUp × Pelvis`，误差约0.70/0.88/2.99微米，且后续零权重帧继续成立。原版三帧GoalWeight为1，Physical跟随Response。

因此不是Response生成了100毫米修正，也不是FBBIK没有追到正权重Goal。旧位置权重门此前让极小脚修正同时承担抵消骨盆平移的职责；卸载实验改变后续残差轨迹，跨过二值门后，这份位置约束突然撤销。该Goal门、Solver及Backend在本步没有修改，但它被本步实际触发，必须计为本步真实回归，不能归咎为“旧代码所以不算”。

Stable最大额外单步36.401→103.360毫米。Path共同665帧对命中205→207，总域205/668→215/742；Contact共同965帧对命中406→344，存在同域改善，但不抵消三个Swing尖峰。总分61.9→56.7仅为摘要，不用分数单独判定成败。

膝盖额外offset单步超过5厘米415→389、超过10厘米103→85，但既有R933约493.846毫米SolvedKnee大步未解决；没有最终Physical Knee测量，不能扩称修复反弯。

## 本轮处置

骨盆目标窗口改善、正式卸载合同已得到本Replay支持；但仍有15次超过50毫米的骨盆强制下降，用户也明确反馈骨盆不合格，且“保持193957 Landing/Swing收益”的整体约束未通过。候选不升格为后续已通过基线。

截至此记录，只保存实验代码与证据，没有回退候选，也没有调整Goal权重、旋转、Reach、膝盖、曲线或参数。继续修正0.1毫米位置约束门会改变现有Goal权重政策，必须作为明确的下一独立行为决策；不能悄悄叠加后把组合效果算成本次单变量通过。用户指定193957保持为正式比较版本。
