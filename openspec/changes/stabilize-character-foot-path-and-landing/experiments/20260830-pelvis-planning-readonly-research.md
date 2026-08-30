# 卸载回退后的骨盆只读调研

## 范围与当前状态

用户要求撤回卸载实验并由主任务自行调研，用户同时并行研究。本轮只做源码回退、原Record恢复Replay及只读数学/代码核对，没有实施新的骨盆规划、权重、余量、Foot目标或膝盖算法。

Runtime回退为`3436cf6`，Diagnostics回退为`27dbef4`。恢复包`20260830-221050-949-065a20d3944f47f29c256606e801ad57`与193957均facts57/diagnosis26，2086脚行、1143列；1119列逐字符串相同，其余24列为采样/宿主元数据及Surface/Path实例身份，逐列双向映射冲突0。50195行geometry仅4个实例身份列不同。37个Target规则、eligible/matched、分数逐值恢复，总分61.9；不表示原骨盆或膝盖问题已经解决。

正式Proof221238匹配212302的1044帧，帧分歧0；没有伪造绑定193957的官方Proof。恢复ZIP、Proof及逐项SHA256清单在`Diagnostics/FootPlacementReplayArchives/20260830-source-lift-unloading`，所有111个原始run目录保留。

## 1. 不是单一的“骨盆弹簧太慢”

现链：

`Resolved Foot Pair -> 主支撑/另一脚Landing选择 -> Stride地面目标+下侧脚净空 -> 主支撑可达目标夹紧 -> 一次3Hz弹簧 -> 主支撑输出夹紧 -> 两脚正式Reach区间交集再夹紧 -> Pelvis Goal -> 唯一FBBIK`

源码入口：

- `CharacterFootPlacementModule.ResolveStrideHips`，1634–1710：输入是同一动画Pose的Hip/Ankle、Resolved有效Ankle和正式支撑身份。
- 同文件1684–1686：`SupportLegCompressionReserve = max(0, RigLegLength - distance(animatedHip, animatedAnkle))`。6.8厘米是当帧原动画弯曲造成的余量，不是可琳新加的固定配置。
- `CharacterFootStrideHipsBuilder.BuildPelvis`，835–869：目标是当前Stride地面相对根高度加最低脚净空，再夹入主支撑区间。
- 同文件905–924：弹簧后再次夹输出，并在触界向外运动时清速度。
- 同文件979–1107：每腿使用正式MinimumLandingLegCompressionReserve，交集还包含第一层支撑区间；再夹目标与输出。

两次夹紧并非对同一个量重复相加；但它们确实能把本来小步推进的响应改成大步位移。第一层还把“保持原动画弯曲程度”放进了硬边界，第二层使用另一份最小弯曲余量，两者业务不能混称实际骨长。

## 2. 只把两腿限制搬到弹簧前面，不足以处理已发生的突降

在193957的455个Stride Accepted帧，用原始输入重算第一层目标、3Hz弹簧和完整两腿区间，最终输出对CSV最大误差1.664微米。Up取当前正式TargetHeightComponentUp，生产链回到同帧ComponentUp；未使用可能为零的GroundPath Up，也未补默认值。

保持这些帧的历史输入不变，把两腿交集提前用于本帧弹簧目标、仍保留最终合法区间，28个大于5厘米的Accepted域突降帧的输出变化最大仅1.453微米。因为原输出已经在本帧新上限之外，弹簧算出的候选仍越界，最终只能回到相同上限。

这是冻结本帧真实历史输入的单步反事实，不是新Replay，不推算修改后全部历史、Foot/Pelvis/IK反馈，也不把28帧扩成全包全部30个大步。它足以否定“只挪一下当前帧调用顺序，就把这些突降当帧消掉”，不能否定更早改变目标或历史会产生收益。

## 3. 主因应分成两类

### 3.1 大多数是实际空间关系持续吃掉腿的余量

以同一个实际限制来源分解，第二层上限为：

`upper = ankleY - hipY + sqrt(usableLegLength² - horizontalDistance²)`。

再把Hip世界Y拆成根世界Y与髋相对根的Y距离。以下是原193957、世界Up稳定窗口的严格相邻差分，不是按导数近似：

| 322帧上限下降来源 | 对上限的影响 |
| --- | ---: |
| 根世界Y上升 | -32.240毫米 |
| 髋相对根继续上升 | -10.441毫米 |
| Hip到Foot目标水平距离增大 | -39.735毫米 |
| TargetAnkle本身Y下降 | -6.945毫米 |
| 保留半径变化 | 0，前后均20毫米余量 |
| 合计 | -89.361毫米 |

466帧同类：根上升贡献-21.892毫米、髋相对根-10.441、水平距离-40.464、TargetAnkle Y -6.945，合计-79.742毫米，半径同样没变。

因此不能把这两个主要窗口归咎于6.8厘米余量或“只需要改竖直高度”。身体向上/向前、髋部动画变化、脚仍受地面目标约束，使同一根腿的可达上限下降。未来计算必须有完整Hip与Ankle XYZ，不只是未来地面Y。

### 3.2 少数窗口由原动画弯曲余量骤增额外压低

675帧主支撑余量约17.563→68.057毫米。上限从+32.673变为-76.901毫米；其中半径改变贡献-60.262毫米、水平距离改变-40.330毫米，其余为根/髋/目标变化。819帧同型余量变化，半径项约-63.725毫米。

单步冻结输入、仅保留两腿正式20毫米余量区间而不叠加原动画余量时：

- 675输出-76.901→-15.185毫米；
- 819输出-117.341→-56.243毫米；
- 28个Accepted域大步仍有26个超过5厘米。

这证明把原动画弯曲程度从硬约束改成姿态偏好具有独立价值，但它改变了承重腿姿态政策，且不能单独解决主要321/322、465/466冲突。以上不是允许删除真实腿长保护的证据，也不是完整修改后的Replay结果。

## 4. 提前下降在记录窗口内有几何空间，但尚无已实现的规划合同

193957的315–322八帧现有完整可达区间交集为约`[-0.757681, -0.131074]`米；459–466八帧交集为约`[-0.660199, -0.156112]`米。说明在这些已知轨迹窗口里，未来较低的骨盆修正并不违反更早帧的这套腿长区间，提前准备并非几何上不可能。

这些是事后读取未来CSV得到的可行性证据，不是Runtime可直接使用的输入；没有据此规定固定八帧时域，也未保证膝盖侧向、姿态美观或碰撞均安全。提前蹲会增加屈腿，原有膝盖翻侧风险必须在后续Replay观察，但不在本次改膝盖。

若保持现有单一弹簧，未来时刻的可达上限要成为更早目标的输入，不能再等本帧越界才处理。恒定目标段的现有弹簧满足`p(t)=a*p0+b*v0+(1-a)*target`，其中`a=(1+omega*t)*exp(-omega*t)`、`b=t*exp(-omega*t)`；因此已知未来边界时可以研究反推目标。该式只是同一响应的数学，不是新增第二层平滑，也不是已经批准的控制器。Support换代、VelocityReset、预测范围及目标逐帧改变必须另外纳入正式合同。

## 5. 已有素材与缺失输入：不能直接复活旧步态源

发现已有`AnimationPredictedFootStepCurveSet`的25点RootLocalFoot/Ankle/Hip Route。`CharacterFootPlacementAnimationAnalyzer`1333–1347把当时目标动画骨架点减authored Root并乘其逆旋转，1421–1454沿上一到下一Landing采样路线。不是从当前Hip常量外推出来的伪预测。

但当前正式`AnimationFootMotionRuntimeSample`并不发布这份路线。旧路线仍依赖自己的EventPhase、ReleasePhase、LiftOffPhase、TimeToLanding及Biomechanical数据；当前Foot Placement走`PosePlanExecutionRuntime.ResolveFootStepObservationFrame`2795–2878，从精确Live Contribution/Clip绑定采样正式FootStepObservation与统一Completion。直接把旧PredictedStep时钟插回Pelvis，会重新引入两套步态判定，不能作为快捷补丁或fallback。

已有`CharacterFutureBodyTranslation`提供正式KCC未来根平移，最多5个带时间样本；它不提供未来Hip骨架姿态。两者有素材，但还未组成一份同Source/Contribution/Cycle/Frame的未来两腿可达合同。

另外`BuildMotionData`548–595采样的是MotionReference，而Landing映射使用独立的TargetRootLocalSolePositions；不能把MotionReference的Hip直接当Target Animation当前表现Hip。未来几何应明确来自实际Target Clip/rig、与正式Foot Motion事件绑定，再与同一Future Body轨迹变换组合。

后续若走规划方向，需要先闭合：

1. 同一正式事件和Pose来源的未来Hip/Ankle几何、时间范围与坐标adapter；只迁移所需几何，不恢复旧Contact/Lock/Release时钟或权重。
2. 当前Verified Anchor在已知承重区间内的未来Foot Goal约束；Swing/Release、Source换代及旋转导致的Sole→Ankle变化不得假装固定。
3. 两腿共同可达区间如何在时间上产生唯一Pelvis目标；输出继续由既有单一响应和安全保护生成。
4. 预测不足必须作为正式适用范围/不可用事实处理，不从旧记录、默认Hip或固定“提前几帧”补值。

## 6. ZZZ能支持到哪里

本轮直接核对`disasm_exact/pik_core`中的97B0、9AF0及9EE0：

- 97B0先按带标记记录聚合MAX，另有MIN/arr128分支；9AF0对输入Foot与匿名中心的距离使用this9C，并在候选中选择。不能把9C改名实际腿长，不能把MAX/MIN简单替换为3C两腿可达区间。
- 9EE0在9F9D/9FBD取得共同候选；9FCE–A0E2处理190/194，A0E2–A18C限速；274为0的普通路径由A1A4直接跳A917写190并构造共同输出。
- 274非零时后面仍有几何和权重混合，不能做“ZZZ限速后永远无其他变化”的全称断言。

在已核对的普通PIK路径，没有3C这种按实际两根腿长在弹簧后强改输出的同构结构。但这不证明ZZZ最终骨骼绝无下陷，也不证明删除3C Reach就安全。提前骨盆规划是保留项目世界Anchor与腿长合同的项目方案，不冒称从ZZZ恢复出的未来步态规划器。

## 调研裁决

两条有价值但不同的工作需分开：

- 保留现有承重/腿长要求，补完整未来几何与骨盆提前规划。业务代价是更早下蹲、输入链和失效边界更复杂；优势是不靠重新悬空或提前卸载换平滑。
- 把严格保持原动画弯曲程度从硬限制改为姿态偏好，同时保留实际腿长与正式最小余量。业务代价是承重腿允许不同于原动画的伸展；针对675/819这类独立放大有依据，但不是主要突降的充分修法。

当前只完成回退与上述有界只读调研。没有采用任一新政策，也没有继续调参数、改Foot目标、改Goal权重或追加第二个平滑器。
