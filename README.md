# FastGithub
github加速神器，解决github打不开、用户头像无法加载、releases无法上传下载、git-clone、git-pull、git-push失败等问题。

* 原仓库 https://github.com/dotnetcore/fastgithub 已经没有了，所以拿了最新版本改了自用。

### 1 写在前面
* **fastgithub不具备“翻墙”功能,也没有相关的计划**
* **fastgithub不支持Windows7等已被发行方停止支持的操作系统，并且也不会主动提供支持**
* **fastgithub不能为您的游戏加速**
* **fastgithub没有主动在github之外的任何渠道发布**
  
### 2 部署方式
源码克隆后不会包含 `FastGithub.UI.exe`。直接使用请到 [Releases](https://github.com/creazyboyone/FastGithub/releases) 下载对应平台的完整压缩包，不要下载 Source code；自行构建见下方“源码构建”。

#### 2.1 windows-x64桌面
* 双击运行FastGithub.UI.exe 

#### 2.2 windows-x64服务 
* `fastgithub.exe start` // 以windows服务安装并启动
* `fastgithub.exe stop` // 以windows服务卸载并删除

#### 2.3 linux-x64终端
* `sudo ./fastgithub`
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`
  
#### 2.4 linux-x64服务
* `sudo ./fastgithub start` // 以systemd服务安装并启动
* `sudo ./fastgithub stop` // 以systemd服务卸载并删除
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`

#### 2.5 macOS-x64
* 双击运行fastgithub
* 安装cacert/fastgithub.cer并设置信任
* 设置系统自动代理为`http://127.0.0.1:38457`，或手动代理http/https为`127.0.0.1:38457`
* [具体配置详情](https://github.com/creazyboyone/FastGithub/blob/master/MacOSXConfig.md)

#### 2.6 docker-compose一键部署
* 准备好docker 18.09, docker-compose.
* 在源码目录下，有一个docker-compose.yaml 文件，专用于在实际项目中，临时使用github.com源码，而做的demo配置。
* 根据自己的需要更新docker-compose.yaml中的sample和build镜像即可完成拉github.com源码加速，并基于源码做后续的操作。
 
### 3 软件功能 
* 提供域名的纯净IP解析；
* 提供IP测速并选择最快的IP；
* 提供域名的tls连接自定义配置；
* google的CDN资源替换，解决大量国外网站无法加载js和css的问题；
  
### 4 证书验证
#### 4.1 git
git 操作提示 `SSL certificate problem` 时，应信任本机 FastGithub 首次启动生成的 CA，保持证书验证开启。Windows 使用 Schannel 的 Git 可使用系统证书库；使用 OpenSSL 的 Git 可以为本次操作指定 CA（将路径替换为实际绝对路径）：

```sh
git -c http.sslCAInfo="C:/FastGithub/cacert/fastgithub.cer" clone https://github.com/owner/repository.git
```

Linux 对应文件名为 `cacert/fastgithub.crt`。如果以前设置过全局 `http.sslverify=false`，可用 `git config --global --unset-all http.sslverify` 清除该设置，再按上面的方法配置信任。新版启动时不再修改 Git 的全局证书验证设置。

#### 4.2 firefox
firefox提示`连接有潜在的安全问题`</br>
设置->隐私与安全->证书->查看证书->证书颁发机构，导入cacert/fastgithub.cer，勾选“信任由此证书颁发机构来标识网站”
  

### 5 安全性说明
FastGithub为每台不同的主机生成自颁发CA证书，保存在cacert文件夹下。客户端设备需要安装和无条件信任自颁发的CA证书，请不要将证书私钥泄露给他人，以免造成损失。

### 源码构建

项目当前使用 .NET 7 SDK，Windows 界面使用 .NET Framework 4.5。请先安装 [SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)。.NET 7 已停止支持，升级运行时仍是后续需要处理的工作。

在仓库根目录运行以下命令，Windows 下最后一个命令会把界面发布到核心程序旁边：

```sh
dotnet test FastGithub.Tests/FastGithub.Tests.csproj -c Release -r win-x64 --self-contained false
dotnet publish FastGithub/FastGithub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
dotnet publish FastGithub.UI/FastGithub.UI.csproj -c Release -o publish/win-x64
```

将 `publish/win-x64` 整个目录一起使用。Linux/macOS 只发布核心项目，将 `win-x64` 替换为 `linux-x64`、`osx-x64` 或 `osx-arm64`。非 Windows 平台不要构建整个包含 WPF 界面的解决方案；发布后为 `fastgithub` 和 `dnscrypt-proxy/dnscrypt-proxy` 添加执行权限。Apple Silicon 使用 `osx-arm64`；交叉构建成功不等于已验证 macOS 证书和系统兼容性。

### Windows 显式代理模式

默认 Windows 模式使用驱动透明拦截网络流量。若与 IIS、localhost 网站或其他代理冲突，可以退出原实例后，在发布目录运行：

```powershell
.\fastgithub.exe FastGithub:EnablePacketIntercept=false
```

也可以在 `appsettings.json` 的 `FastGithub` 节点中加入 `"EnablePacketIntercept": false`，重启生效。该模式只监听默认 `127.0.0.1:38457` 代理端口，不注册 DNS/TCP 拦截器，不接管本地 80/443 端口。浏览器自动代理脚本地址为 `http://127.0.0.1:38457`；Git 可为单次操作指定代理：

```sh
git -c http.proxy=http://127.0.0.1:38457 -c http.sslCAInfo="C:/FastGithub/cacert/fastgithub.cer" clone https://github.com/owner/repository.git
```

此模式不提供透明的 SSH/git 协议拦截。新版界面也可以在「连接设置」中选择显式代理模式，停止服务后切换即可；流量统计使用独立的回环端口，两种模式均可显示。默认透明模式下的回环端口冲突仍需进一步重构解决。

### Windows 界面

新版界面保留原生 Windows 窗口，采用代理客户端常见的侧栏布局，提供概览、运行日志、连接设置和使用帮助：

- 在窗口或托盘菜单中启动、停止服务；停止时通知核心正常退出，以恢复网络设置。
- 查看最近 60 秒的上下行速率及累计流量；启动中、未就绪和意外退出会显示不同状态。
- 搜索日志、筛选警告或错误、暂停自动滚动、复制及导出当前筛选结果。
- 选择透明或显式代理模式，设置是否随界面启动核心，打开配置与证书文件夹。
- 帮助页面使用本地原生控件，不依赖 IE 或远程样式文件。

点击窗口关闭按钮会收起到托盘。完全退出请使用托盘菜单「退出 FastGithub」。界面设置保存在当前用户的本地应用数据目录；随界面启动不等于 Windows 开机自启。

开发者可运行以下命令检查界面，预览不会启动核心或安装证书，截图写入 `artifacts/ui`：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File scripts/preview-ui.ps1
```

### Docker 示例

使用 Docker Compose v2 运行 `docker compose up`。示例会等待 FastGithub 生成 CA，再克隆 Kubernetes；重复启动会复用已有 checkout，并保留其中的本地文件。克隆失败时会清理本次临时目录，最多自动重试三次。`build` 服务等待克隆成功后才启动。

已有 `kubernetes` 目录如果不是 Git checkout，示例会提示手动移走该目录，不会删除其中的数据。

详细原理、issue 对应关系和验证边界见 [项目分析与修复说明](docs/项目分析与修复说明.md)。

### 6 合法性说明
《国际联网暂行规定》第六条规定：“计算机信息网络直接进行国际联网，必须使用邮电部国家公用电信网提供的国际出入口信道。任何单位和个人不得自行建立或者使用其他信道进行国际联网。”
FastGithub本地代理使用的都是“公用电信网提供的国际出入口信道”，从国外Github服务器到国内用户电脑上FastGithub程序的流量，使用的是正常流量通道，其间未对流量进行任何额外加密（仅有网页原有的TLS加密，区别于VPN的流量加密），而FastGithub获取到网页数据之后发生的整个代理过程完全在国内，不再适用国际互联网相关之规定。
