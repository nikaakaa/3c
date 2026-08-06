## 1. 现状对账与旧口径收口

- [x] 1.1 对账本地 Lyra 的 Interface、Base Implementation、武器 Implementation、root Linked Layer 与 selection set 关系
- [x] 1.2 对账本地 GASP 的 Linked Anim Layer/Graph 宿主节点与固定 AnimGraph 边界
- [x] 1.3 对账现有 Linked Pose Interface、Implementation、Group、selector、Call、Mutation、Validator、Projection 与 Preview 代码入口
- [x] 1.4 将“Document 是人工唯一修改链”纠正为“Document 是 AI 唯一包生命周期，UI 与 Document 共享唯一类型化语义写链”
- [x] 1.5 删除只读 Profile/Linked Inspector 作为主要作者流程的旧入口口径

## 2. 类型化 Mutation 与原子资产闭包

- [x] 2.1 新增 Interface 创建、配置、Entry 增删改排序与 typed port 增删改排序 mutation
- [x] 2.2 为 Interface 变更生成确定 revision/signature，并保持 hash 只读派生
- [x] 2.3 扩展 Implementation mutation，支持从 Interface 一次创建全部 required Entry binding
- [x] 2.4 扩展 Graph catalog mutation，原子创建 Entry Graph owner、Graph、Input/Output boundary 与 layout
- [x] 2.5 新增复制 Implementation 闭包命令并为所有新对象生成正式稳定 identity
- [x] 2.6 新增显式 Empty Implementation 模板命令，不把模板作为普通创建 fallback
- [x] 2.7 扩展删除 mutation 的依赖预检与完整 owner 收集
- [x] 2.8 建立 UI 与 Document 共用的 Linked Pose authoring application service、validator 和一个 Undo/rollback 边界
- [x] 2.9 阻止 presenter、Custom Inspector 与 selector UI 直接使用 SerializedObject/YAML/数组写入绕过 mutation

## 3. 统一工作区信息架构

- [x] 3.1 在现有 GraphAuthoringEditorShell 增加 Linked Pose domain pages，不创建第二窗口或 GraphView
- [x] 3.2 将 Navigator 改为 Group -> Contract/Selection/Implementations/Host Calls 层级
- [x] 3.3 为 Group、Interface、selector、Implementation 与 Entry 建立稳定 workspace selection
- [x] 3.4 单击非 Graph 对象时在同一 Details 显示 authoring，保持当前 Canvas 页面
- [x] 3.5 打开 Entry 时在同一 Canvas 与 breadcrumb 下钻到 required Entry Graph
- [x] 3.6 root Call、Group、Implementation 与 Entry 之间提供双向定位命令
- [x] 3.7 为零 Linked Pose 数据提供按依赖顺序的可执行空状态
- [x] 3.8 使用业务显示名和类型受限对象目录，默认隐藏 identity、revision、hash、GUID、local file id 与 compiled handle

## 4. Interface 与 Implementation 作者页

- [x] 4.1 实现 Interface Entry/port 的结构化 Details presenter 与 command state
- [x] 4.2 在 Interface mutation 前显示 Group、Implementation、Call、edge 与 Projection 影响闭包
- [x] 4.3 实现从 Group/Interface 创建普通 Implementation 的工作流
- [x] 4.4 实现复制 Implementation 与显式创建 Empty Implementation 的工作流
- [x] 4.5 在 Implementation 页面显示 required Entry 完整性并原位跳转缺失、重复或 stale 项
- [x] 4.6 实现依赖感知的 Implementation 与 Interface 删除命令
- [x] 4.7 保持普通 Implementation 初始图只有合同边界，不生成业务节点或隐式连线

## 5. Group 与 selector 作者页

- [x] 5.1 实现从现有 Interface 创建 Group 的正式命令
- [x] 5.2 建立 selector authoring capability、presenter 与 typed mutation lowering 合同
- [x] 5.3 接入首个 Equipment selector presenter
- [x] 5.4 使用正式 Equipment Slot 与 Equipment catalog 对象目录编辑精确 mapping
- [x] 5.5 把 Empty mapping 作为必填正式行并限制为同 Interface Implementation
- [x] 5.6 保持 Candidate Closure 派生只读并显示缺失、重复、跨 Interface 与未覆盖诊断
- [x] 5.7 阻止 Linked Pose 工作区按 Equipment 类型硬编码通用 selector 分支

## 6. LinkedPoseCall 作者体验

- [x] 6.1 为 root LinkedPoseCall 提供当前 Profile Group 选择器
- [x] 6.2 从所选 Group Interface 提供 Entry 选择器并派生只读 Interface
- [x] 6.3 从 Interface Entry 统一重投影 Call typed ports
- [x] 6.4 在 Group/Entry 重绑前检查所有现有 edge 的 identity、方向与类型
- [x] 6.5 对不兼容重绑拒绝 mutation并显示可跳转阻塞 edge，不静默删线
- [x] 6.6 提供显式 Create Missing Required Calls 命令，只创建节点与端口，不猜测接线
- [x] 6.7 保持 LinkedPoseCall root-only，并在 Entry context 中继续禁止嵌套调用

## 7. 状态、Preview 与诊断

- [x] 7.1 在 Navigator、Details 与 Toolbar 统一显示 Dirty、Invalid、Stale、Ready 与 Live 状态
- [x] 7.2 将 Linked authoring diagnostics 定位到 Group、selector、Implementation、Entry、Call、port 与 edge
- [x] 7.3 在 Bottom Dock 增加 Preview-only Group/Implementation override
- [x] 7.4 让 Preview override 只消费匹配 revision 的 compiled candidate catalog 与正式 Preview session
- [x] 7.5 显示当前 Implementation、selection revision、generation、Entry completion、Call contribution 与 discontinuity
- [x] 7.6 Projection Stale 时停止 Preview 并保留只读选择状态，不创建临时 Projection
- [x] 7.7 Live Debug 模式保持全部 mutation 禁用且不允许 override 正式 Runtime selection
- [x] 7.8 保持 Validate、Compile、Build 显式触发，阻止 selection、Inspector、Undo、refresh 与 preview target 自动执行重操作

## 8. Inspector、Document 与清理

- [x] 8.1 将 Profile Linked Pose 区域收口为摘要与 Open in Animation Workspace 入口
- [x] 8.2 将 Interface、Implementation 与 selector Custom Inspector 收口为轻量只读摘要、诊断与工作区入口
- [x] 8.3 保持 Custom Inspector 不执行 owner 扫描、codec、Build、Apply 或资产创建
- [x] 8.4 让 UI mutation 后的 Document 状态按现有 revision/hash 规则进入 TreeDirty
- [x] 8.5 保持 Document checkout、rebase、dry-run、apply 与 validate 生命周期不变
- [x] 8.6 保持 UI 与 Document 对同一目标状态生成同类 typed Presentation Mutation、诊断与 revision 变化
- [x] 8.7 删除旧 Linked Pose 只读浏览主路径、重复按钮、直接 Asset Inspector 跳转与任何并行写入口
