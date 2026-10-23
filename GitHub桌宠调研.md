# GitHub 桌宠与 Skill 调研

检索日期：2026-10-22。目标是 Windows 桌面上的 2D Q 版大头，支持平滑鼠标跟随、表情和以后换装。

| 候选 | 类型 | 与本次需求的关系 |
| --- | --- | --- |
| [Y1fe1-Yang/desktop-pet-skill](https://github.com/Y1fe1-Yang/desktop-pet-skill) | 制作 Skill | README 提供图片转桌宠、预设动作和 Electron 打包流程。适合快速原型；未看到自动生成精细 Live2D 绑定的能力。 |
| [Coolkidlab-Yin/windows-desktop-pet-builder](https://github.com/Coolkidlab-Yin/windows-desktop-pet-builder) | Windows 制作教程与 Skill | 面向照片宠物的 Windows 制作流程，适合参考交付和检查方法；角色大头仍需另外设计素材。 |
| [promptwhisper/sprite-pet-skill](https://github.com/promptwhisper/sprite-pet-skill) | 精灵动画制作 Skill | 包含参考图生成、动作图集、视觉检查和 Electron 工作流。适合有明确逐帧动作的方案。 |
| [shang-return/Live2d-Deskpet](https://github.com/shang-return/Live2d-Deskpet) | Windows Live2D 运行框架 | README 明确支持透明置顶、全局鼠标跟随和本地 model3.json 模型。与视频效果的技术方向接近，但必须先有制作好的 Live2D 模型。 |
| [SpacervalLam/Deskpet](https://github.com/SpacervalLam/Deskpet) | Windows Live2D 桌宠程序 | README 包含鼠标跟随、拖动、滚轮和表情菜单，可作另一种运行器参考。 |

这些是本次检索和文档检查得到的候选，并未逐个安装、运行或验证打包结果。没有把第三方 Skill 安装到你的全局技能目录。

## 为什么没有直接依赖“一键生成”

例如第一项的 [create_pet.py 源码](https://github.com/Y1fe1-Yang/desktop-pet-skill/blob/main/scripts/create_pet.py) 中，名为 `analyze_image_with_ai` 的函数目前直接返回默认信息；启动预览还使用了 `lsof`。因此不能把 README 里的一键能力直接等同于在这台 Windows 上完整可用，更不能等同于自动做出视频里的精细角色绑定。

本次选择新写一个 Windows WPF 运行器，直接使用分层素材和连续参数，先让你能实际拖动、缩放、互动和换皮肤。它不依赖上述仓库，也不把它当作已完成的 Live2D 模型。

## 视频能说明什么

从你提供的视频画面能看到缩放、闲置气泡、表情列表和参数混合界面。这些特征与 Live2D 一类的参数驱动方案一致，但仅凭画面不能确定作者具体使用了哪个框架。

连续参数负责头部朝向、轻晃和层间位移；状态机负责触发动作、表情和气泡。换皮肤主要取决于不同素材是否有一致的锚点或独立的布局配置，不需要每次改写整个桌宠程序。

## 盲女相关设计依据

官方公告确认了 Helena Adams / The Mind’s Eye 的角色对应关系：[Identity V 官方公告](https://www.identityvgame.com/en/news/announcement/20260108/35288_1280791.html)。

听觉感知、手杖与 Echo 的设定参照 [Identity V Wiki 的 Helena Adams 条目](https://identityv.fandom.com/wiki/Helena_Adams)，该来源为社区资料。原型将其转化为侧耳倾听和视觉回声波纹；具体动作和气泡短句是此次同人设计，不是官方动作或官方台词。造型以你提供的官方 Q 图和游戏四面图为主要依据。
