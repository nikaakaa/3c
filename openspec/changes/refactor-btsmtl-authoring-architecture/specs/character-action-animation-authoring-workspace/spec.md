## ADDED Requirements

### Requirement: 动作工作区必须恢复精确作者上下文

动作工作区 MUST在脚本编译与重载后恢复仍有效的Character Definition、Action、调用点、Timeline owner、当前页签和可解析选择。恢复 MUST使用已保存的稳定身份和正式引用关系，不得按当前场景、全局selection、显示名称或列表顺序重新猜测动作。恢复失败 MUST显示失效关系及其owner；恢复本身 MUST不修改作者资产、不Build、不发出动作输入，也不自动启动或重建运行。

#### Scenario: 编辑攻击片段后发生重载

- **WHEN** 作者在精确角色的攻击动作工作区选择一个Timeline片段，随后发生脚本重载
- **THEN** 工作区 MUST恢复同一角色、Action、调用点和Timeline
- **AND** 片段仍存在时 MUST恢复其选择与所在页面
- **AND** MUST不因恢复再次触发攻击或发布构建产物

#### Scenario: 原调用点已删除

- **WHEN** 重载后保存的Action调用点已不存在
- **THEN** 工作区 MUST显示该关系失效
- **AND** MUST不自动绑定同名Action的其他调用点或任意Timeline

### Requirement: 动作运行显示刷新必须保持作者操作连续

动作工作区 MUST在更新逻辑时间、表现时间、Slot状态和诊断时保持当前作者选择、Details编辑、页面滚动与区域折叠状态。只读数值刷新 MUST不重复重建完整编辑区域，也不得触发Timeline mutation、重置编辑游标或清除片段选择。作者数据或引用关系确实变化时 MUST按对应变化刷新，并继续服从当前模式的编辑权限；本要求不扩大Live模式可写字段范围。

#### Scenario: Preview数值持续变化时编辑片段

- **WHEN** 在允许编辑的模式中作者修改Timeline片段参数，同时底部Preview数值持续更新
- **THEN** 片段选择、字段输入和滚动位置 MUST保持
- **AND** 只有作者正式提交时才产生对应修改和Undo

#### Scenario: Live模式下切换观察页签

- **WHEN** 作者在Live观察期间切换Slot与诊断页签
- **THEN** 页签 MUST读取同一合法运行绑定的当前事实
- **AND** MUST不重建动作实例、不改变Timeline运行时间或扩大编辑权限

#### Scenario: 底部区域折叠后继续播放

- **WHEN** 底部运行显示区域保持折叠且收到新的运行快照
- **THEN** 系统 MUST不反复清空并创建该区域全部控件
- **AND** 当前角色和动作关系 MUST保持，展开后显示最新合法状态
