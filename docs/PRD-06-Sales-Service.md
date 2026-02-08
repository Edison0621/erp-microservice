# 销售管理服务 (Sales Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：销售订单（SalesOrder）、发货（Shipment）、销售发票触发（交给 Finance 落地）、库存预留与出库联动（与 Inventory 集成）、基础销售分析。聚焦从“接单”到“发货 & 开票”的主流程。

---

## 1. 产品概述

### 1.1 服务定位

销售管理服务负责从 **客户下单 → 内部审核 → 库存预留 → 发货 → 通知财务开票** 的完整流程，是企业收入侧的主入口。

它需要与以下服务紧密配合：
- **Identity**：用户与权限（销售员/销售经理）。
- **MasterData**：客户、物料、价格策略等主数据。
- **Inventory**：库存预留与出库（发货）。
- **Finance**：应收发票（AR Invoice）与收款。

### 1.2 业务目标

- √ 管理销售订单生命周期，避免口头订单、Excel 流转。  
- √ 提供可追踪的发货记录，与库存数量一致。  
- √ 为财务服务生成开票依据，保证“货、单、款”一致。  
- √ 支持基础应收控制（信用额度预校验）。

### 1.3 典型角色

- **销售员（Sales Rep）**：录入销售订单，跟踪订单状态。  
- **销售经理（Sales Manager）**：审批大额订单、折扣。  
- **客服/内勤**：协助修改订单、安排发货。  
- **仓库人员**：根据发货指令进行拣货/出库（属于 Inventory 的实际操作）。  
- **财务人员**：根据已发货/订单信息开具发票（在 Finance）。

---

## 2. 范围与模块

### 2.1 本迭代范围

- **销售订单（SalesOrder）**：
  - 创建/编辑/审批/确认/取消。  
  - 状态管理：Draft → PendingApproval → Confirmed → PartiallyShipped → FullyShipped → Closed/Cancelled。

- **发货（Shipment）**：
  - 对订单行进行多次发货（部分发货）。  
  - 与 Inventory 的出库联动。

- **开票触发（Billing Trigger）**：
  - 生成“待开票清单”，供 Finance 创建 AR Invoice。

- **基础销售查询与统计**：
  - 订单列表、发货状态、未发货数量、按客户/物料统计销售额（仅基础）。

### 2.2 暂不实现（可预留）

- 报价单、销售合同、促销活动。  
- 复杂定价策略（阶梯价、客户特价、促销价等）。  
- 退货/换货、售后服务流程。

---

## 3. 核心业务概念

### 3.1 销售订单（SalesOrder）

#### 3.1.1 SalesOrder Header

主要字段：

```csharp
public class SalesOrder
{
    public string SalesOrderId { get; set; }         // GUID
    public string SONumber { get; set; }             // SO-YYYYMMDD-XXXX

    public string CustomerId { get; set; }
    public string CustomerName { get; set; }

    public DateTime OrderDate { get; set; }
    public DateTime? RequiredDeliveryDate { get; set; }   // 客户要求交期

    public string Currency { get; set; }              // 货币
    public decimal TotalAmount { get; set; }          // 含税/不含税视公司策略

    public SalesOrderStatus Status { get; set; }

    public string SalesPersonId { get; set; }         // 对应用户Id
    public string DepartmentId { get; set; }          // 销售部门，用于数据权限

    public string Remark { get; set; }
}
```

#### 3.1.2 SalesOrder Line

```csharp
public class SalesOrderLine
{
    public string LineNumber { get; set; }            // 1..n
    public string MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }

    public decimal OrderedQuantity { get; set; }      // 订购数量
    public decimal ShippedQuantity { get; set; }      // 已发货数量

    public string UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }         // 折扣率(0–1)
    public decimal LineAmount { get; set; }           // 订单行金额

    public DateTime? RequiredDeliveryDate { get; set; }
}
```

#### 3.1.3 SalesOrder Status

```csharp
public enum SalesOrderStatus
{
    Draft = 0,
    PendingApproval = 1,
    Confirmed = 2,
    PartiallyShipped = 3,
    FullyShipped = 4,
    Closed = 5,
    Cancelled = 6
}
```

状态说明：
- Draft：草稿，未审核，可自由修改。  
- PendingApproval：等待审批（例如超折扣、超过信用额度）。  
- Confirmed：已确认，触发库存预留。  
- PartiallyShipped：部分行或部分数量已发货。  
- FullyShipped：所有行数量已全部发货。  
- Closed：业务上已结束（可由财务/销售关闭）。  
- Cancelled：取消，不能发货。

---

### 3.2 发货（Shipment）

发货记录与销售订单相关联，可一次性或多次发货。

```csharp
public class Shipment
{
    public string ShipmentId { get; set; }
    public string ShipmentNumber { get; set; }        // SHP-YYYYMMDD-XXXX
    public string SalesOrderId { get; set; }
    public string SONumber { get; set; }

    public DateTime ShippedDate { get; set; }
    public string ShippedBy { get; set; }             // 操作人（仓库/发货员）

    public string WarehouseId { get; set; }
    public List<ShipmentLine> Lines { get; set; }
}

public class ShipmentLine
{
    public string LineNumber { get; set; }            // 对应 SalesOrder 行号
    public string MaterialId { get; set; }
    public decimal ShippedQuantity { get; set; }
}
```

发货会驱动：
- 更新 SalesOrderLine 的 ShippedQuantity。  
- 更新 SalesOrder 的状态（Partially / FullyShipped）。  
- 触发库存出库指令（给 Inventory）。  
- 触发待开票记录（给 Finance）。

---

## 4. 功能需求（FR 列表）

### 4.1 销售订单管理

#### FR-SO-001 创建销售订单（Draft）

**目标**：销售员录入客户订单，保存为草稿。

**输入字段**：
- CustomerId（必填）：来自 MasterData.Customer。  
- CustomerName（冗余）。  
- OrderDate（默认当前日期）。  
- Currency（默认 CNY）。  
- SalesPersonId（当前用户）。  
- Lines（至少一行）：
  - MaterialId / MaterialCode / MaterialName。  
  - OrderedQuantity（>0）。  
  - UnitPrice（≥0）。  
  - DiscountRate（0–1，可选，默认 0）。  
  - RequiredDeliveryDate（可选）。

**业务规则**：
- TotalAmount = Σ(LineAmount)。
- Draft 状态下不进行信用/库存检查（可延后到确认阶段）。

#### FR-SO-002 编辑/删除草稿订单

- 仅 Status = Draft 的订单可被编辑或删除。  
- 删除时需检查是否已有后续操作（预留/发货），理论上 Draft 不会有后续。

#### FR-SO-003 提交审批

**目标**：将 Draft 订单提交为 PendingApproval，以触发审批流程（可以简单实现为直接自动通过）。

**触发条件**（可配置）：
- 总金额超过某值（如 100k）。
- 单行折扣率超过某阈值（如 30%）。
- 客户信用额度即将超限（需调用 Finance）。

**结果**：
- Status: Draft → PendingApproval。
- 记录提交人、提交时间。

#### FR-SO-004 审批通过/拒绝

- 审批通过：PendingApproval → Confirmed。
- 审批拒绝：回退到 Draft 或标记为 Rejected（当前可简化为回 Draft）。

审批通过后触发：
- 调用 Inventory 的 Reservation 接口，预留库存：
  - 对于每个行：按 Warehouse + MaterialId 预留 OrderedQuantity。

#### FR-SO-005 直接确认订单（无需审批）

- 对不需要审批的订单，销售员可直接将 Draft → Confirmed。  
- 在 Confirmed 时：执行信用检查 + 库存预留。  
- 若校验不通过可返回错误或标记为 PendingApproval（根据配置）。

#### FR-SO-006 取消订单

**条件**：
- Status ∈ { Draft, PendingApproval, Confirmed }。  
- 对于 Confirmed 状态，需要释放已预留的库存。  
- 不允许取消已发货（Partially/FullyShipped）订单。

**结果**：
- Status → Cancelled。  
- 若存在 Reservation，则调用 Inventory 释放预留接口。

### 4.2 发货管理

#### FR-SHP-001 创建发货记录

**目标**：对 Confirmed / PartiallyShipped 的销售订单进行发货。

**输入**：

```json
{
  "warehouseId": "WH-01",
  "shippedDate": "2026-02-10",
  "lines": [
    {
      "lineNumber": "1",
      "materialId": "MAT-0001",
      "shippedQuantity": 10
    }
  ]
}
```

**业务规则**：
- SalesOrder.Status 必须 ∈ { Confirmed, PartiallyShipped }。  
- 对每个行：`ShippedQuantity(累计) <= OrderedQuantity`。  
- 实际出库由 Inventory 完成：  
  - 对应调用 Inventory.IssueStock 或发送 Shipment 事件。

**状态变更**：
- 若所有行的 `ShippedQuantity == OrderedQuantity` → Status = FullyShipped。  
- 若部分行/数量仍未发货 → Status = PartiallyShipped。

#### FR-SHP-002 发货单查询

- 支持按：日期范围、客户、订单号、仓库、发货人过滤。

### 4.3 开票触发（Billing）

#### FR-BILL-001 生成待开票清单

- 在订单 PartiallyShipped / FullyShipped 后，可查询“已发货未开票”的部分，供 Finance 服务使用：

```http
GET /api/v1/sales/orders/{id}/billable-lines
```

返回结构：

```json
{
  "salesOrderId": "SO-20260206-0001",
  "customerId": "CUS-0001",
  "customerName": "某客户",
  "currency": "CNY",
  "lines": [
    {
      "lineNumber": "1",
      "materialId": "MAT-0001",
      "materialName": "产品A",
      "shippedQuantity": 10,
      "alreadyInvoicedQuantity": 0,
      "billableQuantity": 10,
      "unitPrice": 100.0,
      "discountRate": 0.1
    }
  ]
}
```

- Finance 根据该数据生成 AR Invoice，反向写回“累计开票数量/金额”（后续迭代）。

### 4.4 销售查询与统计

#### FR-REP-001 销售订单列表

- 支持多条件查询：  
  - 客户、订单号、状态、日期范围、销售员。  
- 输出：订单头 + 汇总金额 + 发货状态。

#### FR-REP-002 客户订单历史

- 按某客户查询历史订单及发货状态：  
  - 用于客服/销售查看客户履约情况。

#### FR-REP-003 基础销售分析（可选）

- 按物料、客户、销售员统计：  
  - 期间销售金额、数量。  
- 初期可通过简单聚合视图实现。

---

## 5. 业务流程示例

### 5.1 标准销售流程

```text
1. 客户下单 → 销售录入 SO (Draft)
2. 若金额大/折扣大 → 审批 (PendingApproval → Confirmed)
   否则直接 Confirmed
3. Confirmed 时：
   - 调用 Inventory 预留库存
4. 仓库准备发货 → Sales 创建 Shipment：
   - 更新订单 ShippedQuantity
   - 调用 Inventory 做实际出库
5. 所有行已发完 → Status = FullyShipped
6. Finance 查询可开票明细 → 创建 AR Invoice
7. 回写开票信息（可在后续迭代）
8. 订单 Closed
```

### 5.2 部分发货流程

```text
SO: OrderedQuantity = 100
第一次发货: 60 → PartiallyShipped
第二次发货: 40 → FullyShipped
```

---

## 6. API 设计（建议）

### 6.1 销售订单相关

```http
POST   /api/v1/sales/orders                 # 创建SO (Draft)
PUT    /api/v1/sales/orders/{id}            # 更新Draft SO
POST   /api/v1/sales/orders/{id}/submit     # 提交审批
POST   /api/v1/sales/orders/{id}/approve    # 审批通过
POST   /api/v1/sales/orders/{id}/confirm    # 直接确认
POST   /api/v1/sales/orders/{id}/cancel     # 取消订单
GET    /api/v1/sales/orders                 # 查询列表
GET    /api/v1/sales/orders/{id}            # 详情
```

### 6.2 发货相关

```http
POST   /api/v1/sales/orders/{id}/shipments  # 创建发货记录
GET    /api/v1/sales/orders/{id}/shipments  # 查询该订单的发货记录
```

### 6.3 开票相关

```http
GET /api/v1/sales/orders/{id}/billable-lines    # 查询可开票行
```

### 6.4 报表相关

```http
GET /api/v1/sales/reports/orders               # 订单列表/统计
GET /api/v1/sales/reports/customer-orders      # 某客户历史订单
GET /api/v1/sales/reports/material-sales       # 按物料统计销售
```

---

## 7. 集成设计

### 7.1 与 MasterData

- CustomerId/Name：从 MasterData.Customer 选择。  
- MaterialId/Code/Name：从 MasterData.Material 选择。  
- 客户信用信息（额度/账期）：供销售确认前做校验（调用 Finance 或 MasterData 的信用接口）。

### 7.2 与 Inventory

- 在订单 Confirmed 时：
  - 调用 Inventory 预留库存（Reservation）。
- 在发货时：
  - 对每个 ShipmentLine 调用 Inventory.IssueStock。  
  - 出库应优先释放与该订单关联的 Reservation。

### 7.3 与 Finance

- Sales 提供 `billable-lines` 接口，Finance 使用该数据创建 AR Invoice。  
- 可在后续迭代中实现 Finance 回写“已开票数量/金额”。

### 7.4 与 Identity

- SalesOrder 上记录 SalesPersonId / DepartmentId。  
- Sales 相关 API 应受数据权限控制（仅查看自己/本部门/所有客户等）。

---

## 8. 非功能需求

### 8.1 性能

- 订单查询（分页 20 条）一般场景 ≤ 300ms。  
- 发货创建接口 ≤ 500ms（含调用 Inventory）。

### 8.2 安全

- 所有写操作需严格权限控制：
  - 创建订单：`Sales.Order.Create`
  - 审批订单：`Sales.Order.Approve`
  - 发货操作：`Sales.Shipment.Create`

### 8.3 审计

- 记录关键节点：订单创建、审批、确认、取消、每次发货。  
- 后续可接入统一审计服务。

---

## 9. 实现优先级

### P0（当前迭代建议实现）

1. SalesOrder 聚合 & 状态机：Draft → Confirmed → Cancelled。  
2. 基础订单 CRUD 和列表查询。  
3. 与 Inventory 的预留 + 发货出库最小闭环。  
4. 基础的 `billable-lines` 接口供 Finance 使用。

### P1（下一步）

1. 审批流程（PendingApproval）。  
2. 部分发货、多次发货机制的完善。  
3. 客户信用检查接入 Finance。  
4. 销售统计报表基本版。

### P2（后续）

1. 报价单/合同管理。  
2. 退货/换货、售后流程。  
3. 高级定价策略与促销规则。

---

> 有了该 PRD，你可以按 Finance/Procurement/Inventory 的方式，从 P0 开始落地 Sales：先实现 SalesOrder 聚合+API，再做 Reservation 与 Shipment 的集成，最后再考虑审批和统计报表。