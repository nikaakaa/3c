## 1. 共同接口与迁移范围

- [ ] 1.1 核对当前工作区的 AI 调用者、资产引用和共享输入/Tree/Timeline 依赖，交付按“删除、职责迁移、保护”分类的精确清单，确认不覆盖其它任务改动。
- [ ] 1.2 按design的逐条交接表对齐现行Behavior Designer、角色输入、Action结果、Session与Graph authoring合同，清除针对已退出AI Document／MCP／synthesis能力的delta和引用，不整段覆盖有效增量、不新增旧AI功能。

## 2. 正式观察与输入生产合同

- [x] 2.1 将旧 AI 文件内仍被使用的角色输入目录、typed 输入构造和目标筛选迁回对应业务模块并按职责命名，以调用者清单及剩余引用检查证明只有一份实现。证据：`f13fa36e8`将CommittedActorObservation迁为通用CommittedActorPose/Ingress，Local/Authority/Rollback只依赖正式输入观察合同；旧AI源删除后GameScripts中无旧观察类型引用。
- [ ] 2.2 从正式 Input/Action 状态及已提交结果提供请求身份到排队、过期、拒绝和释放终态的只读关联，交付可被玩家/插件共用的观察合同与权限校验，不复制请求 buffer 或 Action 生命周期。
- [ ] 2.3 为现有 Source 增加显式批量准备能力，按同一观察完成全部本端输入后再交付唯一输入 writer，以 Local/Authority/Rollback 装配和编译结果证明公共 Host/Composer 不依赖插件类型。
- [ ] 2.4 在现有 Source 存储中分离不可改写的捕获/冻结输入事实、生产 frontier 与可恢复消费状态，交付字段所有权和 checkpoint/restore 对照，覆盖已冻结后模拟失败、重发及同 Tick 重读。
- [ ] 2.5 对齐多 Actor 的 request capture sequence、Timing Class、eligible Tick 与 pending 恢复，交付原请求身份和顺序保持的诊断数据，确认不重捕获插件/设备事件、不重复延迟远端请求。

## 3. 插件接入与游戏任务

- [ ] 3.1 建立独立的 Unity 插件接入程序集和必要 Editor 配置表面，交付依赖图及编译结果，确认 portable Core/Float32/Fixed/Relay/.NET Authority 不引用 Opsive 或 Entities 运行类型。
- [ ] 3.2 接入插件受控启动、Manual 批量推进、暂停/恢复和销毁，交付按 Session/Actor 注册与 Tick 计数的诊断结果，确认没有逐 Actor 更新整组、重复 PlayerLoop 推进或第二 Logic target。
- [ ] 3.3 实现显式候选选择与接近/朝向任务，交付可在插件窗口配置的正式绑定和完整 typed 输入输出，确认任务不扫描场景、不写 Transform，退出移动后不残留旧方向。
- [ ] 3.4 实现提交请求与等待正式结果任务，并支持角色目录中实际存在的取消/替换意图，交付原请求到结果的关联；同一激活只提交一次，OnEnd 不隐式取消技能。
- [ ] 3.5 通过插件原生图闭包和正式角色目录校验任务依赖、输出权限和行为版本，交付可定位非法任务/绑定的 Validator 结果，确认不复制插件控制节点规则或修改其内核。

## 4. Local 正式接线

- [ ] 4.1 将稳定 Corin 的本地训练行为改为插件原生图/子树和 Source 绑定，交付经过正式资源/配置校验的资产闭包，保留同一角色、技能、运动和表现资产。
- [ ] 4.2 为 Local Float32/Fixed 完成对应数值转换、观察和批量输入接线，交付两种正式 Composition 的准备/输入记录，确认 Fixed 不借用 Float32 Program，Local 不新增长期 Replay history。
- [ ] 4.3 清理旧 Corin/TrainingEnemy AI 资产及旧组件引用，交付引用扫描结果；TrainingEnemy 只处理 AI 绑定，不修改 Pose、曲线、Projection、Rig 或角色算法。

## 5. Rollback 一端多Actor输入

- [ ] 5.1 分离真实 Peer/Player 身份、Actor 名单与输入所有权，迁移协议、名单 hash、配置和握手，交付一致的版本/字段清单并删除旧一端一角色 reader，不创建假 Peer/Player。
- [ ] 5.2 将 Endpoint 的单一本地 Actor 输入路径改为显式拥有的 Actor 集合，复用唯一输入生产/历史合同，交付按 Actor 隔离的序号、冻结帧和连续冗余编码记录。
- [ ] 5.3 更新 Relay 的逐 Actor 输入权限、立即转发、完整 roster canonical 汇集与确认，交付包含错误所有者拒绝和 Actor 缺帧定位的现有诊断输出，保持原 MTU/单帧预算规则。
- [ ] 5.4 将角色 restore/replay、NoStep 和 World snapshot recovery 接到不可倒退的本端输入事实，交付调用链及输入身份对照，确认旧 Tick 不重新执行 BT、缺失历史不补造输入。
- [ ] 5.5 更新连接 ready/timeout 与 Actor frontier/容量的统计归属，同步既有 Relay 查询和 GM 只读投影，交付可区分真实连接数与角色数的结果，不增加 GM 写命令。
- [ ] 5.6 发布两个真实 Peer、两名玩家角色 Bot 和一名中立 Actor 的正式示例配置，明确指定既有 Peer 生产 Bot 输入，交付角色资产、所有权、Fixed 输入和资源闭包；保持原输入时序及同一 KCC/World batch。

## 6. Unity Authority Bot输入

- [ ] 6.1 在既有 Room/Authority Source 合同中分离真实客户端路由与完整 Actor 输入归属，交付玩家 ticket 与权威本地 Bot 权限的校验结果，确认客户端无法控制 Bot。
- [ ] 6.2 在唯一 Authority 输入装配中合并玩家 accepted 输入和 Worker 本地 Bot 输入，交付完整权威 batch 与来源诊断，确认 Bot 不等待不存在的连接，任务异常不进入玩家丢包保持策略。
- [ ] 6.3 将 Bot 的正式角色/动作结果纳入既有 Replication、远端 Body 观察和表现路由，交付按 Actor 与真实收件人关联的结果，不同步插件游标/黑板或新增角色 Session。
- [ ] 6.4 发布对应 Unity Authority 最小 Bot 配置并落实宿主能力矩阵，交付合法 Unity 装配和不支持的 DotRecast 插件绑定的校验结果，保持无 Bot .NET 产品的原正式运行路径。

## 7. 旧AI实现与作者链退役

- [x] 7.1 删除自研 AI Definition/Tree/节点、Frontend、Semantic IR、Program/Asset、State/codec、专用 operation/执行器与仅供旧能力使用的测试/fixture，交付删除及引用扫描清单，保留技能和输入仍使用的公共基础。证据：`b012bf211`删除73个AI作者/运行链文件；`9e98cb5bf`删除18个旧AI资产与注册；`rg`对GameScripts、Character配置和TreeLocations扫描无旧AI符号。
- [x] 7.2 删除 AI 专用窗口、Graph Role/Capability、菜单导航和旧运行诊断装配，交付剩余作者入口检查，确认技能/Timeline/TreeClip/Pose 的编辑与编译入口仍完整。证据：`b012bf211`删除AI Editor与AI Graph能力/菜单注册；TreeLocations中的AI注册由`9e98cb5bf`移除；保留Skill Flow、Timeline、TreeClip和Pose入口。
- [x] 7.3 从唯一 Document schema、DTO、根解析、Exporter、Reconciler、Mutation、Validator 和 MCP 路由中删除游戏 AIController，交付旧领域拒绝与其它正式领域的既有 Validator/引用结果，不建立插件 AI Document。证据：`b012bf211`移除AI DTO、Exporter、Reconciler、Mutation、Validator和MCP分支；当前Document只接受CharacterController，`editable/ai/perception.json`明确拒绝，Behavior Designer不进入BTSMTL Document。
- [x] 7.4 同步 BTSMTL Agent 技能、MCP 描述与代码地图，交付文档字段/路径检查；保留主重构技能/控制与 Timeline 独立领域，不恢复旧 reader、双写或兼容类型。证据：`.codex/skills/btsmtl-agent-authoring/SKILL.md`及`references/current-contract.md`已删除AI Document/AIProgram流程并改为Character v7；current specs、`project.md`和MCP描述已对齐插件AI边界。

## 8. 内容与产品发布

- [ ] 8.1 沿现有资源发布流程锁定行为/子树、任务/插件版本、角色输入目录和生产者配置，交付精确内容身份及会话版本校验，不新增资源根或运行时补建。
- [ ] 8.2 更新两个网络产品 adapter 的 Candidate/Run 配置闭包与相关 ABI/协议身份，交付从最终目录重读通过的 manifest；公共 Build/Run workflow 不增加插件或模型具体类型分支。
- [ ] 8.3 同步 project.md、2v2vE 当前状态与本变更规范安装记录，交付旧 AI 承诺和并行 delta 的逐项对账；安装时清理已退役且无有效要求的旧能力文档及相关 Purpose，不宣称完整战斗闭环已完成。

## 9. 既有检查与交付证据

- [ ] 9.1 通过当前正式 CLI/Development Center 完成受影响 Unity 编译和已有 portable 产品编译，记录精确 workspace/命令/日志；所有 dotnet build/msbuild 使用项目规定的禁用构建服务器参数并在结束后关闭 build-server。
- [ ] 9.2 复用现有固定输入回放和正式网络运行入口，交付原输入重用、请求身份、完整 Actor roster、最终角色状态/输出的可追溯记录；区分角色输入重演与重新运行 AI，未覆盖的场景明确列出，不新增测试或临时运行入口。
- [ ] 9.3 执行本 change 的 OpenSpec 严格校验、按实际安装标题与顺序的组合规范对账、剩余引用/程序集/资产闭包检查和保护范围 diff 审查，分别交付独立校验与组合检查结果及中文小步提交记录，确认不存在旧 AI 可运行路径、重复改名、丢失的 Skill/Timeline 增量、无依据性能结论或其它任务改动覆盖。
