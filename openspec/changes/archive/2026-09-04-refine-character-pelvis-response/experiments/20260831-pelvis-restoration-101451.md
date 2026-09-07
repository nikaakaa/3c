# 10毫米候选撤销后的20毫米恢复验证

## 最终结论与时间边界

101451已证明本次撤销恢复了085223的运动，不是一个新的骨盆修复。Runtime算法仍是既有共同高度／中性软偏好／3Hz／正速度清理／20毫米硬Reach。

恢复资产提交a0aea66；本次加载的Diagnostics为2cf6da6，存储迁移到facts66语义、diagnosis35小报告及索引。之后的7308791工具meta修复、7ae5793／a9e5f42 Ground Path代码与诊断，以及902019b场景几何缓存时机修正均没有进入这次采样，不能拿本包为当前全部检出代码作动态背书。

## 精确证据

- 输入Record：43357ff3cd384e5cba75d2c31175b116。
- 直接前驱：20260831-085223-610-d02f3b0109c14331be7100a45a6d1a07。
- 失败候选：20260831-092855-825-6211bdaf960f461a8d4e96a533d38f58。
- 恢复包：20260831-101451-028-2cb22b36c1da429b9de78728fe923d9f。
- 正式Proof：20260831-102012-641-18c3eefada4e42efa78e85ca6b9e8e9e.json。
- [持久归档及逐文件清单](../../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-9-compression-reserve-candidate/restored-20mm/README.md)。

原件仍在Client/Diagnostics/FootPlacementRuns及正式Temp/CharacterInputReplayProofs路径，没有覆写。恢复包14个文件完整归档，原字节Proof独立压缩保存，避免Git换行规范化改变字节身份。

## 先纠正暂停时的状态描述

运行中曾看到603/1044、Editor暂停以及“系统分页内存约93%，丢弃Profiler帧”的警告。这只是当时状态快照，不能据此写成最终采样只有603帧。

随后正式发布的Proof有1044条完整frames，采样1043帧／2086脚行，geometry50195行，Frame Gap和Body Reset均0。停止操作回执已指向这份完成产物，最终退出本任务Play。没有重启Unity、杀其它进程、另起采样器或用旧路径填补。本记录不猜测恢复推进的内部时点，也不把警告解释为骨盆算法错误。

## 原始行为恢复

本次文档整理用低内存只读逐行比较，未重新运行Analyzer或Editor：

- samples.csv表头同为1221列，按FrameSequence／Side逐行对齐2086行。
- 1197列逐字符串相同，包括Body、正式Foot输入、原动画、Foot目标、状态、Anchor几何、完整Interpolation、Pelvis目标／Reach／Spring、Goal、Solved Knee及最终Physical点。
- 24个差异列逐项列于[raw-comparison.json](../../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-9-compression-reserve-candidate/restored-20mm/raw-comparison.json)：5个运行／采样元数据、19个Surface／Path身份；字符串双向映射均无冲突，没有泛化忽略所有Identity／Revision。
- geometry共50195行、20列；只有SampleIdentity、GroundPathInputIdentity、GroundContactSurfaceIdentity、GroundContactCandidateIdentity变化，其余16列逐值相同。
- 因此20毫米恢复后的骨盆和Knee坏窗也恢复，不能称它们已解决。092855的1厘米改善和膝盖回归留在失败包，不继续运行。

CSV流式比较进程峰值常驻约98.3MiB；没有同时读多个大facts到内存。该方法只是只读对账，不生成第二份正式诊断。

## Proof与正式报告

官方Proof自动选前一个093153／10毫米候选作基线，matched=false，只有7个Program／Projection aggregate身份差异，divergent_frame_count=0。此false原样保留。

另与085406／085223核对：Runtime identity与1044条完整frames逐值相同。Proof中的samples SHA与原件、analysis.json一致；不是单靠“输入1044匹配”宣布表现恢复，上一节原始行为列提供了独立证明。

37个Target逐项核对id、question、eventKinds、rules、eligible／matched／rate、scorePolicy及完整score，差异0。七维及总分61.9、Evidence86.9保持。quality-score除schema／facts产物引用外，只有3个Unavailable示例的CurrentSupport Surface实例值变化；不是接触覆盖变化。没有声称所有新旧报告文本或辅助measurement逐字相同。详见[diagnostic-comparison.json](../../../../3cDemo/Client/3C_Client/Diagnostics/FootPlacementReplayArchives/20260830-pelvis-response-refinement/step-9-compression-reserve-candidate/restored-20mm/diagnostic-comparison.json)。

新存储为diagnoses/analysis.json＋details.jsonl＋details-index.json＋8个小诊断报告＋quality-score.json。没有顶层facts.json是2cf6da6的正式布局，不是本次缺事实；也不以布局改变冒充行为变化。

## 原始哈希

| 产物 | SHA256 |
| --- | --- |
| samples.csv | e25fd3651bc1aa0d3f7e294e61263a75ed8634313ff645f5f7e4b20b7c6bb78e |
| ground-path-geometry.csv | 9af750cecebfe9e36e49df339c4ac0df01932aab111f3c3f3f65b5dd8d9c19a5 |
| analysis.json | 3f3afc9c56f18e7fe0b036ca07f0ba63fab991220dbc27064a9e624c945bab9b |
| 完整run ZIP | 78d9485495553728baa8262e8cfef15c41d81c097b26820f88f747d40eb0d7f3 |
| 正式Proof原字节 | 832347b107ec8686539dfee1d61ad6c6b7b84e18440617e5aad31299c8548881 |
| replay-proof-raw.zip | 0b6b89bbe779b96e78ebc2c96aa9c6d49f7d2eed22fcdf4ddd787cfc2968032c |

所有ZIP条目的解压流与原件逐项SHA256相同；manifest另存文件大小、身份、覆盖和官方比较原值。没有通过删坏帧、改规则、补旧列或改Proof来恢复。

## 此次恢复之外仍未完成的事项

- 骨盆世界突降、平地下压、R826及其它Solved Knee风险仍存在。
- 并行Ground Path修改需它自己的运行验证，不和本次恢复混成同一个变量。
- 新诊断查询工具GUID修复的Unity导入／动态发现尚未由本次记录验证。
- 只读ZZZ响应模型未实施，不能把本恢复包当那个模型的效果。
