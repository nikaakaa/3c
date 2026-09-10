# behavior-designer-ai-integration Specification

## Purpose

定义项目使用 Opsive Behavior Designer 作为 AI 作者与执行来源时，项目角色管线、输入生产、动作结果、网络和诊断之间的正式边界。

## Requirements

### Requirement: AI必须由Behavior Designer原生内容拥有

系统 MUST 使用已安装的 Opsive Behavior Designer 原生行为树、子树、变量、控制节点和调试作为 AI 的唯一作者与执行来源。项目 MUST 不再保存自研 AI Graph、AI Definition、AI Program、AI State 或同义镜像。插件行为 MUST 不转换到 BTSMTL 行为解释器，也 MUST 不建立插件 AI Document。

#### Scenario: 作者调整追击和攻击顺序

- **WHEN** 作者在 Behavior Designer 图中修改行为分支并发布新内容
- **THEN** 新会话 MUST 直接采用该插件行为版本
- **AND** 系统 MUST 不要求生成 BTSMTL AI 图或 AI Program

#### Scenario: 旧AI配置仍可达

- **WHEN** 角色或产品仍引用已删除的 AI Definition、RootTree 或 generated AI Program
- **THEN** 配置或构建校验 MUST 明确拒绝该引用
- **AND** 系统 MUST 不转换成兼容对象或回退旧执行器

### Requirement: 插件任务只能通过正式角色合同影响玩法

项目任务 MUST 读取所属 Session 的冻结观察，并通过正式 Character Input、TargetData、Action Request 或明确取消/替换请求影响玩法。任务 MUST 不直接写 Transform、Animator、Body、Action、Skill、伤害或表现状态。所有输入字段 MUST 使用稳定目录身份和匹配类型，未声明能力 MUST 在 Session 激活前拒绝。

#### Scenario: 配置接近目标

- **WHEN** 作者配置显式候选 Actor、停止距离和正式移动输入绑定
- **THEN** 任务 MUST 根据逻辑观察产生角色输入
- **AND** 角色 MUST 继续通过同一 C# Control、Simulation 和 World 求解链移动

#### Scenario: 行为包含直接移动任务

- **WHEN** 行为闭包包含直接改写 Transform 或未授予的角色输出
- **THEN** 正式配置校验 MUST 定位任务并拒绝会话
- **AND** 插件内置任务身份 MUST 不豁免该写入边界

### Requirement: 输入生产和角色模拟必须共享一个批次边界

正式 AI Source MUST 在输入生产阶段为本端 Actor 读取同一版本的已提交观察，完成整批插件推进和输出校验后，才把 CharacterSimulationInput 交给既有 Local、Authority 或 Rollback 输入端口。同一个 Session、Actor 和输入 Tick 的插件推进 MUST 唯一；重复读取、等待、重发或角色回滚 MUST 不再次推进插件。

#### Scenario: 多个Actor同时生成输入

- **WHEN** 一个 Session 的多个 Bot 需要同一输入 Tick
- **THEN** 全部插件决策和输入校验 MUST 在角色模拟消费前完成
- **AND** 结果 MUST 不依赖逐 Actor 调用整组更新的次数

#### Scenario: 已冻结输入被重复读取

- **WHEN** 同一输入 Tick 的本端输入已经冻结
- **THEN** Source MUST 返回原输入
- **AND** Behavior Tree、随机选择和请求序号 MUST 不再次推进

### Requirement: 请求结果必须沿正式身份只读关联

一次插件任务激活对同一离散请求 MUST 最多提交一次，并保留 ActorId、RequestId、capture sequence、InputTick 以及关联 ActionInstance 的身份。后续等待 MUST 区分排队、过期、准入拒绝、动作开始、完成和中断。任务 MUST 不以插件返回成功、动画播放时间或 OnEnd 回调推断动作终态，也 MUST 不创建第二套 Action 生命周期。

#### Scenario: 攻击仍在缓冲

- **WHEN** 请求已经提交但角色尚未接受
- **THEN** 等待任务 MUST 继续等待或按明确的拒绝/过期结果结束
- **AND** MUST 不宣布技能已经完成

#### Scenario: 动作被打断

- **WHEN** 正式 Action 结果表明该请求对应的释放已中断
- **THEN** 等待任务 MUST 将同一身份的中断映射为任务终态
- **AND** MUST 不创建新的释放或重发原请求

### Requirement: 插件生命周期不拥有角色取消权和确定性回滚状态

Behavior Designer 的停止、条件打断、Load、Pause、Resume、OnEnd 和插件故障回调 MUST 只处理插件任务自己的状态。角色取消或替换 MUST 通过角色目录中明确存在的请求能力表达，并由唯一 Action 服务裁决。插件游标、随机、计时和行为变量 MUST 不进入 Character Simulation 的逐 Tick Snapshot、Hash 或网络协议。插件开始执行后发生任务异常或非法输出时，Source/Session MUST 进入明确 Faulted 状态，不得用中性输入或旧快照掩盖错误。

#### Scenario: 图停止触发OnEnd

- **WHEN** 图停止或恢复插件数据触发任务 OnEnd
- **THEN** 系统 MUST 不因此自动取消正在执行的角色技能
- **AND** 只有进入正式输入的取消/替换请求才能影响角色

#### Scenario: 角色回滚

- **WHEN** Rollback 恢复 Character 和 World 状态
- **THEN** 系统 MUST 复用已经冻结的 Bot 输入
- **AND** MUST 不倒退插件实例或重新计算历史 AI 决策

### Requirement: 内容版本、网络和诊断必须锁定正式边界

正式 Session MUST 锁定行为树/子树内容版本、任务程序集版本、插件版本、Character Input catalog、输入所有权和 Source capability。网络 MUST 只传正式 Input、Canonical Request、Action/State Hash、Snapshot 和结果，不传插件图、黑板名称、游标、变量对象、Timeline 对象或最终 Pose。项目诊断 MUST 能按生产端、ActorId、ObservationTick、InputTick、请求身份和正式动作结果定位一次决策。

#### Scenario: 行为引用未发布任务

- **WHEN** 行为资源引用运行产品没有提供的任务或角色能力
- **THEN** Session preparation MUST 定位版本/能力缺口并失败
- **AND** MUST 不跳过节点或加载另一版本替代

#### Scenario: 网络客户端尝试控制Authority Bot

- **WHEN** 客户端没有该 Bot 的输入所有权
- **THEN** Authority Source MUST 拒绝该输入
- **AND** Bot 输入 MUST 只由锁定的权威生产端生成
