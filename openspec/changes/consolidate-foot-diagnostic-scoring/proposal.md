# Change: 合并Foot重复质量诊断并发布浅层加权评分

## Why

现有诊断把最终质量、原动画问题、阶段原因和合同一致性一起计Health，同一次接触跳变会同时出现在Swing进入Landing、Plant、Contact Acquisition和Floor交接中。文件平均分会重复扣分，也会被大量100分的合同检查稀释。用户已确认需要7维加权摘要，并明确总分仅作浅显参考，不代表视觉通过。

## What Changes

- 将最终质量限制为7个唯一计分Target，其余独立事实和阶段诊断作为Evidence保留，不参与总分。
- 合并Heel/Toe最终穿透；分离原动画与Foot新增/加重穿透证据。接触间隙与持续离面合并为一个质量Target。
- 普通Swing、Path修订和Contact/Release输出采用同一最终物理偏移定义与互斥帧对归属；阶段原因不重复计分。
- 唯一Publisher同时发布逐项诊断和`quality-score.json`；不存在第二采样、第二Analyzer或旧格式兼容读取。
- 版本升级，保留原始历史包和旧规则结果；不同评分版本不得作为行为改善依据。

## Impact

- 只修改Editor Diagnostics及直接评分合同，不改Foot Runtime、Profile、Gameplay Tick或Unity状态。
- 与current Foot spec的只读正式结果合同一致。active `stabilize-character-foot-path-and-landing`的6.14与Decision9原本禁止全Foot总分，本次用户授权替换该限制；只同步这两处评分表述及直接关联评分spec，不改算法/ZZZ映射。
- 不新增测试文件；复用原始样本副本离线执行同一Analyzer/Publisher，构建并严格校验。
