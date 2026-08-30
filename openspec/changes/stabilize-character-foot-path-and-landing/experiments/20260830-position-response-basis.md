# 位置响应与支撑朝向分责实验

## 当前状态

用户要求持续实验。基线为c519865及130545恢复包；前一轮9bce6c2脚高扣除候选已经否决并完整恢复，本轮不复活该Capture改动。位置basis候选Runtime提交05889f6，规定flags构建27个既有警告、0错误；Diagnostics提交335ac60，Editor构建57个既有警告、0错误，两次build server均已关闭。已在Edit模式Refresh并显式重建Corin Float32、Fixed和Projection，Console错误为0；新Replay尚未完成，不宣称修复成功。

## 可证伪靶点

按同Source/Cycle/Event、三帧NoRevision、NoTargetTracking且Path两端完全稳定筛选，恢复基线有12组Support Direction A-B-A；这些帧Rotation Weight均为0。Left412–414的旧Response相对Desired额外Z为0/-10.315188/0毫米，Right360–362为0/-5.084044/0毫米，Right857–859为0/-6.028007/0毫米。来源是`O=D+N*(c-q)`把尚未消除的位置scalar欠账随Support Normal转向，并非Foot Rotation、Contact Capture或Body输入反切。

本轮预期消除这项直接XZ来源，但不承诺全部Swing速度阶梯消失，也不承诺Sliding的相对动画高度欠账同时解决。任何新增穿透、Contact跳变、Reach/膝盖问题或世界Anchor漂移仍须否决或修正同一步。

## 业务输入与数学

CaptureFoot从同帧Component Pose经`Binding.Animator.transform`（PoseRoot）取得世界Foot/Heel/Toe；OriginalSole是Heel/Toe中点。State Target/WorldResidual发布完整世界Sole目标D。Lifecycle在响应后按正式Position/Rotation Weight反解Ankle，Module再经同一PoseRoot的逆变换编码唯一Goal。PoseRoot不是VisualRoot，不能由Body Up或查询Normal代替。

从真实owner矩阵取：

```text
s = |ownerToWorld.Linear * LocalY|
a = ownerToWorld.Linear * LocalY / s
h = worldToOwner.RowY.xyz * s
q = dot(DesiredSole - OriginalSole, h)
c = MoveTowards(previousC, q, existingSelectedSpeed * PresentationDelta)
ResponseSole = DesiredSole + a * (c - q)
```

c、q及速度继续使用世界米与米/秒。h是dual，不得归一化；它与a满足`h·a=1`。等价式为`G(O).xz=G(D).xz`与`G(O).y=G(B).y+c/s`。正式可见输出转移的PreviousScalar也用h，完整世界Residual仍保留原XYZ捕获、同帧一次Advance和完整向量完成容差。

Support Direction的请求、上一值、角限制和Applied值只继续供Foot Rotation，数值10°不改。配置由`CorrectionResponseMaximumDirectionChangeDegrees`改名为`SupportDirectionMaximumChangeDegrees`，Profile由v35升级v36；旧配置/公开Direction名不保留兼容。两档速率1.8/1.5、目标Height模式、查询参数、Contact/Lock/RotationWeight政策均保持原值。

## ZZZ对应与限制

- 已闭合采用：7910在owner局部Y推进位置，Support Direction不是直接的位置位移轴；同一owner点变换与Foot pivot输入身份已由只读专项确认。
- 项目适配：这里的B/D是正式Sole，而ZZZ的F是预处理Foot pivot，原`desiredRaw=G(P+WorldUp*(S.y*F.y/N.y)).y-F.y`不是上述q；不把本候选称为原样复刻。
- 不混入：ZZZ owner-Y选速率、g响应后高度缩放、k晚期基准混合、writer W旋转权重和幅度折返。
- 不夸大：basis数学允许有限可逆非均匀矩阵，不表示现有Heel/Toe Quaternion重建已验证非均匀父级；Owner自身Up/尺度变化时保留c也不保证绝对世界连续。本记录的PoseRoot Up稳定，验收只对真实覆盖范围下结论。
- SourceFrame、World、矩阵有限性/可逆性或dual关系失败直接拒绝，不补默认Up/旧basis，不创建第二Interpolation、Solver、Writer或Pose低通。

## 既有可见输出读取缺陷与本轮覆盖边界

只读专项及主任务直接代码复核确认：`CharacterPoseConstraintRuntime.SealFrame`在提交bank前清除`FootPlacement.HasFrame`，该字段表示Pending事务开放；下一帧`TryResolvePreviousVisibleOutput`却要求Committed bank的`HasFrame`为true。因此虽然`EvaluateFrame`已保存加权/Reach后Goal对应的左右可见Sole及`HasVisibleFootOutputs`，正式读取一直被拒绝。此问题在7ed6522/c519865已存在，不是05889位置basis候选引入。

085503、124922和130545均2086脚行，`CorrectionResponseVisibleOutputTransferred`均为0。当前完整WorldResidual实际使用Interpolation内部上一ResponseOutput，而不是bank保存的上一加权/Reach后Goal Sole。本轮把正式VisibleTransfer分支改为同一dual投影只是静态合同闭合；没有实际覆盖时，不得用本轮Replay宣称验证了该分支。

本轮不修改该读取门。其修复会首次激活另一份历史输入及scalar重基，必须在位置basis实验封口后单独提交、单独Replay。不得保留SealFrame.HasFrame为true以绕过事务，也不得新增替代历史或用单标量替换完整世界Residual。

## 基线证据

- Input：`Diagnostics/CharacterInputTraces/20260827-183705-081-43357ff3cd384e5cba75d2c31175b116.json`，SHA256 `24D97232F35246C0B85A003B5980AC8F199D6FF63E9F74A0001B082F57EB89A6`，1044帧。
- 基线包：`Diagnostics/FootPlacementRuns/20260830-130545-894-26a85534e5e4427dbd2d7d7979d5c585`，2086脚行、facts52/diagnosis21。
- 主表SHA256：`715ED3920773E76234B749956A919C6D9B0C85A848F83BDC0BFDC52957C2E978`。
- 几何表SHA256：`0C332A69F27E9350F3450AFD7624AE7A72F55F21AF94C11C3260C263306F9922`。
- facts SHA256：`7FEFEB9E66D6102784A173591E9D586CB89F6C029E8E034C8263FB9BBB14F75B`。
- 已持久保存的基线Proof：`Diagnostics/FootPlacementReplayArchives/20260830-contact-height-advance/restored-proof.json`，SHA256 `584A432A59A1C7C2315250F1FDC0A2CA37C8E1D901A2735A1F75CCE6D116286B`，不依赖Temp。
- 7维基线为49/49/74/49/49/100/100，总分60.4。穿透19/78、接触未贴合12/60、Stable Swing145/347、Path206/680、Contact405/1036、腿部0/2、FullAnchor水平漂移0/8；弱样本不升级为全场景证明。

## 候选构建与产物

唯一诊断版本为facts54、Analyzer54、diagnosis-file23；quality-score/1及七维质量规则保持原样。新增8个位置basis标量，删除旧CorrectionResponse方向列并以SupportDirection独立命名；不为旧facts52补字段。

- Float32正式job：`90d15322b24a4d4cbe7d291a2545b86a`；Program Hash=`dc2f2e588d9ac6ff8618b329a2789ec5875162e95cd6c3df2dca612f3847d0a5`，资产SHA256=`d9851b1f08072732b505e9ec8e3615f04962cf437f3c8415f3865d24374e8cca`。
- Fixed正式job：`1cc556e95c344dcc8178e47988fc77c6`；Program Hash=`f233be464df11ceb4bbf514bdab0242ea3a3ee50b68f6556609d74d142676f73`，资产SHA256=`d849f6b55142a5a73bdaf857a39dd618518d4505b7b8c5599d1e669d7be5b4f1`。
- 共同Source Revision=`c546b0b3f23443c142a013dd4b37f7f2a4e27b76d07cd52d3805a8cf1deba0e3`，Semantic Hash=`dc18e2624294eb1390c209f432e7c0baeeb278c6c62307b613282ff5d097433e`。
- Projection Revision=`c7b236184b7bc77ed30604aa446b627f7aa33546eef4793be2e66a2c31d1a895`，Contract Hash=`16ef87e562a46b4a82fcfceb35c6b425b155dbb148fc15fcba7db1f237b0a8b1`，资产SHA256=`c1ff93c5918e50a214c9b27c327763ee70d4fe08f127e8dec6ee67931a81ab4f`。

相对c519865的只读字节核对：Float32/Fixed canonical artifact长度分别保持3079259/3234949字节，每份各301字节变化，仅位于Source Revision、Semantic Hash及Program Hash字符串；执行payload其余字节相同。最终Projection diff仅10处身份/布局hash，Curve/Event数值及引用数据未变。上述静态身份对账不代替逐帧Replay Proof，不修改比较器放行身份变化。

## 对账规则

Profile身份改名/升级后必须显式重建Corin Float32与Fixed产物，身份变化原样记录，不修改Proof比较器；先核对同一输入与逐帧Body轨迹，再判断Foot行为。全部原始采样继续由唯一Recorder/Analyzer/Publisher生成。新basis字段与Support Direction字段缺失必须typed拒绝，不为旧CSV补列。

逐项检查12个稳定ABA窗口、额外XZ、Y跳变、同Event完整Anchor、Landing/Locked/Release变化、Heel/Toe穿透与接触间隙、Source/Desired/Response/Resolved/Goal/Physical首次差异、Support/Pelvis、Reach与膝盖、FBBIK残差及QueryExecuted行数。候选接受前不把g、Contact时限、旋转权重或其它修复叠入；拒绝时只撤销本轮代码/资产/产物并再次回放，保留失败记录。
