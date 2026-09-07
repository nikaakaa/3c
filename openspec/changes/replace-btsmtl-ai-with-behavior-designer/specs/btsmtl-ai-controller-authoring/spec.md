## REMOVED Requirements

### Requirement: AI Controller必须拥有独立Definition与RootTree

**Reason**: 游戏 AI 作者和执行改由插件原生行为资产承担，项目不再维护 AI Definition/RootTree。

**Migration**: 将行为与角色输入绑定迁入正式插件资产和 Source，删除旧 Definition、RootTree、generated Program 与失效引用。

### Requirement: AI Tree编辑必须复用BTSMTL窗口核心

**Reason**: 游戏 AI 使用插件窗口，不再进入 BTSMTL 作者窗口。

**Migration**: 删除 AI 专用窗口、导航和菜单装配，保留技能、Timeline、Pose 等仍使用的共享窗口基础。

### Requirement: AI Controller Tree必须限制节点领域

**Reason**: AIControllerTree 及其节点领域注册被删除。

**Migration**: 插件行为使用正式游戏任务的角色输出权限校验，不保留旧 AI Graph Role 或复制插件控制节点规则。

### Requirement: AI Blackboard必须与Character Blackboard分离

**Reason**: 自研 AI Blackboard 和 AIControllerState 不再存在。

**Migration**: 战术记忆由插件实例持有；角色状态和正式只读观察仍按其原所有者维护，插件不能直接写 Character/Action 状态。

### Requirement: AI Intent节点必须绑定受控Character输入目录

**Reason**: 自研 AI Intent 节点被替换。

**Migration**: 项目插件任务继续从正式角色输入目录绑定类型与身份，删除旧节点、端口与专用编译规则。
