# 身份认证服务 (Identity Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：统一认证（Auth）、账号与组织管理（User/Org）、角色与权限（RBAC）、数据权限（Data Permission）与审计日志（Audit）。

---

## 1. 产品概述

### 1.1 服务定位

身份认证服务是整个 ERP 的 **统一安全中心**，为所有业务微服务（MasterData、Finance、Procurement、Inventory、Sales、Production、HR）提供：

- √ 用户认证（Authentication）：登录、Token 颁发与刷新。
- √ 授权（Authorization）：角色 + 权限控制。
- √ 组织架构：公司、部门、岗位，支撑数据权限。
- √ 多租户（Tenant）：后续支持 SaaS 场景。
- √ 操作审计：登录日志与关键操作日志。

### 1.2 使用角色

- **系统管理员（SysAdmin）**：配置租户、组织、角色与权限。
- **安全管理员（SecurityAdmin）**：维护密码策略、MFA、审计策略。
- **各业务线管理员（模块 Owner）**：为业务模块定义权限点，配置菜单与 API 与权限的映射。
- **普通业务用户**：通过登录访问 ERP 的功能，权限由其角色与数据权限决定。

### 1.3 目标

- 从 **“能登录”** 升级到 **“可控、可审计的访问体系”**：
  - 功能级：某个页面/按钮/接口是否可访问。
  - 数据级：同一页面下看到的数据范围不同，例如本部门 vs 全公司。
  - 字段级（预留）：如薪资字段仅 HR 可见。

---

## 2. 范围与模块

### 2.1 本迭代范围

- 认证模块：
  - 用户名/密码登录。
  - Access Token（JWT）+ Refresh Token。
  - 账号锁定策略（密码错误次数过多）。

- 用户与组织模块：
  - 用户基本信息管理。
  - 部门（Department）树、岗位（Position）。
  - 用户与部门、岗位的关系（一个用户可属于多个部门/岗位）。

- 角色与权限模块（RBAC）：
  - 角色（Role）管理。
  - 权限点（Permission）管理：功能级 + 数据级。
  - 用户-角色分配。

- 数据权限模块（Data Permission）：
  - 行级数据权限：本人、部门、部门及子部门、全公司、自定义范围。
  - 数据域（Data Domain）概念：如“Finance.Invoice”、“Sales.Order”等。

- 审计日志模块：
  - 登录日志。
  - 权限配置变更日志（例如角色权限调整、数据权限模板变更）。

### 2.2 不在当前范围

- 第三方登录（微信、钉钉、企业微信等 SSO）。
- 完整的多租户计费与配额控制。
- 可视化菜单设计器与前端路由管理（当前只做后端配置结构）。

---

## 3. 核心业务概念与模型

### 3.1 用户（User）

**字段示意**：

```csharp
public class User
{
    public string UserId { get; set; }            // 全局唯一
    public string Username { get; set; }          // 登录名（唯一）
    public string DisplayName { get; set; }       // 展示姓名
    public string Email { get; set; }
    public string PhoneNumber { get; set; }

    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }

    public bool IsActive { get; set; }            // 是否启用
    public bool IsLocked { get; set; }            // 是否被锁定
    public int AccessFailedCount { get; set; }    // 连续失败次数
    public DateTime? LockoutEnd { get; set; }     // 锁定截止时间

    public string TenantId { get; set; }          // 所属租户（支持多租户）

    // 组织信息（可扩展为多部门/多岗位映射表）
    public string PrimaryDepartmentId { get; set; }
    public string PrimaryPositionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string LastLoginIp { get; set; }
}
```

### 3.2 部门（Department）、岗位（Position）

- 部门是树形结构，用于组织管理和数据权限划分。
- 岗位描述职责，如“财务专员”、“仓管员”、“生产计划员”。

```csharp
public class Department
{
    public string DepartmentId { get; set; }
    public string Name { get; set; }
    public string ParentId { get; set; }          // 上级部门
    public int Order { get; set; }                // 排序
}

public class Position
{
    public string PositionId { get; set; }
    public string Name { get; set; }              // 岗位名称
    public string Description { get; set; }
}
```

### 3.3 角色（Role）

- 角色是权限的集合；用户通过角色获得权限。
- 可存在系统内置角色（不可删除）和自定义角色。

```csharp
public class Role
{
    public string RoleId { get; set; }
    public string RoleName { get; set; }
    public string RoleCode { get; set; }
    public bool IsSystemRole { get; set; }
    public string TenantId { get; set; }
}
```

### 3.4 权限点（Permission）

权限点是可授权的最小单位，可以覆盖：
- 功能权限（页面/按钮/接口）。
- 数据权限模板（例如“只能看本部门数据”）。

```csharp
public class Permission
{
    public string PermissionId { get; set; }
    public string PermissionCode { get; set; }    // 如 "Finance.Invoice.View"
    public string Name { get; set; }              // 如 "查看发票"
    public string Category { get; set; }          // 分组，如 "Finance", "Sales"
}
```

### 3.5 数据权限（Data Permission）

**核心目标**：在相同的 API/页面下，不同用户能看到不同的数据范围。

#### 3.5.1 数据域（DataDomain）

- 每个需要数据权限控制的业务实体都定义一个 **数据域**（DataDomain）：
  - 示例：
    - `Finance.Invoice`（财务发票）
    - `Sales.Order`（销售订单）
    - `Procurement.PurchaseOrder`

#### 3.5.2 数据范围类型（DataScopeType）

- `Self`：仅本人创建的数据。
- `Department`：仅本人主部门的数据。
- `DepartmentAndSub`：本人部门及其下级部门的数据。
- `All`：全公司数据。
- `Custom`：通过特定过滤条件限制，如指定客户/地区组。

```csharp
public enum DataScopeType
{
    Self = 1,
    Department = 2,
    DepartmentAndSub = 3,
    All = 4,
    Custom = 5
}
```

#### 3.5.3 角色数据权限策略（RoleDataPermission）

- 每个角色可以在某个数据域上配置一个数据范围策略：

```csharp
public class RoleDataPermission
{
    public string RoleId { get; set; }
    public string DataDomain { get; set; }        // 如 "Finance.Invoice"
    public DataScopeType ScopeType { get; set; }

    // Custom 场景扩展字段
    public List<string> AllowedDepartmentIds { get; set; }
    public List<string> AllowedUserIds { get; set; }
    public List<string> AllowedCustomerIds { get; set; } // 以 Finance.Invoice 为例
}
```

**合并规则**：
- 用户可拥有多个角色，对同一 DataDomain：
  - 如果任一角色为 `All`，则整体为 ALL（最宽权限）。
  - 否则合并所有角色的数据范围（部门集合、客户集合求并集）。

---

## 4. 功能需求（FR 列表）

### 4.1 认证模块（Auth）

#### FR-AUTH-001 用户名密码登录

- 输入：`username` + `password`。
- 流程：
  1. 根据 username 查用户。
  2. 校验是否 Active，是否未锁定。
  3. 校验密码（Hash + Salt）。
  4. 如失败：`AccessFailedCount++`，超过阈值（如 5 次） → 锁定一段时间（如 15 分钟）。
  5. 如成功：清零失败次数，更新 LastLoginAt + LastLoginIp。
  6. 生成 JWT Access Token + Refresh Token。
- 返回：
  - `accessToken`：有效期 2 小时。
  - `refreshToken`：有效期 7 天。
  - 基本用户信息与角色列表。

#### FR-AUTH-002 刷新 Token

- 输入：`refreshToken`。
- 校验：
  - RefreshToken 是否有效、未过期、未撤销。
  - 用户仍然有效且未被锁定。
- 结果：
  - 颁发新的 Access Token（可选择刷新 RefreshToken）。

#### FR-AUTH-003 登出

- 将当前使用的 Access Token 标记为失效（短期可忽略黑名单，采用前端删除为主；中长期建议实现黑名单或 Token 版本号机制）。

#### FR-AUTH-004 多因素认证（预留）

- 登录成功后，如启用 MFA，则要求输入二次验证码（短信/OTP）。
- 当前 PRD 先描述，后续迭代实现。

### 4.2 用户管理模块（User Management）

#### FR-USER-001 创建用户

- 字段：Username、DisplayName、Email、PhoneNumber、PrimaryDepartmentId、PrimaryPositionId、初始密码。
- 业务规则：
  - Username 全局唯一。
  - 初始密码需满足密码策略（见安全部分）。

#### FR-USER-002 修改用户信息

- 可修改：DisplayName、Email、PhoneNumber、部门、岗位等。
- 不可修改：UserId；Username 一般禁止修改（除特殊流程）。

#### FR-USER-003 锁定 / 解锁用户

- 管理员可主动锁定账号（如安全风险）。
- 锁定后用户无法登录。

#### FR-USER-004 重置密码

- 管理员执行；或通过找回流程触发。
- 重置时需记录操作日志。

### 4.3 角色与权限模块（RBAC）

#### FR-ROLE-001 创建角色

- 字段：RoleName、RoleCode、描述、是否系统角色（仅内部）。
- 系统内置角色示例：
  - `SYS_ADMIN`、`FINANCE_MANAGER`、`PROCUREMENT_MANAGER`、`WAREHOUSE_ADMIN` 等。

#### FR-ROLE-002 为角色分配权限点

- 接口允许一次性设置角色拥有的 PermissionCode 集合。
- 支持覆盖式保存（全量替换）。

#### FR-ROLE-003 为用户分配角色

- 一用户可绑定多个角色。
- 提供接口：`POST /api/v1/identity/users/{id}/roles`。

### 4.4 数据权限模块

#### FR-DATA-001 配置角色数据权限

- 对每个 Role & DataDomain：
  - 设置 ScopeType（Self/Department/DepartmentAndSub/All/Custom）。
  - 若 Custom：设置允许的 DepartmentId/CustomerId 等集合。

#### FR-DATA-002 查询用户的实际数据权限

- 接口：`GET /api/v1/identity/users/{id}/data-permissions?dataDomain=Finance.Invoice`。
- 返回：

```json
{
  "userId": "U-001",
  "dataDomain": "Finance.Invoice",
  "scopeType": "DepartmentAndSub",
  "allowedDepartmentIds": ["D-01", "D-0101"],
  "allowedCustomerIds": []
}
```

> 实际在业务服务中，可以由 Identity 提供“解析好后的过滤条件”给业务服务使用。

#### FR-DATA-003 与业务服务的集成方式

- 建议两种模式：

1. **静态约定模式（简单）**：
   - 各业务服务在查询时从 Access Token 的 Claims 中获取：DeptIds、ScopeType 等信息，自行拼接 where 条件。

2. **动态查询模式（更灵活）**：
   - 业务服务调用 Identity 的数据权限接口，传入当前用户 + 数据域，由 Identity 返回过滤条件对象。
   - 例如 Finance 服务在查询发票时调用：

```http
GET /api/v1/identity/users/current/data-permissions?dataDomain=Finance.Invoice
```

- 首期实现可以采用模式 1，后续演进到模式 2。

### 4.5 审计日志模块

#### FR-AUDIT-001 登录日志

- 每次登录/登录失败记录：
  - UserId / Username
  - IP / UserAgent
  - 成功/失败及失败原因
  - 时间戳

#### FR-AUDIT-002 权限变更日志

- 下列操作必须记录审计：
  - 创建/删除角色
  - 为角色增删权限点
  - 调整角色的数据权限范围
  - 为用户分配/撤销角色

---

## 5. API 设计（实现导向）

### 5.1 认证相关

```http
POST /api/v1/identity/auth/login        # 用户登录
POST /api/v1/identity/auth/logout       # 用户登出（可选）
POST /api/v1/identity/auth/refresh      # 刷新Token
```

**登录请求示例**：

```http
POST /api/v1/identity/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "P@ssw0rd!"
}
```

**响应示例**：

```json
{
  "accessToken": "jwt-token...",
  "refreshToken": "refresh-token...",
  "expiresIn": 7200,
  "user": {
    "userId": "U-0001",
    "username": "admin",
    "displayName": "系统管理员",
    "roles": ["SYS_ADMIN"],
    "departmentId": "D-01"
  }
}
```

### 5.2 用户管理

```http
POST   /api/v1/identity/users              # 创建用户
GET    /api/v1/identity/users              # 分页查询用户
GET    /api/v1/identity/users/{id}         # 用户详情
PUT    /api/v1/identity/users/{id}         # 更新用户
DELETE /api/v1/identity/users/{id}         # 删除用户(软删)
POST   /api/v1/identity/users/{id}/reset-password
POST   /api/v1/identity/users/{id}/lock
POST   /api/v1/identity/users/{id}/unlock
POST   /api/v1/identity/users/{id}/roles   # 设置用户角色(覆盖)
```

### 5.3 角色与权限

```http
POST /api/v1/identity/roles                      # 创建角色
GET  /api/v1/identity/roles                      # 角色列表
GET  /api/v1/identity/roles/{id}                 # 角色详情
PUT  /api/v1/identity/roles/{id}                 # 更新角色
DELETE /api/v1/identity/roles/{id}               # 删除角色

GET  /api/v1/identity/roles/{id}/permissions     # 获取角色权限点
POST /api/v1/identity/roles/{id}/permissions     # 设置角色权限点(覆盖)
```

### 5.4 数据权限

```http
# 为角色设置某数据域的数据权限
POST /api/v1/identity/roles/{id}/data-permissions

# 查询用户实际数据权限
GET  /api/v1/identity/users/{id}/data-permissions?dataDomain=Finance.Invoice
```

请求示例：

```http
POST /api/v1/identity/roles/{roleId}/data-permissions
Content-Type: application/json

{
  "dataDomain": "Finance.Invoice",
  "scopeType": "DepartmentAndSub",
  "allowedDepartmentIds": ["D-01", "D-02"],
  "allowedUserIds": [],
  "allowedCustomerIds": []
}
```

---

## 6. 安全与策略

### 6.1 密码策略

- 最小长度：8
- 必须包含：大写字母、小写字母、数字、特殊符号中的至少 3 类。
- 密码有效期：可配置，默认 90 天（后续迭代）。
- 历史密码不可重复使用 N 次（预留）。

### 6.2 账号锁定策略

- 连续错误 N 次（默认 5）锁定 M 分钟（默认 15）。
- 系统管理员可以手动解锁。

### 6.3 数据权限强制

- 业务服务应在查询接口中 **必须考虑数据权限**：
  - 要么在中间件统一处理（令牌解析 + 查询过滤）。
  - 要么每个查询显式从 Identity 获取过滤条件。

---

## 7. 实现优先级

### P0（当前必须落地）

- 认证模块：用户名密码登录 + JWT + RefreshToken。
- 用户管理：基础 CRUD + 锁定/解锁 + 重置密码。
- 角色管理：角色 CRUD + 基础权限点分配。
- 简单数据权限：为角色设置某 DataDomain 的 ScopeType（至少支持 Self/Department/All）。

### P1（下一步）

- 数据权限 Custom 范围（客户列表、部门列表）。
- 与 Finance/Sales 等服务打通完整数据权限链路（从 Token/Identity 拿过滤条件）。
- 审计日志：登录日志、权限变更日志查询 API。

### P2（后续）

- 多因素认证（MFA）。
- 第三方登录（微信企业版/钉钉 SSO）。
- 完整多租户管理与计费。

---

> 你后续在实现 Identity 服务时，可以按本 PRD：
> 1）先做 Auth + User + Role 的基础功能；
> 2）再加上 RoleDataPermission 的存储与读取；
> 3）最后和 Finance/Sales 的查询接口联动，在代码里真正用这些数据权限过滤数据。
