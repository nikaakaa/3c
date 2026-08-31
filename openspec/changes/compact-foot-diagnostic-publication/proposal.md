# Change: 收敛 Foot 诊断为小报告和唯一明细存储

## Why

现有停止采样后台任务先生成展开的 facts.json，再全文读回 Publisher；报告还复制完整骨盆帧、Step 候选与事件阶段对象。1043 帧样本产生约 116 MiB facts，日常查看与再次发布承担不必要的磁盘和内存成本。

## What Changes

- 保留原始 samples.csv 和 ground-path-geometry.csv，只解析和校验一次，共享内存事实。
- 用版本化小清单、紧凑逐条明细及随机读取索引替代展开 facts.json；报告只携带统计、至多五个代表事件摘要和明细引用。
- 保留全部适用事件、派生观察、分布和原始帧定位，不把预览上限用于计数、评分或证据完整性。
- 迁移采样结果路径、Launcher、MCP 与 Replay 产物引用；不改变输入、时钟、采样窗口、Foot 行为或 Proof 比较数学。
- 保持七维权重、规则、Health/Evidence 和质量计数不变。旧包不覆盖，不提供旧格式 fallback。

## Impact

- 影响 Editor Foot Diagnostics 与其结果路径消费者，不修改 Runtime、资产或既有用户文档。
- 当前 scoring spec 要求唯一 Publisher、quality-score.json 与正式事实引用；本变更保留这些边界，仅升级存储身份。现行 Foot spec 的同帧 Committed 事实和单向采样边界不变。
- 本轮不运行 Unity、不新增测试，使用已有封存 CSV 的独立输出目录验证。
