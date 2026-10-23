# 听见你 · 海伦娜桌宠

把盲仔搬进桌面。一个在 Windows 本地运行的海伦娜桌面宠物，带有换装、表情、部位互动、读书与甜品、视线跟随和随皮肤配色的回声波纹。

## 朋友下载：从这里开始

**[下载 Windows 免安装运行包](https://github.com/EDWARDEDISON/HelenaPet/releases/latest/download/HelenaPet-Windows.zip)** · [查看版本说明和 SHA-256 校验文件](https://github.com/EDWARDEDISON/HelenaPet/releases/latest)

1. 下载 `HelenaPet-Windows.zip`，右键压缩包 → **全部解压缩**。
2. 打开解压后的 `HelenaPet` 文件夹，双击 **`HelenaPet.exe`**，或双击 `Start.cmd` / `启动盲女.cmd`。
3. 桌宠会出现在桌面右下方附近。**保留完整文件夹**，不要只把 EXE 拖出来，也不要直接在压缩包预览中运行。

不需要安装 Python、Node.js、Git、豆包或 ChatGPT，也不需要 API Key、登录或付费。AI 只是在有电脑操作能力时协助下载；桌宠本身不依赖 AI。

适用环境：**Windows 10 / 11（建议 64 位）**，需系统可用的 .NET Framework 4.x 与 WPF。当前 EXE 使用 CLR 4，Windows 10/11 通常已具备所需组件；若提示缺少组件，请保留错误内容，再通过 Windows 官方方式修复。暂未提供 macOS、Linux、手机原生版本。

## 一句话交给 AI

> 请帮我在 Windows 10/11 电脑上，从 https://github.com/EDWARDEDISON/HelenaPet/releases/latest/download/HelenaPet-Windows.zip 下载海伦娜桌宠，完整解压到用户可写的新文件夹，保留 assets、skins 和随包文件并运行 HelenaPet.exe；无需安装 Python、Node.js 或配置 AI；如果你不能直接操作电脑，请给我下载链接和手动步骤，遇到系统安全拦截先告知我，不要关闭安全防护。

[复制版与详细说明](docs/给朋友的下载指令.md)

## Windows 首次运行提示

当前程序**没有数字签名**。SmartScreen 可能提示“Windows 已保护你的电脑”，表示系统无法仅凭发布者身份或信誉确认它；**不能据此断言有病毒，也不能据此保证安全**。

- 先确认下载来源是本仓库的 Releases，可用同页 `SHA256SUMS.txt` 校验完整性。校验和用于核对文件，不是安全认证。
- 只有你核对来源、了解风险并愿意运行时，若系统提供选项，才考虑“更多信息 → 仍要运行”。不要求关闭 Defender、添加全盘排除项或以管理员身份运行。
- 如果没有“仍要运行”，可能受到 Smart App Control 或组织策略限制。不要关闭整机防护来绕过它，先保留提示并反馈。

参考：[Microsoft 的 SmartScreen 说明](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)。

桌宠运行功能在本地完成：读取随包素材、台词和配置，将偏好保存在所在文件夹。鼠标跟随会读取光标位置。它没有 AI 对话接口，也不需要联网下载模型。

## 可以怎么玩

| 操作 | 效果 |
| --- | --- |
| 左键轻点 | 部位对话、叫醒；不同部位可配置不同反应 |
| 左键按住并移动 | 拖动桌宠 |
| 左键按住、不移动 | 摸摸头 |
| 鼠标滚轮 | 调整桌宠大小 |
| 右键轻点 | 打开操作栏 |
| 右键按住约 0.45–不足 0.9 秒 / 0.9–不足 1.6 秒 / 满 1.6 秒 | 松开后跳 1 / 2 / 3 次，落地出现回声波纹 |
| 操作栏 | 聊天、16 种表情、读书与甜品、换装、头饰、环绕挂件、视线与文字大小 |
| 任务栏托盘图标 | 找回桌宠、设置或退出；图标可能在“显示隐藏的图标”中 |

目前有 16 个皮肤选项，其中包含原皮的有刘海 / 无刘海两种发型。对话来自预置台词，不是实时 AI 生成。

## 运行包里有什么

```text
HelenaPet/
├─ HelenaPet.exe       ← 双击这个
├─ Start.cmd / 启动盲女.cmd
├─ 开始使用.txt
├─ assets/             ← 共用道具、头饰、挂件等素材
├─ skins/              ← 各套皮肤，不能删
├─ settings.json       ← 首次使用默认配置；运行后保存个人偏好
├─ interaction.json
└─ dialogue.json
```

将整个文件夹放在桌面、文档等自己有写入权限的位置。不要放在 `Program Files` 等需要管理员权限的目录。退出后可移动或删除整个文件夹，不会安装服务或添加开机启动项。

更新前先退出旧版本，解压到新文件夹；想保留偏好可备份旧的 `settings.json`、`interaction.json`，自定义台词另备份 `dialogue.json`。

找不到桌宠时先使用托盘菜单找回。若位置或显示设置异常，退出后备份并移走 `settings.json`，再次打开即可恢复默认配置。启动失败请查看同目录的 `error.log`，并附上系统版本和提示截图反馈；无需提供私人文件。

## 源码与制作资料

普通使用请选择上方的 **Windows 运行包**。GitHub 绿色 `Code → Download ZIP` 仍可用，但这是包含源码、预览视频、PSD 和制作记录的完整仓库，体积更大。完整仓库解压后，EXE 在 `HelenaPet-master/HelenaPet.exe`。

- `source/`：C# / WPF 源码；Windows PowerShell 中运行 `source/build.ps1` 可重新编译，使用系统的 .NET Framework 编译器。
- `tools/build-release.ps1`：把现有 EXE、完整素材和默认配置打成精简包，并生成 SHA-256 校验文件；不改动 EXE。
- `preview/`：实际渲染截图和功能演示。
- `Live2D准备/`：分层 PSD 与后续制作资料。
- 根目录其他制作说明和 QA 记录：开发档案，不是启动前要完成的步骤。

本项目是海伦娜相关的个人同人作品，非游戏官方应用。角色、原作设定及相关权利归各自权利人；仓库公开不等同于授予所有素材商业使用权。
