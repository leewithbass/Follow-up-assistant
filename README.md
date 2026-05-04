# FloatBrowser

FloatBrowser 是一款轻量级的 Windows 桌面悬浮浏览器，基于 `C# + .NET 8 + WPF + WebView2` 构建。

专为游戏、直播、多任务处理或监控网页内容时快速侧边浏览而设计。

## 技术栈

- `C#`
- `.NET 8`
- `WPF`
- `Microsoft.Web.WebView2`

## 环境要求

- Windows 10/11 x64
- `.NET 8 SDK`
- WebView2 Runtime

## 功能特性

- 基于 WebView2 的单窗口浏览器
- 扩展系统菜单：
  - 打开 URL
  - 主页
  - 后退 / 前进
  - 刷新 / 停止
  - 添加书签 / 书签列表
  - 切换窗口置顶
  - 快速设置透明度 (100/90/80/70)
  - 设置
- 全局热键（可自定义）
- 本地 JSON 持久化存储（设置与书签）

## 主页行为

- 默认主页 URL 为 `app://bookmarks`
- 启动时自动打开书签主页
- 书签主页支持：
  - 删除模式
  - 全选
  - 删除选中

## 书签管理

- 从当前页面添加书签
- 打开书签
- 删除书签
- 书签窗口中支持多选删除

## 数据存储位置

运行时数据保存在：

`%LocalAppData%\FloatBrowser`

主要文件：

- `settings.json` — 设置
- `bookmarks.json` — 书签
- `floatbrowser.log` — 日志
- `WebView2UserData/` — WebView2 用户数据

## 构建与发布

### 推荐方式（构建输出到根目录 publish）

在仓库根目录运行：

```powershell
.\build.cmd
```

默认构建输出目录：

`.\publish`

可执行文件：

`.\publish\FloatBrowser.App.exe`

### 手动发布

```powershell
dotnet publish .\src\FloatBrowser.App\FloatBrowser.App.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=false -o .\publish
```

## 打包

可以将构建输出打包为 zip，例如：

`.\dist\FloatBrowser-vX.Y.Z-win-x64.zip`

## 说明

- 这是 Windows 桌面应用，无法在 WSL 中作为 Linux GUI 程序直接运行。
- 用户设置文件 `%LocalAppData%\FloatBrowser\settings.json` 优先级高于默认配置。

## 许可证

参见仓库根目录 `LICENSE`。