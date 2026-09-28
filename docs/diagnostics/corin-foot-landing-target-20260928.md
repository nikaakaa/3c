# 接触目标与包络终点修正

## 依据

沿用 65ec 包，不新增录制、不运行 replay。定位时没有恢复历史已撤回的骨盆 Reach 硬夹取，也没有更改骨盆参数或直接平滑膝盖骨骼。

1. 4098–4103 的正式 LockMode 为 Sliding、LockWeight 为 0，但脚处于 Landing。旧 ResolveContactPlant 无条件返回完整接触锚点，并将 PlantLockResponse 记为 None；只有进入 Locked 后才消费正式 Sliding。此时距离误差约 10.9cm，原有 Sliding 公式本应将水平修正减至约 8.3cm。该数字仅是旧采样输入上的公式核对，不是修改后运行结果。
2. 4469 的摆腿水平投影进度为 1，包络末尾两个顶点的 XZ 完全相同，高度分别为 3.60000038 和 3.42。旧顺序扫描提前命中高点，未读取已确认的终点，使摆腿输出继续沿高踏面、随后在 Landing 中消解残差。
3. 原来只有 Releasing 在插值前查询目标姿态的脚掌支撑。Landing/Locked 即使目标低于当前目标脚姿态对应的踏面，仍以低目标插值，再由输出保护补高，前后目标不一致。

## 运行链路修改

提交 `1c979c9dd` 将 Landing 与 Locked 合并为 ResolvePlant。Landing 使用当帧正式 LockRequest.Response，Locked 使用已有离散响应；二者共用原有完整锚定及 Sliding 距离规则。保留同一接触事件、残差交接及落地完成条件，不新增另一套锁脚策略。业务变化是正式 Sliding 在 Landing 尚未完成时也生效，允许原配置范围内的水平滑动；完整锁定请求仍维持锚点。

同一提交将原 ConstrainReleaseTarget 收口为 ConstrainStateTarget，适用于 VerifiedSupport 和 ReleaseResidual。按目标脚姿态查询已有脚跟/脚尖支撑，只有确有踏面要求更高时才提高目标；同步更新 Correction、SupportTarget 和 PlantTargetPoint，使插值与支撑保护面向同一个目标。单端支撑规则不变，不要求另一端贴地。

正式诊断属性由 ReleaseTargetSupport 改为 StateTargetSupport，旧命名不留兼容分支。历史采样和旧文档不改写。新增接触阶段的目标脚掌查询复用原预分配查询实现，不产生新的逐帧托管分配，但增加了物理查询工作量；本轮没有性能实测，不能宣称零成本。

提交 `75c0a1446` 在包络投影进度到达终点时直接取末顶点。BuildForSwing 已确认该顶点与正式预测落点一致。途中包络、接触端输出保护和正常残差历史保留。若脚掌一端仍在高踏面，现有实际脚掌支撑检查仍可抬高目标，不能仅凭中心已到终点就允许穿地。

## 验证和边界

Editor 项目构建 0 错误、91 警告，日志 `tmp/foot-landing-target-build.log`，使用规定的禁用构建服务器参数并在结束后执行 shutdown。没有新增或运行测试，没有执行 Unity batchmode。

本轮修正的是已证实的目标生成错误。它可能减少过度锚定导致的腿部拉伸及错误包络高度带入 Landing，但没有实现新的最终求解器或对所有不可达姿态作保证：完整锁定时目标仍可能超出腿长，最终求解仍可能偏离目标；膝盖自然度、脚尖穿透和残差下降需修改后的实际采样确认。

此前提到的“最终可达姿态与防穿检查完全一致”尚未实现。本轮不把两个上游修正描述成这项整体保证已经完成，也不把编译通过描述为视觉通过。

Unity 加载检查：主实例 e852139597e42532 在改动前已确认 Edit、不编译、不刷新；构建后会话可发现，但 execute_code 连续未返回状态，read_console 报 ping 未响应。没有强制刷新或重启，当前新脚本是否已加载未确认。
