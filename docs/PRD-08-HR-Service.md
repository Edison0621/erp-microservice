# 人力资源服务 (HR Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：员工主数据（Employee）、组织架构同步（与 Identity 协同）、入职（Hire）/异动（Transfer）/晋升（Promote）/离职（Terminate）全流程，基础人事信息与成本归集接口，不做复杂考勤、薪酬，只为其他模块提供标准的人力数据视图。

---

## 1. 产品概述

### 1.1 服务定位

HR 服务是整个 ERP 的“人员中心（People Hub）”，负责：

- 员工基础档案的统一管理；
- 员工在公司/部门/岗位维度的组织关系；
- 员工生命周期（入职、试用、转正、调动、晋升、离职）；
- 为 Identity 提供组织结构支撑，为 Finance/Production/Sales 提供“人”的维度数据。

> 本版本聚焦“基础 HR 主数据 + 员工生命周期事件”，不实现完整薪酬、绩效、招聘等重型模块。

### 1.2 典型角色

- **HR 专员（HR Specialist）**：维护员工档案、办理入转调离。  
- **HR 经理（HR Manager）**：审批重要异动（跨部门调动、高职级晋升、离职等）。  
- **部门经理（Department Manager）**：发起/确认本部门员工的调动、晋升、试用转正建议。  
- **财务人员（Finance）**：读取员工成本中心、雇佣状态，用于成本分摊和预算。  
- **系统管理员（SysAdmin）**：维护组织架构、岗位与权限映射（与 Identity 协同）。

---

## 2. 范围与模块

### 2.1 本迭代范围

- **员工档案（Employee Master Data）**：
  - 基本信息：姓名、工号、证件、联系方式等。
  - 雇佣信息：入职日期、试用期、转正日期、雇佣类型等。
  - 组织关系：公司、部门、岗位、直属经理。
  - 工作状态：在职/离职/停职。

- **员工生命周期管理**：
  - 入职（Hire）。
  - 部门/岗位调动（Transfer）。
  - 职级/岗位晋升（Promote）。
  - 离职（Terminate）。

- **HR 事件日志（EmployeeEvent）**：
  - 记录所有生命周期事件，供审计和报表使用。

- **与 Identity 的同步**：
  - 创建/离职时自动为员工创建/禁用系统账号（可在后续迭代中落地）。

### 2.2 暂不实现（预留）

- 薪酬与奖金模块（Payroll & Compensation）。  
- 考勤与排班（Time & Attendance）。  
- 员工绩效（Performance）。  
- 招聘与人才库（Recruiting）。

---

## 3. 核心业务概念

### 3.1 员工（Employee）

#### 3.1.1 Employee 基本字段

```csharp
public class Employee
{
    public string EmployeeId { get; set; }          // GUID
    public string EmployeeNumber { get; set; }      // 员工工号，如 EMP-000001

    // 基本信息
    public string FullName { get; set; }
    public string Gender { get; set; }              // M/F/Other
    public DateTime? DateOfBirth { get; set; }
    public string IdType { get; set; }              // 身份证/护照等
    public string IdNumber { get; set; }

    // 联系方式
    public string MobilePhone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }

    // 雇佣信息
    public DateTime HireDate { get; set; }          // 入职日期
    public DateTime? ProbationEndDate { get; set; } // 试用期结束日
    public DateTime? RegularizationDate { get; set; } // 转正日期
    public EmploymentType EmploymentType { get; set; } // 全职/兼职/实习/外包

    // 组织信息
    public string CompanyId { get; set; }
    public string DepartmentId { get; set; }
    public string PositionId { get; set; }
    public string ManagerEmployeeId { get; set; }   // 直属上级 EmployeeId

    // 成本归属（供 Finance 使用）
    public string CostCenterId { get; set; }

    // 状态
    public EmployeeStatus Status { get; set; }      // Active/Inactive/Terminated

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public string LastModifiedBy { get; set; }
}
```

#### 3.1.2 枚举定义

```csharp
public enum EmploymentType
{
    FullTime = 1,
    PartTime = 2,
    Intern = 3,
    Contractor = 4
}

public enum EmployeeStatus
{
    Active = 1,        // 在职
    Inactive = 2,      // 停职/长期休假
    Terminated = 3     // 已离职
}
```

### 3.2 员工事件（EmployeeEvent）

用于记录员工生命周期中的关键节点：

```csharp
public class EmployeeEvent
{
    public string EventId { get; set; }
    public string EmployeeId { get; set; }
    public EmployeeEventType EventType { get; set; }    // Hire/Transfer/Promote/Terminate/Change
    public DateTime OccurredAt { get; set; }
    public string PerformedBy { get; set; }
    public string Description { get; set; }

    // 事件前后的重要字段快照（可选）
    public string FromDepartmentId { get; set; }
    public string ToDepartmentId { get; set; }
    public string FromPositionId { get; set; }
    public string ToPositionId { get; set; }
}

public enum EmployeeEventType
{
    Hired = 1,
    Transferred = 2,
    Promoted = 3,
    Terminated = 4,
    InfoChanged = 5
}
```

---

## 4. 功能需求（FR 列表）

### 4.1 员工入职（Hire）

#### FR-HR-001 创建员工档案

**目标**：为新员工创建系统档案，并为后续流程打基础。

**输入字段（主要）**：
- FullName（必填）。  
- Gender（选填）。  
- DateOfBirth（选填）。  
- IdType + IdNumber（必填，需唯一性校验）。  
- MobilePhone / Email（至少一个）。  
- HireDate（必填）。  
- EmploymentType（必填）。  
- CompanyId / DepartmentId / PositionId（必填）。  
- ManagerEmployeeId（可选）。  
- CostCenterId（可选，若不填可默认部门的成本中心）。

**业务规则**：
- 自动生成 EmployeeNumber（如 `EMP-000001` 自增）。  
- 初始 Status = Active。  
- 如果有试用期：根据 HireDate + 公司默认试用月数计算 ProbationEndDate。

**事件**：
- 记录一条 EmployeeEvent：`Hired`。  
- 可发布领域事件供 Identity 创建系统账号（可在后续迭代实现）。

### 4.2 员工信息修改

#### FR-HR-002 更新员工基础信息

- 可修改字段：
  - 联系方式（电话、邮箱、地址）。  
  - 紧急联系人（可扩展）。  
  - 部分个人信息（如婚姻状况等，可后续加）。

- 不可随意修改字段：
  - EmployeeNumber；  
  - IdNumber（如需修改，需走专门流程）。

- 任何重要信息修改应记录 EmployeeEvent：`InfoChanged`。

### 4.3 部门/岗位调动（Transfer）

#### FR-HR-010 员工调动

**目标**：员工在部门或岗位之间发生变化。

**输入**：

```json
{
  "employeeId": "EMP-000001",
  "toDepartmentId": "D-IT-02",
  "toPositionId": "POS-SeniorDev",
  "effectiveDate": "2026-03-01",
  "reason": "内部轮岗"
}
```

**业务规则**：
- 当前 DepartmentId/PositionId 更新为新值。  
- 可记录 `FromDepartmentId/FromPositionId` 到 EmployeeEvent。  
- 若需要审批，可在 HR 或部门经理审核后生效（本版本可简化为立即生效）。

**事件**：
- EmployeeEvent：`Transferred`。  
- 可通知 Identity 更新用户的部门/岗位信息（用于数据权限）。

### 4.4 晋升（Promote）

#### FR-HR-020 员工晋升

**目标**：员工在职级或岗位上升（如从工程师 → 高级工程师）。

**输入**：

```json
{
  "employeeId": "EMP-000001",
  "toPositionId": "POS-SeniorDev",
  "effectiveDate": "2026-04-01",
  "reason": "年度绩效优秀"
}
```

**业务规则**：
- 可复用 PositionId 字段变更逻辑，与 Transfer 类似。  
- 区别在于：通常在同一部门内职位提升，并可能触发薪酬等级变化（后续）。

**事件**：
- EmployeeEvent：`Promoted`。

### 4.5 离职（Terminate）

#### FR-HR-030 员工离职

**目标**：办理员工离职，将其状态调整为 Terminated，并通知相关系统停用账号、停止成本计提等。

**输入**：

```json
{
  "employeeId": "EMP-000001",
  "terminationDate": "2026-05-31",
  "reason": "主动离职/辞退/试用不通过/退休等",
  "note": "交接完成"
}
```

**业务规则**：
- Status: Active/Inactive → Terminated。  
- 可记录 TerminationDate、TerminationReason。  
- 不允许删除员工记录，只可标记为离职。  
- 后续 Finance 可用该信息停止后续期间成本分摊。

**事件**：
- EmployeeEvent：`Terminated`。  
- 可通知 Identity 锁定/禁用系统账号。

### 4.6 员工状态管理

#### FR-HR-040 停职/休假（Inactive）

- 场景：长期病假、停职调查等。  
- Status: Active → Inactive；返回时 Inactive → Active。  
- 不等同于离职，保留劳动关系与账号，但可应用不同权限策略（如禁止登录、或只允许部分操作）。

---

## 5. 业务流程示例

### 5.1 员工生命周期

```text
1. HR 创建员工档案 (Hire) → Status = Active
2. 员工试用期内，如表现良好 → 标记转正 RegularizationDate
3. 岗位/部门调整 (Transfer/Promote)：
   - 更新 DepartmentId/PositionId
   - 记录 EmployeeEvent
4. 员工离职 (Terminate)：
   - Status = Terminated
   - 推送事件给 Identity/Finance 等系统
```

### 5.2 部门调整影响流程

```text
1. 部门结构调整（在 Identity/组织服务中完成）
2. HR 服务更新员工 DepartmentId
3. Identity 同步更新用户的部门，影响其数据权限范围
4. Finance 在下一个结算期按新的部门/成本中心做成本分摊
```

---

## 6. API 设计（建议）

### 6.1 员工档案 API

```http
POST   /api/v1/hr/employees                 # 创建员工(入职)
GET    /api/v1/hr/employees                 # 分页查询员工列表
GET    /api/v1/hr/employees/{id}            # 员工详情
PUT    /api/v1/hr/employees/{id}            # 更新基础信息
```

### 6.2 生命周期操作 API

```http
# 调动
POST /api/v1/hr/employees/{id}/transfer
Content-Type: application/json
{
  "toDepartmentId": "D-IT-02",
  "toPositionId": "POS-SeniorDev",
  "effectiveDate": "2026-03-01",
  "reason": "内部轮岗"
}

# 晋升
POST /api/v1/hr/employees/{id}/promote
Content-Type: application/json
{
  "toPositionId": "POS-Manager",
  "effectiveDate": "2026-04-01",
  "reason": "晋升部门经理"
}

# 离职
POST /api/v1/hr/employees/{id}/terminate
Content-Type: application/json
{
  "terminationDate": "2026-05-31",
  "reason": "主动离职",
  "note": "已办理交接"
}

# 状态变更 (Active/Inactive)
POST /api/v1/hr/employees/{id}/status
Content-Type: application/json
{
  "status": "Inactive",
  "reason": "长期病假"
}
```

### 6.3 事件查询与报表 API

```http
GET /api/v1/hr/employees/{id}/events        # 查询某员工的事件历史
GET /api/v1/hr/reports/headcount            # 人员数量报表(按部门/类型)
GET /api/v1/hr/reports/turnover             # 流失率报表(预留)
```

---

## 7. 集成设计

### 7.1 与 Identity 的集成

- 员工创建时：
  - 可发布 `EmployeeHiredEvent`，由 Identity 创建对应 User（Username、邮箱、初始角色等）。
- 部门/岗位调整时：
  - 发布 `EmployeeTransferredEvent/PromotedEvent`，Identity 更新用户所属部门/岗位，影响数据权限。
- 离职时：
  - 发布 `EmployeeTerminatedEvent`，Identity 锁定或禁用账号，收回角色与权限。

### 7.2 与 Finance 的集成

- Finance 从 HR 读取：
  - 雇佣状态（Active/Terminated）。  
  - 成本中心（CostCenterId）。  
- 成本分摊、薪酬发放等后续由 Finance/薪酬模块实现。

### 7.3 与其他业务服务的集成

- Production：
  - 生产订单的 PlannerId、ReportedBy 均引用 EmployeeId。  
- Sales：
  - SalesPersonId 关联到 EmployeeId，便于统计业绩、控制数据权限。  
- Procurement：
  - BuyerId 也可以关联 EmployeeId。

---

## 8. 非功能需求

### 8.1 性能

- 员工列表查询（分页 20 条）常规场景 ≤ 300ms。  
- 入职/离职操作 ≤ 500ms（不包含外部系统异步处理）。

### 8.2 安全

- 员工信息属于敏感数据，访问需要严格权限控制：
  - HR 角色可以查看/编辑所有员工。  
  - 部门经理只能查看本部门员工的核心信息。  
  - 普通用户仅可查看自己的基础信息（通过 Identity 保护）。

### 8.3 审计

- 所有生命周期操作（Hire/Transfer/Promote/Terminate）必须记录审计日志与 EmployeeEvent。  
- 修改敏感字段（如 IdNumber、CostCenterId）需记录操作者与时间。

---

## 9. 实现优先级

### P0（当前迭代建议实现）

1. Employee 基本档案模型及 CRUD。  
2. Hire/Transfer/Promote/Terminate 四个核心操作及事件记录。  
3. 与 Identity 的基础集成：至少在 Terminate 时禁用账号。

### P1（下一步）

1. 更完整的组织架构同步（Company/Department/Position 管理与对 Identity 的统一）。  
2. 人员数量与结构报表（Headcount）。  
3. 流失率、入离职统计基础报表。

### P2（后续）

1. 与薪酬/绩效系统的深入联动。  
2. 多组织/多法人场景下的复杂人事关系管理。  
3. 员工自助服务（修改个人信息、查看合同/证明等）。

---

> 至此，8 个微服务的 PRD 已全部齐备。你可以优先从基础性强、依赖少的模块（如 MasterData、Identity、Inventory、HR）开始落地，再逐步推进到 Finance、Sales、Procurement、Production 的全链路联动实现。