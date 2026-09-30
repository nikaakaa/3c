# 诊断与原始证据索引

按业务分类的证据入口。实施和审计报告记录对应日期的现场；JSON、反汇编、截图和失败记录用于复核，不因移入分类目录而变成当前版本验收结论。当前业务合同仍由 [project](../../openspec/project.md) 与[现行规格](../../openspec/specs/)拥有。

| 分类 | 查找内容 | 整理时原有文件数 |
| --- | --- | ---: |
| [脚部](foot-placement/README.md) | 地形查询、下坡、E 行走接触、抖动和台阶解释器；[局部复算工具证据](ik-tests/README.md)有独立正式读写目录 | 7 |
| [相机](camera/README.md) | 实施审计、输入/轨道/Delay/Stretch 与震动原生证据 | 493 |
| [战斗](combat/README.md) | 普攻/Rush/E 边界、输入覆盖与运行证据 | 4 |
| [动画](animation/README.md) | TurnBack 动作与相位采样 | 2 |
| [性能](performance/README.md) | 探针覆盖、采集版本、热点与失败记录 | 2 |
| [特效](effects/README.md) | ParticleSystem/Renderer Transfer、依赖扫描与解析失败 | 66 |

2026-09-30 首轮分类覆盖 574 个文件。当前目录按上述六个业务主题与脚部局部复算工具组织，具体证据数量以各目录为准；根目录只保留本入口文件，各类目录没有散落报告。日期与样本身份保留。已经结束的阶段说明位于 [docs/archive/records](../archive/records/)，通用研究位于 [docs/reference](../reference/README.md)。

原始证据包内部相对文件名和内容哈希保持不变。依赖追踪 JSON 的 `scan` 字段保存生成当时的绝对路径，作为历史来源保留；查找现存文件使用本索引或分类入口。实际文件消费者与文档跳转已改为分类后的路径，没有保留旧目录或备用读取入口。完整移动映射与检查见[整理报告](../archive/maintenance/document-cleanup-20260930.md)。

新报告放到所属业务目录；原始证据保存在同业务的带日期证据包中。跨业务问题从其它分类链接到唯一原件，不复制证据。后续维护核对来源版本、样本身份和验证范围；历史记录中的未完成项由[活跃变更](../../openspec/maintenance-audit.md)确认当前归属。
