# local-latency-reconciliation Specification

## Purpose
定义本地高延迟输入模拟、预测、reconciliation 回滚编排和诊断输出，用于在接入真实网络前验证同步边界。
## Requirements
### Requirement: 伪造远端输入延迟队列
系统 MUST 提供纯本地的远端输入延迟模拟能力，使客户端能模拟"远端输入延迟 N tick 到达"的网络条件，不依赖真实网络或外部进程。延迟队列 MUST 按 tick FIFO 顺序投递，第一版不模拟乱序或丢包。

#### Scenario: 写入延迟队列
- **GIVEN** tick N 运行中记录了本地 `PredictionInputFrame`
- **WHEN** 该输入帧被放入延迟队列并指定延迟 D tick
- **THEN** 该输入帧 MUST 在 tick N+D 变为可取出状态
- **AND** tick < N+D 时查询该 tick MUST 返回"未到达"

#### Scenario: 按 tick 顺序取出
- **GIVEN** 延迟队列中有 tick N、N+1、N+2 的输入
- **WHEN** 查询 tick N+1 的已到达输入
- **THEN** tick N 和 N+1 MUST 可取出
- **AND** tick N+2 若延迟尚未到 MUST 返回未到达

#### Scenario: 零延迟直接到达
- **GIVEN** 延迟配置为 0
- **WHEN** tick N 的输入写入延迟队列
- **THEN** 同一 tick N 查询时 MUST 已可取出

#### Scenario: 不模拟乱序
- **GIVEN** 第一版延迟队列
- **WHEN** 输入写入发生
- **THEN** 队列 MUST 保证按 tick 递增顺序投递
- **AND** 后写入的更早 tick MUST NOT 覆盖已到达的更晚 tick

#### Scenario: 纯数据边界
- **WHEN** 延迟队列存储和取出输入帧
- **THEN** 系统 MUST NOT 持有 Unity Object、GameObject、MonoBehaviour 引用
- **AND** 输入帧 MUST 保持纯数据

### Requirement: 输入预测策略
系统 MUST 在远端输入缺失时提供输入预测能力，使客户端能不等待远端输入而继续推进模拟。第一版 MUST 提供"重复上一帧"默认策略，并预留策略扩展点。

#### Scenario: 上一帧存在时重复
- **GIVEN** tick N 的远端输入缺失
- **AND** tick N-1 的远端输入已知（真实或预测）
- **WHEN** 默认预测策略为 tick N 生成预测输入
- **THEN** 系统 MUST 返回与 tick N-1 内容相同的输入帧
- **AND** 预测帧的 Tick MUST 标记为 N

#### Scenario: 无上一帧时无法预测
- **GIVEN** tick 0 的远端输入缺失且无前一帧
- **WHEN** 查询预测输入
- **THEN** 系统 MUST 返回失败或无法预测的诊断

#### Scenario: 预测不修改原始输入历史
- **WHEN** 预测策略生成预测输入帧
- **THEN** 系统 MUST NOT 将预测帧写入 `PredictionInputHistory`
- **AND** 真实输入到达后 MUST 能以真实输入替代预测

#### Scenario: 策略可替换
- **WHEN** 用户实现 `IPredictionInputStrategy` 接口
- **THEN** 系统 MUST 允许注入自定义预测策略
- **AND** 默认策略 MUST 为"重复上一帧"

### Requirement: Reconciliation 核心编排
系统 MUST 提供 reconciliation 编排能力，通过比较本地预测快照与远端输入重放结果，发现 first incorrect tick、执行回滚和追帧重放。该编排 MUST 复用现有 `ILocalRollbackSynctestSimulation` 接口，不绕过现有 replay 边界。

#### Scenario: 预测正确不回滚
- **GIVEN** 远端输入按时到达且与预测输入完全一致
- **WHEN** reconciliation 检查从 confirmed tick 到当前 tick 的快照差异
- **THEN** 所有 tick 快照 MUST 一致
- **AND** first incorrect tick MUST 为 null
- **AND** 系统 MUST NOT 执行回滚

#### Scenario: 延迟到达触发预测
- **GIVEN** tick N 的远端输入未到达
- **AND** reconciliation 需推进 tick N
- **WHEN** 系统获取 tick N 的输入
- **THEN** 系统 MUST 使用预测输入替代
- **AND** MUST 记录该 tick 使用了预测

#### Scenario: 预测错误触发回滚
- **GIVEN** tick M 时用预测输入推进
- **AND** tick M 的真实远端输入后到达且与预测不一致
- **WHEN** reconciliation 用真实输入从 tick M 重放
- **THEN** 重放快照与本地预测快照 MUST 存在差异
- **AND** first incorrect tick MUST 被记录为 M
- **AND** 系统 MUST 从 tick M-1 的快照恢复并重放到当前 tick

#### Scenario: 回滚后收敛
- **GIVEN** first incorrect tick 为 M
- **AND** 从 tick M-1 恢复到当前 tick 的所有真实/预测输入可用
- **WHEN** reconciliation 完成调整（adjust）
- **THEN** 调整后的最终快照 MUST 与"从头用远端输入重放"的结果在容差内一致
- **AND** Console MUST 输出 reconciliation 结果（PASS/FAIL）

#### Scenario: 快照缺失时停止
- **GIVEN** first incorrect tick 为 M
- **AND** tick M-1 的快照不可用（已被裁剪）
- **WHEN** reconciliation 尝试恢复
- **THEN** 系统 MUST 停止并输出诊断"missing snapshot for rollback"
- **AND** MUST NOT 继续用不完整的数据执行回滚

#### Scenario: 不绕过 ILocalRollbackSynctestSimulation
- **WHEN** reconciliation 执行 restore、advance 或 capture snapshot
- **THEN** 系统 MUST 通过 `ILocalRollbackSynctestSimulation` 接口执行
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接调用 `BasicLocomotionPipeline`

### Requirement: Reconciliation 结果诊断
系统 MUST 输出 reconciliation 的完整诊断信息，使开发者能定位是输入预测错误、回滚边界不足还是 replay 本身不一致。

#### Scenario: 输出 first incorrect tick
- **WHEN** reconciliation 检测到预测错误
- **THEN** Console MUST 输出 first incorrect tick 值
- **AND** MUST 输出该 tick 的预测输入和真实输入的差异字段

#### Scenario: 输出回滚范围
- **WHEN** reconciliation 执行回滚
- **THEN** Console MUST 输出 restore tick、first incorrect tick 和 replay end tick
- **AND** MUST 输出追帧数量

#### Scenario: 输出最终比对差异
- **WHEN** reconciliation 完成调整后的快照比较
- **THEN** Console MUST 按字段列出差异（position、yaw、action state、animation facts 等）
- **AND** differences 格式 MUST 与现有 `CharacterSimulationSnapshotComparison` 一致

### Requirement: Play Mode 延迟调试入口
系统 MUST 提供 Play Mode MonoBehaviour 组件，允许在 Unity Editor 中配置延迟参数、选择预测策略，并按键触发延迟 reconciliation 测试。

#### Scenario: 配置延迟参数
- **GIVEN** `LocalLatencyReconciliationDebugRunner` 挂载在角色上
- **WHEN** Inspector 中设置 `LatencyTicks = 3`
- **THEN** 远端输入 MUST 延迟 3 tick 到达

#### Scenario: 按键触发 reconciliation
- **GIVEN** 延迟模拟器和 reconciliation runner 已配置
- **WHEN** 用户按下配置的触发键（默认 F7）
- **THEN** 系统 MUST 执行一次完整的 reconciliation 检查
- **AND** Console MUST 输出 PASS/FAIL 和诊断信息

#### Scenario: 安全探针语义
- **GIVEN** reconciliation 未启用"应用结果到场景"
- **WHEN** 触发键按下并完成 reconciliation
- **THEN** 角色 MUST 恢复到触发前的最新现场快照
- **AND** 角色状态 MUST NOT 因 reconciliation 而永久改变

#### Scenario: 可见 correction 模式
- **GIVEN** 启用了"应用结果到场景"
- **AND** 配置了 `PresentationTransformInterpolator`
- **WHEN** reconciliation 完成后 position 或 yaw 发生校正
- **THEN** 表现根 MUST 从触发前 visual pose 插值追到校正后逻辑根

### Requirement: Fantasy 前置边界
系统 MUST 将本变更限制为本地延迟和 reconciliation 模拟，不得在本变更中接入真实 Fantasy 网络、修改协议文件或实现完整的服务器通信层。本变更完成后，后续 MAY 直接规划 Fantasy transport 替换。

#### Scenario: 不修改协议
- **WHEN** 实施延迟模拟和 reconciliation
- **THEN** 系统 MUST NOT 修改 `3cDemo/Tools/NetworkProtocol/**/*.proto`
- **AND** MUST NOT 运行协议导出工具

#### Scenario: 不新增真实网络流程
- **WHEN** 实施延迟模拟和 reconciliation
- **THEN** 系统 MUST NOT 新增 C2G/G2C 发送接收
- **AND** MUST NOT 新增 Socket/UDP/TCP 连接
- **AND** MUST NOT 引用 Fantasy 程序集

#### Scenario: 为 Fantasy 接入做前置
- **WHEN** 本变更验收完成
- **THEN** reconciliation 管线 MUST 能在后续替换远端输入源为真实网络输入时不做架构变化
- **AND** IPredictionInputStrategy 和 ILocalRollbackSynctestSimulation 接口 MUST 保持不变
