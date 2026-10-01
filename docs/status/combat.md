# 战斗进展与证据入口

整理日期：2026-10-01。普通攻击、Rush、Branch、接招、命中与动画表现的完整运行闭环尚不能宣称完成。

## 当前作者与输入链

Control 消费正式输入请求并提交 Ability；准入来自对应 AdmissionProfile，FSM/StateBody 播放正式 Timeline。TreeClip 内 Gameplay 节点把攻击碰撞与属性交给所属 Gameplay 执行域，正式命中结果再交给效果与表现消费者。Timeline 只处理内容身份、时间边界和事务，不恢复旧 ActionCue 分发链。

源数据与静态接线分别查[控制器对照](../replication/corin-controller-map.md)、[普通攻击事件对照](../replication/corin-normal-attack-map.md)与[Rush 作者对照](../replication/corin-rush-map.md)。后续改动由 [Corin 作者/Replay 闭环](../../openspec/changes/integrate-corin-dump-authoring-replay/proposal.md)和 [Rush 正式链](../../openspec/changes/add-corin-rush-attack-formal-chain/proposal.md)拥有；具体未完成项直接读取对应清单。

## 已有证据及其限制

| 证据 | 已建立的事实 | 仍不能据此宣称 |
| --- | --- | --- |
| [第五段收尾发布](../archive/records/combat/corin-attack5-end-repair-20260927.md) | 9 月 28 日完成收尾动画、缓存、ACL 和 Animation Domain 正式发布 | 第五段完整视觉或连招手感已验收 |
| [Rush 对照中的回放](../replication/corin-rush-map.md) | 指定录制完成，部分强化状态链与普通连段已有观察 | Rush 窗口接招、事件爆发分支和所有普通分支均已覆盖 |
| [战斗阶段证据](../archive/records/combat/combat-closure-evidence-20260920.md) | 保存各次作者生成、发布和运行失败/成功的精确身份 | 其中的旧阻塞和旧源码版本仍是当前状态 |
| [相机完整审计](../diagnostics/camera/corin-camera-completion-audit-20260929.md) | 命中 A 类资源进入 catalog 与资源引用 | 运行命中结果已经送达相机求值器 |

实际命中查询与连续扫掠的方案来源见[命中检测研究](../reference/combat/hit-detection-options-20260929.md)。2026-10-01 已实现独立[扫掠判定模块](../路径/命中判定扫掠查询.md)：业务提供逻辑样本，Unity / Fixed 原生查询返回稳定相交候选；Editor 局部查询与 0 GC 证据见[查询核对](../测试/命中判定查询核对20261001.md)。攻击触发、武器逻辑采样、接受规则、伤害和表现接线继续由业务方完成，不能据此宣称完整攻击链已闭合。源状态、片段边界和事件完整性按当前作者资产核对，旧普攻复核已进入历史区。

同日后续已补 Fixed 连续旋转、凸包和凸部件 Mesh，portable / server 编译与内存局部查询通过；目标 Editor MCP 未连接，本批尚无 Editor 编译或技能资产发布证据。用户明确下一步尽快验证判定后的全部技能逻辑：受击 / 硬直 / 打断、格挡 / 招架 / 反击、震动 / 停帧共同消费唯一正式命中结果，不拆成互斥选项。当前技能侧仍没有生产调用 `SweepAll`，该接线尚未完成。

旧录制、故障基线、分工和回放记录见[战斗阶段记录](../archive/records/combat/)。报告附带的运行 JSON 与输入覆盖归入[战斗诊断](../diagnostics/combat/README.md)，正式 Proof 包保留项目原目录；输入有请求、发布成功、帧对账完成和行为正确分别记录。
