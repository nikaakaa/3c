## ADDED Requirements

### Requirement: 相机预览必须消费现有预览 owner 的正式输入

相机 MUST 向现有 Timeline Preview/ScenePlay owner 提供正式 Camera 资源、只读运行绑定、显式 Rig、目标/物理输入、Reset/替换和实际采用身份。会话、输入来源和历史重建继续由对应 owner 管理；相机 MUST 不创建独立 Preview 会话、第二角色运行链或自己的 seek 执行器。预览 MUST 不实现相机求值、不要求恢复角色全量 Build 或整包 Projection，也不得隐式搜索补齐输入。

#### Scenario: 预览需要碰撞环境

- **WHEN** 已启用碰撞的相机计划进入预览
- **THEN** owner MUST 提供明确物理输入和输出绑定
- **AND** 缺失时 MUST 报告不可用，不得静默按无碰撞运行

### Requirement: 相机历史重建必须走统一 owner

有状态镜头需要重新定位时，系统 MUST 通过现有 owner 的正式 Reset/历史重建合同重新生成状态；Timeline 游标移动不能直接改 Simulation 或插入第二更新循环。缺少重建能力时必须明确不可用，不得将相机预览临时接到旧 Controller。

#### Scenario: 重定位到有持续效果的时刻

- **WHEN** 预览 owner 支持该次正式重建
- **THEN** 相机 MUST 使用同一资源、请求和求值模块恢复对应状态
- **AND** MUST 不残留上一次时间位置的平滑或效果历史

### Requirement: Timeline 相机诊断必须读取实际运行快照

Timeline Live Debug MUST 读取正式相机快照中的来源身份、请求状态、响应、混合、效果、目标与输出，并导航到真实 Clip/资源。它 MUST 不根据游标位置伪造活动请求或重算另一份镜头结果。

实际采用的相机资源/内容版本、绑定实例/代际及失败原因 MUST 由 Camera 领域返回。预览 MUST 不把资源处理完成、技能编译完成、旧 Projection 或旧 Runtime DLL 的调用结果冒充新相机绑定已采用。

#### Scenario: 动作镜头被取消

- **WHEN** 相应请求已经进入正式退出
- **THEN** Live Debug MUST 显示真实退出状态与原因
- **AND** MUST 不因游标仍位于 Clip 范围而显示镜头继续生效
