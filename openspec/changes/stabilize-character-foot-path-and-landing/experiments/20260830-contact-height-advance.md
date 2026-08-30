# 同Event接触交接继续正式脚高实验

## 状态与授权

用户要求根据现有ZZZ材料自行实验、分小步可回退并完成Replay，不再等待新的ZZZ活体同步证据。本记录只覆盖接触交接候选；Runtime由主任务负责，Editor Diagnostics由既有诊断任务独立提交。当前状态：候选已实现，尚未完成Editor Replay，不宣称质量改善。

## 唯一变量

保持现有世界Anchor、Swing/Approach轨迹、Projection、所有Profile参数、查询和Response数学不变。只有在相邻帧有效Swing Ground的正式Next Landing Event等于本帧首次Verified Anchor Event且Response历史仍有效时，使用：

`heightAdvance = normalized(ComponentUp) × (currentFormalFootHeight - previousSwingFormalFootHeight)`

`capturedResidual = previousWorldOutput + heightAdvance - selectedWorldTarget`

此前为`previousWorldOutput - selectedWorldTarget`。非准入帧`heightAdvance=0`是明确的未执行事实，不是缺失输入的替代值。之后仍在同帧执行既有Residual Advance、完整Vector完成容差与Correction Response；不改scalar重基、Direction或Goal权重。

这是项目的正式脚高交接实验，不是已证明的ZZZ字段映射。ZZZ已闭合的是新鲜Foot基准、独立目标采用与有限步长响应，不是3C世界Residual公式。禁止把旧失败的相对动画Capture、Approach整脚混合或单scalar换轴重新命名后引入。

## 原始依据

085503 Right475正式Foot Height为0.0285553113米，Response/Desired Y均为1.11716437；476正式脚高为0，Verified Anchor Y为1.08000016。原捕获Y为0.03716421，衰减后Y为0.0252863429。478–480希望目标仍在Anchor之上且Response已准确到达；FBBIK不是该段间隙的来源。

本候选可能减少这份尾差，也可能增加接触帧的速度或穿透。它不保证改善483的独立Response欠账，不以几毫米代表窗代替全包判断。

## 基线与持久证据

- Runtime基线：`7ed6522`；候选开始HEAD：`b8ed3c8`，后者只增加诊断与文档，不改变Runtime。
- Input Record：`3cDemo/Client/3C_Client/Diagnostics/CharacterInputTraces/20260827-183705-081-43357ff3cd384e5cba75d2c31175b116.json`。
- Input SHA256：`24D97232F35246C0B85A003B5980AC8F199D6FF63E9F74A0001B082F57EB89A6`。
- 原始包：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260830-085503-819-259090e6db3f45dc9ab4f24f0511458b`，1043输出帧、2086脚行；原包不覆盖。
- samples.csv SHA256：`F89385CD920E88898241561A59F3956BE9D5D3C52440AAAB5FAA71786AC13D7A`。
- 原Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260830-085623-668-473e5357b5954dbc8a2103576a6cfa48.json`。
- 持久Proof副本：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-contact-height-advance/baseline-proof.json`。
- 两份Proof SHA256均为`BFF5B93541C944C7A8D326DF202E8437BAAA6EBE50D9B67592770FA26C498119`；在本次构建前复制并核验。
- 评分基线：诊断任务使用085503原始副本生成facts52/diagnosis21；七维权重20/20/15/15/15/10/5，总分60.4只作浅层参考，腿部2段和锁脚8段不能代替充分证据。

## 接受或拒绝口径

按同一1044帧输入的完整Replay核对：Body/输入Proof、采样覆盖、Contact Anchor不变、接触间隙与持续段、接触帧位移、最终Heel/Toe穿透、Swing/Path、Pelvis、Reach、脚锁漂移、Bend异常及FBBIK成功率/残差。新增事实必须精确重算捕获参考与既有Decay，不得通过放宽阈值或补旧CSV列通过。

新旧schema不同的事实只比较共同语义，评分分项不因诊断重排被解释成运行改善。候选无效或增加回归时以独立撤销提交恢复，并保存失败采样；不动既有用户proposal/project修改和诊断评分提交。
