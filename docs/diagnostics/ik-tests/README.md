# 脚部局部复算工具证据

本目录由现有 `CharacterFootCapturedContactTests` 读写：读取保存的接触输入，调用脚部接触目标生成/消费链，再输出局部复算解释页。输入和结果属于脚部诊断，统一从[脚部入口](../foot-placement/README.md)查找。

| 文件 | 用途 |
| --- | --- |
| [接触输入](expired-contact-input.json) | 从指定脚部采样中保存的帧、查询与来源哈希 |
| [接触复算页](expired-contact.html) | 过期接触目标的局部复算说明与结果 |

此处是工具固定的输入/输出位置；分类不修改已有测试代码或执行方式。局部复算只建立对应输入及调用范围内的事实，实际回放、画面与完整角色验收仍由正式运行证据说明。整体调查见[台阶连续性解释器](../foot-placement/ik-stair-continuity-explainer-20260930.html)。
