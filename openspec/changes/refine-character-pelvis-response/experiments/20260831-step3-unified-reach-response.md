# 第3步：姿态偏好、硬Reach与一次骨盆响应

## 当前裁决

已按用户批准的第三步完成实现、编译、同Record回放和独立诊断。保留为可对照候选，不升级为新的质量基线，也不宣称整体优于193957。脚部保护守住，但骨盆大步换窗、膝盖极端峰扩大是实际代价；本轮停止追加行为变量，不自动回退，也不改膝盖或评分掩盖。

固定对照仍是193957。直接上一小步为235033。新包为20260831-010821-653-84292890c54d430181d20701b714d3be。

## 代码链路与改变范围

- 第1步df0c956/0550308：有效Ready Foot位置约束不因Correction小于0.1毫米撤销，作者权重0、Unavailable、旋转政策保留。
- 第2步b17b335/628e293：共同HeightTarget为同帧较低目标Sole减较低动画Sole，替换旧地形相对高度加正向补偿。这是KKK启发的项目适配，ZZZ只作为候选先于共同响应的结构参考。
- 第3步c4eb68b/725d795：Module先形成原有左右Reach准入，再一次调用ResolvePelvis。PosturePreference保留原动画弯曲需求但只影响目标；左右真实腿长减正式安全余量形成硬区间。目标先受完整硬区间约束，原Critical Spring积分一次，输出仅作一次硬夹紧并清除朝外速度。
- 删除Module.ApplyLandingReach后二次改写、WithLandingReachOutput、旧SupportReach字段与无生产者的SupportLegUnreachable枚举。没有新增配置、另一个Spring/Writer/Solver、Foot查询、Foot状态或膝盖策略。
- 交集失败仍明确保合法Primary优先，相关Foot经原夹紧和FullLock门处理。组合Role的Event属于实际Foot Request，不伪装成Primary Event。

第3步唯一Diagnostics升级facts61/Analyzer61/diagnosis30，1216列=1156−7+67。公共pelvisFrames包含HeightTarget、PosturePreference、Reach、Response，不保留旧列或补值兼容。

## 构建、加载与证据

Runtime规定flags两次构建0错误，27/1个既有警告；Editor规定flags57个既有警告、0错误，每次结束均shutdown。两项change及全量OpenSpec strict为95/95。Unity只在Edit执行一次完整Refresh；新回放完成、Finalizer封口，Console0错误，随后返回Edit。

三包均1043采样帧/2086脚行、Frame3–1045。新Proof011000对第二步235246官方matched1044、0分歧，runtime身份、输入/Body/start hash和逐帧数组一致。193957没有直接绑定的官方Proof，不伪造该关系；固定raw输入/Body/时序/几何另行对账。

三包geometry均50195行20列，只有四个实例身份列不同。新CSV SHA256为5635F04BACDB60B13853A4DC6DB232421978511300E9FDC419480A4A35BB1EE9。12文件ZIP及独立Proof已保存到Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-3-unified-reach-response，逐文件哈希相同。所有原包、前两步、193957及212054失败材料均未覆盖或清理。

## 一次响应的数学与覆盖

1043份公共事实左右逐值一致，与现有landing-leg-extension报告中的pelvisFrames一致。HeightTarget/Posture消费455帧且全部PostureAvailable；588帧为Releasing，不把未执行的高度/姿态字段当零需求测量。

Reach与Response均1043帧执行；所有Reach均Available/AllRequestedLegs。硬目标夹紧255次，硬输出夹紧171次，朝外速度清零5次，Handoff速度重置99次。实际PositionWeight为1039帧1、4帧0，作者权重全1。

硬半径复算误差至多0.032微米，区间复算3.817微米；实际Output×PositionWeight越界至多3.5纳米，按正式脚请求核对均未违反半径保护。一次Spring复算的未夹紧/最终Output误差至多0.025微米，Target相同。38帧输出位于旧姿态区间外、仍在真实硬区间内，证明它不再被姿态偏好直接夹紧。

无动态覆盖：横向不可达、无交集、最终PrimarySupportOnly选择、PostureUnavailable、SpringCompleted、非1作者权重。不能以该包声称这些边界均已运行验证。存在Primary-only输入角色不等于曾选择Primary-only最终边界。

## Foot保护结果

相对235033，State、LockResponse、Anchor几何、Foot权重无变化；Ankle/Heel最大版本差1.063微米，Toe1.022微米。Ankle Goal残差最大约0.995微米。L339/L515/R611及322/466/675的脚目标守住，GoalClamp仍0。

固定193957的525个可测Contact集合保持，Gap超过1/2/5/10厘米仍178/118/44/11，端点平面负距仍77/41/6/1。Gap/穿透事件3/60、34/90保持。此距离只对应已验证ContactPlane，不证明有限Collider面仍在脚下，也不宣称现存全部接触问题已清。

相对235033，37项Target的规则、eligible、matched、Health/Evidence保持；辅助测量存在真实变化及微米舍入，不写成所有统计逐值相同。相对193957的Stable144→143、Path205→199、Contact435→433改善属于第1步，第三步不重复计功。总分三包61.9不能衡量下面的全程骨盆和膝盖风险。

## 骨盆：改善与新增坏窗并存

统计口径为共享每帧实际加权修正量Output×PositionWeight的相邻差，共1042对；Releasing默认AnimatedPelvis不作为真实动画点。真实Physical Pelvis组件位移另行对账，与Goal差值变化误差至多0.51微米。

| 指标 | 193957 | 235033 | 010821 |
| --- | ---: | ---: | ---: |
| 骨盆修正单步中位数 | 8.972 mm | 8.446 mm | 7.743 mm |
| P90 | 24.267 mm | 24.085 mm | 24.774 mm |
| P99 | 74.938 mm | 74.938 mm | 75.304 mm |
| 超过50 mm | 30 | 30 | 30 |
| 最大单步 | 89.362 mm | 89.362 mm | 89.362 mm |

675：旧姿态边界−76.901毫米不再直接压输出，新输出为真实硬上界−15.185毫米；单步下降73.546→11.053毫米。711单步52.361→2.097毫米，81964.293→4.927毫米。这三项移出超过5厘米集合。

新增超过5厘米为26624.087→50.122毫米、59142.163→60.101毫米、78947.092→57.856毫米。已有23152.723→69.937、26753.528→70.808、30354.033→71.267毫米加重。322与466仍下降89.362/79.742毫米，来自真实硬边界。348/852仍约40毫米修正下降，第1步的外溢并未消失。

新增大步不是姿态偏好又被当硬边界：266的上一输出35.712毫米，本帧Spring仍32.806，硬上界已到−14.409，必须夹到上界；591为65.320→5.219，789为48.244→−9.611。减少了前面的预压后，后续真实几何边界仍快速收紧，形成更晚的追降。唯一响应的结构本身不能保证移动硬边界永远连续。

第三步超过5厘米的完整帧集：
196,214,215,231,232,249,250,266,267,268,285,286,287,303,304,321,322,448,465,466,484,501,502,574,591,592,610,772,789,790。

## Knee：没有顺手修复，极端峰确实扩大

同侧2084帧对，extra为Component域Δ(SolvedKnee−OriginalKnee)，不冒称新增Final Physical Knee观测。

| 指标 | 193957 | 235033 | 010821 |
| --- | ---: | ---: | ---: |
| extra超过50 mm | 415 | 418 | 431 |
| extra超过100 mm | 103 | 98 | 89 |
| extra最大 | 592.955 mm | 592.721 mm | 601.725 mm |
| 实际Solved Knee最大单步 | 541.183 mm | 541.465 mm | 626.929 mm |

R826的单步534.530→524.029→626.929毫米成为新全包峰。该段同Source/Event持续Swing、脚目标不动；骨盆历史改变了Hip/目标几何与Bend历史。R826髋踝距离约43.843毫米、仅腿长6.304%，BendWeight=0。不是新Foot/Path大跳，不能宣称本步修好了反弯。

R1043原214.632毫米峰移至1044的新221.154毫米；第二步R881→882、R933→934的风险也还在。675左膝extra52.324→102.117毫米而右膝改善；711左膝46.966→68.420毫米而右膝改善。不能仅展示同一帧骨盆或某一侧的收益。

## 交付边界

这三步作为完整代码/诊断/数据候选保留供对照，不自动撤销，也不把它替换193957为“最好版本”。脚部目标有效性与统一骨盆计算的合同已实现，部分不必要即刻下压被消除；但骨盆整体尾部、深压缩膝盖仍不合格。本轮到此封口，不调参数、不新增前视或膝盖策略、不修改原评分来宣布胜出。
