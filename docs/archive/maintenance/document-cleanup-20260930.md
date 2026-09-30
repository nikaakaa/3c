# 2026-09-30 文档整理报告

## 结果

原 `docs/` 根目录的 13 份散落文档收拢为一个[文档入口](../../README.md)。普通文档分为业务进展、抄录、参考和按主题保存的历史记录。迁移 57 份文档，其中 46 份进入历史区；两份 Rush 对照合成一份；删除一份只转发到已完成重构 change 的冗余入口。

已完成 OpenSpec 变更归档 7 个，活跃目录从 18 个减为 11 个。补齐 5 份现行规格，合并 2 份已有规格的有效增量，并保留后续架构已经取代的旧 delta 为历史。归档没有把尚未完成的 Foot、Camera、战斗、性能、AI、预览或工作台业务标成完成。

## 清理依据与现行 owner

| Finding | 原维护负担及分类 | 处理及现行 owner | 验证与保留边界 |
| --- | --- | --- | --- |
| S1 | 根目录混有来源、当前进展和历史现场 | 用一个 docs/README 导航，按 status、replication、reference、archive 归位 | 核对全部普通文档与入站链接；正式合同仍归 project/specs |
| S2 | 日期报告中的旧“当前/待完成”被当作活跃任务 | 阶段记录按 foot-placement、camera、combat、animation、architecture、coordination 归档，三份 status 只组织证据和未闭环范围 | 原事实和失败边界保留；不删除采样、JSON、CSV、二进制、截图或运行失败 |
| S3 | Rush Ability/FSM 与 Timeline 分散维护 | 合成 replication/corin-rush-map，保留全部独有来源、条件、窗口和回放范围 | 合并后的入口、来源和回放数据链接检查；没有改变实际作者资产 |
| S4 | 重构说明只重复 proposal/tasks/design 的跳转 | 删除 architecture-refactoring-plan，入站链接改为已归档 change 的收口记录 | 检查旧入口残留；完整设计和 29 项实施仍可访问 |
| S5 | July 行为基准混有可观察结果和已退役实现 | 完整原文归档，reference/gameplay-behavior-baseline 只保留玩家结果并引用现行 owner | 不再将“Foot 只验证 Landing”或旧 Program 当当前合同；未重跑行为验收 |
| S6 | 7 个已完成 change 仍占用活跃规划，部分 delta 未安装或被后续架构替代 | 有效能力写入现行规格后归档，独立 archive-summary 记录承接与排除理由 | 正式 CLI 检查任务及 artifact；现行严格校验；不覆盖新版 Slate、原生 Pose 或两域 TreeDecision |
| S7 | 原 Rendering 入口夹带旧编译阻塞与阶段恢复状态 | 7 份阶段记录移到 Tools/Rendering/Archive/Records；数据、LUT、MatCap、静态合同和推导保留原工具 owner | 修正引用，保留原画面/程序/参数来源；没有修改渲染代码或美术资源 |

全部候选均在文档所有权、来源、链接和正式合同边界内处理。第三方 Packages/Assets 文档、许可证、AGENTS、skills 执行规则和已有原始诊断数据不属于删除范围。未完成的活跃 change 保留原 tasks，不为归档制造虚假勾选。

## OpenSpec 归档

| Change | 完成项 | 归档与承接记录 |
| --- | ---: | --- |
| `refactor-character-runtime-and-authoring-boundaries` | 29/29 | [角色运行与作者职责重构](../../../openspec/changes/archive/2026-09-30-refactor-character-runtime-and-authoring-boundaries/archive-summary.md) |
| `add-network-model-locomotion-presentation-policy` | 22/22 | [网络模型 locomotion 表现策略](../../../openspec/changes/archive/2026-09-30-add-network-model-locomotion-presentation-policy/archive-summary.md) |
| `extend-modify-bone-and-add-corin-lean` | 15/15 | [通用骨骼变换与 Corin 侧倾](../../../openspec/changes/archive/2026-09-30-extend-modify-bone-and-add-corin-lean/archive-summary.md) |
| `sync-turnback-entry-root-motion` | 10/10 | [TurnBack 完整运动与 RunLoop 相位入口](../../../openspec/changes/archive/2026-09-30-sync-turnback-entry-root-motion/archive-summary.md) |
| `add-timeline-clock-domain-config` | 56/56 | [Timeline 秒制时间与时钟职责](../../../openspec/changes/archive/2026-09-30-add-timeline-clock-domain-config/archive-summary.md) |
| `add-open-ended-treeclip-preview` | 14/14 | [开放式 TreeClip 观察](../../../openspec/changes/archive/2026-09-30-add-open-ended-treeclip-preview/archive-summary.md) |
| `integrate-native-fsm-skill-authoring` | 46/46 | [原生 FSM 技能作者链](../../../openspec/changes/archive/2026-09-30-integrate-native-fsm-skill-authoring/archive-summary.md) |

归档前读取当前 project、主规格、delta、CLI status 和 specs instructions。所有任务与 artifact 完成后，先人工合并当前有效合同，再执行 `openspec archive --skip-specs --yes`；没有使用 `--no-validate`。旧条款因被后续正式规格取代而不机械覆盖，具体理由由各 archive-summary 拥有。

补齐：btsmtl-timeline-clock-domain、btsmtl-flowcanvas-authoring、character-pose-bone-transform、corin-locomotion-lean、character-control-motion-entry。合并：character-animation-event-graph、character-animation-layer-runtime。开放式预览已经由现行 btsmtl-timeline-editor-preview 的动态长度/退出事实规则承接，不恢复旧 Presentation 禁止 TreeDecision 的规则。

## 文件处理明细

| 操作 | 原路径 | 新路径/承接 |
| --- | --- | --- |
| 历史归档 | `docs/coordination-progress.md` | [docs/archive/records/coordination/coordination-progress-20260914.md](../records/coordination/coordination-progress-20260914.md) |
| 历史归档 | `docs/replay-closure-20260919.md` | [docs/archive/records/combat/replay-closure-20260919.md](../records/combat/replay-closure-20260919.md) |
| 历史归档 | `docs/combat-closure-evidence-20260920.md` | [docs/archive/records/combat/combat-closure-evidence-20260920.md](../records/combat/combat-closure-evidence-20260920.md) |
| 历史归档 | `docs/zzz-corin-normal-attack-timeline-review.md` | [docs/archive/records/combat/normal-attack-timeline-review.md](../records/combat/normal-attack-timeline-review.md) |
| 归位/改名 | `docs/zzz-corin-controller-map.md` | [docs/replication/corin-controller-map.md](../../replication/corin-controller-map.md) |
| 归位/改名 | `docs/zzz-corin-copy-sources.md` | [docs/replication/corin-copy-sources.md](../../replication/corin-copy-sources.md) |
| 归位/改名 | `docs/zzz-corin-normal-attack-cue-map.md` | [docs/replication/corin-normal-attack-map.md](../../replication/corin-normal-attack-map.md) |
| 归位/改名 | `docs/gdc2016-foot-ik学习文案.md` | [docs/reference/foot-placement/gdc2016-fitting-the-world.md](../../reference/foot-placement/gdc2016-fitting-the-world.md) |
| 归位/改名 | `docs/预测ik经验_这个是ai写的_可行度很一般.md` | [docs/reference/foot-placement/implementation-lessons.md](../../reference/foot-placement/implementation-lessons.md) |
| 归位/改名 | `docs/NKGMobaBasedOnET-frame-sync-reference.md` | [docs/reference/network/frame-sync-reference.md](../../reference/network/frame-sync-reference.md) |
| 归位/改名 | `docs/foot-placement参考参考_kkk/1.md` | [docs/reference/foot-placement/predict-foot-ik-ue5-shadow.md](../../reference/foot-placement/predict-foot-ik-ue5-shadow.md) |
| 归位/改名 | `docs/foot-placement参考参考_kkk/2.md` | [docs/reference/foot-placement/predict-foot-ik-ue5-experiment.md](../../reference/foot-placement/predict-foot-ik-ue5-experiment.md) |
| 归位/改名 | `docs/foot-placement参考参考_kkk/predict-foot-ik-implementation-summary.md` | [docs/reference/foot-placement/predict-foot-ik-implementation-summary.md](../../reference/foot-placement/predict-foot-ik-implementation-summary.md) |
| 归位/改名 | `docs/diagnostics/hit-detection-options-research-20260929.md` | [docs/reference/combat/hit-detection-options-20260929.md](../../reference/combat/hit-detection-options-20260929.md) |
| 归位/改名 | `docs/diagnostics/corin-camera-shake-replication-20260928.md` | [docs/reference/camera/corin-camera-shake-source-parity-20260928.md](../../reference/camera/corin-camera-shake-source-parity-20260928.md) |
| 历史归档 | `openspec/character-pipeline-runtime-behavior-baseline.md` | [docs/archive/records/architecture/character-pipeline-runtime-behavior-baseline-20260723.md](../records/architecture/character-pipeline-runtime-behavior-baseline-20260723.md) |
| 历史归档 | `docs/diagnostics/animation-validation-cleanup-20260928.md` | [docs/archive/records/animation/animation-validation-cleanup-20260928.md](../records/animation/animation-validation-cleanup-20260928.md) |
| 历史归档 | `docs/diagnostics/btsmtl-treeclip-editor-lag-20260928.md` | [docs/archive/records/architecture/btsmtl-treeclip-editor-lag-20260928.md](../records/architecture/btsmtl-treeclip-editor-lag-20260928.md) |
| 历史归档 | `docs/diagnostics/character-assembly-boundaries-20260929.md` | [docs/archive/records/architecture/character-assembly-boundaries-20260929.md](../records/architecture/character-assembly-boundaries-20260929.md) |
| 历史归档 | `docs/diagnostics/corin-action-scalar-availability-20260928.md` | [docs/archive/records/animation/corin-action-scalar-availability-20260928.md](../records/animation/corin-action-scalar-availability-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-attack-steering-dump-20260928.md` | [docs/archive/records/combat/corin-attack-steering-dump-20260928.md](../records/combat/corin-attack-steering-dump-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-attack5-end-repair-20260927.md` | [docs/archive/records/combat/corin-attack5-end-repair-20260927.md](../records/combat/corin-attack5-end-repair-20260927.md) |
| 历史归档 | `docs/diagnostics/corin-camera-basis-and-hit-20260929.md` | [docs/archive/records/camera/corin-camera-basis-and-hit-20260929.md](../records/camera/corin-camera-basis-and-hit-20260929.md) |
| 历史归档 | `docs/diagnostics/corin-camera-consumption-audit-20260928.md` | [docs/archive/records/camera/corin-camera-consumption-audit-20260928.md](../records/camera/corin-camera-consumption-audit-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-camera-delivery-20260927.md` | [docs/archive/records/camera/corin-camera-delivery-20260927.md](../records/camera/corin-camera-delivery-20260927.md) |
| 历史归档 | `docs/diagnostics/corin-downhill-response-20260927.md` | [docs/archive/records/foot-placement/corin-downhill-response-20260927.md](../records/foot-placement/corin-downhill-response-20260927.md) |
| 历史归档 | `docs/diagnostics/corin-foot-2dad-review-20260928.md` | [docs/archive/records/foot-placement/corin-foot-2dad-review-20260928.md](../records/foot-placement/corin-foot-2dad-review-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-b173-review-20260928.md` | [docs/archive/records/foot-placement/corin-foot-b173-review-20260928.md](../records/foot-placement/corin-foot-b173-review-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-c28-review-20260928.md` | [docs/archive/records/foot-placement/corin-foot-c28-review-20260928.md](../records/foot-placement/corin-foot-c28-review-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-c75d-walk-attack-20260928.md` | [docs/archive/records/foot-placement/corin-foot-c75d-walk-attack-20260928.md](../records/foot-placement/corin-foot-c75d-walk-attack-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-contact-reach-20260928.md` | [docs/archive/records/foot-placement/corin-foot-contact-reach-20260928.md](../records/foot-placement/corin-foot-contact-reach-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-floor-owner-fix-20260928.md` | [docs/archive/records/foot-placement/corin-foot-floor-owner-fix-20260928.md](../records/foot-placement/corin-foot-floor-owner-fix-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-height-handoff-20260928.md` | [docs/archive/records/foot-placement/corin-foot-height-handoff-20260928.md](../records/foot-placement/corin-foot-height-handoff-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-landing-target-20260928.md` | [docs/archive/records/foot-placement/corin-foot-landing-target-20260928.md](../records/foot-placement/corin-foot-landing-target-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-observation-roundoff-20260928.md` | [docs/archive/records/foot-placement/corin-foot-observation-roundoff-20260928.md](../records/foot-placement/corin-foot-observation-roundoff-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-query-geometry-20260927.md` | [docs/archive/records/foot-placement/corin-foot-query-geometry-20260927.md](../records/foot-placement/corin-foot-query-geometry-20260927.md) |
| 历史归档 | `docs/diagnostics/corin-foot-release-target-20260928.md` | [docs/archive/records/foot-placement/corin-foot-release-target-20260928.md](../records/foot-placement/corin-foot-release-target-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-residual-tail-20260928.md` | [docs/archive/records/foot-placement/corin-foot-residual-tail-20260928.md](../records/foot-placement/corin-foot-residual-tail-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-sample-65ec-20260928.md` | [docs/archive/records/foot-placement/corin-foot-sample-65ec-20260928.md](../records/foot-placement/corin-foot-sample-65ec-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-sole-support-20260928.md` | [docs/archive/records/foot-placement/corin-foot-sole-support-20260928.md](../records/foot-placement/corin-foot-sole-support-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-foot-support-face-20260928.md` | [docs/archive/records/foot-placement/corin-foot-support-face-20260928.md](../records/foot-placement/corin-foot-support-face-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-hit-chain-handoff-20260929.md` | [docs/archive/records/combat/corin-hit-chain-handoff-20260929.md](../records/combat/corin-hit-chain-handoff-20260929.md) |
| 历史归档 | `docs/diagnostics/corin-motionwarp-action-lifetime-20260928.md` | [docs/archive/records/animation/corin-motionwarp-action-lifetime-20260928.md](../records/animation/corin-motionwarp-action-lifetime-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-motionwarp-loop-boundary-20260928.md` | [docs/archive/records/animation/corin-motionwarp-loop-boundary-20260928.md](../records/animation/corin-motionwarp-loop-boundary-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-pose-write-monitor-20260928.md` | [docs/archive/records/foot-placement/corin-pose-write-monitor-20260928.md](../records/foot-placement/corin-pose-write-monitor-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-sampling-20260927.md` | [docs/archive/records/foot-placement/corin-sampling-20260927.md](../records/foot-placement/corin-sampling-20260927.md) |
| 历史归档 | `docs/diagnostics/corin-steering-implementation-20260928.md` | [docs/archive/records/animation/corin-steering-implementation-20260928.md](../records/animation/corin-steering-implementation-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-turnback-phase-2026-09-27.md` | [docs/archive/records/animation/corin-turnback-phase-2026-09-27.md](../records/animation/corin-turnback-phase-2026-09-27.md) |
| 历史归档 | `docs/diagnostics/corin-walk-phase-audit-20260928.md` | [docs/archive/records/animation/corin-walk-phase-audit-20260928.md](../records/animation/corin-walk-phase-audit-20260928.md) |
| 历史归档 | `docs/diagnostics/corin-walkstart-handoff-20260928.md` | [docs/archive/records/animation/corin-walkstart-handoff-20260928.md](../records/animation/corin-walkstart-handoff-20260928.md) |
| 历史归档 | `Tools/Rendering/ZZZ-衣服与眼睛验证-20260905.md` | [Tools/Rendering/Archive/Records/ZZZ-衣服与眼睛验证-20260905.md](../../../Tools/Rendering/Archive/Records/ZZZ-衣服与眼睛验证-20260905.md) |
| 历史归档 | `Tools/Rendering/ZZZ-衣服发黑-Overlay漏绑诊断-20260905.md` | [Tools/Rendering/Archive/Records/ZZZ-衣服发黑-Overlay漏绑诊断-20260905.md](../../../Tools/Rendering/Archive/Records/ZZZ-衣服发黑-Overlay漏绑诊断-20260905.md) |
| 历史归档 | `Tools/Rendering/ZZZ-可琳头部受光轴对账-20260906.md` | [Tools/Rendering/Archive/Records/ZZZ-可琳头部受光轴对账-20260906.md](../../../Tools/Rendering/Archive/Records/ZZZ-可琳头部受光轴对账-20260906.md) |
| 历史归档 | `Tools/Rendering/ZZZ-原描边接入与验收-20260906.md` | [Tools/Rendering/Archive/Records/ZZZ-原描边接入与验收-20260906.md](../../../Tools/Rendering/Archive/Records/ZZZ-原描边接入与验收-20260906.md) |
| 历史归档 | `Tools/Rendering/ZZZ-Scene深度接口与剩余渲染差异-20260906.md` | [Tools/Rendering/Archive/Records/ZZZ-Scene深度接口与剩余渲染差异-20260906.md](../../../Tools/Rendering/Archive/Records/ZZZ-Scene深度接口与剩余渲染差异-20260906.md) |
| 历史归档 | `Tools/Rendering/ZZZ-课件对账与剩余差异-20260906.md` | [Tools/Rendering/Archive/Records/ZZZ-课件对账与剩余差异-20260906.md](../../../Tools/Rendering/Archive/Records/ZZZ-课件对账与剩余差异-20260906.md) |
| 历史归档 | `Tools/Rendering/ZZZ-Shader恢复进度.md` | [Tools/Rendering/Archive/Records/ZZZ-Shader恢复进度.md](../../../Tools/Rendering/Archive/Records/ZZZ-Shader恢复进度.md) |
| 合并 | `docs/zzz-corin-rush-ability-fsm-map.md` | [docs/replication/corin-rush-map.md](../../replication/corin-rush-map.md) |
| 合并 | `docs/zzz-corin-rush-timeline-map.md` | [docs/replication/corin-rush-map.md](../../replication/corin-rush-map.md) |
| 删除冗余入口 | `docs/architecture-refactoring-plan.md` | [openspec/changes/archive/2026-09-30-refactor-character-runtime-and-authoring-boundaries/archive-summary.md](../../../openspec/changes/archive/2026-09-30-refactor-character-runtime-and-authoring-boundaries/archive-summary.md) |

## 业务与验证边界

只修改文档和链接，没有修改 C#、Shader、工具执行代码、Unity 资产、运行配置或原始诊断数据。工作区原有文档修改在原内容上归位和修正链接，保留其未提交状态；其它在途修改不进入本轮提交。

本次不执行 Unity 编译、Player 构建、Play、replay、性能采集或端到端测试。静态文档校验不建立新的玩法正确性、画面等价、性能收益或整角色 0 GC 结论。

## 校验与恢复

`openspec validate --specs --strict`：106 项通过，0 项失败。归档后的 11 个活跃目录与 maintenance-audit 索引逐项一致；559 个本地链接完成检查，迁移回归、新入口失效和遗留断链均为 0。原有归档中的目录层级错误已修正；文件系统已不存在的旧 Pose/Skill 记录保留原引用文本，并明确其历史路径已退役，不能再作为活跃依赖。

`git diff --cached --check` 与 UTF-8/换行检查只覆盖本轮文档整理；原有无关修改不进入提交。已验证的完整路径迁移与链接前后比较规则同步到本机 simplify-codebase 的 decision-records.md，不记录无法确认原因的单次文件写入问题。

置信度：文件归属、链接和任务完成检查充分；运行与视觉结果仅引用原记录声明的范围。没有退休运行能力、外部 API、序列化数据或资产引用。维护负担减少的是散落入口、重复对照和“历史状态即当前状态”的同步责任。

完整整理前内容和 Git 状态已保存到本机忽略目录 `.codex-tmp/document-cleanup-20260930/original/`，操作映射保存在同目录 operations.json。恢复时只按本轮操作映射反向移动并还原所需文件；不能整体 checkout 工作区或回退其它在途修改。原有在途文档和未跟踪的本机证据继续保留其未提交状态。

暂存按文档原有 Git 状态分离：修改过的原文只暂存本轮路径/链接迁移，已有内容差异继续留在工作区。当前仓库曾有零字节 index.lock 残留约 79 分钟，排他打开核对后移到本机备份，未修改原 index 内容；本轮暂存通过一次 Git index-info 事务完成。

## 补充整理：诊断数据分类

上轮只分类和归档了阶段报告，原始证据与仍在维护的诊断文档仍混放。本次按用户反馈补齐整个 `docs/diagnostics/` 的分类，覆盖原有 574 个文件、24 个根目录条目；根目录只留下[诊断索引](../../diagnostics/README.md)。新增各分类入口，跨业务问题链接到唯一证据原件。

| 分类 | 原有文件数 | 内容 |
| --- | ---: | --- |
| [脚部](../../diagnostics/foot-placement/README.md) | 7 | 下坡、E 行走接触、抖动、查询图与台阶解释器 |
| [相机](../../diagnostics/camera/README.md) | 493 | 三份实施/审计报告、两个日期证据包与绑定/字段初查 |
| [战斗](../../diagnostics/combat/README.md) | 4 | 普攻/E、Rush、输入覆盖与起手数值边界 |
| [动画](../../diagnostics/animation/README.md) | 2 | TurnBack JSON 与对应图；混合旋转引用脚部原件 |
| [性能](../../diagnostics/performance/README.md) | 2 | 当前采集记录与旧 Player 探针覆盖指标 |
| [特效](../../diagnostics/effects/README.md) | 66 | Transfer 报告与 65 个解析/扫描/失败证据文件 |

### 移动映射

下表原条目均位于旧 `docs/diagnostics/` 根目录；目录连同全部子文件一起移动，不复制、不删除证据。日期、录制身份和内部文件名保留。

| 原条目 | 新分类 |
| --- | --- |
| `camera-basis-runtime-20260929/` | [相机基础证据包](../../diagnostics/camera/camera-basis-runtime-20260929/) |
| `camera-shake-runtime-20260928/` | [相机震动证据包](../../diagnostics/camera/camera-shake-runtime-20260928/) |
| `corin-camera-binding-20260927.json` | [相机绑定](../../diagnostics/camera/corin-camera-binding-20260927.json) |
| `corin-camera-source-audit-20260927.json` | [相机字段初查](../../diagnostics/camera/corin-camera-source-audit-20260927.json) |
| `corin-camera-clock-and-vertical-20260929.md` | [相机时钟](../../diagnostics/camera/corin-camera-clock-and-vertical-20260929.md) |
| `corin-camera-completion-audit-20260929.md` | [相机审计](../../diagnostics/camera/corin-camera-completion-audit-20260929.md) |
| `corin-camera-implementation-20260929.md` | [相机实施](../../diagnostics/camera/corin-camera-implementation-20260929.md) |
| `effect-particle-native-transfer-20260930/` | [特效证据包](../../diagnostics/effects/effect-particle-native-transfer-20260930/) |
| `effect-particle-native-transfer-20260930.md` | [特效报告](../../diagnostics/effects/effect-particle-native-transfer-20260930.md) |
| `corin-downhill-response-20260927.json` | [脚部下坡](../../diagnostics/foot-placement/corin-downhill-response-20260927.json) |
| `corin-e-walk-contact-fix-20260927.json` | [脚部 E 接触](../../diagnostics/foot-placement/corin-e-walk-contact-fix-20260927.json) |
| `corin-e-walk-manual-sampling-20260927.json` | [脚部 E 采样](../../diagnostics/foot-placement/corin-e-walk-manual-sampling-20260927.json) |
| `corin-foot-query-edge-20260927.svg` | [脚部查询图](../../diagnostics/foot-placement/corin-foot-query-edge-20260927.svg) |
| `corin-jitter-existing-data-20260927.json` | [脚部旧抖动](../../diagnostics/foot-placement/corin-jitter-existing-data-20260927.json) |
| `corin-jitter-fix-20260927.json` | [脚部抖动修正](../../diagnostics/foot-placement/corin-jitter-fix-20260927.json) |
| `ik-stair-continuity-explainer-20260930.html` | [脚部解释器](../../diagnostics/foot-placement/ik-stair-continuity-explainer-20260930.html) |
| `corin-action-runtime-20260927-180403.json` | [战斗运行](../../diagnostics/combat/corin-action-runtime-20260927-180403.json) |
| `corin-e-start-boundary-20260927.json` | [战斗起手边界](../../diagnostics/combat/corin-e-start-boundary-20260927.json) |
| `corin-existing-input-coverage-20260927.json` | [战斗输入覆盖](../../diagnostics/combat/corin-existing-input-coverage-20260927.json) |
| `corin-rush-runtime-20260927-171133.json` | [Rush 运行](../../diagnostics/combat/corin-rush-runtime-20260927-171133.json) |
| `corin-turnback-runtime-20260927.json` | [动画 TurnBack](../../diagnostics/animation/corin-turnback-runtime-20260927.json) |
| `corin-turnback-runtime-20260927.png` | [动画 TurnBack 图](../../diagnostics/animation/corin-turnback-runtime-20260927.png) |
| `performance-coverage-20260929.md` | [性能覆盖](../../diagnostics/performance/performance-coverage-20260929.md) |
| `performance-run-20260929.md` | [性能采集](../../diagnostics/performance/performance-run-20260929.md) |

### 引用与实际消费者

文档总入口、三份业务进展、抄录对照、相机参考、历史记录和性能工具入口同步修正链接。HTML 解释器只调整跳转地址，嵌入采样及全部脚本保持一致。

相机作者链还有实际文件输入：`CorinCameraResourcesAuthoring.PublishInput` 从玩家输入快照读取轴增益、设备归一化常量和处理器比例，再发布到 `CharacterCameraProfile.Input`。读取路径已从旧根目录改为 `docs/diagnostics/camera/camera-basis-runtime-20260929/pointer-input-snapshot.json`，没有增加备用目录或双读取入口。其它已有 C# 内容改动继续留在工作区。

原始 JSON 的历史来源字段保留。例如特效依赖追踪的 `scan` 仍记录生成时的旧绝对路径，分类入口直接链接它现在对应的正式扫描文件。原始 evidence manifest 中的包内相对路径与哈希没有重写，历史来源信息不冒充当前文件入口。

### Spec 同步范围

重新核对本轮归档的 7 个 change 和各自 `archive-summary.md`：新增现行 `btsmtl-flowcanvas-authoring`、`btsmtl-timeline-clock-domain`、`character-control-motion-entry`、`character-pose-bone-transform`、`corin-locomotion-lean`，合并 `character-animation-event-graph` 与 `character-animation-layer-runtime`。其余有效条款已由现行相应规格承接；被后续秒制 Timeline、原生 Pose、Slate 和两域 TreeDecision 取代的旧设计保留为历史，不覆盖新版合同。历史 delta 的正文没有改写为今天的状态。本次没有对全部旧归档做重新设计，也没有把在途 Camera、Foot、战斗或性能任务的实施记录直接写成已完成规格。

### 检查与提交边界

原有 574 个文件全部存在于对应分类；567 个原始数据/图像/反汇编文件逐项 SHA-256 一致，另 7 份 Markdown/HTML 只修正路径。516 份 JSON 可解析，相机作者实际路径能解析到原输入快照。703 个本地链接完成静态检查，迁移前有效链接和新入口无失效。再次运行 `openspec validate --specs --strict`，106 项通过、0 项失败；本次分类没有修改 spec 合同正文。`git diff --cached --check` 通过。

未执行 Unity 编译、Play、Player 构建、replay 或性能采集；这里验证的是文件整理与路径可达性。已有修改只暂存本次路径变化，原有未跟踪报告及证据包保持未跟踪，未把这些大体积证据加入 Git。本次完整移动映射、原始哈希、文本备份、暂存回执和检查结果位于本机 `.codex-tmp/diagnostics-classification-20260930/`，不进入仓库提交。

## 分支重建后的分类恢复

2026-09-30 当前分支重建时带回了前两次文档整理提交，遗漏了 `286955518` 的诊断分类提交；已分类的未跟踪文件仍在新目录，已跟踪文件回到旧位置，造成新旧混放。本次以当前文件和索引重新建立快照，逐文件归位，补回分类索引和实际读取路径；没有回退其它任务的内容或美术本地化规则。

新出现的 `ik-tests/` 是 `CharacterFootCapturedContactTests` 固定读取输入、生成结果的工具目录，作为[脚部局部复算证据](../../diagnostics/ik-tests/README.md)单独收录并从脚部分类引用，没有修改其测试代码。此前查询边缘 SVG 从已保存提交恢复为本机诊断文件，遵循当前美术文件的 Git 忽略规则。

当前检查和恢复信息保存在 `.codex-tmp/diagnostics-reconcile-20260930/`；保留当前工作内容、原始证据哈希和其它任务的暂存项。本次重新检查根目录，普通文件只有 README，各业务报告与取证包均从对应分类进入。
