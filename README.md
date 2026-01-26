<div align="center">
   <img alt="logo" width="100" height="100" src="https://foruda.gitee.com/images/1733417239320800627/3c5290fe_716974.png">
   <h2>youlai-aspnet</h2>
   <img alt=".NET" src="https://img.shields.io/badge/.NET-8-blueviolet.svg"/>
   <img alt="ASP.NET Core" src="https://img.shields.io/badge/ASP.NET Core-8-blue.svg"/>
   <a href="https://gitee.com/youlaiorg/youlai-aspnet" target="_blank">
     <img alt="Gitee star" src="https://gitee.com/youlaiorg/youlai-aspnet/badge/star.svg"/>
   </a>     
   <a href="https://github.com/youlaitech/youlai-aspnet" target="_blank">
     <img alt="Github star" src="https://img.shields.io/github/stars/youlaitech/youlai-aspnet.svg?style=social&label=Stars"/>
   </a>
</div>

<p align="center">
  <a target="_blank" href="https://vue.youlai.tech/">🖥️ 在线预览</a>
  <span>&nbsp;|&nbsp;</span>
  <a target="_blank" href="https://www.youlai.tech/youlai-aspnet">📑 阅读文档</a>
  <span>&nbsp;|&nbsp;</span>
  <a target="_blank" href="https://www.youlai.tech">🌐 官网</a>
</p>

## 📢 项目简介

`youlai-aspnet` 是 `vue3-element-admin` 配套的 .NET 后端实现，基于 .NET 8, ASP.NET Core 8, EF Core, JWT, Redis, MySQL 构建，是 **youlai 全家桶** 的重要组成部分。

- **🚀 最新技术栈**: 基于 .NET 8 长期支持版（LTS），享受最新的性能优化和语言特性。
- **🔐 企业级安全**: 深度整合 ASP.NET Core Identity，提供 JWT 无状态认证和 Redis 会话管理双重机制。
- **🔑 精细化权限**: 内置经典的 RBAC 模型，权限控制可精确到菜单、按钮及后端 API 接口。
- **🛠️ 完善的功能模块**: 提供用户、角色、菜单、部门、字典等开箱即用的核心功能。

## 🌈 项目源码

| 项目类型 | Gitee | Github | GitCode |
| --- | --- | --- | --- |
| ✅ .NET 后端 | [youlai-aspnet](https://gitee.com/youlaiorg/youlai-aspnet) | [youlai-aspnet](https://github.com/youlaitech/youlai-aspnet) | [youlai-aspnet](https://gitcode.com/youlai/youlai-aspnet) |
| vue3 前端 | [vue3-element-admin](https://gitee.com/youlaiorg/vue3-element-admin) | [vue3-element-admin](https://github.com/youlaitech/vue3-element-admin) | [vue3-element-admin](https://gitcode.com/youlai/vue3-element-admin) |
| uni-app 移动端 | [vue-uniapp-template](https://gitee.com/youlaiorg/vue-uniapp-template) | [vue-uniapp-template](https://github.com/youlaitech/vue-uniapp-template) | [vue-uniapp-template](https://gitcode.com/youlai/vue-uniapp-template) |

## 📚 项目文档

| 文档名称 | 访问地址 |
| --- | --- |
| 项目介绍与使用指南 | [https://www.youlai.tech/youlai-aspnet](https://www.youlai.tech/youlai-aspnet) |

## 📁 项目目录

<details>
<summary> 目录结构 </summary>

```text
youlai-aspnet/
├─ src/                             # 源码目录
│  ├─ Youlai.Api/                   # Web API层
│  ├─ Youlai.Application/           # 应用层
│  ├─ Youlai.Domain/                # 领域层
│  └─ Youlai.Infrastructure/        # 基础设施层
├─ sql/                             # 数据库脚本
├─ tests/                           # 测试项目
├─ youlai-aspnet.sln                # 解决方案文件
└─ global.json                      # .NET SDK版本锁定
```

</details>

## 🚀 快速启动

### 1. 环境准备

| 要求           | 说明        |
| -------------- | ----------- |
| **.NET SDK 8** | 8.0+ LTS    |
| **MySQL**      | 5.7+ 或 8.x |
| **Redis**      | 7.x 稳定版  |

> ⚠️ **重要提示**：MySQL 与 Redis 为项目启动必需依赖，请确保服务已启动。

### 2. 数据库初始化

推荐使用 **Navicat**、**DBeaver** 或 **MySQL Workbench** 执行 `sql/mysql/youlai_admin.sql` 脚本，完成数据库和基础数据的初始化。

### 3. 修改配置

编辑 `src/Youlai.Api/appsettings.Development.json` 文件，根据实际情况修改 MySQL 和 Redis 的连接字符串。

### 4. 启动项目

```bash
# 还原依赖
dotnet restore

# 启动项目
dotnet run --project src/Youlai.Api
```

启动成功后，访问 [http://localhost:8000/swagger](http://localhost:8000/swagger) 验证项目是否成功。

## 🤝 前端整合

`youlai-aspnet` 与 `vue3-element-admin` 前后端协议完全兼容，可无缝对接。

```bash
# 1. 获取前端项目
git clone https://gitee.com/youlaiorg/vue3-element-admin.git
cd vue3-element-admin

# 2. 安装依赖
pnpm install

# 3. 配置后端地址 (编辑 .env.development)
VITE_APP_API_URL=http://localhost:8000

# 4. 启动前端
pnpm run dev
```

- **访问地址**: [http://localhost:3000](http://localhost:3000)
- **登录账号**: `admin` / `123456`

## 🐳 项目部署

### 1. Kestrel + Nginx

```bash
# 发布项目
dotnet publish src/Youlai.Api -c Release -o ./publish

# 运行
./publish/Youlai.Api
```

### 2. Docker 部署

```bash
# 构建镜像
docker build -t youlai-aspnet:latest .

# 运行容器
docker run -d -p 8000:80 --name youlai-aspnet youlai-aspnet:latest
```

## 💖 技术交流

- **问题反馈**：[Gitee Issues](https://gitee.com/youlaiorg/youlai-aspnet/issues)
- **技术交流群**：[QQ 群：950387562](https://qm.qq.com/cgi-bin/qm/qr?k=U57IDw7ufwuzMA4qQ7BomwZ44hpHGkLg)
- **博客教程**：[https://www.youlai.tech](https://www.youlai.tech)
