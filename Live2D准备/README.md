# Live2D 尝试结果与 VRMA 说明

本轮完成了素材准备与格式验证，尚未把桌宠切换为正式 Live2D Cubism。现在运行的依然是自制的 2D 分层网格、表情绘制与鼠标跟随。

实际检查：在当前项目及参考目录内没有海伦娜 .moc3、.model3.json 或 .cmo3；常见安装目录与 Windows 卸载登记中也未发现 Cubism Editor。没有声称扫描了整台电脑的所有位置。原有分层贴图不能直接作为 .moc3 播放。

已将原皮两版和 14 套新皮肤导出为 16 组对齐素材：每组有 helena-layered.psd、各层透明 PNG、合成预览与来源清单。PSD 包含现有头发、脸、镜框、帽饰、嘴部等独立图层，原皮源图保持原样，高级皮肤使用当前单一光头脸底与相同的五官定位；不伪造 .cmo3 或 .moc3 文件。原皮鼓脸图层只作为历史替代素材存档，不进入当前动作菜单。

要完成正式接入，仍需在 Cubism Editor 中建立网格、变形器和参数绑定，处理图层遮挡与物理，导出 .moc3、.model3.json、纹理，再为当前 WPF 应用接入 Cubism Core 和渲染适配层。当前只是建模准备包，未完成 Editor 绑定，也没有跑通过 Cubism Core。

建议参数：ParamAngleX/Y/Z 对应头部转向和歪头；自定义 ButtonEyeX/Y 对应镜片内纽扣或花纹跟随；ParamEyeLOpen/ROpen 对应闭眼、眨眼；ParamMouthOpenY/Form 对应嘴部；HairSway 和 OrnamentSway 对应发梢与饰物。闭眼和嘴型若要完全交由 Cubism，需要继续拆出对应美术层，目前部分由代码绘制。

官方说明：[Cubism Editor 嵌入数据与导出](https://docs.live2d.com/zh-CHS/cubism-editor-manual/export-moc3-motion3-files/)列出 .moc3、.model3.json 和纹理等运行数据；[Cubism Core](https://docs.live2d.com/en/cubism-sdk-manual/cubism-core/)说明 Core 来自官方 SDK 包，未在 GitHub 公开。本轮未安装 SDK，也未将官方 Core 或示例角色打包为海伦娜。

## 好奇的 VRMA 问题

拥有兼容的 3D 人形模型后，通常可以通过支持 VRMA 的运行时复用动作；仍需标准人形骨骼映射、姿态与比例重定向，以及必要的表情映射、穿模检查。任意几根 3D 骨骼并不自动满足这些条件；只给大头添加骨骼，也无法复现缺失身体的全身动作。

[VRM Animation 官方概述](https://vrm.dev/en/vrma/)说明其针对 VRM 人形动画；[VRMC_vrm_animation 规范](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_vrm_animation-1.0/README.md)定义 humanoid 节点到人形骨骼的映射。

按你的要求，本轮没有制作 3D、跳舞或把 VRMA 硬套到现有大头。
