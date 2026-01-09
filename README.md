# Youlai ASP.NET 后端（.NET 8）

## 项目简介

`youlai-aspnet` 是 **Youlai Admin** 的 ASP.NET Core 8 后端实现，接口路径与返回结
构对齐 `youlai-boot`（Java/Spring Boot）与前端 `vue3-element-admin`。

主要能力：

- 认证与授权（JWT 登录、刷新、退出）
- 权限控制（`HasPerm` 权限码校验）
- 系统模块（用户、菜单、角色、部门、字典、参数配置、日志、统计、通知公告）
- 数据权限（DataScope：全部/本部门及子级/本部门/仅本人）
- WebSocket（STOMP 协议）用于在线人数、字典变更通知、站内消息推送

> 说明：项目已对 `long/bigint` 做 **统一序列化为字符串** 的处理，避免前端
> JavaScript 精度丢失。

---

## 目录结构

```
./youlai-aspnet
├─ youlai-aspnet.sln
├─ src/
│  ├─ Youlai.Api/                # Web API 入口（Controllers/中间件/WebSocket/鉴权）
│  ├─ Youlai.Application/        # 应用层（DTO、接口定义、业务抽象）
│  ├─ Youlai.Domain/             # 领域层（实体）
│  └─ Youlai.Infrastructure/     # 基础设施层（EF Core、Redis、业务实现、WebSocket Broker）
└─ tests/
   └─ Youlai.Api.Tests/          # 测试项目
```

---

## 技术栈

- **.NET**：.NET 8 / ASP.NET Core 8
- **ORM**：Entity Framework Core（MySQL）
- **数据库**：MySQL 8.x（推荐）
- **缓存**：Redis（用于角色权限缓存、会话安全版本、验证码等）
- **鉴权**：JWT（Bearer Token）
- **WebSocket**：原生 WebSocket + STOMP（轻量 Broker）
- **API 文档**：Swagger（Swashbuckle）

---

## 环境准备（Windows）

### 1) 安装 .NET 8 SDK

- 下载地址：
  - https://dotnet.microsoft.com/download/dotnet/8.0
- 安装完成后验证：

```bash
dotnet --version
```

### 2) 安装 MySQL

- 推荐 MySQL 8.x
- 下载地址：
  - https://dev.mysql.com/downloads/mysql/

初始化库：

- 数据库名：`youlai_admin`
- 表结构脚本：`sql/mysql/youlai_admin.sql`

（你可以使用任意客户端导入，例如 Navicat / DBeaver）

### 3) 安装 Redis

- Windows 可选：
  - Redis for Windows（第三方发行版）
  - 或使用 WSL / Docker
- 参考下载：
  - https://redis.io/download/

默认连接：`localhost:6379`

### 4) 推荐编辑器 / IDE

- Visual Studio 2022（推荐）
  - 下载：https://visualstudio.microsoft.com/vs/
  - 需要安装：**ASP.NET 和 Web 开发**、**.NET 桌面开发**（可选）
- JetBrains Rider（可选）
  - 下载：https://www.jetbrains.com/rider/
- VS Code（可选）
  - 下载：https://code.visualstudio.com/
  - 插件：C# Dev Kit

---

## 本地开发配置

本地开发一般只需要改 `src/Youlai.Api/appsettings.Development.json`：

- 数据库：`Database:ConnectionString`
- Redis：`Redis:ConnectionString`
- JWT 密钥：`Security:Session:Jwt:SecretKey`

本项目默认通过 `src/Youlai.Api/Properties/launchSettings.json` 设置
`ASPNETCORE_ENVIRONMENT=Development`，启动时会自动读取 `appsettings.json` +
`appsettings.Development.json`（后者覆盖前者）

关键配置项示例：

```json
{
  "Database": {
    "ConnectionString": "server=localhost;port=3306;database=youlai_admin;user=youlai;password=123456;CharSet=utf8mb4;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Security": {
    "Session": {
      "AccessTokenTimeToLive": 7200,
      "RefreshTokenTimeToLive": 604800,
      "Jwt": {
        "SecretKey": "..."
      }
    }
  }
}
```

---

## 启动项目（开发环境）

在 `youlai-aspnet` 目录执行：

```bash
# 还原 NuGet 依赖
dotnet restore

# 启动 Web API（默认会使用 src/Youlai.Api/Properties/launchSettings.json 中的端口配置）
dotnet run --project src/Youlai.Api -c Release
```

默认启动后：

- HTTP：`http://localhost:8000`
- HTTPS：`https://localhost:8001`
- Swagger（Development 环境启用）：
  - `http://localhost:8000/swagger`
  - `https://localhost:8001/swagger`
- OpenAPI JSON：
  - `http://localhost:8000/swagger/v1/swagger.json`

健康检查：

- `GET http://localhost:8000/api/v1/health`

---

## WebSocket（STOMP）

- 连接地址：`ws://<host>/ws`
- 客户端在 STOMP `CONNECT` 帧携带 `Authorization: Bearer <accessToken>`

常用订阅：

- `/topic/dict`：字典变更通知
- `/topic/online-count`：在线人数
- `/user/queue/message`：个人消息

---

## 部署指南

### 方案 A：Kestrel + Nginx（推荐）

1. 发布：

```bash
dotnet publish src/Youlai.Api -c Release -o ./publish
```

2. 服务器上运行：

```bash
./publish/Youlai.Api.exe
```

3. Nginx 反向代理（示例）：

```nginx
server {
  listen 80;
  server_name your-domain.com;

  location / {
    proxy_pass http://127.0.0.1:8000;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  }

  location /ws {
    proxy_pass http://127.0.0.1:8000/ws;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "Upgrade";
  }
}
```

生产环境配置建议：

- 不要把密钥写进仓库，优先使用环境变量或部署平台的机密管理
- 典型环境变量：
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ASPNETCORE_URLS=http://0.0.0.0:8000`
  - `Database__ConnectionString=...`
  - `Redis__ConnectionString=...`
  - `Security__Session__Jwt__SecretKey=...`

### 方案 B：IIS 部署（Windows）

- 安装 ASP.NET Core Hosting Bundle：
  - https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- 在 IIS 创建站点，指向 `dotnet publish` 输出目录

### 方案 C：Docker（可选）

当前仓库未内置 Dockerfile / docker-compose。如果需要容器化，可以补充：

- API Dockerfile
- MySQL/Redis 的 docker-compose
- 生产级健康检查与日志

---

## 常见问题

### 1) 前端 ID 丢失/精度问题

本项目已将 `long/bigint` 统一序列化为字符串，避免 JavaScript 精度丢失。

### 2) 数据库连接失败

- 确认 MySQL 已启动
- 检查 `Database:ConnectionString` 用户名/密码/端口
- 确认已导入 `youlai_admin.sql`

### 3) Redis 连接失败

- 确认 Redis 已启动
- 检查 `Redis:ConnectionString`

---

## License

遵循仓库根目录 License（如有）
