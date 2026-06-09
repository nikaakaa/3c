## 1. 准备与边界确认
- [x] 1.1 读取 `proposal.md`、`design.md` 和本任务清单，确认只实现纯 HFSM Core。
- [x] 1.2 读取 `Assets/Scripts/FSM/Core/HFSM.cs`，确认当前文件内容和命名空间状态。
- [x] 1.3 读取 `Assets/Scripts/FSM/Core/SimpleHFSM.cs`，确认新实现不会破坏旧 demo 编译。
- [x] 1.4 搜索 `SimpleHFSM`、`HFSMBuilder`、`State<` 的引用，记录需要保持不动的旧路径。
- [x] 1.5 若发现必须修改角色移动、动画、相机或 BBB 路径才能实现，停止并回到 OpenSpec。

## 2. 定义核心类型
- [x] 2.1 在 `HFSM.cs` 中建立独立命名空间，避免和 `SimpleHFSM` 类型冲突。
- [x] 2.2 定义状态基类，暴露只读 `Name`、`Parent`、状态路径相关查询。
- [x] 2.3 定义组合状态类型，内部维护子状态、初始子状态、历史策略和 transition 列表。
- [x] 2.4 定义叶子状态类型，只承载生命周期回调，不允许添加子状态。
- [x] 2.5 定义 transition 类型，包含 from、to、guard、priority、注册顺序、reason、payload 和 action。
- [x] 2.6 定义转移请求与转移结果类型，区分成功、guard 不满足、CanExit 拒绝、CanEnter 拒绝和非法 target。

## 3. StateMachine 运行入口
- [x] 3.1 定义 `StateMachine<TContext>` 或等价运行时入口。
- [x] 3.2 实现 Initialize/Start，进入 root 到默认叶子的完整路径。
- [x] 3.3 实现 Stop/Dispose 或等价退出入口，按叶子到 root 顺序退出当前路径。
- [x] 3.4 暴露 `CurrentLeaf`、`CurrentPath`、`PreviousPath`、`LastTransition`。
- [x] 3.5 暴露状态变化和转移拒绝事件，供调试和后续角色层监听。

## 4. 路径与 LCA 跨层转移
- [x] 4.1 实现从任意状态计算 root 到该状态的路径。
- [x] 4.2 实现最近公共父节点查找。
- [x] 4.3 实现跨层转移时只退出当前路径到 LCA 之下的状态。
- [x] 4.4 实现跨层转移时只进入 target 路径中 LCA 之下的状态。
- [x] 4.5 实现进入组合状态时自动补全到 initial 或 history 叶子。
- [x] 4.6 验证 `Root/A/B/C -> Root/A/D/E` 不会退出 `Root/A`。

## 5. Transition 解析规则
- [x] 5.1 实现从当前叶子向祖先逐层收集可用 transition。
- [x] 5.2 实现 guard 检查。
- [x] 5.3 实现 priority 高者优先。
- [x] 5.4 实现 priority 相同时更深作用域优先。
- [x] 5.5 实现 priority 和作用域相同时注册顺序优先。
- [x] 5.6 实现 transition action 在路径切换的明确时机执行。

## 6. CanEnter / CanExit 与重入保护
- [x] 6.1 为状态提供 `CanEnter` 和 `CanExit` 钩子。
- [x] 6.2 在执行路径退出前检查当前路径需要退出的状态是否允许退出。
- [x] 6.3 在执行路径进入前检查目标路径需要进入的状态是否允许进入。
- [x] 6.4 当转移被拒绝时不改变当前路径。
- [x] 6.5 在 Enter/Exit/action 期间收到的新转移请求进入队列。
- [x] 6.6 当前转移完成后再处理队列中的后续请求。

## 7. History 与 Pushdown Stack
- [x] 7.1 定义组合状态的 history 策略：None、Shallow、Deep。
- [x] 7.2 退出组合状态时记录浅历史。
- [x] 7.3 退出组合状态时记录深历史。
- [x] 7.4 再次进入组合状态时按策略恢复历史，否则进入 initial。
- [x] 7.5 实现 Push，将当前路径保存到栈后切到目标状态。
- [x] 7.6 实现 Pop，退出当前路径并恢复栈顶路径。
- [x] 7.7 Pop 空栈时返回明确失败结果，不改变当前路径。

## 8. 事件队列
- [x] 8.1 定义框架级事件结构，至少包含 name/reason 和 payload。
- [x] 8.2 实现 `Send` 或等价 API，将事件加入队列。
- [x] 8.3 Tick 时按入队顺序消费事件。
- [x] 8.4 支持 transition guard 读取当前事件。
- [x] 8.5 单帧事件处理需要有最大步数保护，避免无限循环。

## 9. Builder 与构建校验
- [x] 9.1 提供最小 Builder API 创建组合状态、叶子状态、初始状态和 transition。
- [x] 9.2 校验每个有子状态的组合状态必须有 initial 或明确允许空组合。
- [x] 9.3 校验状态不能重复挂载到多处。
- [x] 9.4 校验 transition 的 from/to 都属于同一棵树。
- [x] 9.5 校验状态名称在同一父节点下唯一。
- [x] 9.6 构建失败时返回清晰异常或错误列表。

## 10. 调试与快照
- [x] 10.1 实现当前路径字符串输出，例如 `Root/Alive/Locomotion/MoveLoop`。
- [x] 10.2 实现当前状态树简要 dump。
- [x] 10.3 实现运行时快照，包含当前路径、上一条转移、history 和 push stack。
- [x] 10.4 实现从快照恢复当前路径，非法快照必须失败且不改变当前路径。
- [x] 10.5 保留必要调试信息，不主动删除现有 log。

## 11. 自动验证
- [ ] 11.1 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore -v:minimal`。（已运行但被缺失 `Temp/obj/Assembly-CSharp/project.assets.json` 阻塞）
- [x] 11.2 静态搜索确认 `HFSM.cs` 没有引用 Animancer、Cinemachine、CharacterController 或角色移动命名空间。
- [x] 11.3 静态搜索确认没有新增角色控制器、动画配置表或相机运行时覆盖路径。
- [x] 11.4 记录编译输出中的错误、警告和已知历史警告。

## 12. 最小手动验证
- [x] 12.1 构造最小 root/compose/leaf 用例，验证初始化进入默认叶子。
- [x] 12.2 验证同层 transition 能按 guard 切换。
- [x] 12.3 验证跨层 LCA 转移只退出和进入必要路径。
- [x] 12.4 验证 shallow history 恢复直接子状态。
- [x] 12.5 验证 deep history 恢复完整叶子路径。
- [x] 12.6 验证 push 到临时状态后 pop 回原路径。
- [x] 12.7 验证 CanExit/CanEnter 拒绝时当前路径不变。
- [x] 12.8 验证事件队列按顺序触发 transition。

## 13. 收尾
- [x] 13.1 确认没有绕过当前系统新增未审批业务路径。
- [x] 13.2 更新本任务清单为真实完成状态。
- [ ] 13.3 向用户说明实际改动文件。
- [ ] 13.4 向用户说明自动验证和手动验证结果。
