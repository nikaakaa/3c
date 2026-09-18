## ADDED Requirements

### Requirement: 共享工作区必须通过领域操作承载场景预览

共享工作区 MUST通过领域提供的操作和状态合同承载预览场景选择、运行控制、精确作者/运行目标绑定及生效状态。角色领域提供 Actor/SkillDefinition/ActionInstance 与调用路径，独立 Timeline 领域提供正式内容根、业务 owner/播放 identity、调用点和 generation；外壳 MUST不判断 Gameplay、Pose、Action、独立内容或相机字段，也不强制所有领域具有角色。已有 Toolbar、Navigator、Canvas、Details 和 Bottom Dock MUST继续复用同一交互和数据来源，不得复制为新的预览工作台。

#### Scenario: 从Graph进入场景预览

- **WHEN** 当前 Graph 领域提供合法的预览操作
- **THEN** 共享 Toolbar MUST显示其场景和运行状态
- **AND** Canvas 的 selection、clipboard、Undo 和导航 MUST继续使用原有实现

#### Scenario: 独立Timeline领域连接场景

- **WHEN** 领域适配器提供合法的非 Skill 内容与调用方观察合同
- **THEN** 共享操作 MUST复用同一场景启停和状态，显示领域提供的准确目标
- **AND** 外壳 MUST不添加 Character/Action 校验或持有内容播放状态

### Requirement: 窗口生命周期必须只管理本地预览视图

窗口 MUST只拥有本地技能、独立 Timeline、Pose 及其它仍保留的领域文档、选择、运行观察绑定和 interest；C#控制只提供正式配置与代码来源观察，不提供可编辑角色总控RootTree。关闭窗口、切换文档和折叠区域 MUST不终止受控场景运行；明确结束操作 MUST交给唯一场景预览 owner。重载后 MUST按稳定 identity 重新绑定，不能恢复旧运行对象或因旧外壳描述恢复已退役领域。

#### Scenario: 关闭最后一个作者窗口

- **WHEN** 受控预览运行期间最后一个作者窗口关闭
- **THEN** 运行 MUST继续由 Unity Play 和对应正式业务 owner 管理，角色 Session 与非 Skill 播放各守原归属
- **AND** 窗口 interest MUST全部释放，用户 MUST仍能通过 Unity Stop 结束运行

#### Scenario: 从技能页返回控制配置

- **WHEN** 作者从技能 Root/Timeline 返回 Character 控制配置
- **THEN** 窗口 MUST显示已登记 C# 模块、可配置字段及代码来源，不创建角色图
- **AND** 场景运行和其它窗口的准确实例绑定 MUST保持原归属
