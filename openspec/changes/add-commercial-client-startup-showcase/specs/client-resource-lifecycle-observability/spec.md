## ADDED Requirements

### Requirement: 业务资源生命周期必须由项目层 ProductResourceRuntime 统一拥有

HotFix 产品代码 MUST通过项目层 ProductResourceRuntime 申请、实例化和释放业务资源。ProductResourceRuntime MUST调用 TEngine `IResourceModule` 和 YooAsset 正式 package API，MUST不修改 `Packages/com.alex.tengine`、直接持有 YooAsset 内部 handle 或建立第二个 AssetObject pool。

#### Scenario: Home 请求资源

- **WHEN** Home 业务需要加载 UI、展示角色或背景
- **THEN** 业务 MUST通过 Home ResourceScope 调用 ProductResourceRuntime
- **AND** MUST不直接调用 YooAssets.GetPackage 或散落调用 ResourceModule.UnloadAsset

#### Scenario: TEngine 包升级或替换实现

- **WHEN** ProductResourceRuntime 适配 TEngine 公共 API 变化
- **THEN** 业务 ResourceScope 与 PreloadPlan 合同 MUST保持在项目目录
- **AND** 项目业务代码 MUST不迁入 TEngine package

### Requirement: 正式资源所有权必须分为 Global、Home、Gameplay 与 Transient Scope

ProductResourceRuntime MUST提供唯一 Global scope，并允许 ProductStartupCoordinator 创建 Home、Gameplay 与显式 Transient scope。每个 lease MUST精确归属一个 scope；缺失 owner、重复 owner 或跨 scope 隐式转移 MUST失败。

#### Scenario: ProductShell 创建 Home

- **WHEN** 认证完成并开始 Home PreloadPlan
- **THEN** ProductStartupCoordinator MUST创建唯一 Home scope
- **AND** Home 全部 lease MUST登记到该 scope

#### Scenario: 进入 StandaloneGameplay

- **WHEN** Gameplay PreloadPlan 开始
- **THEN** ProductStartupCoordinator MUST创建唯一 Gameplay scope
- **AND** Home 与 Gameplay lease MUST保持可区分 ownership

#### Scenario: 未指定 Scope 加载业务资源

- **WHEN** HotFix 业务代码调用 ProductResourceRuntime 但没有提供有效 scope
- **THEN** 请求 MUST明确失败
- **AND** MUST不自动归入 Global scope

### Requirement: 同一物理资源的并发加载必须合并

ProductResourceRuntime MUST使用 package、规范化 location 与 asset type 构成物理加载 identity。同一 identity 的并发请求 MUST共享一个 in-flight physical load；每个成功调用方 MUST获得独立 logical lease。TEngine 已有加载合并和 pool MUST继续是物理加载底座，项目层 MUST不发起重复 YooAsset handle。

#### Scenario: 二十个调用方同时加载同一 Texture

- **WHEN** 二十个有效 scope 在物理加载完成前请求相同 package/location/type
- **THEN** diagnostics MUST记录 logical load 为二十
- **AND** physical load MUST为一
- **AND** in-flight join MUST为十九

#### Scenario: 物理加载失败

- **WHEN** 共享 in-flight load 失败或被取消
- **THEN** 所有等待调用方 MUST收到同一 generation 的失败结果
- **AND** MUST不登记半完成 lease 或缓存 null 资源

### Requirement: Prefab 资源唯一性与实例生命周期必须分开

Prefab asset 的相同 identity MUST只占用一个物理资源加载；每次实例化得到的 GameObject MUST拥有独立 instance identity、scope ownership 和销毁责任。Diagnostics MUST分别显示 asset physical load 与 live instance 数量，MUST不把多个实例描述为重复加载资源。

#### Scenario: 同一角色 Prefab 实例化三次

- **WHEN** Gameplay scope 从同一个已加载 Prefab 创建三个 GameObject
- **THEN** Prefab physical load MUST保持一份
- **AND** live instance MUST为三
- **AND** 每个实例销毁 MUST不提前释放仍被其它 lease 持有的 Prefab

### Requirement: 页面和玩法预加载必须使用显式业务 Barrier

Home 与 Gameplay MUST各自提供 immutable PreloadPlan。Plan MUST按稳定 barrier 顺序执行，并允许同一 barrier 内安全并发。Plan MUST只声明业务 location 和期望类型，MUST不保存 AssetBundle 文件名或复制 YooAsset dependency graph。

#### Scenario: Home 预加载

- **WHEN** Home PreloadPlan 执行
- **THEN** Shared UI barrier MUST先于 Home UI barrier 提交
- **AND** Home presentation barrier MUST在其依赖 lease 可用后提交
- **AND** 全部 barrier 成功前 HomeReady MUST为 false

#### Scenario: Gameplay 预加载

- **WHEN** Gameplay 标签下载完成
- **THEN** Gameplay PreloadPlan MUST按正式 barrier 准备 Scene、Corin presentation 与必要共享资源
- **AND** YooAsset MUST继续自行解析每个 location 的 Bundle 依赖

### Requirement: Scope Dispose 必须原子停止新资源所有权

Scope Dispose MUST先将 scope 标记为 Closing，拒绝新请求，取消未提交请求，再释放已提交 lease 和实例。Dispose 完成后 scope MUST进入 Disposed 且不可恢复使用；重复 Dispose MUST不造成二次 UnloadAsset 或负引用。

#### Scenario: Home 退出时仍有异步加载

- **WHEN** Home scope 开始 Dispose 且一个物理加载尚未完成
- **THEN** Home 对该请求的逻辑等待 MUST取消
- **AND** 完成回调 MUST不向已关闭 Home 登记 lease
- **AND** 其它仍有效 scope 对共享 physical load 的等待 MUST不受影响

#### Scenario: 重复释放同一 Lease

- **WHEN** 业务对已释放 lease 再次调用 Dispose
- **THEN** ProductResourceRuntime MUST不再次 Unspawn 物理资源
- **AND** diagnostics MUST记录重复释放错误

### Requirement: 零引用资源必须只在正式安全点执行全局回收

关闭普通窗口 MUST只释放其 lease。TEngine/YooAsset `UnloadUnusedAssets` 和 Unity 全局 unused asset 回收 MUST只在 Single Scene 切换完成、返回 Home 加载遮罩、显式资源维护阶段或 low-memory 处理中执行。普通窗口关闭 MUST不直接调用 `Resources.UnloadUnusedAssets` 或 `GC.Collect`。

#### Scenario: 关闭一个 Home 子面板

- **WHEN** 子面板释放自己的 Transient scope
- **THEN** 对应 lease MUST归零或继续被其它 scope 持有
- **AND** 系统 MUST不立即执行全局 unused asset scan

#### Scenario: 从 Gameplay 返回 Home

- **WHEN** StandaloneGameplay Session 与 Gameplay scope 已按顺序销毁
- **THEN** 加载遮罩安全点 MUST触发零引用资源回收
- **AND** Global 与新 Home scope 仍持有的资源 MUST保持有效

### Requirement: Low-memory 处理不得破坏活动 Scope

Unity low-memory 回调 MUST进入 ProductResourceRuntime 的正式资源维护入口，释放全部零引用 pool/cache 资源并记录前后 snapshot。Global、Home 或 Gameplay 活动 lease 引用的资源 MUST不被强制销毁；系统 MUST不通过清空 scope 伪造内存下降。

#### Scenario: Gameplay 中收到 low-memory

- **WHEN** Gameplay scope 仍持有角色、场景和表现资源
- **THEN** low-memory 维护 MUST保留这些活动 lease
- **AND** 只释放零引用资源
- **AND** diagnostics MUST记录释放前后资源数和内存

### Requirement: 资源诊断必须同时表达逻辑与物理计数

ResourceRuntimeSnapshot MUST至少包含 logical load、physical load、in-flight join、cache hit、active lease、live instance、scope count、每 scope lease 数、TEngine pool 计数、YooAsset package/version/tag 和最近 unload 结果。Snapshot MUST只读且有界，MUST不使用反射访问 TEngine/YooAsset 私有成员。

#### Scenario: 查看并发加载结果

- **WHEN** 相同资源并发演示完成
- **THEN** UI MUST同时显示 logical、physical、join 和 active lease
- **AND** MUST能证明“一份资源”指 physical asset 而不是 GameObject instance

#### Scenario: Scope 已销毁

- **WHEN** Gameplay scope Dispose 完成
- **THEN** snapshot MUST不再列出该 active scope
- **AND** 最近已冻结的 dispose 统计 MAY保留在有界历史中

### Requirement: 内存诊断必须使用公开运行时指标和正式预算

MemoryRuntimeSnapshot MUST通过 Unity 公开 ProfilerRecorder/Profiler API 与项目公开资源指标记录 Total Used、Total Reserved、GC、Texture、Mesh、active scope、lease 和实例数量。项目 MUST提供一个正式平台预算配置；缺失预算 MUST报告配置错误，MUST不使用硬编码默认值或按当前峰值自动生成预算。

#### Scenario: Home Ready 捕获内存快照

- **WHEN** Home 全部 preload barrier 已提交
- **THEN** diagnostics MUST冻结一份 HomeReady 内存与资源 snapshot
- **AND** MUST显示相对正式 Home 预算的使用情况

#### Scenario: Gameplay 退出后捕获快照

- **WHEN** Gameplay scope 释放且安全点回收完成
- **THEN** diagnostics MUST冻结回收后 snapshot
- **AND** UI MUST能对比 GameplayReady 与回收后的公开指标

### Requirement: 诊断不得驱动资源和 Gameplay 结果

Startup、Resource 和 Memory diagnostics MUST只消费正式 snapshot。打开、关闭、筛选或捕获诊断 MUST不创建 lease、不改变 scope、不触发下载、不修改 Session/Gameplay state，也 MUST不成为 unload 时机的唯一来源。

#### Scenario: 关闭诊断页

- **WHEN** 用户关闭 ProductShell diagnostics
- **THEN** ResourceScope、下载器、Auth Session 和 Gameplay Session MUST保持原状态
- **AND** 诊断 UI 自己的资源 MUST按其明确 UI scope 释放

### Requirement: Fault Lab 必须只存在于 Editor 与 Development Build

Fault Lab MUST通过编译和构建边界从普通 Release Player 排除。它 MAY取消当前 downloader、损坏一个显式选中的缓存 Bundle、并发申请同一资源、Dispose 指定非 Global scope和调用正式 low-memory 入口；它 MUST不伪造成功结果、替换 endpoint、禁用 TLS、注入 mock Auth Server 或直接修改引用计数。

#### Scenario: Development Build 打开 Fault Lab

- **WHEN** Development Build 的 ProductShell 打开 Fault Lab
- **THEN** UI MUST只列出当前正式边界支持的操作
- **AND** 每次操作 MUST生成结构化 diagnostics event

#### Scenario: Release Player 构建

- **WHEN** 普通非 Development Player 构建完成
- **THEN** Fault Lab UI、命令 handler 和缓存破坏入口 MUST不进入运行闭包

### Requirement: Gameplay Session 必须先于 Gameplay Scope 被最终回收

离开 StandaloneGameplay 时 MUST先停止并销毁 SimulationSessionHost、Actor registration、Endpoint 与 Scene-owned runtime，再释放 Gameplay scope，最后在安全点回收零引用资源。ResourceRuntime MUST不因释放 Gameplay 资产提前破坏仍活动的 Program、Projection、Solver 或 Presentation。

#### Scenario: 从 Gameplay 返回 Home

- **WHEN** 产品开始离开 StandaloneGameplay
- **THEN** Gameplay Session runtime MUST先完成 teardown
- **AND** Gameplay scope MUST随后 Dispose
- **AND** unused asset 回收 MUST最后执行

#### Scenario: Gameplay teardown 失败

- **WHEN** Session runtime 未能完成正式销毁
- **THEN** 产品 MUST报告 teardown 错误
- **AND** MUST不强制清空 Gameplay scope 后继续 Home

