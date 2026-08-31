# Change: 建立开发版 GM 与共用采样诊断入口

## Why

双端可以运行，但观察、记录和定位问题仍依赖编辑器窗口与零散日志。2026-08-31 的回滚双端在接回未来身体位移源、将最大预测领先量改为 8 Tick 后，仍在 Actor B 的逻辑帧约 5723 同时出现 `Action playback command has no matching Select`。当前记录能说明失败位置，不能完整展示该动作的 Select、Sample、撤销、生命周期登记和骨骼输出经过了哪些边界。

现有 `RuntimeDiagnosticsTargetRegistry`、`AnimationPresentationRuntimeTargetRegistry`、结构化 Trace、Pose Watch 和 Foot 已提交快照可以复用。缺口集中在 Player 入口、显式目标身份、持久采样以及跨记录查询：`CharacterFootLandingPredictionSampler` 仍属于 Editor，且只扫描固定名称的单机角色，没有接入 Rollback Host。

用户已明确下一步先做 GM，再补采样和诊断。本提案只设计这条工具链，不继续修复 Action，也不以工具建设替代 Action 的后续修复。

## What Changes

- 按三个可独立交付的阶段实施：GM 入口、共用采样、离线诊断。阶段一不放尚未实现的采样按钮或空命令。
- GM 是开发版客户端的本地操作入口，使用显式注册的命令目录、参数合同和结果合同；菜单与命令输入调用同一执行器。
- 第一版按观测与诊断控制设计：列出目标、查看运行身份、选择诊断频道；后续阶段接入采样开始、停止、状态与结果定位。改血量、传送、强制切动作、单端暂停联机模拟不在本次范围。
- 复用现有目标注册与 provider，增加明确的进程、业务 Session、Actor、运行实例和版本元数据。不得从角色名字、场景遍历顺序或 Host 类型名单挑选目标。
- 将现有 Foot 采样中的纯采集、序列化和文件封口职责迁入运行时采样模块，保留一份字段定义与 Writer；Editor、GM 只作适配入口。
- 采集已提交的 Gameplay Trace、Action 生命周期与命令边界、Body、Pose、Foot、Goal、FBBIK 和最终骨骼证据；失败记录单独发布，不伪装为已提交表现帧。
- 沿用现有 Foot Analyzer、七维评分、紧凑明细存储及 `summary/events/detail/frame` 查询方式；GM 不运行第二套分析数学。
- 双端各自记录本进程看到的自己角色和另一角色，通过启动批次、业务 Session、Actor、逻辑帧和内容身份关联；IK、骨骼、诊断命令不进入玩法同步协议。

## Impact

- 新增能力：`development-gm-console`、`runtime-diagnostic-capture`、`runtime-diagnostic-query`。
- 修改合同：`btsmtl-runtime-diagnostics` 的跨入口控制归属、`character-foot-diagnostic-storage` 的分析提交边界；扩展 `character-input-pipeline` 的开发工具输入焦点边界。
- 直接涉及现有运行时目标、三类角色注册、Input Adapter 边界、Gameplay Lab 开发装配、Network Test 启动身份、Editor Foot 采样与诊断入口。
- 首批完整装配范围是 Gameplay Lab 的 Local Float32、Local Fixed 与 DeterministicRollback。Unity Authority、DotRecast Authority 和商业启动的 GM 装配不在本提案内，不宣称已支持。
- `compact-foot-diagnostic-publication` 与 `consolidate-foot-diagnostic-scoring` 已有工作区实现，仍是 active change。本提案以它们的唯一 Writer/Publisher、紧凑存储和当前评分规则为迁移边界，不改其数学，不将其提前归档。
- 不新增测试代码，不运行 Unity batchmode。实施任务不包含人工验证清单。

## 与现行规格的对比

| 对比项 | 当前合同或实现 | 本提案处理 |
| --- | --- | --- |
| 目标选择 | 诊断 spec 要求显式身份；Foot 采样实现扫描 `gameplay-lab-player` 并只识别两种 Host | 实现落后于合同；删除该扫描选择路径，使用已注册目标 |
| 控制入口 | `RuntimeDebugSession` 的控制归属只描述 Editor | 以 delta 明确共用运行时控制服务；保留 Editor 页面自己的 Follow/Pin |
| Action 队列名称 | Trace channel 场景仍称 `ActionAnimationPlaybackCommandQueue`，代码已使用 `ActionPlaybackCommandInbox` | 在对应 delta 同步名称，不改变动作执行语义 |
| 只读诊断 | 不得反写 Gameplay、Presentation 或作者资产 | 保留；GM 的诊断控制只改变订阅与采样状态 |
| Foot 数据身份 | 同一 Frame、Completion、Rig、Bank 的已提交结果 | 保留；跨进程不能用 RenderFrame 相等证明同一时刻 |
| Foot 文件发布 | active storage change 要求停采后唯一 Finalizer 自动分析并发布 | 原条款没有区分 Player 封存与离线提交，不能直接套用；本提案提供对应 MODIFIED delta，保留 Editor 停采自动提交，Player 封存后由离线接收端执行同一 Finalizer |
| Foot 评分 | active scoring change 的七维规则、覆盖和去重 | 保持原规则；缺数据不能按无问题或满分处理 |
| 暂停和回放 | 现有 Session Debug Port 仍拒绝其录制/回放命令；普通 Tick 控制不等于联机安全控制 | 不把这些接口包装成“已经支持”的 GM 功能 |

不存在需要删除的现行 GM spec。现有诊断、输入、Foot、动画和网络模型 spec 均保留；只合并本提案列出的增量，不修改用户正在编辑的 `openspec/project.md` 和 Foot proposal。`character-foot-diagnostic-storage` 尚在 active change，本提案的修改必须在其合同安装后合并，或在同一批准的合并过程中显式对账，不能作为不存在基线的独立 archive。

## 待确认范围

当前草案按“每个客户端操作本进程、第一版不修改玩法状态”设计。若用户需要第一版包含玩法命令或一个窗口统一控制两个进程，应先调整本提案的命令范围与进程控制合同；不在实现中临时增加权限、远程调用或第二入口。
