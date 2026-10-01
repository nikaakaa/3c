# 相机进展与证据入口

整理日期：2026-10-01。完整可琳相机复刻仍未完成。当前已修正构图历史逐帧往返与 Pitch 限位符号翻转，Zoom／Stretch 的当帧参数已进入 Delay；默认轨道已分别消费 Follow／Aim 偏移，Stretch 之后统一求瞄准方向。手动上下输入已在轨道比例入口转换方向，可琳碰撞开关已启用并完成已有遮挡墙的缩回／恢复检查，修前失败见本次检查记录。用户反馈普通 freelook 仍晃动，正在对照原作输入、跟随及实际 Rig 输出，尚未闭环。前批生产函数检查和 Unity 编译通过，20,000 帧求值链分配检查为 0 字节；实机画面与完整帧 GC 尚未验收。

本轮已复现并修正环境层在无遮挡时额外平滑世界位置的问题：转镜最大偏差由约 0.449 米降至浮点误差范围，普通跟随与距离变化不再被碰撞层拖慢；碰撞后的距离沿当前镜头射线按缩短比例恢复，同时推镜不会产生零半径计划。540 帧无遮挡、183 帧墙体恢复与四组上下方向检查通过，环境求解及 Unity 查询 20,000 帧分配为 0 字节。原作两阶段模块执行及五个模块注册顺序见[流水线与输入方向](../参考/ZZZ相机流水线与输入方向.md)。修后连续实机场景采样未完成，这项修正不代表全部晃动与完整复刻已验收。

继续核对 ZZZ 战斗碰撞消费者后，本批修复球扫重复扣半径、旧位置重叠误判本帧无解，以及穿墙候选被标为安全的问题。四组修前／修后物理复现、180 帧恢复、过滤和四组上下方向检查通过，环境求解与 Unity 查询累计 20,000 帧分配为 0 字节。当前固定保护球、缩短比例恢复和特殊过滤尚未完成原作对齐；碰撞与完整相机仍不能标为全部完成，详细结果见[检查记录](../测试/可琳相机构图与仰角检查.md)。

## 当前读取顺序

1. [当前求值链](../路径/可琳相机求值链.md)与[本次检查](../测试/可琳相机构图与仰角检查.md)：当前输入、处理顺序、输出及本次已确认的修正。
2. [实施记录](../diagnostics/camera/corin-camera-implementation-20260929.md)：此前各批次修正和历史验证范围。
3. [完整链路审计](../diagnostics/camera/corin-camera-completion-audit-20260929.md)：区分 Profile 常驻资源、静态引用和真正进入求值器的资源；这是审计时点的快照。
4. [震动时钟与升降资格](../diagnostics/camera/corin-camera-clock-and-vertical-20260929.md)：原生时钟、取消和资格证据。
5. [震动原生消费者依据](../reference/camera/corin-camera-shake-source-parity-20260928.md)：信号、空间衰减、仲裁与取证边界。

## 未闭环范围与职责

Follow/Aim 的实际绑定点与原作 AvatarHeight/CameraBaseRoot 身份、Delay 原生模式资格和旋转通道、震动静默/保持/取消的业务来源，以及 Zoom/Stretch 完整生命周期仍以最新实施和审计为准。A 类命中震动需要正式攻击查询、命中结果及相机请求生产者，不能由相机自行推断命中。支援、反击、切人和演出镜头还依赖相应上层业务入口。

相机实施由 [ZZZ Camera 重建](../../openspec/changes/rebuild-character-camera-from-zzz/proposal.md)拥有；正式合同见[相机管线](../../openspec/specs/character-camera-pipeline/spec.md)、[作者入口](../../openspec/specs/character-camera-authoring/spec.md)和[源数据对齐](../../openspec/specs/character-camera-source-parity/spec.md)。

早期接入、消费初查与分工证据移至[相机阶段记录](../archive/records/camera/)。实施报告、基础取证与震动取证统一从[相机诊断分类](../diagnostics/camera/README.md)读取；原始证据包内部内容未改。记录里的历史编译成功不代替当前画面、GC 或完整业务验收。
