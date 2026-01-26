## 项目简介

[`youlai-aspnet`](https://gitee.com/youlaiorg/youlai-aspnet) 是 **[vue3-element-admin](https://gitee.com/youlaiorg/vue3-element-admin)** 的 ASP.NET Core 8（.NET 8）后端实现，接口路径与返回结构完全对齐，可直接为 `vue3-element-admin` 前端项目提供后端服务，形成完整的 **youlai 全家桶**企业级解决方案。

## 项目源码

| 项目 | 仓库地址 |
| --- | --- |
| **.NET 后端** | [https://gitee.com/youlaiorg/youlai-aspnet](https://gitee.com/youlaiorg/youlai-aspnet) |
| **Vue3 管理端** | [https://gitee.com/youlaiorg/vue3-element-admin](https://gitee.com/youlaiorg/vue3-element-admin) |

## 核心特性

`youlai-aspnet` 的设计初衷是为了提供一个稳定、安全且易于维护的后端基础。为此，我们在技术选型和功能设计上都遵循了企业级标准：

- **🚀 最新技术栈**：项目基于 .NET 8 和 ASP.NET Core 8 构建，享受长期支持（LTS）和最新的性能优化。我们承诺会持续跟进技术社区，保持项目活力。

- **🔐 企业级安全**：深度整合 ASP.NET Core Identity，提供基于 JWT 的无状态认证和基于 Redis 的会话管理双重机制。你可以根据业务场景灵活选择，轻松构建高安全性的身份验证体系。

- **🔑 精细化权限**：内置经典的 RBAC (基于角色的访问控制) 模型，权限控制可精确到用户的菜单、按钮乃至后端的每一个 API 接口，满足复杂业务场景下的权限需求。

- **🛠️ 完善的功能模块**：提供用户、角色、菜单、部门、字典等开箱即用的核心功能模块，帮你节省大量基础功能的开发时间，让你更专注于业务本身。

## 技术栈

| 分类     | 技术选型                      | 说明                |
| -------- | ----------------------------- | ------------------- |
| 运行时   | .NET SDK 8                    | 版本锁定：`8.0.416` |
| Web 框架 | ASP.NET Core 8                | 高性能 Web API 框架 |
| ORM      | Entity Framework Core (MySQL) | 数据访问层          |
| 缓存     | Redis 7.x                     | 会话存储、数据缓存  |
| 认证     | JWT Bearer                    | 基于令牌的身份验证  |
| 接口文档 | Swagger/OpenAPI               | 交互式 API 文档     |

## 项目目录结构

```bash
youlai-aspnet/
├─ src/                             # 源码目录
│  ├─ Youlai.Api/                   # Web API层（控制器、中间件、认证授权）
│  ├─ Youlai.Application/           # 应用层（服务接口定义、DTO、业务逻辑）
│  ├─ Youlai.Domain/                # 领域层（实体模型、领域对象、枚举）
│  └─ Youlai.Infrastructure/        # 基础设施层（EF Core实现、仓储、缓存）
│
├─ sql/                             # 数据库脚本
│  └─ mysql/
│     └─ youlai_admin.sql           # 数据库初始化脚本（包含表结构和基础数据）
│
├─ tests/                           # 测试项目
├─ youlai-aspnet.sln                # Visual Studio 解决方案文件
└─ global.json                      # .NET SDK版本锁定配置
```

## 环境准备

### 1. 基础环境

| 要求 | 说明 | 安装指引 |
| --- | --- | --- |
| **.NET SDK 8** | 8.0.416 或更高版本 | [官方下载](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0) |
| **MySQL** | 5.7+ 或 8.x | 业务数据存储，必需安装：[Windows](https://youlai.blog.csdn.net/article/details/133272887) / [Linux](https://youlai.blog.csdn.net/article/details/130398179) |
| **Redis** | 7.x 稳定版 | 会话缓存，必需安装：[Windows](https://youlai.blog.csdn.net/article/details/133410293) / [Linux](https://youlai.blog.csdn.net/article/details/130439335) |
| **MySQL 客户端** | Navicat / DBeaver / MySQL Workbench | 推荐使用图形化管理工具 |

> ⚠️ **重要提示**：MySQL 与 Redis 为项目启动必需依赖，请确保服务已启动。

### 2. 开发工具

**Visual Studio 2022+**（推荐）：

- 社区版即可，安装工作负载：ASP.NET 和 Web 开发

**VS Code**：

1. 安装 [.NET SDK 8](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)

![](https://i-blog.csdnimg.cn/direct/a21efdc55d73434fbcbf09147b058515.png)

```
dotnet --version
```

![在这里插入图片描述](https://i-blog.csdnimg.cn/direct/17ac80176b58491d81a02221b88b1123.png)

2. 安装以下扩展插件（VS Code 扩展市场搜索安装）：

| 插件名称                  | 作用                    |
| ------------------------- | ----------------------- |
| **C# Dev Kit**            | .NET 开发核心套件，必备 |
| **C#**                    | C# 语言支持             |
| **NuGet Package Manager** | NuGet 包管理            |

## 数据库初始化

推荐使用 **Navicat**、**DBeaver** 或 **MySQL Workbench** 等数据库管理工具执行初始化脚本：

1. 打开数据库管理工具，连接到 MySQL 服务器
2. 执行项目根目录下的脚本文件：`sql/mysql/youlai_admin.sql`
3. 脚本会自动创建 `youlai_admin` 数据库及相关表结构
4. 插入初始化数据（包含默认管理员账号：admin/123456）

## 配置说明

开发环境配置文件：`src/Youlai.Api/appsettings.Development.json`

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Port=3306;Database=youlai_admin;User=root;Password=123456;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=,defaultDatabase=0"
  },
  "Security": {
    "Session": {
      "AccessTokenTimeToLive": 7200,
      "RefreshTokenTimeToLive": 604800,
      "Jwt": {
        "SecretKey": "设置一个至少32位的安全密钥"
      }
    }
  }
}
```

**配置项说明：**

- `Database:ConnectionString`：MySQL 数据库连接字符串
- `Redis:ConnectionString`：Redis 连接配置
- `Security:Session:Jwt`：JWT 认证相关配置（生产环境请务必修改 SecretKey）

## 快速启动

```bash
# 1. 克隆项目
git clone https://gitee.com/youlaiorg/youlai-aspnet.git
cd youlai-aspnet

# 2. 下载并还原项目依赖（NuGet 包）
dotnet restore

# 3. 启动项目
dotnet run --project src/Youlai.Api -c Release

```

启动成功后，你可以访问以下地址进行验证：

- **Swagger 文档**: [http://localhost:8000/swagger](http://localhost:8000/swagger)
- **健康检查**: [http://localhost:8000/health](http://localhost:8000/health)

你也可以使用 API 工具（如 Postman）测试登录接口：

- **URL**: `POST` [http://localhost:8000/api/v1/auth/login](http://localhost:8000/api/v1/auth/login)
- **账号**: `admin`
- **密码**: `123456`

## 前端整合

`youlai-aspnet` 专为 `vue3-element-admin` 设计，前后端完全兼容：

```bash
# 1. 获取前端项目
git clone https://gitee.com/youlaiorg/vue3-element-admin.git
cd vue3-element-admin

# 2. 安装依赖（推荐使用 pnpm）
npm install -g pnpm
pnpm install

# 3. 配置后端接口地址
# 编辑 .env.development 文件，修改：
VITE_APP_API_URL=http://localhost:8000

# 4. 启动前端
pnpm run dev

```

启动前端服务后，请访问以下地址：

- **访问地址**: [http://localhost:3000](http://localhost:3000)
- **登录账号**: `admin` / `123456`
- **系统特点**: 菜单、路由、按钮权限均由后端动态控制。

## 项目部署

### 1. Windows 部署（IIS）

1. **安装 IIS**：
   - Windows 控制面板 → 程序 → 启用或关闭 Windows 功能
   - 勾选「Internet Information Services」和「.NET Core 运行时」

2. **发布项目**：

   ```bash
   dotnet publish src/Youlai.Api -c Release -o ./publish
   ```

3. **配置 IIS**：
   - 打开 IIS 管理器
   - 右键「网站」→「添加网站」
   - 设置网站名称、物理路径（选择 publish 文件夹）
   - 设置端口（如 8000）
   - 应用程序池选择「无托管代码」

4. **安装 .NET Core Hosting Bundle**：
   - 下载安装：[https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
   - 选择「.NET Core Hosting Bundle」下载安装

### 2. Windows 部署（Kestrel）

```bash
# 1. 发布项目
dotnet publish src/Youlai.Api -c Release -o ./publish

# 2. 配置守护进程（使用 NSSM）
# 下载 NSSM：https://nssm.cc/download
nssm install YoulaiApi ./publish/Youlai.Api.exe
nssm set YoulaiApi AppDirectory ./publish
nssm set YoulaiApi AppParameters --urls http://*:8000

# 3. 启动服务
nssm start YoulaiApi
```

### 3. Linux 部署

```bash
# 1. 安装 .NET 8 Runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0

# 2. 发布项目
dotnet publish src/Youlai.Api -c Release -o ./publish

# 3. 创建服务文件
sudo nano /etc/systemd/system/youlai-api.service

# 添加以下内容：
[Unit]
Description=Youlai API Service
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/youlai-api/Youlai.Api.dll
WorkingDirectory=/var/www/youlai-api
User=www-data
Group=www-data
Restart=always

[Install]
WantedBy=multi-user.target

# 4. 启动服务
sudo systemctl enable youlai-api.service
sudo systemctl start youlai-api.service
```

### 4. Docker 部署

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
COPY ./publish .
ENTRYPOINT ["dotnet", "Youlai.Api.dll"]
```

构建并运行：

```bash
docker build -t youlai-aspnet .
docker run -d -p 8000:80 --name youlai-api youlai-aspnet
```

## 技术交流

- **问题反馈**：[Gitee Issues](https://gitee.com/youlaiorg/youlai-aspnet/issues)
- **技术交流群**：[QQ 群：950387562](https://qm.qq.com/cgi-bin/qm/qr?k=U57IDw7ufwuzMA4qQ7BomwZ44hpHGkLg)
- **博客教程**：[https://www.youlai.tech](https://www.youlai.tech)

---

**如果这个项目对您有帮助，欢迎 Star ⭐ 支持！**
