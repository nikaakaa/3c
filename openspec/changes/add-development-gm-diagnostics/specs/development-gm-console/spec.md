## ADDED Requirements

### Requirement: GM 必须是显式装配的开发版入口

GM MUST 只装配在 Editor 或 Development Player 的正式开发场景，并使 Local Float32、Local Fixed 与 DeterministicRollback 使用同一 UI 和控制模块。非 Development Player MUST 不包含可执行 GM 入口或配置，构建验证 MUST 拒绝误装。GM MUST 不依赖商业认证启动才能运行，也不得伪造另一条 Gameplay Session。

#### Scenario: 启动回滚双端

- **WHEN** 正式 Network Test 产品启动两个客户端
- **THEN** 每个客户端 MUST 各自装配一个本进程 GM 入口
- **AND** Relay MUST 不依赖该 UI，GM 不向玩法 UDP 通道发送工具或 IK 数据

### Requirement: GM 必须通过唯一命令目录执行操作

GM MUST 使用显式注册的命令 Id、中文名称、分类、参数 schema、目标范围、操作类别、能力与结果 schema。菜单/表单和文本命令 MUST 调用同一执行器；新增模块 MUST 不要求修改执行器的业务分支。系统 MUST 不扫描程序集执行任意方法、求值代码字符串或注册未安装的空处理器。

#### Scenario: 采样模块尚未安装

- **WHEN** GM 仅完成入口阶段
- **THEN** MUST 提供真实可用的目标列表、详情与诊断频道命令
- **AND** MUST 不展示可以执行但没有功能的采样按钮

### Requirement: GM 必须使用明确的本进程目标

GM MUST 从现有注册目标及正式元数据选择目标，展示进程、业务 Session、Peer/角色、Actor、运行实例与内容身份。MUST 不按显示名、固定 Actor 名字、注册顺序或场景扫描自动选择。目标结束后 MUST 标为 Ended；同名新实例 MUST 不自动替换旧目标。

#### Scenario: 两个角色共用同一个 Corin Definition

- **WHEN** GM 列出本进程中的自己角色和另一角色
- **THEN** MUST 显示两个可区分的 Actor/runtime instance
- **AND** 每次命令 MUST 指向用户选择的精确目标或明确目标集合

### Requirement: GM 操作必须遵守已安装能力边界

查询、诊断控制和玩法控制 MUST 明确分型。首版 MUST 只公开已安装的查询和诊断控制；MUST 不直接改 Transform、Animation clock、Action registry、Graph state 或使用 Time.timeScale 暂停联机进程。能力不可用或目标结束 MUST 返回明确结果，不选择其它目标或执行替代操作。

#### Scenario: 在联机角色上请求未安装的暂停能力

- **WHEN** 调用方提交当前范围不支持的暂停或回放命令
- **THEN** MUST 返回能力不支持
- **AND** MUST 不停止 Tick、断开另一个客户端或临时切到 Local Source

### Requirement: GM 窗口生命周期不得隐式结束采样

窗口显示、焦点和查询页选择 MUST 属于入口本身；正在运行的采样 MUST 由共用控制服务拥有。关闭窗口 MUST 释放窗口自己的观察订阅和输入焦点，不得删除封存记录或停止独立采样。

#### Scenario: 作者关闭 GM 后继续移动

- **WHEN** 采样期间作者关闭 GM 窗口
- **THEN** 本地输入 MUST 恢复到正式设备适配边界
- **AND** 采样 MUST 按原目标与频道继续，直到明确停止或触及正式结束条件
