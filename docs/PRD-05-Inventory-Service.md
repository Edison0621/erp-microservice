# 库存管理服务 (Inventory Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：库存台账（InventoryItem）、出入库流水（Stock Transaction）、库存预留（Reservation）、盘点与调整、基础库存报表。覆盖“数量维度”的库存，不做金额核算（金额由 Finance 成本模块处理）。

---

## 1. 产品概述

### 1.1 服务定位

库存管理服务负责管理企业所有物料在各个仓库、库位上的**实物数量**，提供实时、准确、可追溯的库存信息，为以下场景提供支撑：

- 采购收货入库（来自 Procurement）。
- 销售发货出库（来自 Sales）。
- 生产领料、完工入库（来自 Production）。
- 盘点、调整、报废（内部操作）。
- 库存占用与预留（Sales/Production 的预占需求）。

> 本服务聚焦“数量”的准确性，价值金额由 Finance 的成本/总账模块负责。

### 1.2 业务目标

- √ 确保任意时刻，库存数量（OnHand / Reserved / Available）可查询、可追溯。  
- √ 避免出库超卖：控制 Available 不为负（除非业务允许）。  
- √ 支持多仓库、多库位、多批次（当前 PRD 先做仓库维度，库位/批次可逐步落地）。

### 1.3 典型用户

- 仓库管理员（Warehouse Clerk）：日常入库/出库操作、盘点。  
- 库管主管（Warehouse Manager）：监控库存结构、设置安全库存、审批大额调整。  
- 采购/销售/生产人员：查看库存可用量以做业务决策（只读）。  
- 财务：查询数量信息用于成本结转（只读）。

---

## 2. 范围与模块

### 2.1 本迭代范围

- **库存台账（InventoryItem）**：
  - 每个维度组合一条记录：`Tenant + Warehouse + Material`（当前先忽略批次与库位）。
  - 字段：OnHandQuantity、ReservedQuantity、AvailableQuantity。

- **出入库流水（Stock Transaction）**：
  - 记录每一笔库存变动（来源业务 + 数量 +方向）。

- **库存预留（Stock Reservation）**：
  - 对 SalesOrder / ProductionOrder 等的库存占用。

- **库存盘点与调整（Stock Adjustment）**：
  - 盘点差异 → 调整记录。

- **基础报表**：
  - 库存余额查询：按仓库/物料。  
  - 库存流水查询：按时间/物料/单据源。

### 2.2 暂不实现范围（预留）

- 批次（Batch）、序列号（SerialNumber）维度的库存（可在将来拆为 InventoryItem 子维度）。
- 保质期管理、冷链、危化品等特殊管理逻辑。
- 库内作业（移库、波次拣货、WMS 丰富功能）。

---

## 3. 核心业务概念

### 3.1 InventoryItem（库存台账）

**维度**：
- TenantId（多租户）。
- WarehouseId（仓库）。
- MaterialId（物料）。

**主要字段**：

```csharp
public class InventoryItem
{
    public string InventoryItemId { get; set; }      // 可为 WarehouseId+MaterialId 组合
    public string TenantId { get; set; }
    public string WarehouseId { get; set; }
    public string MaterialId { get; set; }
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }

    public decimal OnHandQuantity { get; set; }      // 实物在库数量
    public decimal ReservedQuantity { get; set; }    // 已预留数量（Sales/Production）
    public decimal AvailableQuantity { get; set; }   // 可用数量 = OnHand - Reserved

    public decimal SafetyStock { get; set; }         // 安全库存（来自 MasterData 或本服务维护）
    public DateTime LastMovementAt { get; set; }     // 最近出入库时间
}
```

### 3.2 StockTransaction（出入库流水）

**作用**：
- 记录所有影响 OnHand/Reserved 的动作，支持追溯与对账。  
- 不做复杂聚合，更多是 Event/ReadModel 的表现。

主要字段：

```csharp
public class StockTransaction
{
    public string TransactionId { get; set; }
    public string InventoryItemId { get; set; }
    public DateTime OccurredAt { get; set; }

    public string SourceType { get; set; }   // 如: "PO_RECEIPT", "SO_SHIPMENT", "PROD_ISSUE", "ADJUSTMENT"
    public string SourceId { get; set; }     // 对应源单号Id
    public string SourceLineId { get; set; } // 对应源单行Id（可选）

    public decimal QuantityChange { get; set; } // 正数入库，负数出库
    public string WarehouseId { get; set; }
    public string MaterialId { get; set; }

    public string PerformedBy { get; set; }
}
```

### 3.3 StockReservation（库存预留）

用于锁定部分库存给特定订单，不立即出库。

```csharp
public class StockReservation
{
    public string ReservationId { get; set; }
    public string InventoryItemId { get; set; }
    public string SourceType { get; set; }   // "SALES_ORDER", "PRODUCTION_ORDER"
    public string SourceId { get; set; }     // 对应订单Id
    public decimal Quantity { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
}
```

> Reservation 对 ReservedQuantity 的影响由领域逻辑统一维护：新增预留 → Reserved 增加；释放预留/转为出库 → Reserved 减少。

### 3.4 调整单（StockAdjustment）

用于盘点差异或管理原因引起的修正，例如损耗、报废、账务调整。

```csharp
public class StockAdjustment
{
    public string AdjustmentId { get; set; }
    public string InventoryItemId { get; set; }
    public decimal OldQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public decimal Difference { get; set; }
    public string Reason { get; set; }
    public DateTime AdjustedAt { get; set; }
    public string AdjustedBy { get; set; }
}
```

---

## 4. 功能需求（FR 列表）

### 4.1 入库（Receive Stock）

#### FR-INV-001 外部收货入库

**来源场景**：
- 采购收货（Procurement GoodsReceivedEvent）。
- 生产完工入库（Production 完工事件）。

**输入**（通常以 Integration Event 或 API 形式）：

```json
{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "quantity": 50.0,
  "sourceType": "PO_RECEIPT",
  "sourceId": "PO-20260206-0001",
  "sourceLineId": "1",
  "performedBy": "U-WH-001",
  "occurredAt": "2026-02-06T10:30:00Z"
}
```

**业务规则**：
- 找到或创建对应 `InventoryItem`：Tenant + Warehouse + Material。
- `OnHandQuantity += quantity`。
- `AvailableQuantity = OnHandQuantity - ReservedQuantity`。
- 记录一条 `StockTransaction`（入库，QuantityChange > 0）。

#### FR-INV-002 内部入库

- 场景：退货入库、调整入库、盘盈等。  
- 逻辑与外部入库类似，只是 SourceType 不同（如 `ADJUSTMENT_IN`）。

### 4.2 出库（Issue Stock）

#### FR-INV-010 销售出库

**来源场景**：
- Sales 服务的发货（Shipment 确认）。

**输入**：

```json
{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "quantity": 20.0,
  "sourceType": "SO_SHIPMENT",
  "sourceId": "SO-20260206-0001",
  "sourceLineId": "1",
  "performedBy": "U-WH-001",
  "occurredAt": "2026-02-07T09:00:00Z"
}
```

**业务规则**：
- 校验：`AvailableQuantity >= quantity`（不允许负库存，若业务允许可配置）。
- `OnHandQuantity -= quantity`。
- 若之前有 Reservation 与该订单关联：释放该部分预留（ReservedQuantity 减少）。
- `AvailableQuantity = OnHandQuantity - ReservedQuantity`。
- 记录出库流水（QuantityChange < 0）。

#### FR-INV-011 生产领料出库

- 来源：Production 服务的领料需求。  
- 逻辑与销售出库相同，只是 SourceType 为 `PROD_ISSUE`。

### 4.3 预留（Reservation）

#### FR-INV-020 创建库存预留

**应用场景**：
- 销售订单确认后，还未发货，但需要锁定库存保证可用。  
- 生产计划下达后，提前锁定关键物料。

**输入**：

```json
{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "quantity": 30,
  "sourceType": "SALES_ORDER",
  "sourceId": "SO-20260206-0001",
  "expiryDate": "2026-02-10T00:00:00Z"
}
```

**业务规则**：
- 校验：`AvailableQuantity >= quantity`。
- 新建一条 `StockReservation` 记录。
- `ReservedQuantity += quantity`，`AvailableQuantity = OnHand - Reserved`。

#### FR-INV-021 释放库存预留

场景：
- 订单取消、失效、改量；
- 或发货后无需再预留。

**逻辑**：
- 根据 ReservationId 或 (SourceType + SourceId + MaterialId) 找到预留；
- `ReservedQuantity -= reservation.Quantity`；
- 重新计算 `AvailableQuantity`；
- 删除或标记该 Reservation 为已释放。

### 4.4 盘点与调整

#### FR-INV-030 盘点任务（可后续迭代）

- 当前版本可直接支持“手工调整”：输入实际数量 → 生成差异。

#### FR-INV-031 手工调整库存

**输入**：

```json
{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "newQuantity": 98.0,
  "reason": "年度盘点盘盈/盘亏",
  "adjustedBy": "U-WH-Manager"
}
```

**业务规则**：
- `difference = newQuantity - OnHandQuantity`。
- `OnHandQuantity = newQuantity`。
- 若 ReservedQuantity > OnHandQuantity，则需要业务判断：
  - 强制将 ReservedQuantity 调整为不超过 OnHand；或不允许此调整。
- 记录 `StockAdjustment` 与对应的 `StockTransaction`（差额正/负）。

### 4.5 库存查询与报表

#### FR-INV-040 库存余额查询

**接口**：

```http
GET /api/v1/inventory/items?
    warehouseId=WH-01&materialCode=MAT-0001&onlyPositiveAvailable=true&pageIndex=1&pageSize=20
```

**支持过滤**：
- WarehouseId / MaterialId / MaterialCode；
- 仅显示 Available > 0 的物料；
- 按 MaterialCode、MaterialName 模糊查询。

#### FR-INV-041 库存流水查询

**接口**：

```http
GET /api/v1/inventory/transactions?
    warehouseId=WH-01&materialId=MAT-0001&from=2026-02-01&to=2026-02-28&sourceType=PO_RECEIPT
```

返回：按时间排序的 StockTransaction 列表，用于审计和问题排查。

---

## 5. 业务流程示例

### 5.1 采购收货 → 库存入库

```text
Procurement: PO 已收货(GoodsReceivedEvent)
   ↓
Inventory: ReceiveStockHandler 处理事件
   ↓
  - 找到/创建 InventoryItem
  - OnHand += quantity
  - Available = OnHand - Reserved
  - 记录 StockTransaction(PO_RECEIPT)
```

### 5.2 销售订单 → 预留 → 发货 → 出库

```text
Sales: SalesOrder 确认
   ↓
Inventory: 创建 Reservation(StockReservedEvent)
   ↓
  - Reserved += quantity
  - Available = OnHand - Reserved

Sales: 创建发货(ShipmentConfirmedEvent)
   ↓
Inventory: IssueStockHandler
   ↓
  - OnHand -= quantity
  - Reserved -= quantity (释放预留)
  - Available = OnHand - Reserved
  - 记录 StockTransaction(SO_SHIPMENT)
```

### 5.3 盘点调整

```text
Warehouse: 线下盘点 → 输入系统实际数量
   ↓
Inventory: AdjustStock
   ↓
  - 计算差额 difference
  - 更新 OnHand
  - 记录 StockAdjustment + StockTransaction(ADJUSTMENT)
```

---

## 6. API 设计（建议）

### 6.1 供外部服务调用的库存接口

1. **查询可用库存**

```http
GET /api/v1/inventory/available?
    warehouseId=WH-01&materialId=MAT-0001
```

**响应**：

```json
{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "onHand": 120.0,
  "reserved": 30.0,
  "available": 90.0
}
```

2. **预留库存**（供 Sales/Production 调用）

```http
POST /api/v1/inventory/reservations
Content-Type: application/json

{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "quantity": 30.0,
  "sourceType": "SALES_ORDER",
  "sourceId": "SO-20260206-0001",
  "expiryDate": "2026-02-10T00:00:00Z"
}
```

3. **释放预留**

```http
POST /api/v1/inventory/reservations/release
Content-Type: application/json

{
  "sourceType": "SALES_ORDER",
  "sourceId": "SO-20260206-0001",
  "materialId": "MAT-0001"
}
```

> 或者用 ReservationId 精确释放。

### 6.2 内部管理接口（盘点/调整）

```http
POST /api/v1/inventory/adjustments
Content-Type: application/json

{
  "warehouseId": "WH-01",
  "materialId": "MAT-0001",
  "newQuantity": 98.0,
  "reason": "年度盘点",
  "adjustedBy": "U-WH-Manager"
}
```

---

## 7. 集成设计

### 7.1 与 MasterData 的集成

- InventoryItem 中的 MaterialCode / MaterialName 仅做冗余展示，主数据以 `MaterialId` 为准。  
- WarehouseId 对应 MasterData.Warehouse。

### 7.2 与 Procurement 的集成

- 订阅 `GoodsReceivedIntegrationEvent`：
  - 来源：Procurement 收货。
  - 行为：调用 ReceiveStock 方法，实现入库。

### 7.3 与 Sales 的集成

- 下单确认时：
  - Sales 调用 Inventory 预留接口（或发送 Reservation 事件）。
- 发货时：
  - Sales 发送 Shipment 事件；  
  - Inventory 执行 IssueStock，并释放对应 Reservation。

### 7.4 与 Production 的集成

- 生产领料：
  - Production 调用 IssueStock 或发送 PROD_ISSUE 事件。
- 完工入库：
  - Production 发送生产完工事件；Inventory 入库成品。

### 7.5 与 Finance 的集成

- Finance 只关心数量与相关 SourceId：
  - 可从 StockTransaction 获取数量、时间、源单据，配合成本信息做成本核算。

---

## 8. 非功能需求

### 8.1 性能

- 常规库存余额查询：在合理数据量下，分页查询应 ≤ 300ms。  
- 预留/出入库写操作：单次应 ≤ 500ms（含事件发布）。

### 8.2 安全

- 所有接口必须通过 Identity 的 JWT 鉴权。  
- 库存调整、盘点等高风险接口需额外权限（如 `Inventory.Adjust`）。

### 8.3 审计

- 所有 StockTransaction 必须不可篡改（只能追加）。  
- 调整操作需记录调整人、原因、时间。

---

## 9. 实现优先级

### P0（当前必做）

1. InventoryItem 台账维护（OnHand/Reserved/Available）。  
2. ReceiveStock / IssueStock + StockTransaction 记录。  
3. Reservation 的创建与释放，对 Reserved/Available 的影响。  
4. 基础库存查询接口：余额 & 流水。

### P1（下一步）

1. 与 Procurement 的入库集成（真正订阅 GoodsReceived 事件）。  
2. 与 Sales 的预留+发货集成。  
3. 调整/盘点操作及简单盘点报表。

### P2（后续）

1. 批次 / 序列号维度的库存。  
2. 库位（Location）粒度管理。  
3. 安全库存预警与补货建议。  
4. 更复杂的 WMS 级作业（上架、移库、波次拣货等）。

---

> 有了这份 PRD，你可以像 Finance、Procurement 一样，从最核心的 P0 开始：
> - 先把 InventoryItem + ReceiveStock/IssueStock 打通（从事件或 API 入口）。
> - 再补 Reservation 和 Adjust。  
> 不需要一次性实现所有复杂功能，按 P0/P1/P2 迭代推进即可。
