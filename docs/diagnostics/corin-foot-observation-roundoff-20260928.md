# E 行走足部观测曲线的浮点边界异常

## 复现证据

用户报错 `AnimationFootMotionRuntimeSample` 构造失败，参数标记为 `footHeight`。原构造函数把全部字段及事件帧校验合并到一个条件，任意失败均使用该参数名，所以报错字段不可靠。

只读扫描当前 `CorinPoseNativeDomainResourceSet` 的 39 个动画来源及双脚观测曲线；常规段内采样未发现越界，进一步检查零值关键帧附近，定位到 E 行走源 `b44ef817701d48b45908aa56ec34c2a4/7400000` 的左脚 `Support`。

对应动画：`Avatar_Female_Size01_Corin_Ani_Attack_Branch_Walk_FootMotionTarget.anim`。

曲线关键帧：

- time=0，value=0.177269891，outTangent=-11.522543。
- time=0.0153846154，value=0，inTangent=-11.522543，outTangent=0。

在 normalizedTime=0.0153846135 时，Unity `AnimationCurve.Evaluate` 返回 `Support=-1.49011612E-08`。直接调用当前正式 `Left.Sample`，复现相同 `ArgumentOutOfRangeException / footHeight`。因此本轮修正的是归一化权重采样的浮点误差处理，并非改变 IK 求解。

## 修改

- Contact、LockWeight、Support 允许最多 0.000001 的浮点边界偏差，通过检查后规范到 `[0,1]`。
- 超过容差、NaN、Infinity 仍拒绝；脚高、速度和误差仍执行原非负约束。
- 分字段校验，异常包含真实参数名和值；无效事件帧单独报告。
- 不改曲线关键帧、IK 算法或参数，不重烘焙、不重编动画资源。

## 检查

Unity 编译和域重载完成，Editor 为 Edit 模式且非编译状态，Console 错误数为 0，`git diff --check` 通过。

用同一资产、同一 normalizedTime 重新调用正式 `Left.Sample`：原始曲线值仍为 -1.49011612E-08，输出 Support=0，样本有效且不再异常。单独检查 -0.001、1.001、NaN、正 Infinity，仍全部拒绝，参数正确标记为 `support`。

未运行 Play/replay，未新增测试代码；上述为原失败点的局部计算复查，完整画面表现交由用户手测。

## 移除每帧重复的资源检查

用户指出运行时检查过多后，沿实际采样链删除三处深度校验：`CharacterPoseFootMotionSource` 构造时的观测对检查、`AnimationFootStepObservationCurveSet.Sample` 的整曲线检查、`AnimationFootStepLandingEventTable.Resolve` 的事件表检查。

正式加载边界已经由 `CharacterPoseNativeSourceResourceCatalog` 构造函数对普通动画与技能动画分别调用 `plan.RequireValid()`，继续检查完整观测曲线、关键帧值域及落脚事件表。作者构造和显式校验接口也保留。

之前每帧主来源双脚要重复执行 4 次曲线集合检查，读取 40 份 `curve.keys` 数组，并执行 6 次事件表检查；现从该采样链移除。运行时仍处理当前采样数值的有限性、值域和浮点边界容差，不改变采样计算、IK 或数据资产。本轮未做性能实测，不据此声明全链路零 GC。

移除重复检查后，Unity 编译与域重载完成，Console 错误数为 0，`git diff --check` 通过。为编译已退出用户当时的 Play，未重新进入 Play 或执行 replay。
