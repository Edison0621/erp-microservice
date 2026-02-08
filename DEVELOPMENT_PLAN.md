# ERP 系统开发架构与实施计划

## 1. 项目概述 (Project Overview)
**目标**: 基于现有 PRD 文档，构建一套面向电商行业的高性能、可扩展的现代 ERP 系统。
**核心理念**: 采用领域驱动设计 (DDD) 指导业务建模，云原生架构保障系统伸缩性，事件溯源 (Event Sourcing) 确保数据完整性与可追溯性。

## 2. 技术栈架构 (Technology Stack)

### 2.1 后端核心 (Backend Core)
### 2.1 后端核心 (Backend Core)
*   **Framework**: .NET 10 (Preview/Latest)
*   **Orchestration**: **.NET Aspire** (用于开发态编排与云原生部署)
*   **Sidecar Model**: **Dapr** (Distributed Application Runtime)
*   **Database**: PostgreSQL 16+ (TimescaleDB optional) + Redis (State/PubSub)

### 2.2 架构模式 (Architectural Patterns)
*   **Architecture Style**: Clean Architecture + Microservices w/ Dapr
*   **Domain Modeling**: DDD (Domain-Driven Design)
*   **Communication**: 
    *   **Synchronous**: Dapr Service Invocation (gRPC/HTTP)
    *   **Asynchronous**: Dapr Pub/Sub (abstraction over RabbitMQ/Redis)
*   **Data Persistence**: 
    *   **Event Sourcing**: Custom/Marten via Postgres
    *   **State Management**: Dapr State Management (for caches/ephemeral state)

### 2.3 基础设施 & 运维 (Infrastructure & DevOps)
*   **App Host**: Aspire AppHost (replace manual Docker Compose)
*   **Gateway**: YARP + Aspire Service Discovery
*   **Security**: 
    *   **Identity**: Intercepted via Dapr Middleware or Gateway
    *   **Signature Verification**: Integrated as ASP.NET Core Middleware behind Dapr
*   **Observability**: Aspire Dashboard (OTLP built-in)

## 3. 系统架构设计 (Architecture Blueprint)

### 3.1 逻辑分层 (Clean Architecture)
每个微服务内部遵循以下分层：
1.  **API Layer** (Presentation): Controllers, Grpc Services, ViewModels.
2.  **Application Layer**: Commands/Queries Handlers, DTOs, Event Handlers (Orchestrators).
3.  **Domain Layer** (Core): Aggregates, Entities, Value Objects, Domain Events, Repository Interfaces.
4.  **Infrastructure Layer**: EF Core/Marten Config, Message Bus Imp, External Service Clients.

### 3.2 服务划分 (Service Boundary)
基于 PRD 文档，系统将划分为以下微服务：
1.  **Identity Service**: 身份认证、权限管理 (RBAC)、组织架构。
2.  **MasterData Service**: 物料、供应商、客户、仓库基础数据。
3.  **Procurement Service**: 采购申请、采购订单、收货。
4.  **Finance Service**: 应收应付、发票、成本核算。
5.  **Inventory Service**: 库存变动、盘点、调拨。
6.  **Sales Service**: 销售订单、发货。
7.  **Production Service**: 生产计划、工单管理 (如有).
8.  **HR Service**: 员工档案 (如有).

### 3.3 关键技术实现策略

#### Event Sourcing + CQRS
*   **Write Model**: 聚合根的状态变更通过 Application Event (Domain Event) 产生。事件被持久化到 `Events` 表（Postgres JSONB）。
*   **Read Model**: 通过订阅这些事件，异步投影 (Projects) 到关系型表 (Read DB)，供 API 高效查询。

#### Outbox/Inbox Pattern (可靠消息投递)
*   **Outbox**: 在保存领域事件到 EventStore 的同一事务中，保存一条 `OutboxMessage`。由后台 Worker 读取并发布到 RabbitMQ，确保 "保存事件" 与 "发布消息" 的原子性。
*   **Inbox**: 消费者接收消息后，先存入 `Inbox` 表去重，处理成功后标记完成，实现幂等性。

## 4. 实施阶段计划 (Phased Implementation Plan)

### Phase 1: 基础设施搭建与共享内核 (Infrastructure & Shared Kernel)
**目标**: 建立统一的开发规范和基础类库，打通 CI/CD 流程。
*   **Task 1.1**: 创建 Solution 结构 (`ErpSystem.sln`)。
*   **Task 1.2**: 实现 `BuildingBlocks` (Shared Kernel)：
    *   DDD 基类 (`AggregateRoot`, `Entity`, `ValueObject`, `IDomainEvent`).
    *   Event Sourcing 基础设施 (EventStore Repository 接口与实现).
    *   CQRS 基础 (`ICommand`, `IQuery`, `ICommandHandler`).
    *   Outbox/Inbox 机制实现.
    *   MassTransit + RabbitMQ 配置封装.
*   **Task 1.3**:配置 Docker Compose (Postgres, RabbitMQ, Seq/Jaeger).

### Phase 2: 核心支撑服务 (Identity & MasterData)
**目标**: 完成用户登录、权限控制及基础档案管理，跑通整个架构流程。
*   **Task 2.1**: 开发 **Identity Service**。
    *   实现用户/角色/部门/数据权限聚合。
    *   集成 JWT, Refresh Token。
    *   验证 Outbox 发送 `UserCreatedEvent`。
*   **Task 2.2**: 开发 **MasterData Service**。
    *   物料 (Material) 聚合设计与 Event Sourcing 实现。
    *   供应商/客户聚合。
    *   实现 Material 读模型 (Read Model) 投影。

### Phase 3: 业务核心链路 (Procurement -> Inventory -> Finance)
**目标**: 实现极其核心的 "采购入库" 业务闭环。
*   **Task 3.1**: 开发 **Procurement Service**。
    *   调用 MasterData (gRPC/Http 或 数据冗余) 获取供应商/物料信息。
    *   下采购单 -> 生成 `PurchaseOrderCreatedEvent`.
*   **Task 3.2**: 开发 **Inventory Service**。
    *   监听采购收货事件 -> 增加库存。
*   **Task 3.3**: 开发 **Finance Service**。
    *   监听采购完成事件 -> 生成应付账款。

### Phase 4: 前端开发与全链路联调
*   **Frontend**: 建议使用现代框架 (如 React + Vite 或 Blazor WebAssembly) 构建 Single Page Application (SPA)。
*   **Gateway**: 配置 YARP 网关，统一对外暴露 API。

## 5. 即刻行动建议 (Immediate Next Steps)
1.  **确认**: 使用NET 10开发，架构上保持前瞻性。
2.  **初始化**: 是否开始创建解决方案目录结构和 `BuildingBlocks` 项目？
3.  **优先**: 优先落地 Identity Service，因为它是所有服务的依赖。

---
**Prepared by**: DeepMind Advanced Agent
**Date**: 2026-02-06
