# 下坡脚部高度与落地交接修正（2026-09-28）

## 本次范围

保持原动画水平运动，修正高度目标、包络高度取样和落地后的高度历史。没有新建三维脚轨迹，没有修改骨盆、FinalIK、动画资源、烘焙算法或 ACL。按用户要求不运行 replay，不新增测试。

## 修改前证据

手动采样：`20260927-162321-fbdd6347e6b84e1e92dc7d54b0ec2315`。

- 右脚 2361–2375：缓存落点 Z=10.402、踏面 Y=3.78000021。2375 的新预测 Z=10.3764982，累计输入位移 0.04867877m，未超过 0.05m，因此复用旧结果。2376 正式接触查询改用 Y=3.60000014，落地残差 Y=0.174885109m。
- 左脚 1781：原动画脚底 Y=2.45876932，预测路径高度目标 Y=2.34766841，运行时把负修正截为零；另有 0.008222683m 摆腿残差。1782 接触目标 Y=2.34000015，落地残差 Y=0.109673671m。
- 本次提取到的 23 次下坡 Swing→Landing 中，21 次末帧预测高度与接触目标高度一致。因此缓存换面不能解释全部悬脚。
- 右脚 2361–2390 的同坐标系目标踝与求解踝最大距离约 1.84e-7m。这段证据指向目标生成和交接，不能据此概括所有姿态的求解质量。

## 烘焙数据核对

`CharacterFootMotionDataBuilder.BuildStepEvidence` 使用前后落脚基线，生成 `max(0, AnimationHeight - BaselineHeight)`；`CharacterFootMotionCurveAuthoringService` 把该结果发布到正式 FootHeight 曲线。运行时消费的语义是相对原动画 Foot Path 的抬脚高度。没有发现必须重烘焙的证据，本次未重新分析资源。

## 正式运行链修改

1. `CharacterFootSwingMotionBuilder`：将当前动画脚底在 LastLanding→NextSwingLanding 水平轴上的投影作为取样位置。包络各段也按同一水平轴插值，不再用 SmoothStep 时间进度乘三维包络总长度。最终只输出沿 ComponentUp 的高度修正，动画 XZ 不由本模块重写。
2. 正式高度目标仍为 `EnvelopeHeight + FormalFootHeight`。Builder 与 Swing 插值允许它低于原动画脚高。采样位置限于既有路径范围，不延伸包络。
3. `CharacterFootStateTargetResolver`：Releasing 仍保留非负的预测修正目标。避免历史实验中有符号 SwingMotion 顺带改变释放阶段的行为。
4. `CharacterFootHardConstraintResolver`：有效 Swing 的下界由当前位置的包络负责，不再被 PreparedPlant 的只读诊断分支遮住。未来落点仍不作为摆腿硬下界。Landing/Locked 的已确认接触点下界正式执行。
5. `CharacterFootInterpolationRuntime.ApplyHardConstraint`：把本次硬约束增量同步到最终修正、上一输出与所属历史。接触阶段修正 PlantWorldResidual；摆腿阶段修正响应标量。避免最终输出被抬出踏面、下一帧历史却仍从踏面下面继续。
6. `CharacterFootLifecycle`：在两处正式硬约束出口提交上述历史更新。
7. `CharacterFootPlacementModule` / `CharacterFootLandingPrediction`：进入正式 InApproachContactToLanding 阶段时，采用既有量化输入发生变化即重查的刷新方式；滑步仍使用同一方式。提前预测阶段保持原距离缓存，正式接触继续强制校验。枚举名称由 ChangedSlidingAdmissionInput 改为 ChangedContactApproachInput，数值不变。

## 与历史失败实验的区别

- `ab259951c` 只放开负修正，连 Releasing 一起变化，且已确认接触下界仍不执行；本次限制释放阶段的变化并闭合硬约束输出历史。
- `25d80a3f2` 把未来落点当当前摆腿下界；本次使用当前脚位对应的包络，未来落点保持诊断用途。
- `89bf9a6cb` 清空落地残差导致瞬移；本次不清空正向悬空残差，只在输出将低于有效地形下界时修正，并把修正反馈到历史。

## 边界与待观察结果

这次改动仍保留原修正速度、方向响应和残差衰减。它不证明所有落地都能准时收敛。包络取样使用当前脚底在既有路径轴上的投影，不是完整鞋子扫掠，也不宣称覆盖所有转弯和侧向障碍。最终接触下界限制的是正式脚底参考点，不保证所有旋转姿态的鞋网格绝不穿透。

编译结论单独记录；无新运行采样，不把静态检查当作视觉通过。

## 编译与加载记录

- 本次出现的 `CharacterFootGroundPathResult.ComponentUp` 成员引用错误已修复：直接传入调用方已经验证的 ComponentUp，不从结果类型读取不存在的成员。
- .NET Editor 构建被其他窗口正在迁移的 TreeDesigner Editor 文件旧工程引用阻挡（CS2001）。没有修改这些文件；两次构建结束均执行 build-server shutdown。日志：`tmp/foot-height-handoff-build.log`。
- Unity 正式编译与域重载后，显式实例 `e852139597e42532` 的只读反射确认：ApplyHardConstraint 已加载、旧时间进度方法已移除、ChangedContactApproachInput 已加载；项目路径确认是 3C_Client。随后读取 Console 返回 0 条错误。
- 这证明本次运行时代码已进入 Unity 程序集，不代表动作效果通过。查询时编辑器另有一轮编译进行中，不在此期间修改代码或刷新。
