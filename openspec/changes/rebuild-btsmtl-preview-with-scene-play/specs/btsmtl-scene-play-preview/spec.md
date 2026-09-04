## Purpose

定义 BTSMTL 完整角色预览使用可配置独立 Unity 场景和 Play Mode 的作者行为，统一场景选择、正式运行、角色输入、多窗口观察、直接作者调参、试验重建与退出恢复，使预览结果具有明确的场景、配置、输入和运行来源。

## ADDED Requirements

### Requirement: 完整角色预览必须使用明确选择的独立场景

系统 MUST允许作者明确选择一个正式保存的独立预览场景。环境、角色、目标、出生条件、光照和相机 MUST由该场景及其正式 Prefab 引用配置；窗口 MUST不复制角色 Definition、动画拓扑或 Gameplay 配置。未选择场景或场景上下文无效时 MUST保持资产可编辑，并禁用启动、显示缺失原因，不得自动使用当前场景或搜索替代角色。

#### Scenario: 选择斜坡试验场景

- **WHEN** 作者选择一个配置了 Corin 和斜坡的独立场景并开始预览
- **THEN** 系统 MUST运行该精确场景及其角色配置
- **AND** 当前其它编辑场景中的角色 MUST不被接管

#### Scenario: 场景没有唯一正式运行上下文

- **WHEN** 所选场景没有合法 Session 引用或登记了多个无法唯一识别的预览上下文
- **THEN** 系统 MUST报告具体配置错误并停止启动
- **AND** MUST不选择第一个找到的 Host 或创建默认配置

### Requirement: 场景预览必须由正式角色运行链产生结果

预览 MUST进入 Unity Play Mode，由场景明确配置的正式 Session、Numeric Target、输入、世界求解和角色表现产生结果。角色状态、Timeline、TreeClip、Motion、Window、GameplayEffect、动画、IK 及已安装的相机能力 MUST通过正式运行链执行；预览窗口 MUST不创建表现专用替代会话、不直接求值作者对象，也不为缺失能力补造简化结果。

#### Scenario: 动作位移被障碍阻挡

- **WHEN** 场景中的正式动作产生位移请求且正式世界求解检测到障碍
- **THEN** 角色表现与作者观察 MUST使用该次正式求解和提交的结果
- **AND** 预览 MUST不按作者位移曲线另行移动视觉根

#### Scenario: 选择不同Numeric Target

- **WHEN** 作者选择明确配置为 Fixed 的预览场景
- **THEN** 预览 MUST使用该场景的 Fixed Session 和匹配产物
- **AND** MUST不自动切换 Float32 以使预览可用

### Requirement: 同一Unity实例必须只有一个受控预览运行

同一 Unity 实例 MUST只有一个受控场景预览请求拥有启动、暂停、重建和停止权限。各作者窗口 MUST共享该次运行并保存自己的选择、观察目标和 interest。第二次启动 MUST显示已有运行；窗口切页、折叠和关闭 MUST只改变本地视图，不隐式终止预览。未由预览请求启动的 Play MUST只能经原有 Live 入口观察，不得被自动接管。

#### Scenario: 从Timeline下钻TreeClip

- **WHEN** 作者在运行中从 Timeline 打开 TreeClip Graph 并关闭 Timeline 窗口
- **THEN** 同一次预览 MUST继续执行
- **AND** Graph 窗口 MUST观察原正式 Session，不创建或接管动画时钟

#### Scenario: 外部Play已经运行

- **WHEN** Unity 已运行其它任务启动的场景而作者尝试开始预览
- **THEN** 预览 MUST拒绝抢占并显示已有运行
- **AND** MUST不停止、换场景或修改外部运行状态

### Requirement: 启动与退出必须恢复完整编辑环境

预览 MUST保存和恢复原有编辑场景布局及启动场景设置，包括场景加载状态、顺序和 active scene。未保存场景 MUST经过 Unity 正式保存流程，取消时 MUST取消启动。跨 Domain Reload MUST只恢复请求与稳定定位信息，重新等待真实 Session；结束、启动失败或请求失效 MUST通过同一生命周期释放受控运行并恢复编辑环境，不自动重试。

#### Scenario: 多场景编辑后结束预览

- **WHEN** 作者在多个场景加载的编辑环境中启动并结束独立预览
- **THEN** 原编辑场景的加载布局与 active scene MUST恢复
- **AND** 已合法保存的作者参数 MUST保留

#### Scenario: 用户取消保存

- **WHEN** 启动前 Unity 保存流程被取消
- **THEN** 系统 MUST不进入 Play、不更换场景、不留下待执行启动请求

#### Scenario: 进入Play发生Domain Reload

- **WHEN** 脚本域重载使编辑器和窗口对象重新创建
- **THEN** 预览 MUST按请求 identity 和场景 identity 重新等待正式登记
- **AND** MUST不恢复旧 Runtime 对象或重放已经消费的命令

### Requirement: 预览动作必须通过正式输入和准入产生

作者操作 MUST经所选角色的明确输入映射进入正式输入链，同一角色同一阶段 MUST保持唯一输入 owner。动作能否开始、进入哪个状态、产生什么 Timeline 或表现请求 MUST由正式业务决定。缺少输入映射、目标或准入条件时 MUST显示真实原因，不得直接设置 Action winner、当前 State、Grounded、动画 Selection 或相机效果。

#### Scenario: 动作请求被拒绝

- **WHEN** 作者请求攻击但正式 Action admission 拒绝该请求
- **THEN** 预览 MUST显示拒绝结果并保持正式运行状态
- **AND** MUST不绕过条件播放选中的攻击 Clip

#### Scenario: 查看Locomotion变化

- **WHEN** 作者通过正式移动输入让角色加速
- **THEN** PoseState、Blend Space 与 Motion Matching 查询 MUST读取实际提交的角色事实
- **AND** MUST不由预览窗口直接覆盖速度 Fact 或查询结果

### Requirement: 暂停和重建试验必须保持正式生命周期

暂停与继续 MUST使用真实 Play 暂停状态和正式调度。正式产物仍匹配时，重建试验 MUST在同一次 Play 中完整结束原预览场景 Session 并重新加载所选场景，从保存的初始条件建立新 Session；旧输入、角色、物理、Action、表现、事件和观察绑定 MUST按正式 owner 释放。新场景 generation 产生前 MUST不接受对旧目标的命令，失败 MUST停止受控预览并报告原因。调参后正式构造检查要求重新发布时 MUST显示构建并重启，不得使用旧产物加补丁绕过校验。

#### Scenario: 攻击中途重建试验

- **WHEN** 角色仍有攻击、动画淡出和相机状态时作者点击重建试验
- **THEN** 旧 Session 与场景状态 MUST完整结束，新场景 MUST按配置重新建立
- **AND** MUST不只清空游标或移动角色到出生点

#### Scenario: 暂停期间修改参数

- **WHEN** 作者在暂停期间修改一个合法运行参数
- **THEN** 作者数据 MUST正常保存且显示待生效
- **AND** 窗口绘制 MUST不主动执行额外逻辑或表现帧

#### Scenario: 调参后重建要求重新发布

- **WHEN** 作者要求重建试验而正式产物检查发现作者数据尚未发布
- **THEN** 系统 MUST显示需要明确构建并重启
- **AND** MUST不拿旧产物创建新 Actor 后补参数跳过构造验证

#### Scenario: 暂停中重建试验

- **WHEN** 作者在暂停状态点击重建试验且产物仍匹配
- **THEN** 系统 MUST通过受控的正常运行阶段完成加载和准备，并在完成后恢复暂停
- **AND** 准备期间 MUST不接收试验输入或由 Editor 直接调用业务帧

### Requirement: 运行中调参必须直接保存正式作者数据

运行中作者参数修改 MUST通过现有共享能力、领域验证、正式 Mutation 与 Undo 写入并保留真实作者资产。系统 MUST不创建预览试用资产、作者值镜像或第二个保存入口。运行观察字段 MUST保持只读，运行状态 MUST不反写成作者默认值。

#### Scenario: 调参后退出Play

- **WHEN** 作者合法修改混合时长并退出预览
- **THEN** 正式作者参数 MUST保留修改且能够通过正式 Undo 撤回
- **AND** 本轮位置、当前状态和活动 Action MUST不被保存为作者数据

#### Scenario: 作者修改验证失败

- **WHEN** 输入值违反领域值域或引用规则
- **THEN** Mutation MUST拒绝修改并显示具体原因
- **AND** MUST不向运行实例提交该非法候选

### Requirement: 作者保存与运行采用必须分别确认

只有领域明确支持的参数 MUST能够更新当前精确 Actor，保持原有生效时机与原子更新规则。作者修改成功后，系统 MUST分别显示待生效、已采用或运行应用失败；已采用 MUST以运行端确认而不是提交成功为依据。运行拒绝候选时 MUST保留作者修改和上一份正式运行参数，不自动回退作者资产。Undo/Redo MUST通过同一规则提交新候选。

#### Scenario: 参数在下一次激活生效

- **WHEN** 作者修改一个只允许下一次激活生效的参数
- **THEN** UI MUST显示作者已修改且运行待激活
- **AND** 只有正式激活并确认采用后 MUST显示已生效

#### Scenario: 运行应用失败

- **WHEN** 作者 Mutation 成功但运行实例拒绝候选
- **THEN** 作者修改 MUST保留，运行 MUST保持上一份已提交参数
- **AND** UI MUST同时显示保存状态、失败原因与实际采用状态

#### Scenario: 两个角色引用共享Profile

- **WHEN** 作者为明确选中的角色 A 调整共享 Profile 字段
- **THEN** 资产默认值 MUST更新，运行候选 MUST只提交到角色 A
- **AND** 角色 B MUST不被暗中更新，其运行采用状态 MUST独立显示

### Requirement: 结构变化必须经过明确构建和重新启动

图结构、资源装配或参数布局变化 MUST保持正式构建要求。普通开始预览、参数修改、选择和窗口刷新 MUST不自动构建。需要构建的字段 MUST在 Edit Mode 编辑；外部修改或 Undo 导致运行拓扑过期时 MUST使预览失效。明确的构建并开始／重启操作 MUST在 Edit Mode 针对精确 Definition 和 Target 构建，成功后才进入预览，失败保持编辑状态。

#### Scenario: 产物过期时开始预览

- **WHEN** 作者点击开始而所选角色的已发布产物过期
- **THEN** 系统 MUST显示缺失或过期的精确目标及构建入口
- **AND** MUST不使用旧产物、临时编译产物或偷偷启动 Build

#### Scenario: 构建并重启失败

- **WHEN** 作者明确要求构建并重启而正式 Build 失败
- **THEN** 系统 MUST保留正式失败诊断并停留 Edit Mode
- **AND** MUST不进入另一场景或使用其它 Numeric Target

#### Scenario: 加载后才发现产物缺失

- **WHEN** 实际场景登记后才能确定某个正式角色缺少产物
- **THEN** 系统 MUST结束受控 Play 并在 Edit Mode 显示该精确目标的构建入口
- **AND** MUST不把窗口当前文档猜作所有角色的构建目标

### Requirement: 编辑游标与历史浏览必须不改变真实运行

编辑游标 MUST只定位作者内容；Capture 历史位置 MUST只选择既有历史事实；实时播放标记 MUST来自真实运行。系统 MUST明确区分三者，不得把任意游标变化解释为运行 seek、重置、额外采样或已重建到该时刻。本次完整角色预览 MUST不提供未实现状态恢复的任意时间跳转。

#### Scenario: 查看较早的Capture帧

- **WHEN** 作者浏览历史攻击帧而当前角色仍在运行
- **THEN** 视图 MUST标明历史观察，当前角色 MUST继续按原正式调度执行
- **AND** MUST不重新触发该帧的攻击、伤害、动画或相机事件

### Requirement: 预览等待和失败必须显示真实阶段

系统 MUST分别报告配置检查、数据编译、分析产物生成或复用、产物发布、进入 Play、Session 准备、目标连接和试验重建的实际状态与耗时。没有测量值 MUST显示未测量。编译轮询等待、C# 重载和场景启动 MUST不合并伪装成图编译耗时；失败 MUST定位实际阶段，不自动重跑构建或更改 Unity 重载设置。

#### Scenario: 分析生成与数据构建分别计时

- **WHEN** 作者通过明确的正式操作生成分析产物并构建角色数据
- **THEN** 输出 MUST区分已经实际执行的分析生成、图与表现数据编译及资产发布耗时
- **AND** 后续复用分析时 MUST显示复用状态而不是报告一次未发生的生成

### Requirement: 迁移后完整角色预览必须只有场景运行入口

迁移 MUST删除旧完整角色预览的窗口播放器、独立 Fact/Action/Query 输入、私有预览场景、时间推进、目标接管和仅供这些路径使用的资源。全部正式作者入口 MUST接入同一场景运行操作，不能保留兼容开关或重命名后的替代播放器。原生素材、曲线和几何编辑 MUST继续使用各自正式作者工具。

#### Scenario: 从不同作者页面开始预览

- **WHEN** 作者分别从 Timeline、Pose Graph、Blend Space 或 Action Workspace 请求完整角色预览
- **THEN** 各入口 MUST使用同一场景预览合同和正式运行链
- **AND** MUST不因页面不同而选择不同的角色执行器
