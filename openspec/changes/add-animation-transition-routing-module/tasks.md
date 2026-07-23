# Tasks

## 1. 建立模块边界

- [x] 1.1 确认模块可复用的stable identity与canonical hash基础类型。
- [x] 1.2 确认模块不得引用Unity动画、Animancer、Pose Graph与Character Profile类型。
- [x] 1.3 建立Transition Routing运行时目录。
- [x] 1.4 建立Transition Routing Editor Fixture目录。
- [x] 1.5 收口模块runtime与Editor依赖方向。
- [x] 1.6 禁止现有动画Runtime程序集反向引用Fixture。

## 2. 定义Blend Logic合同

- [x] 2.1 定义`AnimationTransitionBlendLogic`。
- [x] 2.2 只安装`StandardBlend`值。
- [x] 2.3 只安装`Inertialization`值。
- [x] 2.4 禁止独立`HardCut`值。
- [x] 2.5 定义零时长Standard Blend的Hard Cut outcome。
- [x] 2.6 禁止`Custom`值。
- [x] 2.7 禁止Stored Pose值。
- [x] 2.8 定义非法Blend Logic reason。

## 3. 定义endpoint与rule

- [x] 3.1 定义稳定Transition Endpoint identity。
- [x] 3.2 定义Empty endpoint。
- [x] 3.3 定义selection generation。
- [x] 3.4 定义稳定Rule identity。
- [x] 3.5 定义不可变Transition Rule。
- [x] 3.6 定义Duration合同。
- [x] 3.7 定义Blend Curve identity合同。
- [x] 3.8 定义Blend Profile identity合同。
- [x] 3.9 禁止Rule保存Unity对象引用。

## 4. 定义模块专属Definition

- [x] 4.1 定义Transition Routing Definition schema。
- [x] 4.2 定义ordered endpoint catalog。
- [x] 4.3 定义exact source-target rule table。
- [x] 4.4 定义Definition revision。
- [x] 4.5 定义显式schema version。
- [x] 4.6 禁止Definition引用Character Profile。
- [x] 4.7 禁止Definition进入Character Authoring Discovery。

## 5. 编译Routing Plan

- [x] 5.1 定义不可变Compiled Transition Routing Plan。
- [x] 5.2 编译ordered endpoint catalog。
- [x] 5.3 编译完整exact pair索引。
- [x] 5.4 计算canonical plan hash。
- [x] 5.5 拒绝重复endpoint。
- [x] 5.6 拒绝重复Rule identity。
- [x] 5.7 拒绝重复source-target pair。
- [x] 5.8 拒绝缺失pair。
- [x] 5.9 拒绝未知endpoint。
- [x] 5.10 拒绝Inertialization到Empty。
- [x] 5.11 拒绝非正Inertialization duration。
- [x] 5.12 拒绝非法Standard Blend duration。
- [x] 5.13 输出结构化compile diagnostics。

## 6. 定义Frame输入输出

- [x] 6.1 定义不可变Transition Routing Frame Input。
- [x] 6.2 定义Plan identity输入。
- [x] 6.3 定义Frame identity输入。
- [x] 6.4 定义owner node identity输入。
- [x] 6.5 定义current与requested endpoint输入。
- [x] 6.6 定义target readiness输入。
- [x] 6.7 定义capture plan readiness输入。
- [x] 6.8 定义capture completion输入。
- [x] 6.9 定义release completion输入。
- [x] 6.10 定义reset输入。
- [x] 6.11 定义不可变Frame Output。
- [x] 6.12 定义Standard Blend command输出。
- [x] 6.13 定义request输出。
- [x] 6.14 定义capture与release许可输出。
- [x] 6.15 定义结构化runtime reason输出。

## 7. 定义typed request

- [x] 7.1 定义Request Event identity。
- [x] 7.2 定义Request Generation。
- [x] 7.3 定义`PoseInertializationRequest`。
- [x] 7.4 写入owner node identity。
- [x] 7.5 写入source与target endpoint。
- [x] 7.6 写入Rule identity。
- [x] 7.7 写入selection generation。
- [x] 7.8 写入duration与Blend Profile identity。
- [x] 7.9 禁止request保存Pose。
- [x] 7.10 禁止request保存播放器handle。
- [x] 7.11 禁止request保存consumer对象。

## 8. 实现Routing状态机

- [x] 8.1 建立固定runtime workspace。
- [x] 8.2 实现Idle状态。
- [x] 8.3 实现AwaitingTarget状态。
- [x] 8.4 实现Prepared状态。
- [x] 8.5 实现AwaitingCaptureCompletion状态。
- [x] 8.6 实现Committed状态。
- [x] 8.7 实现Invalid状态。
- [x] 8.8 实现Standard Blend立即决策。
- [x] 8.9 实现零时长Hard Cut outcome。
- [x] 8.10 实现request准备。
- [x] 8.11 实现匹配generation的capture提交。
- [x] 8.12 实现capture后release许可。
- [x] 8.13 拒绝过期capture completion。
- [x] 8.14 拒绝过期release completion。

## 9. 实现连续打断

- [x] 9.1 实现Standard到Standard规则替换。
- [x] 9.2 实现Standard到Inertialization准备。
- [x] 9.3 实现Inertialization到Inertialization generation提升。
- [x] 9.4 输出rebase required。
- [x] 9.5 失效旧generation completion。
- [x] 9.6 保持单一pending request。
- [x] 9.7 实现Inertialization期间Standard命令输出。
- [x] 9.8 禁止创建第二request accumulator概念。
- [x] 9.9 实现Empty target限制。

## 10. 实现reset与plan replacement

- [x] 10.1 实现显式Reset。
- [x] 10.2 实现seek reset reason。
- [x] 10.3 实现owner generation reset。
- [x] 10.4 实现Plan replacement reset。
- [x] 10.5 清理pending request。
- [x] 10.6 清理completion等待。
- [x] 10.7 提升模块generation。
- [x] 10.8 禁止reset后输出旧release许可。

## 11. 建立snapshot

- [x] 11.1 定义模块snapshot schema。
- [x] 11.2 输出Plan identity与revision。
- [x] 11.3 输出current与requested endpoint。
- [x] 11.4 输出active Rule与Blend Logic。
- [x] 11.5 输出request event与generation。
- [x] 11.6 输出request lifecycle。
- [x] 11.7 输出capture与release状态。
- [x] 11.8 输出rebase标记。
- [x] 11.9 输出reset与invalid reason。
- [x] 11.10 建立有界事件时间线。

## 12. 建立Editor Fixture Definition

- [x] 12.1 定义Fixture资产。
- [x] 12.2 引用模块专属Routing Definition。
- [x] 12.3 定义有序Frame Fact序列。
- [x] 12.4 定义target readiness编辑项。
- [x] 12.5 定义capture readiness编辑项。
- [x] 12.6 定义capture completion编辑项。
- [x] 12.7 定义release completion编辑项。
- [x] 12.8 定义reset编辑项。
- [x] 12.9 禁止Fixture引用AnimationClip。
- [x] 12.10 禁止Fixture引用Character Profile或Pose Graph。

## 13. 建立Editor Fixture工作区

- [x] 13.1 新增独立菜单入口。
- [x] 13.2 新增Definition选择区。
- [x] 13.3 新增规则矩阵区。
- [x] 13.4 新增Frame Sequence区。
- [x] 13.5 新增当前状态区。
- [x] 13.6 新增事件时间线区。
- [x] 13.7 新增结构化诊断区。
- [x] 13.8 新增显式Compile按钮。
- [x] 13.9 新增显式Reset Runtime按钮。
- [x] 13.10 新增显式Step Frame按钮。
- [x] 13.11 新增显式Run Sequence按钮。
- [x] 13.12 新增Clear Timeline按钮。
- [x] 13.13 显示`Pose Evaluation: Not Connected`。

## 14. 禁止自动重操作

- [x] 14.1 字段修改只标记Dirty。
- [x] 14.2 选择Fixture不得自动Compile。
- [x] 14.3 打开窗口不得自动Compile。
- [x] 14.4 domain reload不得自动Compile。
- [x] 14.5 Play Mode变化不得自动Compile。
- [x] 14.6 规则修改不得自动Run。
- [x] 14.7 asset import不得自动Run。

## 15. 收口模块出口

- [x] 15.1 确认现有Character Profile没有新增引用。
- [x] 15.2 确认现有Pose Graph没有新增节点或edge。
- [x] 15.3 确认现有Projection schema没有变化。
- [x] 15.4 确认现有Player、BlendStack和Inertialization没有调用模块。
- [x] 15.5 确认模块没有Pose或Unity动画依赖。
- [x] 15.6 记录后续integration唯一允许使用的公开合同。
- [x] 15.7 同步项目文档中的“模块已存在但尚未接入”状态。

