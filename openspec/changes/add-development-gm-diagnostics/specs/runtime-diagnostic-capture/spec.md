## ADDED Requirements

### Requirement: Editor 与 Player 必须共用一个持久采样模块

持久采样 MUST 使用同一控制状态机、记录合同、Writer 和封口流程，且运行时模块不得依赖 UnityEditor。GM、Editor 和其它工具入口 MUST 只作适配。迁移 MUST 删除旧固定 Actor 名字扫描、旧 Host 类型选择分支、重复字段和 Writer，不创建新旧两条采样路径。

#### Scenario: 从 GM 采集 Rollback 远端角色

- **WHEN** 用户选择本进程已注册的另一 Actor 并开始采样
- **THEN** MUST 使用与单机 Editor 相同的采样核心与字段数学
- **AND** MUST 不要求该角色具有 `gameplay-lab-player` 名字或某个单机 Host 类型

### Requirement: 采样必须锁定目标并完整区分时序身份

采样 MUST 在开始时锁定目标集合、频道和内容身份，记录进程/启动批次、采样运行、业务 Session、Actor、runtime instance、逻辑 tick/sequence、表现 RenderFrame、Body sample tick/alpha、Completion 与结果类型。跨进程 MUST 不把 HostInstanceId、RenderFrame 或显示名当共同时间与角色身份。

#### Scenario: 同一 Actor 在两个客户端的帧率不同

- **WHEN** 导入两个客户端的记录
- **THEN** MUST 能按业务 Session、Actor、模型/内容身份和逻辑边界关联
- **AND** MUST 保留各自表现时钟，不补造帧号一一对应关系

### Requirement: 采样必须消费正式结果且不影响执行

采样 MUST 消费正式 Trace 和已提交 provider 快照；Foot、Goal、FBBIK、最终写入 MUST 保持同一 Frame/Completion/Rig/Bank 身份。采样 MUST 不重新求值动画、查询物理、读取下一帧实时 Transform 补旧帧，也不得写入同步状态或网络模型。

#### Scenario: 记录一个 Foot 完成帧

- **WHEN** 当前角色的表现事务成功 Seal
- **THEN** 记录 MUST 来自该完成身份下的正式结果
- **AND** 打开或关闭采样 MUST 不改变 IK 与 Gameplay 的计算结果

### Requirement: Action 与失败记录必须保留原始边界证据

Action 记录 MUST 关联 Select/Sample/Complete/Release 的 PlaybackId、Producer/Generation、ActionInstanceId、EventId 和 tick/sequence，并区分发布、替换/撤销、Inbox 消费与生命周期结果。失败 MUST 保留首个失败上下文与重复次数，标明 Attempted 阶段，不得生成假的 Committed Pose 或补造丢失命令。

#### Scenario: Sample 没有匹配的 Select

- **WHEN** 正式生命周期边界拒绝一个 Sample
- **THEN** MUST 记录该命令身份、失败阶段与已有证据覆盖
- **AND** MUST 不通过采样工具创建 Select、清空 Inbox 或吞掉异常

### Requirement: 采样写盘必须有界且明确完整性

采样 MUST 使用 Idle、Capturing、Finalizing、Sealed、Failed 生命周期，按 owner 管理订阅，主线程只进行有界冻结与入队。后台溢出、写盘失败、目标结束或进程退出 MUST 记录明确覆盖和结束原因。持久写盘 MUST 不阻塞 Gameplay，也不得把丢失范围隐藏成完整包。

#### Scenario: 后台写入速度不足

- **WHEN** 写盘队列达到正式容量
- **THEN** MUST 按明确失败/封口政策停止接收并保留已有完整段
- **AND** 清单 MUST 标记不完整及丢失边界，运行时角色不被阻塞

### Requirement: 新采样必须发布唯一封存清单并保持产品目录只读

新采样 MUST 写入开发配置显式指定的可写根，发布目标、频道、文件/hash/schema、边界、完整性和错误的唯一清单。不得写入已校验 ProductRoot、Assets，或通过写盘失败 fallback 更换目录。已有 Foot 原始字段合同 MUST 保持；历史封存证据不得被迁移覆盖。

#### Scenario: Player 停止采样

- **WHEN** 用户请求停止且 Writer 已完成封口
- **THEN** MUST 返回精确封存包与状态
- **AND** MUST 不在游戏帧循环运行完整 Analyzer，Network Product manifest 的文件集合不变
