## ADDED Requirements

### Requirement: Center必须提供唯一版本运行与报告入口

唯一 Tools/3C/Launcher MUST以工作区、产品、候选、场景、操作、运行和报告表达开发实验。Local Fixed 性能产品与三个 Network 产品 MUST通过显式能力描述提供可用操作；不存在的网络 Replay/Capture MUST显示不支持，不生成通过结果。界面 MUST分别显示运行完整性、行为结果、预算结果和证据缺失。

#### Scenario: 作者比较修改前后版本

- **WHEN** 作者显式选择两个合法 Local Fixed 候选与同一录制场景
- **THEN** Center MUST为每个候选分别显示源码、测量工具、Run/Gate/Capture/Analysis 和比较状态
- **AND** MUST不把“尚未运行”或“分析失败”显示为通过

### Requirement: 工作区登记必须独立于常驻Editor

Workspace 登记 MUST绑定精确项目路径、Git 公共目录和项目身份，不要求每个 worktree 常驻 Unity Editor。现有交互 Editor 操作 MUST显式携带 unity_instance 并验证实际项目路径；后台构建 MUST绑定精确 projectPath、Unity 安装身份和 OperationId，不依赖全局 active instance 或 MCP 连接。

#### Scenario: 后台构建没有MCP连接

- **WHEN** 已登记工作区没有运行交互 Editor 而作者提交后台构建
- **THEN** 调度器 MUST按精确项目路径执行正式命令行构建请求
- **AND** MUST不为了连接 MCP 启动常驻第二 Editor

### Requirement: 后台Unity构建必须复用唯一工作流并有界退出

本机显式后台构建 MUST通过 batchmode/executeMethod 调用同一 Development Build Workflow，完成或失败后退出。nographics MUST是已验证产品配方的显式选择，不能失败后切换。已有交互 Editor 占用目标项目时 MUST报告冲突，禁止启动第二个同工程实例。该例外 MUST不扩展到 CI、PlayMode 测试或未声明自动运行。

#### Scenario: 命令行构建完成

- **WHEN** 后台 Unity 完成构建并发布合法候选
- **THEN** 进程 MUST退出并保留日志、退出码与候选引用
- **AND** MUST不继续驻留为第二个 Editor

#### Scenario: 交互工程被占用

- **WHEN** 后台请求指向正在打开的同一工程
- **THEN** Center MUST明确要求选择该 Editor 的显式构建或等待释放
- **AND** MUST不关闭未知 Editor 或悄悄换工作区

### Requirement: 控制面必须复用同一命令与持久状态

Launcher、正式 CLI 和 development.* MCP MUST只适配同一命令服务，返回持久化 OperationId/RunId。域重载只恢复显示，不重跑。旧独立 Network/Performance 顶层调度、performance.* 别名和全局当前候选 MUST删除；项目偏好与当前 Editor SessionState 隔离实现 MUST保留并接入统一模型。

#### Scenario: 域重载恢复长任务

- **WHEN** Unity 在构建或采样期间重载脚本
- **THEN** Center MUST按固定请求与 Host 状态恢复同一任务
- **AND** MUST不重新发布 Scenario、启动第二 Player 或切换工具版本

### Requirement: GUI回调不得执行重操作

OnGUI、OnInspectorGUI 与界面轮询 MUST只提交明确操作和读取轻量快照。文件哈希、目录扫描、日志分析、进程等待 MUST在后台模块执行；Unity 必需的主线程构建步骤 MUST由明确 Editor 调度入口启动，不在绘制回调内执行。

#### Scenario: 打开包含大量历史的Center

- **WHEN** 作者展开候选与报告列表
- **THEN** UI MUST先显示缓存状态并异步请求准确索引
- **AND** MUST不在每次重绘扫描数据目录或计算大文件哈希
