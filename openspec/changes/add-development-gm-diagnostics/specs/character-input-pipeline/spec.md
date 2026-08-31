## ADDED Requirements

### Requirement: 开发工具输入焦点必须在正式设备适配边界处理

开发工具 UI MUST 使用共用的显式输入焦点策略，作用于本地设备输入到 portable input 的现有边界。GM 获得交互焦点时 MUST 依据 Program input catalog 生成 neutral gameplay 输入且不产生动作请求，相机不得消费被 UI 占用的鼠标输入。焦点变化 MUST 不清理 committed request、历史输入或网络队列，不暂停 Session，也不得分别改写 Fixed、Float32 或 Rollback 内部状态。

#### Scenario: 点击 GM 采样按钮

- **WHEN** 本地作者在 GM 中点击按钮或输入命令
- **THEN** UI 操作 MUST 不穿透为角色 Attack、Dodge 或相机旋转
- **AND** 其它客户端及本进程模拟角色 MUST 按正式 Session 输入与时序继续运行

#### Scenario: 关闭 GM

- **WHEN** GM 释放交互焦点
- **THEN** 本地设备输入 MUST 由同一正式适配器恢复采集
- **AND** MUST 不把 UI 期间的点击或按键补发为离散动作请求
