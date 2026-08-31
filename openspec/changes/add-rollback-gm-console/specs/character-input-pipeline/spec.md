## ADDED Requirements

### Requirement: Rollback 开发控制台焦点必须在正式设备适配边界处理

游戏内控制台 MUST 使用显式输入焦点，作用于本地设备到 portable input 的现有边界。控制台获得交互焦点时 MUST 按 Program input catalog 生成 neutral gameplay 输入且不产生动作请求，相机不得消费 UI 占用的鼠标输入。焦点变化 MUST 不清理 committed request、输入历史或网络队列，不暂停 Session。本轮只装配 Rollback 控制台，共享适配器在没有控制台焦点时 MUST 保持原有行为。

#### Scenario: 输入 GM 命令

- **WHEN** 本地作者打开控制台并输入命令或点击界面
- **THEN** UI 操作 MUST 不穿透为 Attack、Dodge 或相机旋转
- **AND** 其它客户端及本进程模拟角色 MUST 按正式输入与时序继续运行

#### Scenario: 关闭控制台

- **WHEN** 控制台释放交互焦点
- **THEN** 本地设备输入 MUST 由原正式适配器恢复采集
- **AND** MUST 不把 UI 期间的按键补发为动作，不改变已经进入 Program 的请求
