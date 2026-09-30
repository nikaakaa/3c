# 拆分替代归档记录

2026-09-12：用户要求拆出剩余工作并归档旧总变更。归档性质为“已拆分替代”，不是85项功能全部完成或用户已端到端验收。归档前任务为59项已勾选、26项未完成；原勾选、源码提交、job/hash及失败证据保留，不把转交改成完成。

## 新的执行入口

- [原生FSM与Agent接入](../2026-09-30-integrate-native-fsm-skill-authoring/tasks.md)：唯一主要作者迁移任务。
- 通用Skill运行观察（历史路径已退役，原引用：`../../finish-skill-runtime-observation/tasks.md`；历史见 Git 提交 `f99572df9`）：独立观察收尾。
- [Corin网络与正式运行](../../integrate-corin-dump-authoring-replay/tasks.md)：复用已有闭环，不新建网络执行链。

## 26项未完成任务逐项去向

| 旧任务 | 接收change | 新任务 |
|---|---|---|
| 2.4.2 | integrate-native-fsm-skill-authoring | 3.2 |
| 4.5.5 | integrate-corin-dump-authoring-replay | 6.1 |
| 5.3 | integrate-native-fsm-skill-authoring | 3.2 |
| 6.4.2 | finish-skill-runtime-observation | 2.4 |
| 7.2.2 | finish-skill-runtime-observation | 2.1 |
| 7.3 | finish-skill-runtime-observation | 2.2 |
| 7.4 | finish-skill-runtime-observation | 2.3 |
| 7.5.2 | finish-skill-runtime-observation | 2.5 |
| 8.1 | integrate-corin-dump-authoring-replay | 6.2，复用4.2/4.3 |
| 8.4.2 | 按交付领域拆分 | FSM 5.3、观察3.2、Corin 5.1 |
| 9.1 | integrate-native-fsm-skill-authoring | 1.1 |
| 9.2 | integrate-native-fsm-skill-authoring | 1.2 |
| 9.3 | integrate-native-fsm-skill-authoring | 2.1 |
| 9.4 | integrate-native-fsm-skill-authoring | 2.2 |
| 9.5 | integrate-native-fsm-skill-authoring | 2.3 |
| 9.6 | integrate-native-fsm-skill-authoring | 3.1 |
| 9.7 | integrate-native-fsm-skill-authoring | 3.2 |
| 9.8 | integrate-native-fsm-skill-authoring | 3.3 |
| 9.9 | integrate-native-fsm-skill-authoring | 3.4，仅FSM映射接口 |
| 9.10 | integrate-native-fsm-skill-authoring | 4.1 |
| 9.11 | integrate-native-fsm-skill-authoring | 4.2 |
| 9.12 | integrate-native-fsm-skill-authoring | 4.3 |
| 9.13 | integrate-native-fsm-skill-authoring | 4.4 |
| 9.14 | integrate-native-fsm-skill-authoring | 4.5 |
| 9.15 | integrate-native-fsm-skill-authoring | 5.1 |
| 9.16 | integrate-native-fsm-skill-authoring | 5.2 |

## 六份delta的归属与同步评估

| 原delta | current状态 | 后续唯一归并owner |
|---|---|---|
| btsmtl-flowcanvas-authoring | 尚无同名current文件，包含原生FSM待实施要求 | integrate-native-fsm-skill-authoring，完整接收 |
| btsmtl-flowcanvas-runtime-observation | 尚无同名current文件，观察证据未完成 | finish-skill-runtime-observation，完整接收 |
| btsmtl-agent-authoring-document-sync | current仍为v7；新delta含v8改名及新增FSM/owner事务 | integrate-native-fsm-skill-authoring，完整接收，实施后逐场景合并 |
| btsmtl-graph-core | current存在，各Requirement标题存在，正文需保留后来增量 | integrate-native-fsm-skill-authoring，完整接收Skill适用范围基线 |
| graph-authoring-domain-framework | current存在，各Requirement标题存在，不能仅凭同名认定相同 | integrate-native-fsm-skill-authoring，完整接收，保留非Skill后来场景 |
| graph-authoring-editor-shell | current存在，provider-aware作者入口尚未全部安装 | integrate-native-fsm-skill-authoring，完整接收，不重做无关窗口 |

本次归档跳过delta同步，不写current specs，避免把尚未实现的FSM/v8安装为现行能力。六份delta在后继change中保留原Requirement/Scenario；归档副本只供历史追溯，禁止再次从本目录执行apply或安装旧delta。后继实施仅补未完成缺口，旧完成任务不重复打开。

## 已知边界

共同业务参数/规则仍归unify-skill-authoring-data-model，转移专项已完成的Edge成果仍保留；不修改另一窗口未提交规划。旧记录中的v7、TreeDirty、当前进度等只代表记录时点。StopThreshold仍有正式消费者，清理不擅自改变阈值归属或ControlModule跑步意图规则；并非为了归档删除未决业务。

拆分与归档仅修改文档和入口索引，没有执行Unity、Build、Play、代码/资产迁移或新增测试。
