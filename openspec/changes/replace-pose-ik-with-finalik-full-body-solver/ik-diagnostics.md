# IK诊断与踩坑记录

本文档维护Foot Placement与Full Body IK的长期诊断结论。每次出现新的IK现象、错误假设或正式修正，都必须先更新这里，再同步design、spec与tasks。本文档记录原因链，不把截图观感当成结论，也不把临时阈值调大当成修复。

## 当前结论

持续输入时IK消失，不是FinalIK FBBIK求解失败，也不是左右脚烘焙数据缺失。直接原因是Goal Source曾把包含actor世界平移的脚速映射成整个Foot Goal的Position/Rotation Weight。角色持续跑动时两脚世界位置都随actor移动，脚速长期超过退出阈值，Goal在进入solver前已经被清零；松开输入后actor平移停止，脚速下降，Goal才恢复，所以肉眼看到“没有输入之后才有IK”。

正式链路已经改为：

| 信号 | 输入 | 唯一职责 | 禁止职责 |
| --- | --- | --- | --- |
| `PlacementWeight` | 作者Foot Placement Weight、Body Grounded、合法Current Grounding命中 | 控制未约束Foot Goal Position/Rotation | 不读取脚速、Plant Confidence、surface distance或历史帧 |
| `PlantConfidence` | 单Clip烘焙后按最终Pose contribution混合 | Plant Contact enter/exit迟滞 | 不连续缩放Foot Goal或Pelvis |
| `AnimationFootSpeed` | 混合后的`SoleLocalVelocity.magnitude` | Plant Contact进入、退出和Contact约束渐退 | 不拼接Body速度、actor平移、yaw点速度或最终sole世界差分 |
| `PlantSupportWeight` | Plant Contact与Placement Weight | `AllPlantedFeet`等模式的Pelvis支撑选择 | 不控制普通Foot Goal |
| `ContactWeight` | Plant Contact、Plant Policy、anchor有效性与Animation Foot Speed | anchor、lock、slide | `Unlocked`下不得存在，不衰减普通Foot Goal |

Corin正式阈值为：

- `PlantSpeedThreshold = 0.6m/s`
- `UnalignmentSpeedThreshold = 2.0m/s`

这两个阈值不表示“低于0.6才有IK，高于2.0没有IK”。它们只表示低速脚可以进入Plant Contact，高速脚退出Plant Contact，区间内Contact约束连续渐退。

## FinalIK Grounding为什么不会把摆动脚硬压到地面

FinalIK `Grounding.Leg`先计算脚到命中面的高度差，再减去动画脚到Root参考平面的高度`rootYOffset`：

`heightFromGround = FootToHitHeight - FootToRootReferenceHeight`

最终`IKPosition = AnimatedAnkle - Up * IKOffset`。因此输出表达的是踏面相对Root参考平面的高度差，动画脚本身的离地高度仍保留。合法Current Grounding Goal在跑动时保持Placement Weight，不等于把摆动脚绝对吸到地面。

对应项目源码：

- `Assets/Plugins/RootMotion/FinalIK/Grounder/GroundingLeg.cs`中的`GetHeightFromGround`、`rootYOffset`和`IKPosition`
- `CharacterFinalIkGroundingAdapter.BuildFootResult`只把该正式结果转换到Component Space
- `CharacterPredictiveFootPlacementGoalSource.ResolveFoot`只决定Goal职责和约束状态，不重算Grounding脚高

## 左右脚是不是烘焙数据

是，但要区分“烘焙特征”和“运行时权重”。

左右脚`SoleLocalVelocity`、`SoleHeight`与`PlantConfidence`分别由同一个AnimationClip中各自校准Heel/Toe轨迹采样生成，不是把左脚复制给右脚，也不是运行时根据角色移动速度临时制造。分析器先得到左右独立曲线，运行时再按最终Pose contribution的source权重与visual time scale混合。

正式数据链：

1. `CharacterFootPlacementAnimationAnalyzer`分别采样Left/Right Heel与Toe。
2. 每脚sole位置取对应Heel/Toe中点，`SoleLocalVelocity`由该Clip相邻采样差分得到。
3. `PlantConfidence`由该脚鞋底高度与垂直速度迟滞生成。
4. `AnimationSlotBlendJobMath`和Pose Graph按最终贡献混合左右特征，并按visual time scale缩放速度。
5. Foot Placement只把混合后的`SoleLocalVelocity.magnitude`用于Plant Contact，不把它变成普通Goal总权重。

烘焙数据是真的；错误发生在运行时给它安排了错误职责。

## UE 5.7源码对照

本次对照的是本机UE 5.7安装源码：

- `Engine/Plugins/Animation/AnimationWarping/Source/Runtime/Public/BoneControllers/AnimNode_FootPlacement.h`
- `Engine/Plugins/Animation/AnimationWarping/Source/Runtime/Private/BoneControllers/AnimNode_FootPlacement.cpp`

源码结论：

1. 默认`SpeedThreshold = 60cm/s`，`UnalignmentSpeedThreshold = 200cm/s`。
2. Graph模式使用动画Root Motion属性和脚Component Space变化计算输入脚速；Manual模式读取逐脚速度曲线。它不把actor组件世界平移再次加进脚速。
3. `WantsToPlant`用速度与到Plant Plane距离决定Plant意图，不是整个Foot IK开关。
4. `AlignmentAlpha`用于plant plane过渡和有限roll/hyperextension行为。
5. 整条腿回到FK由独立`DisableLeg`曲线控制。
6. `AlignPlantToGround`保留输入动画脚相对IK Root平面的高度，再把该高度框架对齐到Plant Plane。

项目没有复制UE实现，但职责边界必须一致：动画空间脚速负责Plant状态，普通Grounding Goal由独立作者权重控制，整腿关闭不能偷偷借用脚速阈值。

## 已踩过的坑

### 1. 把Plant Confidence连续映射成Goal Weight

错误公式曾把`PlantConfidence`执行`InverseLerp(0.5, 1)`。Run混合得到`0.65`时，Goal只剩`0.3`。运行采集证明FBBIK completion连续、failure为None、满Goal权重帧residual接近0，所以低权重发生在solver之前。

固定规则：Plant Confidence只做接触意图迟滞，不直接乘Foot Goal或Pelvis。

### 2. Grounding和Goal Source重复衰减

旧链路曾把Plant权重传入Grounding，再在Goal Source缩放一次。同一语义被两个层级重复解释，任何阈值调整都会产生不可预测的二次降权。

固定规则：Grounding先生成完整唯一结果，Goal Source只附加正式Placement、Support与Contact职责。

### 3. 把烘焙脚速、Body速度和yaw点速度相加

一次运行快照得到左脚约`10.64m/s`、右脚竖直约`5.938m/s`，而Body Grounded、Current Grounding Hit和solver都正常。原因是单Clip局部脚速已经描述动画运动，又额外加入Body可见速度和转向点速度，重复计算了运动。

固定规则：动画脚速不和Body或actor运动拼接。

### 4. 改成最终sole世界差分后仍把它当Goal总闸门

这一步解决了重复拼接，却没有解决职责错误。最终sole世界位置天然包含actor平移，持续跑动时左右脚都可能长期高速。已采集到移动帧左/右约`2.855m/s`和`11.983m/s`，Goal同时归零；松开输入后约`0.083m/s`和`0.264m/s`，Goal恢复。

固定规则：最终世界脚速可以作为诊断量，但不得决定普通Foot Goal是否存在。当前实现直接删除该历史与字段。

### 5. 用surface distance关闭Goal

曾出现脚速约`0.034m/s`但sole到support距离约`0.295m`，Goal仍被distance阈值清零。距离越大往往越需要IK修正，用它关闭Goal会在台阶、高低差和穿插最明显时失去效果。

固定规则：surface distance只用于诊断、Plant意图或明确的安全拒绝，不连续缩放普通Grounding Goal。

### 6. 只调大阈值

把`0.14/0.42m/s`改成`0.6/2.0m/s`如果仍控制整个Goal，只会推迟归零，不会修复持续输入包含actor平移的问题。角色跑速超过阈值时仍会整段失去IK。

固定规则：先修权威和职责，再讨论数值。UE阈值只能放在UE对应的Plant职责上。

### 7. 用首帧历史缺失解释跑动IK消失

相邻帧世界差分要求Reset后先积累历史，首帧会输出0。这个设计会制造切换、重置或重新附着时的短暂无IK，但它解释不了持续输入整段无IK。最终实现不再需要逐脚sole历史。

固定规则：生命周期缺样本不能成为普通Grounding Goal关闭条件。

### 8. Profile改了但生成Projection或Fixed Program仍是旧产品

Foot Placement Profile、Tuning Layout、Presentation Projection、Float32/Fixed Program与Native Pose Program存在正式revision依赖。只改源码或Profile文本，运行时仍可能消费旧生成产品，造成“代码看起来改了，效果没变”或直接stale错误。

固定规则：源码与作者资产改完后，等待用户明确触发Character Build一次性发布所有正式产品；不自动构建，不手改Generated资产，不保留兼容路径。

本轮还出现过更早一层的`Foot Placement Profile revision is stale: actual/computed`。它不是FBBIK运行错误，而是Profile schema或字段已经变化，作者资产保存的`m_Revision`仍属于旧内容；`CharacterFootPlacementProfile.RequireValid`在加载Projection引用的Profile时先拒绝了这份资产。这里必须先让作者Profile的revision与当前内容一致，之后仍由用户明确触发Character Build发布依赖它的新Projection与Program。两类stale不能混为一个问题，也不能通过关闭校验绕过。

### 9. 在Inspector或Continuous Capture路径做重操作

Continuous Capture如果在Inspector刷新链中执行大量格式化、遍历、导出或反复附着，会让Unity主线程卡住，表现为点击后无法操作其它控件。

固定规则：`OnInspectorGUI`只读取完成snapshot并低频显示；采集使用有界segment；CSV导出只在明确按钮触发；构建、资产扫描和编译必须是明确命令，禁止由Inspector repaint触发。

### 10. 用截图代替因果数据

截图只能证明某一帧姿势不好，不能区分Grounding无命中、Goal在solver前为0、Pelvis冲突释放、FBBIK失败或输出被后续覆盖。

固定规则：先读取同一PresentationFrame的typed snapshot，再决定需要用户提供哪张图。视觉截图只用于确认最终观感，不用于替代权重与completion链路。

### 11. 把MCP Build的Timeout直接当成构建失败

Character Build会原子写入大体积Program与Projection资产，并触发AssetDatabase导入和MCP bridge重载。调用端可能在产品已经落盘后收到`TimeoutError`，此时直接重试会重复执行重构建，也不能证明上一轮失败。

固定规则：Timeout后先检查目标Program、Projection或Fixed wrapper是否获得新的修改时间，再核对Editor.log中的目标资产导入、Unity Console error和精确Definition的`btsmtl.validate`。只有目标资产未发布或出现正式构建错误时才重试。

## 为什么改了很多轮仍没有做好

前几轮每次都修掉了一个真实问题，但一直保留同一个根错误：把某个“脚是否像支撑脚”的信号当成整个Foot Goal的总权重。

错误演进如下：

1. Plant Confidence直接控制Goal，Run混合权重偏低。
2. 改为烘焙脚速加Body/yaw速度，数值严重虚高。
3. 改为最终sole世界速度，数值来源更真实，但actor平移仍让持续输入高速。
4. 移除surface distance后，大修正不再被距离关闭，但脚速总闸门仍在。
5. 只有彻底拆开Placement、Plant Support与Contact职责，问题才从结构上结束。

因此失败不是“阈值还没调准”，而是信号权限一直没有拆干净。

## 固定排查链路

遇到“IK没效果、权重低、停下才恢复、脚穿地或膝盖异常”时，按以下顺序判断：

1. 检查Goal Set Completion与Solver Completion是否匹配，`SolverFailure`是否为None。
2. 检查Body `Target/Before/After Grounded`与Current Grounding Hit。
3. 检查`PlacementWeight`与最终Goal Weight。Placement为1但Goal为0，说明Goal被约束或Pelvis冲突释放，不是脚速问题。
4. 检查`PlantConfidence`、`AnimationFootSpeed`和`PlantContact`，只判断支撑/锁脚状态。
5. 检查`PlantSupportWeight`与`ContactWeight`，确认Pelvis贡献和anchor职责。
6. 检查Pelvis `RejectLeftGoal/RejectRightGoal`和逐腿可达区间。
7. 最后检查FullBodyIK residual、bend constraint与solved pose。

正常Corin `Unlocked`跑动帧应满足：

- 合法地面上`PlacementWeight`接近作者Foot Placement Weight。
- 最终Goal Weight不因Animation Foot Speed升高而归零。
- `ContactWeight = 0`，不创建anchor。
- stance脚可有`PlantSupportWeight`，swing脚通常为0。
- solver completion匹配且failure为None。

## 发布边界

本轮已经在用户明确授权后通过精确Definition MCP Build生命周期发布：Corin Float32 Program、Corin Fixed wrapper、Corin Presentation Projection与内嵌Native Pose Program，以及TrainingEnemy Float32 Program、Presentation Projection与内嵌Native Pose Program。Corin与TrainingEnemy的正式Agent Validate均成功，发布后Unity Console为0条error。TrainingEnemy没有正式Fixed wrapper资产，因此没有创建并行Fixed路径。

以后只要源码、Profile schema、Tuning Layout或作者资产再次变化，仍必须等待用户明确触发对应Character Build。禁止自动刷新Unity、选中资产、启动编译或运行构建，也禁止手改Generated资产。
