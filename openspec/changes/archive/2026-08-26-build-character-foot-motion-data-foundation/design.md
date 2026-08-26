# Design: 用原生AnimationClip承载GDC脚步数据

## Context

本设计把“自动生成”和“正式作者真相”分开：Analyzer只根据骨骼与Rig生成候选；作者Apply后，原生AnimationClip注册曲线是唯一正式数据。Artifact保存原始证据和候选lineage，用于重建与解释，但不能与`.anim`形成双主线。

## Decision 1: 固定24条可见曲线

全局曲线：

| Channel Id | Animation Window | 单位/范围 | 用途 |
|---|---|---|---|
| `presentation.locomotion-phase` | `Gait Phase` | 连续相位 | 只供同步组 |
| `presentation.foot-placement-weight` | `Foot IK` | `[0,1]` | 整体Foot IK作者权重 |

每脚曲线，`{side}`替换为`left-foot`或`right-foot`，可见名使用`L`或`R`：

| Channel Id后缀 | 可见名 | 单位/范围 | 插值 |
|---|---|---|---|
| `step-time-seconds` | `L/R Step Time` | 秒，`>=0` | Event内线性下降，边界跳变 |
| `step-distance` | `L/R Step Dist` | 米，`>=0` | Event内常量 |
| `height-above-path` | `L/R Foot Height` | 米，`>=0` | 连续 |
| `toe-height` | `L/R Toe Height` | 米 | 连续 |
| `toe-speed` | `L/R Toe Speed` | 米/秒，`>=0` | 连续 |
| `ground-pose-position-error` | `L/R Pos Error` | 米，`>=0` | 连续 |
| `ground-pose-rotation-error` | `L/R Rot Error` | 度，`>=0` | 连续 |
| `contact` | `L/R Contact` | `[0,1]` | 连续 |
| `lock-mode` | `L/R Lock Mode` | `0/1/2` | Constant |
| `lock-weight` | `L/R Lock Weight` | `[0,1]` | 连续 |
| `support` | `L/R Support` | `[0,1]` | 连续 |

`Lock Mode`固定映射：

```text
0 = Unlocked
1 = Sliding
2 = Locked
```

Foot Forward不新增曲线。它继续由AnimationClip中的真实脚骨骼水平轨迹表达，避免位置双写。

## Decision 2: 统一原始采样与双空间

每个正式Target AnimationClip由Analysis Source中的显式Motion Reference Binding绑定一个Editor-only运动源：

```text
Target Clip：Runtime播放并拥有24条正式Curve
Motion Reference Clip：Editor-only采样真实Root Motion与骨骼轨迹
```

两者必须具有相同Duration、Loop、Sample Rate，并且Motion Root、Pelvis与左右Hip/Knee/Ankle/Toe分析闭包除声明Root Motion通道外逐时刻一致。Prop、毛发和上肢等不进入Foot Analysis输入身份。不得按文件名、目录或显示名猜配对。Analyzer按Source Sample Rate采样Motion Reference Clip：

Motion Reference的Root Curve必须从0开始且覆盖素材秒域。Unity原生最后一个Source Sample区间允许由曲线尾值Clamp；只有尾段不超过两个Source Sample区间，并且按末端Tangent估算的平移分量变化不超过2 mm、Quaternion分量变化不超过0.01时，才允许第二个区间继续Clamp。更长尾段或仍明显运动的尾段必须拒绝，不能把缺失Root数据当成静止。

```text
Root Motion Transform
Hip / Knee / Ankle / Heel / Toe / Sole
Root-local Position / Rotation
Clip-motion Position / Rotation
Linear / Vertical / Angular Velocity
```

Root-local用于记录Landing时脚相对身体的位置。Clip-motion以Motion Reference Clip起点为共同原点并保留Root Motion，用于跨时刻计算真实步长。Target为In-place时不得把Target的恒等Root当作运动证据。

Rig Calibration唯一提供Up、Ground Reference、Sole Frame、Heel与Toe。Analyzer必须先完成全部位置采样，再用中心差分生成速度；不得从当前Scene、Transform名称或Clip自身最低点补数据。

## Decision 3: Landing Event统一Step Time与Distance

每脚先根据Toe/Heel接触证据生成稳定Contact区间：

```text
低Height
低Motion-space Vertical/Total Speed
Ground Pose Error可接受
+ 进入/退出滞回
+ 最短持续时间
```

Contact由false进入true生成Landing Event，由true进入false生成LiftOff。循环首尾连续Contact必须先合并再编号。

逐采样帧：

```text
Step Time = NextLandingAbsoluteTime - SampleAbsoluteTime
```

同一Event内Step Time按秒线性趋近0。Landing采样帧记录0；下一采样帧切换到下一个Event的时间。Curve使用采样网格和Constant边界表达跳变，不伪造连续上升段。

有限Clip使用素材边界补全Event外侧的解释域，但边界不生成Landing Event：首个Landing之前以Clip开始姿态作为该步起点；最后一个Landing完成后Step Time保持0、Step Distance保持该已完成步的距离，Foot Path继续相对最后Landing高度解释。若有限Clip某脚整段没有Landing，该脚进入显式No Step域：Event页为空、Step Time/Distance为0、Foot Height相对Calibration Ground记录，Contact与Lock仍按证据生成；不得伪造Landing。Loop Clip是否属于移动循环只由Motion Reference实际Root净位移判定：净位移非零时左右任一脚没有Landing必须整体失败，净位移为零时允许Stationary No Step。不得根据Clip名称预设Walk、Run、Grounded或Flight。

位置展开只属于Loop Clip。有限Clip的每个Path Sample必须直接读取同索引Raw Sole，尤其最后一帧不得因为`sample == intervals`而映射成“下一周期第0帧 + 整段Root变换”。语义Validator必须逐帧验证有限Clip的`Animation Height == Raw Sole Motion Y`以及`Height Above Path == max(0, Animation Height - Baseline Height)`。

每个Landing记录RootLocalLanding与MotionSpaceLanding：

```text
StepVector = NextMotionSpaceLanding - PreviousMotionSpaceLanding
StepDistance = length(ProjectOnPlane(StepVector, CalibrationUp))
```

Step Distance在目标Event区间保持常量。它只描述动画步长，不包含Runtime速度、输入方向或世界地形。

## Decision 4: Foot Height严格相对动画Foot Path

Foot Forward直接读取Motion Reference Sole的Clip-motion平面轨迹。相邻Landing之间按该轨迹累计平面距离计算Path Progress：

```text
BaselineHeight = Lerp(PreviousLandingHeight, NextLandingHeight, PathProgress)
HeightAbovePath = max(0, AnimationSoleHeight - BaselineHeight)
```

`Foot Height`保存HeightAbovePath。它不得包含未来世界Landing高度、Ground Envelope、Current Trace、Anchor或IK修正。前后Landing端点必须在正式容差内回到0。

## Decision 5: Toe Filter与Ground Pose Filter都可见

`Toe Height`是Toe相对Calibration Ground的Component Up距离。`Toe Speed`是Toe在Clip-motion空间的线速度模长。

Ground Pose Filter在Editor中假设当前脚位于Calibration Ground：沿Up投影Sole、按Ground Up构造脚掌目标并执行确定性腿部几何检查，输出：

```text
Pos Error = 动画脚到假设Ground Pose所需的位置变化
Rot Error = 动画Sole到Ground-aligned Sole的角度变化
```

Contact候选由Toe Height/Speed、Heel/Sole内部证据与Pos/Rot Error共同生成。虽然Heel/Sole完整证据留在Artifact中，GDC要求直接观察的Toe与Pose Filter结果必须写入`.anim`。

逐采样Ground Pose必须把当前Sole中心沿Calibration Up投影到Reference Ground，并保留当前Sole平面朝向构造Ground-aligned Sole Rotation。Analyzer必须从当前Sole到Ankle的局部关系得到目标Ankle，以当前Hip-Knee与Knee-Ankle长度、作者动画膝弯平面和余弦定理求出唯一目标Knee。不可达目标必须夹紧到双骨段可达区间并保留Residual。Pos Error固定为当前Ankle/Knee到解算Ankle/Knee的RMS位移加Residual；Rot Error固定为当前Sole到Ground-aligned Sole的Quaternion角度。Reach必须只从同一Residual生成，禁止恢复独立近似公式。

## Decision 6: Lock与Support不是Contact副本

Lock分类：

```text
Locked:
    Contact高、Toe/Sole速度低、Pos/Rot Error小、腿可达

Sliding:
    Contact仍有效，但平面速度或Pose Error位于中间区间

Unlocked:
    离地、抬脚、误差过大或腿不可达
```

Mode使用versioned滞回和最短持续时间。`Lock Weight`表达位置约束强度，不能直接等于Contact。

低速Sole证据只负责在Contact区间建立动画Lock Anchor。Anchor建立后，Locked退出必须使用Sole相对该Anchor的累计最大平面漂移、Pos/Rot退出误差与腿可达性，不得由单个中心差分速度尖峰直接释放。平面进入、退出距离预算分别由现有进入、退出速度阈值乘以最短持续时间得到，使速度阈值在正式时间域内表达可观察位移。Lock Mode完成最短持续时间过滤后，Lock Weight必须从同一有效Anchor的累计最大漂移、Pos/Rot Error、Contact和Reach生成；同一次Lock内已经因漂移降低的Weight不得因脚返回Anchor附近重新升高，被过滤为Sliding或Unlocked的短Locked片段不得保留独立满权重尖峰。

Support表达动画承重意图，不以Contact、Lock Mode或Lock Weight作为有效性门槛。每脚先生成四项连续证据：Heel/Toe最低点相对Calibration Ground的Ground Proximity；以Minimum Landing Segment为中心窗口、以进入/退出速度乘窗口时长为位移预算的Sole Vertical Stability；相对该脚整段最大Root-local Hip-Sole向下距离的Downward Extension；Hip-Ankle长度除以Rig Leg Length并在0.55到0.8间映射的Leg Extension。

绝对候选固定为`Ground Proximity × Vertical Stability × Lerp(0.5, 1, Sqrt(Downward Extension × Leg Extension))`。地面与稳定度决定Presence是否存在，腿姿态只在0.5到1之间调制强度，避免动画骨长或左右细微差异把真实承重整段误清零。左右Pelvis投影只计算Share，不得改变绝对Presence。最终`LeftSupport + RightSupport`必须等于`max(LeftCandidate, RightCandidate)`；单侧存在时该侧Support必须等于自身Candidate而不是固定1。明确腾空允许左右都为0；Sliding或暂时不可锁的承重脚仍可保持Support。这样现有两条曲线同时编码`总和=Presence`与`比例=Share`，无需新增第三条Presence Curve。

正式语义Validator必须独立于Curve格式校验，至少验证Ground Pose有限性、Landing端点、Lock Mode/Weight一致性、Support总和与Candidate Presence一致、明确双脚弱Candidate时Support为0。Bake Session的`Same`只证明Target逐Key等于Candidate，不能代替语义Validator。

## Decision 7: 候选Apply是唯一写入事务

候选必须携带：

```text
AnimationClip对象身份
Full Dependency Hash
AnimationClipAnalysisInputHash
Motion Reference对象身份与AnimationClipAnalysisInputHash
Registered Curve Hash
Artifact identity/content hash
Rig / Sampling Rig / Calibration / Geometry identity
Analyzer format/algorithm version
左右脚22条完整秒域Curve
```

Apply前重新校验全部lineage。任一字段变化则整体Stale。成功Apply在一个Undo事务中替换22条完整Curve并更新Registered Curve Hash；不得写单条、补缺失默认值或保留旧property binding。

## Decision 8: 单Clip作者入口必须共享唯一Bake Session

Inspector、Corin批处理与Unity自定义工具必须只调用同一个`CharacterFootMotionBakeService`，不得各自重建Artifact、比较Curve或直接写AnimationClip。一次Analyze请求必须只接受精确Analysis Source与Target原生AnimationClip；Motion Reference只能从Source的正式Target配对解析并只读返回，不能由调用者临时覆盖。

Analyze必须重建当前Artifact、生成完整22条Candidate，并把Target当前曲线组分类为`Empty / Same / Different / Partial`。Plan必须携带Target Registered Curve Hash、Artifact identity/content hash、完整changed channel列表和稳定plan hash。`Empty`允许首次Apply，`Same`返回No Change，`Different`与`Partial`默认拒绝。Apply必须提交Analyze返回的精确plan hash；任一输入、Artifact或注册Curve变化都使plan失效。只有显式`replaceExisting=true`才能覆盖`Different`或`Partial`，成功后必须逐Key重新读取22条Curve验证与Candidate完全相同。

Inspector对`Different / Partial`必须显示changed channel并在动作时弹出Replace确认。批处理必须完成全部Analyze后一次报告所有会覆盖的Clip，并在作者确认前不写任何Clip。Unity自定义工具必须接受精确Assets路径、action、expected plan hash与replace确认，不读取Selection、不按名称或目录猜Target。

## Decision 9: 数据阶段不进入Runtime

本change只扩展AnimationClip catalog、Analyzer、Artifact、候选、Apply、Agent Document和Build数据质量校验。Projection可以把新Registered Curve Hash纳入依赖，但不得发布新Runtime payload；Player Foot Placement继续使用基线数据和公式。

后续行为change按独立小步选择消费者：

```text
Step Time/Distance -> Landing Prediction
Foot Height -> Swing
Contact/Lock -> Plant与Release
Support -> Pelvis
```

每一步不得同时接入其它曲线。

## Risks And Tradeoffs

- 每个Clip新增22条可见曲线，Animation Window内容较多；换取运行时所需数据都能直接审查。
- 自动生成并显式Apply增加作者步骤；换取Analyzer不能静默改变游戏行为。
- Step Time和Lock Mode包含离散跳变，必须使用规定的Constant边界；普通平滑会篡改事件语义。
- Support的自动估计是作者候选，不保证等同真实力学；作者可检查完整Curve，但Runtime接入仍由后续change决定。
- 本阶段不会改善IK画面；换取数据正确性与行为问题彻底分离。

## Migration

1. 恢复`bd5780a`/`8fc704a`运行行为并删除失败Runtime消费。
2. 清除当前实验Contact/Support binding，保留`Gait Phase`与`Foot IK`。
3. 建立短名`Clip Curves`24-channel catalog。
4. 为Corin Target Clip建立显式Motion Reference Binding，严格校验Foot Analysis骨骼闭包除Root Motion外逐时刻一致。
5. 从Motion Reference升级Raw Sampling、Event、Step、Path与Lock候选，从动画生物力学姿态独立生成Support。
6. 为Corin可达Target Clip显式生成并原子Apply22条曲线。
7. 重建双Clip Artifact identity与Registered Curve Hash校验，但不发布Runtime payload。
8. 删除旧字段、旧曲线binding和兼容reader。
9. 用唯一Bake Session替换Inspector和批处理各自的Analyze/Apply编排，并发布精确单Clip Unity工具。
10. 用逐采样双骨段Ground Pose、绝对Support Presence和跨曲线语义Validator替换近似数据；Motion Reference Root轴允许Unity原生最后一个Source Sample区间Clamp，第二个区间只接受有界收稳尾段，更长或仍明显运动的尾段必须拒绝；升级算法身份并重烘全部Corin正式Target。
