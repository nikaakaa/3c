# 同Event接触交接继续正式脚高实验

## 状态与授权

用户要求根据现有ZZZ材料自行实验、分小步可回退并完成Replay，不再等待新的ZZZ活体同步证据。本记录只覆盖接触交接候选；Runtime由主任务负责，Editor Diagnostics由既有诊断任务独立提交。当前状态：候选已被完整Replay否决，Runtime由4be1f51恢复，专属诊断由811dacb恢复，130545恢复Replay完成且全部原始行为字段与085503基线一致。没有把该候选交付为修复；原有踏空和反弯问题仍未宣称解决。

## 被否决候选的唯一变量

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

## 实际执行与身份

- Runtime候选：`9bce6c20704d988ff23fbfcf01db6024d1e095d4`；Diagnostics候选：`da438fa`。
- Runtime规定flags构建27个既有警告、0错误，Editor规定flags构建57个既有警告、0错误，每次立即shutdown；本change及全量strict均94/94。
- Unity只在Edit Mode刷新；首次出现3条SourceAssetDB文件时间戳导入错误，Refresh域重载恢复后正常启动Replay，Play中的Console为0错误；未在Play下刷新，未重启Editor或运行batchmode。
- 正式入口：`character.fixed_input_trace/replay_start`，精确trace `43357ff3cd384e5cba75d2c31175b116`，无新Record、无AD或直线测试。
- 候选完整包：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260830-124922-215-fa94eaea1fa04b3d96b7a275028d2bb7`。
- 1043输出帧、2086脚行、1148列、facts53/diagnosis22；8个新增事实列真实采样，Finalizer成功。没有补旧CSV列或覆写旧包。
- 候选Proof原路径：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260830-125110-104-7ca2e66dfea345e18aedfae9bd535cdd.json`，精确对085623基线`matched:1044`。
- 持久副本：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-contact-height-advance/candidate-proof.json`，原件和副本SHA256均`A3A6BA343B0F8B0D3E3C5E5EC21EDFB111B5500948E83CBC9FB2BDE4103CBDCE`。
- samples.csv SHA256：`A71CACC2A852DEAA277B463189110A0CFACB5D21543BAAA808DA8603B5AE3D33`。
- ground-path-geometry.csv SHA256：`98273314CF96B90A49722B624C06B1ABC820EEA97B2C4CAAE11973D1BF9B3BFA`。
- facts.json SHA256：`14380699133C918D73ACE2C0290E9AF8FF2E905572EDDA7714F9AA81B6983AAD`。
- b8ed3c8的正式评分基线整包已从Temp复制到`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/ArchivedAnalyses/facts52-b8ed3c8-085503`，12文件、150803355字节逐文件哈希一致；facts52 SHA256为`1CAF1CF15338A0E31A13FFE1DE9C63E5A370BD33F24DD28BA455C70FC3F40772`，quality-score.json为`753FF1A50AF4D5B5A96E4745DDC352D7D8F546D93347E74A0A9F7BB480C8C70C`。

## 公式正确不等于候选正确

525个Plant行，46个准入（Left24、Right22）；50个上一Swing历史匹配，473个清除/无历史匹配，2个采样起点明确Unavailable。facts捕获误差最大3.73e-9米，CSV独立复算最大3.64e-6米；没有通过修改实际位移公式或放宽阈值隐藏主动下降。

Body Tick、Presentation Delta和采样Alpha完全一致，动画Sole输入在1微米内一致。主表Landing Observation的QueryExecuted行数1349、Ground Path为1787、Heel/Toe SphereCastExecuted各2086，两包完全相同，不将这些出口行数冒称全部底层API调用总数。两包全部2086行FBBIK成功，候选最终脚踝Goal残差最大6.801e-7米，基线7.153e-7米。因此面下结果来自交给Solver的目标，而不是Solver失准。

| 真实接触帧 | 基线中心净空mm | 候选中心净空mm | 基线Heel净空mm | 候选Heel净空mm |
| --- | ---: | ---: | ---: | ---: |
| Right476 | 25.286 | 5.857 | 11.335 | -8.094 |
| Right478 | 11.706 | 2.712 | 11.711 | 2.717 |
| Right483 | 11.557 | 11.557 | 22.208 | 22.208 |
| Left530 | 17.054 | -8.531 | 6.646 | -18.939 |
| Right548 | -7.375 | -26.804 | -21.327 | -40.756 |
| Right656 | 5.025 | -12.248 | -8.926 | -26.199 |
| Left710 | 12.024 | -12.297 | -0.199 | -24.520 |
| Left746 | 12.025 | -13.608 | -0.199 | -25.831 |
| Right908 | -8.853 | -27.022 | -22.804 | -40.973 |

负净空表示面下。Right476物理Sole单帧位移26.526→39.276毫米；中心看似贴近，不代表Heel没有穿透。Left745的37.673毫米正式Height已被约-20毫米Swing Residual抵消，下一帧再扣完整Height就把该负残差交成Contact面下目标。正式Height由`max(0,动画SoleY-作者前后Landing插值基线Y)`生成，不能解释成最终输出里完整保留的一份位移。

20条State变化：17行Landing提前成为Locked；Left643由原Locked延后为Landing；Left255/619由原Swing延后为Releasing。FullAnchor段8→10，Sliding段17→20。最终Sole的XZ也发生后续变化，单轴最大约10.102毫米；骨盆Y最大版本间差约21.735毫米。不能把本帧WorldAdvance沿Up解释成完整轨迹、状态或腿姿态不变。

## 完整37项对账

下表为相同规则下matched/eligible；基线来自字节保留的facts52评分副本，候选来自Editor的facts53。旧Unity与离线.NET可能存在已有浮点/tie-break细差，否决同时由上面的同帧原始物理数据独立支持，恢复Replay还需核对同Editor口径。

| Target | 基线 | 候选 |
| --- | ---: | ---: |
| final-contact-plane-penetration | 19/78 | 27/81 |
| contact-plane-penetration-contribution | 72/78 | 76/81 |
| landing-leg-extension | 0/2 | 0/2 |
| landing-observation-reuse-contract | 0/1841 | 0/1841 |
| identity-only-residual-rebuild | 0/229 | 0/229 |
| path-revision-contract-mismatch | 0/731 | 0/730 |
| releasing-to-swing-envelope-violation | 1/53 | 1/53 |
| residual-deadline-miss | 0/48 | 0/48 |
| residual-growth-without-revision | 0/252 | 0/252 |
| late-approach-landing-revision | 179/325 | 179/325 |
| missed-landing-entry | 0/54 | 0/54 |
| early-landing-entry | 6/60 | 6/60 |
| landing-without-contact-plane | 0/60 | 0/60 |
| landing-not-closing | 11/60 | 10/60 |
| landing-wrong-exit | 0/60 | 0/60 |
| landing-exit-jump | 49/60 | 50/60 |
| landing-persists-after-formal-unlock | 0/60 | 0/60 |
| release-flyback | 2/59 | 2/59 |
| swing-to-landing-floor-handoff | 15/53 | 39/53 |
| plant-interpolation-output-jump | 315/523 | 316/523 |
| contact-acquisition-continuity | 49/54 | 52/54 |
| lock-weight-completion-by-contact-event | 32/60 | 29/60 |
| approach-progress-ownership | 0/376 | 0/376 |
| action-hard-ownership | 0/0 | 0/0 |
| contact-transition-context | 0/2086 | 0/2086 |
| formal-goal-weight-policy | 0/2086 | 0/2086 |
| contact-reentry-output-geometry | 0/0 | 0/0 |
| contact-support-gap | 12/60 | 13/60 |
| contact-state-output-jump | 405/1036 | 428/1038 |
| locked-horizontal-drift | 0/8 | 0/10 |
| locked-vertical-anchor-evidence | 1/25 | 2/30 |
| step-time-candidate-selection-observations | 0/2086 | 0/2086 |
| stable-swing-output-jump | 145/347 | 144/346 |
| path-revision-output-jump | 206/680 | 207/679 |
| path-revision-amplification-evidence | 206/680 | 207/679 |
| swing-actual-foot-envelope-counterfactual | 8/347 | 8/346 |
| stable-swing-correction-response-cadence | 8/182 | 8/182 |

七维分数依次为49/49/74/49/49/100/100→49/49/84/49/49/100/100，总分60.4→61.9；增加1.5分全部来自Stable Swing，腿部和锁脚仍是弱覆盖，不能据此通过。穿透段最大深度P90为23.993→25.842毫米、最大187.069→187.976毫米；接触物理附加位移P90为38.235→38.656毫米。虽然整脚间隙P90为72.618→60.266毫米、最长间隙持续时间中位数33.333→16.667毫秒，穿透、接触跳变及异常段数量仍恶化。

## 拒绝与恢复

裁决：拒绝，不加clamp、不清Residual.Y、不调Tolerance或Response速率补救同一失败候选。删除本次快照、DTO和诊断字段，恢复原`Captured = previousWorldOutput - selectedTarget`，保留b8ed3c8评分、a407368接触间隙诊断及全部历史样本。

Runtime撤销提交为`4be1f51cf6d40936d850994982ec79fae4eb25fa`，逐文件恢复且对b8ed3c8无差异；没有修改Profile、Projection、Program、输入Record、TrainingEnemy或用户proposal/project。恢复版Runtime另行用规定flags构建27个既有警告、0错误并立即shutdown。

Diagnostics撤销提交：`811dacb256090ebd49b7cc4d349e13ad96c19d49`，精确撤销da438fa六个诊断文件及6.30描述，目录对b8ed3c8无diff；facts52/diagnosis21、原Capture公式和七维评分恢复，不保留53兼容字段。已用规定flags构建恢复版Editor，57个既有警告、0错误并立即shutdown，strict94/94。已否决6.29/6.30从实施清单移除，失败结论仅在本记录与design保留。

## 恢复Replay封口

- 恢复后在Edit Mode执行完整Refresh，域重载恢复、Console为0错误后才启动同一trace，未在Play刷新。
- 包：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260830-130545-894-26a85534e5e4427dbd2d7d7979d5c585`，1043输出帧、2086脚行、1140列、facts52/diagnosis21；8个失败候选列均已删除，无兼容补值。
- 工具生成Proof：`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260830-130717-764-ca1ff10436ca4f7191456305cdbb1286.json`。正式工具自动对上一125110候选Proof给出`matched:1044`，此结果原样保留。
- 另只读比较085623原始Proof与恢复Proof：runtime_identity、trace/input/start-body/body-trajectory hash、驱动模式、起始Tick和1044条逐帧数组全部一致；没有修改比较器或Proof比较对象。
- 恢复Proof持久副本：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-contact-height-advance/restored-proof.json`，原件与副本SHA256均`584A432A59A1C7C2315250F1FDC0A2CA37C8E1D901A2735A1F75CCE6D116286B`。
- 恢复samples.csv SHA256：`715ED3920773E76234B749956A919C6D9B0C85A848F83BDC0BFDC52957C2E978`。
- 恢复geometry SHA256：`0C332A69F27E9350F3450AFD7624AE7A72F55F21AF94C11C3260C263306F9922`。
- 恢复facts.json SHA256：`7FEFEB9E66D6102784A173591E9D586CB89F6C029E8E034C8263FB9BBB14F75B`。

按FrameSequence+Side对085503原始主表逐列比较：2086行全部一一对应，1140共同列中1116列逐值完全相同，24个差异列仅为5个采样/实例元数据和19个Surface/Path身份字段。Body、正式Foot输入、原动画Sole、Interpolation、State、Pelvis、Goal、物理Heel/Toe、Solver成功与残差全部回到原值；没有任何State变化。恢复最大脚踝残差回到7.15255737e-7米，BendWeight为0的行数恢复1439。50195行、20列geometry只有SampleIdentity、GroundPathInputIdentity、GroundContactSurfaceIdentity、GroundContactCandidateIdentity四身份列变化，几何数值全部一致。

全部37个Target的规则、eligible、matched、rate与scorePolicy回到表中基线值，七维分数恢复49/49/74/49/49/100/100，总分60.4；接触可测525帧、60段与基线一致。离线.NET基线报告和Editor报告仍有既有统计舍入细差（包括速度/加速度/jerk派生量），没有把这些统计浮点写成逐值完全相等；原始物理数据的逐值一致独立证明本轮行为恢复。候选和恢复均已退出Play，当前Assets相对b8ed3c8无diff，实验只留下失败/恢复证据和明确禁止重复采用的合同说明。
