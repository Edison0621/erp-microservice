# 采购管理服务 (Procurement Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：采购申请（可选）、采购订单（PO）、收货（GR）、采购发票（AP Invoice，交由 Finance 落地）、供应商价格与绩效，聚焦端到端"从要货到收货"闭环。

---

## 1. 产品概述

### 1.1 服务定位

采购管理服务负责企业对外采购业务的全流程管理：

> 需求 → 申请 → 审批 → 下单 → 收货 → 结算

在整体 ERP 中，它与下列模块紧密集成：

- MasterData：物料、供应商主数据。
- Inventory：采购入库、库存增加。
- Finance：应付发票（AP Invoice）、付款。
- Production / Sales：提供采购履约状态（到货情况）。

### 1.2 角色

- **采购员（Buyer）**：创建、维护采购订单，跟进交期。
- **采购经理（Purchase Manager）**：审批采购订单，维护供应策略。
- **仓库管理员（Warehouse Clerk）**：收货、质检、入库。
- **财务会计（AP Accountant）**：处理采购发票和付款（在 Finance 服务）。
- **需求方（Req. Owner）**：提出采购需求（生产/项目/部门）。

### 1.3 目标

- 规范采购流程，减少随意采购和越权行为。
- 提供清晰的 PO 状态和到货、开票、付款状态视图。
- 利用采购价格历史支撑成本控制和供应商谈判。

---

## 2. 范围与模块

### 2.1 本迭代范围

- **采购订单（PurchaseOrder）**：
  - 创建/编辑/审批/下发/取消。
  - 状态管理：Draft → PendingApproval → Approved → SentToSupplier → PartiallyReceived → FullyReceived → Closed / Cancelled。

- **收货（GoodsReceipt）**：
  - 多次收货（部分收货）。
  - 与库存服务（Inventory）同步入库。

- **供应商价格历史**：
  - 基于 PO 行项生成，供后续查询与分析。

- **采购与 Finance 的集成接口占位**：
  - 后续由 Finance 服务生成应付发票（AP Invoice）。

> 采购申请（Purchase Request）、询报价、合约采购等复杂场景留作后续版本（在 PRD 中可描述，但不强制当前实现）。

---

## 3. 核心业务概念与模型

### 3.1 采购订单（PurchaseOrder）

#### 3.1.1 结构

- **头信息（Header）**：
  - `PurchaseOrderId`：内部唯一标识（GUID）。
  - `PONumber`：展示用的订单号，如 `PO-YYYYMMDD-XXXX`。
  - `SupplierId` / `SupplierName`：供应商信息。
  - `OrderDate`：下单日期。
  - `ExpectedDeliveryDate`：整体期望到货日期（可在行级覆盖）。
  - `Currency`：币种（默认 CNY）。
  - `Status`：订单状态。
  - `Requester`：需求提出人（关联 HR/User）。
  - `CreatedBy` / `CreatedAt`、`LastModifiedBy` / `LastModifiedAt`。

- **行信息（Lines）**：
  - LineNumber：行号（1..n）。
  - MaterialId / MaterialCode / MaterialName：物料信息（冗余自 MasterData）。
  - OrderedQuantity：订购数量。
  - ReceivedQuantity：已收数量（从 GR 累加）。
  - UnitOfMeasure：计量单位。
  - UnitPrice：单价（含税/不含税，取决于公司政策）。
  - Amount：行总金额 = OrderedQuantity * UnitPrice。
  - RequiredDate：该行期望交付日期。
  - WarehouseId：指定收货仓库。
  - Note：备注。

- **金额字段**：
  - `TotalAmount`：所有行 Amount 总和。
  - `TaxAmount`（预留）：行税额 + 税率信息（如需）。

#### 3.1.2 状态（Status）

```csharp
public enum PurchaseOrderStatus
{
    Draft = 0,             // 草稿
    PendingApproval = 1,   // 待审批
    Approved = 2,          // 已审批
    SentToSupplier = 3,    // 已发送供应商
    PartiallyReceived = 4, // 部分收货
    FullyReceived = 5,     // 完全收货
    Closed = 6,            // 已完结
    Cancelled = 7          // 已取消
}
```

**状态流转示例**：

- Draft → PendingApproval → Approved → SentToSupplier → PartiallyReceived → FullyReceived → Closed。
- 在 Draft / PendingApproval 状态允许取消（Cancel）。
- 在 PartiallyReceived/Approved 状态可强制关闭（Close），但需说明原因。

### 3.2 收货记录（GoodsReceipt）

收货记录是对某个采购订单一次“收货动作”的描述：

- GRNumber（系统号，如 GR-YYYYMMDD-XXXX）。
- POId / PONumber：对应采购订单。
- ReceiptDate：收货日期。
- ReceivedBy：收货人。
- Lines：
  - POLineNumber：对应 PO 行号。
  - ReceivedQuantity：本次收货数量。
  - Warehouse / Location：可精确到库位。
  - QualityStatus：合格/不合格（简单质检占位）。

> 在当前服务中不一定需要单独的 GR 聚合，可由领域事件 `GoodsReceivedEvent` + 读模型支撑查看；也可以建独立聚合，PRD层面允许你选实现方案。

### 3.3 供应商价格历史（SupplierPriceHistory）

- 从 Approved/Received 的 PO 行数据生成：
  - SupplierId、MaterialId、UnitPrice、Currency、EffectiveDate（下单日期或收货日期）。
  - 用于后续查询 "某物料最近的采购价格"。

---

## 4. 功能需求（FR 列表）

### 4.1 采购订单管理

#### FR-PO-001 创建采购订单（草稿）

**目标**：由采购员创建 Draft 状态的 PO。

**输入字段**：
- SupplierId（必填）：通过 MasterData 选择供应商。
- SupplierName（必填）：冗余。
- OrderDate（默认当前日期）。
- Currency（默认 CNY）。
- Lines（必填）：至少 1 行。
  - MaterialId 或自定义描述（允许“不建物料”的杂项采购，但正式版本建议用物料编码）。
  - OrderedQuantity（>0）。
  - UnitPrice（≥0）。
  - WarehouseId（默认主仓）。
  - RequiredDate（可选）。

**业务规则**：
- TotalAmount = Σ(OrderedQuantity * UnitPrice)。
- Draft 状态下可反复修改。

#### FR-PO-002 提交审批

**目标**：将 Draft 提交为 PendingApproval。

**条件**：
- Status = Draft。
- SupplierId、Lines 必须填写完整。

**结果**：
- Status: Draft → PendingApproval。
- 触发 `PurchaseOrderSubmittedEvent`，可供审批流/通知系统使用（后续）。

#### FR-PO-003 审批通过 / 拒绝

**通过**：
- 输入：POId，ApprovedBy，ApprovalComment。
- 条件：Status = PendingApproval。
- 结果：Status → Approved，记录 ApprovedBy/ApprovedAt。

**拒绝**（可选）：
- Status → Draft 或单独 Rejected 状态（视需求，可暂简化为回到 Draft）。

#### FR-PO-004 发送给供应商

**目标**：标记 PO 已发送给供应商，通常在 Approved 后执行。

**输入**：
- SentBy（用户）。
- SentMethod（Email/Fax/Portal/EDI）。

**条件**：Status = Approved。

**结果**：Status → SentToSupplier，记录 SentAt、SentBy、SentMethod。

#### FR-PO-005 修改采购订单

- 在 Draft / PendingApproval 状态：允许修改头和行。
- 在 Approved / SentToSupplier 状态：只允许修改部分可控字段（如备注），金额/数量变更需重新审批（后续扩展）。
- 在 PartiallyReceived / FullyReceived / Closed / Cancelled 状态：禁止修改关键字段。

#### FR-PO-006 取消采购订单

**条件**：
- Status ∈ { Draft, PendingApproval, Approved }。
- 已收货数量 = 0（未收货）。

**结果**：
- Status → Cancelled，记录取消人/时间/原因。

#### FR-PO-007 完结采购订单

**条件**：
- Status ∈ { PartiallyReceived, FullyReceived }。
- 必须提供 Reason（若未完全收货则为"强制完结"）。

**结果**：
- Status → Closed。

### 4.2 收货（Goods Receipt）

#### FR-GR-001 创建收货记录

**目标**：对已发送给供应商的 PO 进行收货登记。

**输入**：
- POId / PONumber。
- ReceiptDate。
- ReceivedBy。
- Lines:
  - POLineNumber。
  - ReceivedQuantity（>0）。
  - WarehouseId / LocationId。
  - QualityStatus：简单枚举（Accepted/Rejected）。

**业务规则**：
- 对每个行：`累计 ReceivedQuantity <= OrderedQuantity`，否则拒绝（不支持超收，或需要特殊权限）。
- 若全部行 `ReceivedQuantity == OrderedQuantity`，则 PO.Status → FullyReceived；部分行则 → PartiallyReceived。

**集成**：
- 触发 `GoodsReceivedEvent` → Inventory 服务进行入库（调用 Command 或 API）。

#### FR-GR-002 收货查询

- 可按 PO 号、供应商、物料、日期范围、收货人过滤。

### 4.3 供应商价格历史

#### FR-PRICE-001 记录采购价格

- 在 PO 从 Approved / FullyReceived 后：
  - 把 PO 行写入价格历史表：SupplierId + MaterialId + UnitPrice + Currency + OrderDate。

#### FR-PRICE-002 查询最近采购价格

- 按 Supplier + Material 查询：最近 N 次价格。
- 按 Material 查询：所有供应商最近价格列表，用于比价页面。

---

## 5. 业务流程

### 5.1 典型采购流程

```text
需求方提出需求(未来可用 PurchaseRequest 表达) →
采购员创建 PO (Draft) → 提交审批(PendingApproval) →
采购经理审批通过(Approved) → 下发给供应商(SentToSupplier) →
供应商发货 → 仓库收货(多次 GR，更新 PO 行 ReceivedQty) →
PO 状态变为 PartiallyReceived / FullyReceived →
财务根据收货信息与发票生成 AP Invoice(Finance) →
付款(Payments in Finance) → 采购关闭(Closed)
```

### 5.2 异常情况

- 供应商部分发货且终止合作 → 采购强制完结（Closed with reason）。
- 订单下发后需要减少数量/变更价格 → 进入"变更单"流程（当前 PRD 提到，实际实现可留待后续迭代）。

---

## 6. 数据模型（领域模型视角）

### 6.1 PurchaseOrder 聚合根（已部分实现）

> 你代码中已有 `PurchaseOrder`、`PurchaseOrderLine`、`PurchaseOrderState`、`PurchaseOrderCreatedEvent` 等，这里是概念上的完整字段建议。

```csharp
public class PurchaseOrder : AggregateRoot<PurchaseOrder, PurchaseOrderId>
{
    // 标识
    public string PONumber { get; private set; }
    public PurchaseOrderStatus Status => _state.Status;

    // 供应商
    public string SupplierId => _state.SupplierId;
    public string SupplierName => _state.SupplierName;

    // 日期
    public DateTime OrderDate => _state.OrderDate;
    public DateTime ExpectedDeliveryDate => _state.ExpectedDeliveryDate;

    // 金额
    public decimal TotalAmount => _state.TotalAmount;
    public string Currency => _state.Currency;

    // 行
    public IReadOnlyList<PurchaseOrderLine> Lines => _state.Lines;

    // 行为
    public void CreatePurchaseOrder(...)
    public void Approve(...)
    public void SendToSupplier(...)
    public void ReceiveGoods(...)
    public void Close(...)
    public void Cancel(...)
}
```

### 6.2 PurchaseOrderLine 值对象

```csharp
public class PurchaseOrderLine
{
    public string LineNumber { get; private set; }
    public string MaterialId { get; private set; }
    public string MaterialCode { get; private set; }
    public string MaterialName { get; private set; }
    public decimal OrderedQuantity { get; private set; }
    public decimal ReceivedQuantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime RequiredDate { get; private set; }
    public string WarehouseId { get; private set; }
}
```

### 6.3 GoodsReceipt 读模型（建议）

```csharp
public class GoodsReceiptReadModel : IMongoDbReadModel
{
    public string Id { get; set; }              // GR Id
    public string GRNumber { get; set; }
    public string PurchaseOrderId { get; set; }
    public string PONumber { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string ReceivedBy { get; set; }
    public List<GoodsReceiptLine> Lines { get; set; }
}

public class GoodsReceiptLine
{
    public string LineNumber { get; set; }              // 对应 PO 行号
    public decimal ReceivedQuantity { get; set; }
    public string WarehouseId { get; set; }
    public string LocationId { get; set; }
    public string QualityStatus { get; set; }           // Accepted / Rejected
}
```

---

## 7. API 设计（实现导向）

### 7.1 采购订单 API

1. **创建采购订单（Draft）**

```http
POST /api/v1/procurement/purchase-orders
Content-Type: application/json

{
  "supplierId": "SUP-0001",
  "supplierName": "上海某供应商",
  "orderDate": "2026-02-06",
  "expectedDeliveryDate": "2026-02-20",
  "currency": "CNY",
  "lines": [
    {
      "lineNumber": "1",
      "materialId": "MAT-0001",
      "materialCode": "MAT-0001",
      "materialName": "冷轧钢板",
      "orderedQuantity": 100,
      "unitPrice": 50.00,
      "warehouseId": "WH-01",
      "requiredDate": "2026-02-18"
    }
  ],
  "requester": "U-001"
}
```

2. **提交审批**

```http
POST /api/v1/procurement/purchase-orders/{id}/submit
```

3. **审批通过**

```http
POST /api/v1/procurement/purchase-orders/{id}/approve
Content-Type: application/json
{
  "approvedBy": "U-Manager",
  "comment": "同意采购"
}
```

4. **发送给供应商**

```http
POST /api/v1/procurement/purchase-orders/{id}/send
Content-Type: application/json
{
  "sentBy": "U-001",
  "sentMethod": "Email"
}
```

5. **取消采购订单**

```http
POST /api/v1/procurement/purchase-orders/{id}/cancel
Content-Type: application/json
{
  "reason": "需求取消"
}
```

6. **完结采购订单**

```http
POST /api/v1/procurement/purchase-orders/{id}/close
Content-Type: application/json
{
  "reason": "订单已全部履约"  // 或 "强制完结"
}
```

7. **采购订单查询**

```http
GET /api/v1/procurement/purchase-orders?
    supplierId=SUP-0001&status=Approved&fromDate=2026-01-01&toDate=2026-12-31&pageIndex=1&pageSize=20
```

### 7.2 收货 API

1. **创建收货记录**

```http
POST /api/v1/procurement/purchase-orders/{id}/receipts
Content-Type: application/json

{
  "receiptDate": "2026-02-19",
  "receivedBy": "U-WH-001",
  "lines": [
    {
      "lineNumber": "1",           // 对应 PO 行号
      "receivedQuantity": 50,
      "warehouseId": "WH-01",
      "locationId": "A-01-01",
      "qualityStatus": "Accepted"
    }
  ]
}
```

> 收到请求后，服务应：
> - 验证数量（不得超订购量）。
> - 发出 GoodsReceivedEvent → Inventory 服务执行实际库存变动。
> - 更新 PurchaseOrderState（ReceivedQuantity、Status）。

2. **收货记录查询**

```http
GET /api/v1/procurement/purchase-orders/{id}/receipts
```

### 7.3 供应商价格查询 API

```http
GET /api/v1/procurement/supplier-prices?
    materialId=MAT-0001&supplierId=SUP-0001&top=5

GET /api/v1/procurement/material-prices?
    materialId=MAT-0001&top=5
```

---

## 8. 与其他服务的集成

### 8.1 与 MasterData 集成

- 在创建 PO 时：
  - 供应商信息来自 MasterData.Supplier。
  - 物料信息来自 MasterData.Material。

### 8.2 与 Inventory 集成

- 在 `ReceiveGoods` 时，发布 `GoodsReceivedEvent`：

```csharp
public class GoodsReceivedIntegrationEvent
{
    public string PurchaseOrderId { get; set; }
    public string SupplierId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public List<GoodsReceivedItem> Items { get; set; }
}

public class GoodsReceivedItem
{
    public string MaterialId { get; set; }
    public string WarehouseId { get; set; }
    public string LocationId { get; set; }
    public decimal Quantity { get; set; }
}
```

- Inventory 服务订阅此事件，将对应数量入库到 InventoryItem。

### 8.3 与 Finance 集成

- 当 PO 已收货 & 发票到达时，通常由 Finance 服务创建 AP 发票：
  - Procurement 提供接口查询某 PO 已收货但未开票的金额。
  - Finance 根据此数据生成 AP Invoice。

---

## 9. 非功能需求

### 9.1 性能

- 单页 PO 列表（分页 20 条）在正常数据量下响应时间 ≤ 300ms。
- 收货提交操作应在 500ms 内完成（不含异步库存和财务处理）。

### 9.2 安全

- 所有接口必须经过 Identity 的 JWT 鉴权与权限控制：
  - 例如 `Procurement.PurchaseOrder.Create`、`Procurement.PurchaseOrder.Approve` 等权限点。

### 9.3 审计

- 需要记录下列操作：
  - 创建/修改/取消/关闭采购订单。
  - 收货记录创建/修改（如有）。

---

## 10. 实现优先级

### P0（必须实现）

1. PurchaseOrder 的创建、审批、发送、取消、收货、完结全流程（不含复杂变更单）。  
2. 基于事件的收货 → Inventory 入库集成点。  
3. 基础的供应商价格历史记录与查询。

### P1（下一步）

1. 采购申请（PurchaseRequest）与多级审批。  
2. 与 Finance 的 AP 发票联动（PO → GR → AP Invoice）。  
3. 采购价格分析报表（按时间、供应商、物料维度）。

### P2（后续）

1. 合同/框架协议采购。  
2. 询价/比价流程。  
3. 供应商评级与黑名单策略更深度整合。

---

> 有了这份 PRD，你可以像 Finance 一样，按模块分阶段落地：先完成 PurchaseOrder 聚合和 API，再增加收货与供应商价格历史，然后再慢慢接 Finance/Inventory。