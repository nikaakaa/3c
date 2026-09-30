# 落地残差末尾跳变修正

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 可复核证据

仍使用 c75d 历史采样；它早于 f37f8b4df，不用于证明控制权修复后的效果。该包 159 条脚记录执行过残差完成容差清零。用记录的衰减前残差、实际半衰期和 presentation delta 还原自然衰减，70 条被额外清掉超过 5mm，最大约 9.97mm。

E Attack_Branch_Walk 右脚 Completion 4185→4186：Body Y 均为 2.20650148，PlantFilteredPoint Y 均为 2.164，没有目标换代或末端补高；衰减前残差 Y=0.0118372664，dt=0.007535575，halfLife=0.03。自然衰减后应约为 0.009945737，但代码因小于 0.01 完成容差直接归零。最终踝 Y 从 2.2567637 降到 2.24492645，约 11.84mm；其中约 9.95mm 来自额外归零。这是当前链路中可见的小跳来源，不等于全部踏地抖动都由此产生。

## 历史边界

2743ef7fd 将同事件支撑和 Plant 世界残差连续接管统一，使用 LandingLockCompletionTolerance 同时判断完成和残差归零。8fbf1d32a 撤回破坏世界脚锁的相对修正候选后保留了此逻辑。本轮保留世界锚点、世界残差、响应衰减、正式 LockMode/Weight 及完成资格，未恢复全局末端限速或 Contact 硬清零实验。

## 修改

EvaluatePlant 将“允许 Landing 完成”和“数值残差归零”分开。Landing 完成继续使用现有 1cm 容差及 LockWeight、Event、Reach 条件；残差按原 halfLife 继续衰减，仅在现有 GeometryEpsilon=0.0001m 范围内归零。

这样不会为完成状态增加一个等待到 0.1mm 的门槛；剩余毫米级修正仍会继续消退。业务取舍是保留一小段逐渐消失的细小运动，以替代单帧吸附；没有把大高度差的下降时间拉长。目标切换、离地及硬约束继续沿既有交接处理，不能据此保证所有速度连续。

采样字段正式重命名为 PlantWorldResidualZeroTolerance / PlantWorldResidualClearedAtZeroTolerance，删除旧 CompletionTolerance 命名，避免把状态完成条件和数值清理混为一谈。历史采样不改写。没有新增配置或查询；代码修改只涉及数值运算及已有值类型字段，未测量运行时分配。没有新增测试或运行回放。

## 基本检查

ThirdPersonClient.Editor.csproj 编译通过，0 错误、91 警告；使用规定的禁用构建服务器参数，结束后已关闭构建服务器。日志为 tmp/foot-residual-tail-build.log。本轮三个源码文件 diff 检查通过。

编译后目标 Editor 的 MCP 会话短暂未注册，随后恢复。已核对目标工程路径、Edit 模式、未编译且未刷新；统一采样布局已加载两个 zero-tolerance 新字段，旧 completion-tolerance 字段未出现，Console 错误为 0。未运行回放或 Play 验收，视觉改善由用户手测确认。

## 尚未处理

真实台阶跨边后才发现的补高、不可达腿导致最终脚姿态偏移、停止后蹲姿恢复以及残差刚捕获时的速度交接未在本轮修改。不能用这项毫米级末尾修正代替上述问题，也不能在没有新采样时宣布总体平滑通过。
