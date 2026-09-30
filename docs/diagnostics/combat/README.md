# 战斗诊断

当前作者链、未完成范围与后续 owner 见[战斗进展](../../status/combat.md)。本目录保存运行观察和输入覆盖，动作出现不等于窗口、位移或视觉效果已全部验收。

| 证据 | 内容与边界 |
| --- | --- |
| [普攻与 E 运行记录](corin-action-runtime-20260927-180403.json) | 2716 帧回放中的动作顺序；观察器中途接入，不据创建事件推导转移时刻 |
| [Rush 运行记录](corin-rush-runtime-20260927-171133.json) | 强化退出修正后的同输入回放 |
| [已有输入覆盖](corin-existing-input-coverage-20260927.json) | 离线解码 AttackHeld/BranchHeld；与本目录 action-runtime 对应 |
| [E 起手边界](corin-e-start-boundary-20260927.json) | Fixed 时间量化与完成阈值；不代表持续 E 的画面验收 |

正式来源与事件窗口见[Rush 对照](../../replication/corin-rush-map.md)，阶段交付、失败和旧回放见[战斗历史](../../archive/records/combat/)。E 行走的脚部接触数据归[脚部分类](../foot-placement/README.md)。
