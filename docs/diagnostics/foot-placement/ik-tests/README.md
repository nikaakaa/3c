# 脚部局部复算工具证据

本目录由 `CharacterFootCapturedContactTests` 读写：读取保存的连续接触输入，调用脚部接触目标生成、插值与输出链，自动更新对应 HTML 的逐帧结果及通过或失败状态。输入和结果属于脚部诊断，统一从[脚部入口](../README.md)查找。

| 文件 | 用途 |
| --- | --- |
| [接触输入](expired-contact-input.json) | 从指定脚部采样中保存的帧、查询与来源哈希 |
| [接触复算页](expired-contact.html) | 过期接触目标的局部复算说明与结果 |

此处是测试固定的输入与输出位置，测试代码已同步使用本目录。运行入口为 Unity EditMode 的 `CharacterFootCapturedContactTests.CapturedContactToSwingReleasesExpiredTargetWithoutLosingValidSupport`；场景需为 `GameplayLabFixed`。局部复算只建立对应输入及调用范围内的事实，实际回放、画面与完整角色验收仍由正式运行证据说明。整体调查见[台阶连续性解释器](../ik-stair-continuity-explainer-20260930.html)。
