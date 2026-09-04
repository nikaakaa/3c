## ADDED Requirements

### Requirement: Rollback GM 控制台必须形成服务端执行闭环

文本控制台 MUST在额外 GM 进程的可见终端窗口运行，通过统一 GM HTTP API 提交命令，由服务端校验、分发、执行并返回结构化结果。前端 MUST只负责输入、连接和结果展示，不得自行授权、直接执行处理器或本地 fallback。本轮 MUST只作为 Rollback Development 产品的独立工具，Unity Player MUST不安装游戏内控制台、GM 专用输入焦点或工具凭据。

#### Scenario: 查询会话

- **WHEN** 作者输入 `session.info`
- **THEN** 请求 MUST 到达配置指定的服务端处理器并返回带服务/会话运行身份的结果
- **AND** 控制台 MUST 根据真实响应显示成功或失败，不能使用客户端缓存冒充本次服务端执行结果

### Requirement: 命令必须独立实现并显式注册

服务端 MUST 通过正式命令描述和独立处理器注册命令。描述 MUST 声明稳定 Id/版本、名称、参数、权限及结果合同。控制台与分发器 MUST 不包含具体命令业务分支，不扫描方法执行、不求值代码、不安装占位处理器。

#### Scenario: 增加新的查询命令

- **WHEN** 后续模块提供完整命令描述和处理器
- **THEN** MUST 通过显式装配注册进入同一命令目录
- **AND** MUST 不修改控制台或分发器以识别该命令的业务

### Requirement: 首批命令必须只返回已有服务端事实

本轮 MUST 安装 `help [command]`、`session.info`、`actor.list`、`runtime.status`。结果 MUST 来自服务端命令目录、正式会话配置和 Relay 只读运行快照，区分预期 roster 与实际锁定/输入状态。不得伪造客户端 FPS、骨骼、IK、角色属性或完整 Action 状态，也不得改变预测上限、Gameplay 或 Presentation。

#### Scenario: 角色尚未加入

- **WHEN** manifest 声明角色但运行时尚未形成完整 roster
- **THEN** `actor.list` MUST 标明预期名单与实际运行状态的区别
- **AND** MUST 不把配置中存在 ActorId 当成已连接证明

### Requirement: 服务端必须校验权限参数和目标身份

服务端 MUST 按正式开发访问配置和命令描述校验请求权限、参数、版本以及服务/会话运行身份，不信任客户端校验或自报权限。未授权、未知命令、参数错误、目标已结束和执行失败 MUST 明确区分。操作记录不得包含访问凭据。

#### Scenario: 请求指向上一次服务运行

- **WHEN** 控制台提交的服务运行身份与当前实例不匹配
- **THEN** MUST 拒绝请求并报告目标过期
- **AND** MUST 不把命令自动送给同名新实例或另一会话

### Requirement: 请求结果必须正确关联且资源有界

请求和响应 MUST 使用一致请求 Id，分别表示请求发送、接受及执行结果。消息、在途请求、历史和输出 MUST 有明确容量；超时或断线不得报告成功。Unity 主线程和 Relay 运行循环不得被网络等待或 UI 绘制阻塞，查询不得在异步网络线程无保护地遍历 Relay 可变集合。

#### Scenario: 服务端响应超时

- **WHEN** 请求在规定边界内没有取得有效响应
- **THEN** 控制台 MUST 显示该请求超时或结果未知
- **AND** MUST 不重复提交为成功、不转成本地执行，也不冻结角色运行

### Requirement: GM 查询必须保持 Relay 模型和产品边界

查询 MUST 通过独立 GM 服务和 Relay 窄只读查询桥执行，不进入 canonical input、rollback history、replay/hash 或角色 Pipeline。GM 服务不得让 Relay 加载 Program、KCC、Unity、Fantasy、Presentation 或 GM 命令处理器。产品 MUST声明独立 GM executable、工具 manifest 及四进程 topology；新增的产品启动辅助脚本 MUST随 GM artifact 发布并进入 exact closure，不得运行未校验的替代脚本。

#### Scenario: 安装独立 GM 服务

- **WHEN** Build 发布 GM 服务、Relay 和 Player
- **THEN** GM MUST通过单独认证的 HTTP 查询入口获取 Relay 运行线程生成的快照
- **AND** MUST保持 Relay Runtime 的非 Gameplay 职责，不安装同进程 GM 处理器作为另一条执行路径

### Requirement: 首版不得捆绑后续业务采样和诊断实现

本 change MUST 不包含玩法状态修改、GM 帧命令、采样启停、联合录制、数据收集、Analyzer 或评分改造。已有 Action/Foot/骨骼问题 MUST 保留真实失败行为，不能通过控制台吞错或补造状态。

#### Scenario: 用户输入尚未安装的采样命令

- **WHEN** 控制台收到不在首批正式目录中的命令
- **THEN** 服务端 MUST 返回命令未安装或未知
- **AND** MUST 不调用旧 Editor 采样路径、创建空任务或假装执行成功
