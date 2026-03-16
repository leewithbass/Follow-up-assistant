# FloatBrowser (.NET 8 WPF)

FloatBrowser 是一个 Windows 10/11 x64 的轻量悬浮浏览器，基于 **C# + .NET 8 + WPF + WebView2**，支持单标签浏览、置顶、无边框、全局热键、鼠标侧键控制、书签与本地 JSON 配置持久化。

## 依赖

1. Windows 10/11 x64
2. .NET 8 SDK
3. WebView2 Runtime（通常系统已安装）

## 运行

```bash
dotnet restore FloatBrowser.sln
dotnet build FloatBrowser.sln -c Release -p:Platform=x64
```

在 Visual Studio 2022 中打开 `FloatBrowser.sln`，将 `FloatBrowser.App` 设为启动项目后运行。

## 目录

- `src/FloatBrowser.App`：WPF 应用
- `tests/FloatBrowser.Tests`：单元测试

## 配置与数据

- 配置：`%LocalAppData%/FloatBrowser/settings.json`
- 书签：`%LocalAppData%/FloatBrowser/bookmarks.json`
- 日志：`%LocalAppData%/FloatBrowser/floatbrowser.log`
- WebView2 用户数据：`%LocalAppData%/FloatBrowser/WebView2UserData`

## 默认配置

见 `src/FloatBrowser.App/Config/default.settings.json`。
