## ADDED Requirements

### Requirement: 每次运行必须由唯一Host拥有请求状态和进程

Development Run Host MUST是每次运行的顶层状态、心跳、Windows Job Object 与角色进程所有者。产品 adapter 和采集 Worker MUST通过 Host 的统一进程能力启动/终止声明角色，不实现第二套 Run 状态、进程扫描或取消策略。Worker MAY拥有已登记的 Recorder/WPR 领域资源清理。

#### Scenario: 采集Worker异常退出

- **WHEN** Worker 在 Recording 期间异常退出
- **THEN** Host MUST把本 Run 标记为故障并清理身份匹配的本次角色与已登记采集资源
- **AND** MUST不停止另一个 Run 或未知 WPR 实例

### Requirement: 机器资源协调必须跨worktree和工具版本统一

机器资源租约 MUST使用显式 MachineControlRoot 与稳定机器级命名空间，独立于 ArtifactRoot、ProjectId、worktree 路径和工具版本。Slot 和实际 endpoint MUST同时受保护。租约格式不支持、所有者身份不明或资源仍占用时 MUST拒绝启动，不另建锁空间或动态换端口。

#### Scenario: 两个工作区选择同一Rollback槽位

- **WHEN** 一个工作区的 Run 已占用 rollback-a 而另一工作区再次申请
- **THEN** 第二次申请 MUST在启动业务进程前报告精确占用者
- **AND** MUST不因两个工作区目录不同而同时获得该槽位

### Requirement: 正式性能采样必须独占受管理负载窗口

构建、功能 Player、Replay 和分析导出 MUST登记机器负载租约。正式性能运行 MUST从 Player 启动到 Recorder/Collector 停止独占受管理重负载资源；已有任务结束后按公平队列启动，不抢占其他任务。外部进程不受本锁控制的事实 MUST进入环境记录；明确污染的结果 MUST不能自动用作基线。

#### Scenario: 构建尚未结束时请求采样

- **WHEN** 一个工作区仍在构建而另一个请求正式性能采样
- **THEN** 采样 MUST排队并显示所等待的任务
- **AND** MUST不边编译边发布无干扰性能结论

### Requirement: Unity构建必须全机串行并持续受内存约束

全机 MUST最多同时执行一个受管理 Unity 构建。正式机器配置 MUST显式声明启动可用内存、运行最低余量、持续判定窗口和终止超时。内存不足时 MUST等待；持续越界时 MUST取消本次拥有的构建、记录 MemoryBudgetExceeded 并保留日志及合法导入缓存，不发布未校验候选、不自动重试、不关闭其他 Editor。

#### Scenario: 首次导入持续消耗内存

- **WHEN** 后台 Unity 已通过启动检查但导入阶段持续低于配置的内存余量
- **THEN** Host MUST按有界取消流程终止本次构建并保留缓存与原因
- **AND** Center MUST显示构建未完成，而不是把程序集编译通过当作 Player 成功

### Requirement: 停止与恢复必须根据精确所有权判断

Stop MUST只作用于目标 Run 的进程启动身份、Job 和已登记资源。Editor 域重载不改变 Run；机器重启、心跳过期或跨权限查询失败 MUST触发明确所有权核对，不能把查询不可见当成已退出。资源状态未知时 MUST阻止复用。

#### Scenario: 停止两个并行运行之一

- **WHEN** 作者停止 Run A，Run B 使用其他合法槽位仍在运行
- **THEN** Host MUST只释放 A 的角色与租约
- **AND** B 的进程、配置和状态 MUST不被修改

### Requirement: 运行记录必须固定所有输入并保存失败证据

RunRequest MUST固定候选、Host、adapter、嵌入/外部工具、场景、资源、操作、Profile、前序 Gate 与源 manifest 哈希。结果 MUST明确区分排队、执行、取消、故障与完成，并保留已产生的日志/原始证据。修改标签或新发布版本 MUST不改变已有请求。

#### Scenario: 排队请求被取消

- **WHEN** 作者取消尚未获得机器资源的请求
- **THEN** Host MUST记录取消且不启动任何角色
- **AND** MUST不把尚未执行的 Smoke/Replay 标成通过
