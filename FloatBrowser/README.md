# FloatBrowser

FloatBrowser 是一个基于 `C# + .NET 8 + WPF + WebView2` 的 Windows 桌面悬浮浏览器。

它面向 Win10/Win11 桌面场景，提供一个轻量、可置顶、可调透明度的单窗口浏览器，适合在游戏、直播、挂机、多任务处理等场景下作为辅助窗口使用。

当前版本重点特性：

- 单窗口网页浏览
- 标题栏系统菜单扩展
- 置顶显示
- 透明度调节
- 主页快速跳转
- 书签保存与管理
- 全局键盘快捷键
- 本地 JSON 配置持久化

## 技术栈

- `C#`
- `.NET 8`
- `WPF`
- `Microsoft.Web.WebView2`

## 运行环境

需要以下环境：

- Windows 10/11 x64
- `.NET 8 SDK`
- `WebView2 Runtime`
- Visual Studio 2022 或支持 .NET 8 WPF 的构建环境

说明：

- 这是 Windows 原生桌面程序，不能直接在 Linux/WSL 图形环境里运行。
- 可以在 WSL 中管理代码，但必须在 Windows 侧构建和运行。

## 项目结构

- `FloatBrowser.sln`
  解决方案文件
- `src/FloatBrowser.App`
  WPF 主程序
- `src/FloatBrowser.App/Config`
  默认配置与常量
- `src/FloatBrowser.App/Services`
  浏览器、设置、书签、窗口状态、全局热键等服务
- `src/FloatBrowser.App/ViewModels`
  界面 ViewModel
- `src/FloatBrowser.App/Views`
  设置窗口、书签窗口、地址输入窗口

## 当前界面与交互

当前版本已经做了简化，主界面不再保留顶部工具栏。

主窗口特点：

- 使用标准 Windows 有边框窗口
- 主界面仅显示网页内容
- 右上角可显示轻量状态提示
- 窗口支持正常拖动、缩放、最小化、最大化、关闭

右键标题栏系统菜单中扩展了应用功能项，包括：

- 打开地址
- 主页
- 后退
- 前进
- 刷新
- 停止
- 添加书签
- 书签列表
- 置顶
- 透明度切换
- 设置

## 功能说明

### 1. 浏览功能

支持基本网页浏览：

- 打开 URL
- 后退
- 前进
- 刷新
- 停止加载
- 跳转主页

主页默认值为：

`http://www.bilibili.com`

### 2. 书签功能

支持将当前页面保存为书签，并在书签列表窗口中进行管理：

- 添加书签
- 打开书签
- 删除书签

### 3. 置顶与透明度

可通过系统菜单控制窗口：

- 置顶开关
- 透明度 100%
- 透明度 90%
- 透明度 80%
- 透明度 70%

这适合“浏览器悬浮在其他程序上方”的使用方式。

### 4. 设置项

当前设置窗口支持以下内容：

- 后退快捷键
- 前进快捷键
- 刷新快捷键
- 显示/隐藏窗口快捷键
- 主页快捷键
- 播放/暂停快捷键
- 主页 URL

说明：

- 鼠标侧键控制功能已经从应用内移除。
- 如果你需要用鼠标侧键控制浏览器，建议直接在鼠标驱动软件中配置。

### 5. 全局快捷键

默认全局快捷键如下：

- 后退：`Alt+Left`
- 前进：`Alt+Right`
- 刷新：`Ctrl+Alt+R`
- 显示/隐藏窗口：`Ctrl+Alt+Space`
- 主页：`Ctrl+Alt+H`
- 播放/暂停媒体：`MediaPlayPause`

这些快捷键可在设置窗口中修改并保存。

## 配置与数据文件

程序数据默认保存在：

`%LocalAppData%\FloatBrowser`

主要文件：

- `settings.json`
  用户设置
- `bookmarks.json`
  书签数据
- `floatbrowser.log`
  运行日志
- `WebView2UserData`
  WebView2 用户数据目录

## 默认配置

默认配置文件位于：

`src/FloatBrowser.App/Config/default.settings.json`

当前默认值：

- 窗口大小：`960 x 600`
- 置顶：`true`
- 透明度：`1.0`
- 主页：`http://www.bilibili.com`

注意：

- 实际运行时优先读取 `%LocalAppData%\FloatBrowser\settings.json`
- 修改默认配置不会自动覆盖已有用户配置
- 如果需要恢复默认值，请在设置窗口中执行“恢复默认”

## 构建方式

### 方式一：使用 `dotnet`

在 Windows PowerShell 中进入项目目录：

```powershell
cd \\wsl$\Ubuntu\home\leewi\Follow-up-assistant\FloatBrowser
dotnet restore FloatBrowser.sln
dotnet build FloatBrowser.sln -c Release
```

说明：

- 不要对 `.sln` 使用 `-p:Platform=x64`
- 当前解决方案配置是 `Release|Any CPU`
- 项目本身已经在 `csproj` 中指定 x64 目标

### 方式二：使用 Visual Studio 2022

直接打开：

`FloatBrowser.sln`

然后：

- 将 `FloatBrowser.App` 设为启动项目
- 选择 `Release` 或 `Debug`
- 运行即可

## 运行方式

编译完成后可直接运行：

```powershell
.\src\FloatBrowser.App\bin\Release\net8.0-windows10.0.19041.0\FloatBrowser.App.exe
```

如果你在构建时遇到文件占用错误，通常是因为程序还在运行。先关闭旧进程再重新构建：

```powershell
Get-Process FloatBrowser.App -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet build FloatBrowser.sln -c Release
```

## WSL 下的开发方式

推荐工作流：

1. 在 WSL 中编辑代码、管理 Git
2. 在 Windows 中执行 `dotnet build`
3. 在 Windows 中启动程序测试

适用原因：

- WPF 程序只能在 Windows 上运行
- WebView2 也依赖 Windows 环境

## 已知限制

- 当前只支持单窗口、单浏览视图
- 应用内不再提供鼠标侧键控制
- 某些网站对宿主控制命令的响应可能与普通浏览器略有差异
- 这是桌面辅助浏览器，不是完整替代型浏览器

## 常见问题

### 1. 为什么在 WSL 里不能直接运行？

因为这是 `WPF + WebView2` Windows 桌面程序，不是跨平台 GUI 应用。

### 2. 为什么设置改了以后没有立即生效？

当前设置保存后会重新加载热键配置。若仍感觉没有生效，建议关闭程序后重新启动再验证。

### 3. 为什么修改了默认主页，但程序还是打开旧页面？

因为用户配置优先级高于默认配置。已有用户会继续读取：

`%LocalAppData%\FloatBrowser\settings.json`

你可以：

- 在设置里手动修改主页并保存
- 或使用“恢复默认”

## 开发说明

当前仓库已经做过以下方向的调整：

- 修复多处缺失 `using` 导致的编译问题
- 修正设置保存链路
- 将主页默认值改为 `http://www.bilibili.com`
- 移除应用内鼠标侧键控制
- 简化界面，移除顶部工具栏
- 将主要功能放入标题栏系统菜单

## 许可证

本项目使用仓库根目录中的 `LICENSE`。
