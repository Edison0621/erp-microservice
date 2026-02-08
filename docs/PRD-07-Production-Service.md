# 生产管理服务 (Production Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：生产订单（ProductionOrder）、生产计划与排产（简化）、投料（领料出库）、报工（半成品/完工入库）、在制品状态管理，与 Inventory / MasterData / Finance 紧密配合。聚焦从“下达生产指令”到“完工入库”的闭环。

---

## 1. 产品概述

### 1.1 服务定位

生产管理服务负责管理企业内部的制造过程：

> 销售/计划需求 → 生成生产订单 → 下达生产 → 投料 → 报工 → 完工入库

在整体 ERP 中，与以下服务协同：
- **MasterData**：物料（成品/半成品/原材料）、BOM（物料清单）、工艺路线（可后续扩展）。
- **Inventory**：原材料领用出库、半成品/成品完工入库。
- **Sales**：根据销售订单生成生产需求。
- **Finance**：基于生产投入与产出做成本归集与结转（当前仅预留接口）。

### 1.2 业务目标

- √ 统一管理生产订单及其状态，替代线下纸质工单或 Excel。  
- √ 能明确“每个生产订单的进度”：已投料多少、已报工多少、在制数量、完工数量。  
- √ 与库存数量实时同步，确保原材料/成品库存准确。  
- √ 为成本核算提供结构化数据基础（订单级投入与产出）。

### 1.3 典型角色

- **生产计划员（Planner）**：根据销售/预测生成生产订单，排期。  
- **车间主任/班组长（Supervisor）**：下达生产、跟踪执行，确认报工。  
- **一线操作员（Operator）**：实际投料/报工录入（可由系统代录或终端扫码）。  
- **仓库人员（Storekeeper）**：根据生产订单发放物料、接收入库成品。  
- **成本会计（Cost Accountant）**：基于生产订单进行成本分析（后续）。

---

## 2. 范围与模块

### 2.1 本迭代范围

- **生产订单（ProductionOrder）**：
  - 创建/修改/下达/开始/完工/关闭/取消。  
  - 状态：Created → Released → InProgress → PartiallyCompleted → Completed → Closed / Cancelled。

- **投料（Material Issue for Production）**：
  - 根据 BOM 及实际需要从仓库领用原材料。  
  - 驱动 Inventory 服务出库。

- **报工（Production Reporting）**：
  - 报告完工数量/不良数量，可分多次报工。  
  - 驱动 Inventory 服务将半成品/成品入库。

- **基础在制品（WIP）视图**：
  - 每个生产订单的计划数量、已领料、已报工、完工数量等。

### 2.2 暂不实现范围（预留）

- 详细工艺路线（Routing）、工序级别的进度与报工。  
- 产能负荷分析与高级排产（APS）。  
- 委外加工/外协生产。  
- 质量检验（IQC/IPQC/OQC）与不良分析。

---

## 3. 核心业务概念

### 3.1 生产订单（ProductionOrder）

#### 3.1.1 Header 字段

```csharp
public class ProductionOrder
{
    public string ProductionOrderId { get; set; }      // GUID
    public string OrderNumber { get; set; }            // PRD-YYYYMMDD-XXXX

    public string MaterialId { get; set; }             // 要生产的物料（成品/半成品）
    public string MaterialCode { get; set; }
    public string MaterialName { get; set; }

    public decimal PlannedQuantity { get; set; }       // 计划生产数量
    public decimal ReportedQuantity { get; set; }      // 累计已报工数量（合格）
    public decimal ScrappedQuantity { get; set; }      // 累计报废数量

    public ProductionOrderStatus Status { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public string ProductionLineId { get; set; }       // 生产线/工作中心（可选）
    public string PlannerId { get; set; }              // 计划员
    public string Remark { get; set; }
}
```

#### 3.1.2 状态（ProductionOrderStatus）

```csharp
public enum ProductionOrderStatus
{
    Created = 0,          // 已创建，未下达
    Released = 1,         // 已下达到车间
    InProgress = 2,       // 生产中（已开始报工/领料）
    PartiallyCompleted = 3, // 有部分完工报工
    Completed = 4,        // 已全部完工（ReportedQuantity >= PlannedQuantity）
    Closed = 5,           // 已关闭（业务完结）
    Cancelled = 6         // 已取消
}
```

状态说明：
- Created：计划阶段，未对库存产生影响。  
- Released：可以开始领料、报工。  
- InProgress：有领料或报工发生。  
- PartiallyCompleted：已部分达到计划数量。  
- Completed：达到或超过计划数量，禁止再报工（除返工场景）。  
- Closed：由管理人员关闭，通常在财务结算后。  
- Cancelled：取消生产，不再领料和报工（需处理已领未用物料）。

### 3.2 投料记录（MaterialConsumption）

表示针对某个生产订单的一次领料动作：

```csharp
public class MaterialConsumption
{
    public string ConsumptionId { get; set; }
    public string ProductionOrderId { get; set; }
    public string MaterialId { get; set; }         // 原材料/半成品Id
    public string WarehouseId { get; set; }

    public decimal Quantity { get; set; }          // 本次领用数量
    public DateTime ConsumedAt { get; set; }
    public string ConsumedBy { get; set; }

    public string SourceType { get; set; }         // "PROD_ISSUE"
    public string Remark { get; set; }
}
```

> 实现上可以仅作为读模型，实际数量变动由 Inventory 的 IssueStock 负责。

### 3.3 报工记录（ProductionReport）

用于记录每次生产完成的产量及不良量：

```csharp
public class ProductionReport
{
    public string ReportId { get; set; }
    public string ProductionOrderId { get; set; }
    public DateTime ReportedAt { get; set; }

    public decimal GoodQuantity { get; set; }      // 合格品数量
    public decimal ScrapQuantity { get; set; }     // 不良/报废数量

    public string WarehouseId { get; set; }        // 完工入库仓库
    public string ReportedBy { get; set; }
    public string Remark { get; set; }
}
```

合格数量将驱动成品/半成品入库，ScrapQuantity 可用于后续质量与成本分析。

---

## 4. 功能需求（FR 列表）

### 4.1 生产订单管理

#### FR-PRD-001 创建生产订单

**目标**：由计划员创建生产订单，指示车间生产某个物料的某个数量。

**输入字段**：
- MaterialId（必填）：来自 MasterData.Material，类型应为“半成品/成品”。  
- PlannedQuantity（必填，>0）。  
- PlannedStartDate / PlannedEndDate（可选）。  
- ProductionLineId（可选）。  
- 来源信息（可选）：例如来自某 SalesOrder/Forecast。  
- PlannerId（当前用户）。

**业务规则**：
- 初始状态：Created。  
- 不触发库存变动。

#### FR-PRD-002 修改/删除生产订单

- 仅当 Status = Created 时：允许修改计划数量、日期、备注等。  
- 删除生产订单需保证无领料、无报工记录。

#### FR-PRD-003 下达生产订单（Release）

**目标**：将订单下达到车间，允许领料与报工。

**条件**：
- Status = Created。

**结果**：
- Status: Created → Released。  
- 记录下达人与时间。

#### FR-PRD-004 开始生产（InProgress）

**触发条件**：
- 当首次发生领料或报工时，自动将状态置为 InProgress，并记录 ActualStartDate。

#### FR-PRD-005 完成生产（Completed）

**条件**：
- `ReportedQuantity >= PlannedQuantity`。  
- 或由主管手动标记为 Completed（强制完工）。

**结果**：
- Status → Completed。  
- 记录 ActualEndDate。

#### FR-PRD-006 关闭生产订单（Closed）

**条件**：
- Status = Completed。  
- 通常在财务做完成本结算后，由管理人员关闭，防止后续误操作。

#### FR-PRD-007 取消生产订单（Cancelled）

**条件**：
- Status ∈ { Created, Released }。  
- 未进行任何报工；如有领料则需先通过调整/退料处理。

**结果**：
- Status → Cancelled。

### 4.2 投料（领料出库）

#### FR-MAT-001 生产领料

**目标**：根据生产订单从指定仓库领用原材料/半成品。

**输入**：

```json
{
  "productionOrderId": "PRD-20260206-0001",
  "warehouseId": "WH-RAW-01",
  "lines": [
    { "materialId": "MAT-RAW-001", "quantity": 100 },
    { "materialId": "MAT-RAW-002", "quantity": 50 }
  ],
  "consumedBy": "U-OP-001"
}
```

**业务规则**：
- ProductionOrder.Status 必须 ∈ { Released, InProgress }。  
- 对每种原材料：
  - 调用 Inventory.IssueStock 或发送 PROD_ISSUE 事件，扣减库存。  
- 记录 MaterialConsumption 读模型，用于后续分析。

> BOM 校验（如按标准用量校验过量/少量领料）可在后续迭代实现。

### 4.3 报工（完工入库）

#### FR-RPT-001 生产报工

**目标**：记录某次生产的完工数量与报废数量，并驱动成品入库。

**输入**：

```json
{
  "productionOrderId": "PRD-20260206-0001",
  "goodQuantity": 80,
  "scrapQuantity": 5,
  "warehouseId": "WH-FG-01",
  "reportedBy": "U-OP-002",
  "remark": "第一批报工"
}
```

**业务规则**：
- ProductionOrder.Status 必须 ∈ { Released, InProgress, PartiallyCompleted }。  
- `goodQuantity` 与 `scrapQuantity` 均 ≥ 0。  
- `ReportedQuantity += goodQuantity`，`ScrappedQuantity += scrapQuantity`。  
- 若 `ReportedQuantity >= PlannedQuantity` → Status 至少变为 Completed（或 PartiallyCompleted/Completed 再由逻辑判断）。

**与 Inventory 集成**：
- 调用 Inventory.ReceiveStock，向指定成品仓 `WH-FG-xx` 入库 `goodQuantity`。

#### FR-RPT-002 报工记录查询

- 按生产订单号、日期范围、报工人查询报工历史。

### 4.4 在制品视图（WIP）

#### FR-WIP-001 在制品列表

- 输出字段（按生产订单）：
  - OrderNumber、Material、PlannedQuantity。  
  - TotalIssuedQuantity（总领料量）。  
  - ReportedQuantity（合格品报工量）。  
  - ScrappedQuantity。  
  - Status、Planned/Actual 日期。  
- 支持过滤：物料、生产线、状态、日期范围。

---

## 5. 业务流程示例

### 5.1 标准生产流程

```text
1. Planner 创建 ProductionOrder (Created)
2. 下达生产 (Released)
3. 车间按需领料 → Inventory 出库
   - 若首次领料，则状态变为 InProgress
4. 多次报工：
   - 每次报工，更新 ReportedQuantity & ScrappedQuantity
   - 并向 Inventory 完工入库
5. 当 ReportedQuantity >= PlannedQuantity:
   - 状态标记为 Completed
6. 财务完成成本结转后：
   - 生产主管关闭订单 (Closed)
```

### 5.2 部分完工与强制完工

- 若一部分数量已报工，生产决定停止剩余部分：
  - 主管可将订单标记为 Completed，即使 ReportedQuantity < PlannedQuantity。  
  - 剩余未生产数量视为取消需求，后续可在 Sales/Planning 层进行评估。

---

## 6. API 设计（建议）

### 6.1 生产订单 API

```http
POST   /api/v1/production/orders                  # 创建生产订单(Created)
PUT    /api/v1/production/orders/{id}             # 修改Created订单
POST   /api/v1/production/orders/{id}/release     # 下达生产(Released)
POST   /api/v1/production/orders/{id}/cancel      # 取消订单(Cancelled)
POST   /api/v1/production/orders/{id}/close       # 关闭订单(Closed)
GET    /api/v1/production/orders                  # 查询生产订单列表
GET    /api/v1/production/orders/{id}             # 获取生产订单详情
```

### 6.2 领料与报工 API

```http
# 生产领料
POST /api/v1/production/orders/{id}/material-issues

# 生产报工
POST /api/v1/production/orders/{id}/reports

# 查询报工记录
GET  /api/v1/production/orders/{id}/reports
```

### 6.3 在制品视图

```http
GET /api/v1/production/wip?
    materialId=MAT-0001&status=InProgress&fromDate=2026-02-01&toDate=2026-02-28
```

---

## 7. 集成设计

### 7.1 与 MasterData 集成

- MaterialId/Code/Name 来自 MasterData.Material。  
- 可在后续版本引入 BOM（物料清单）以指导标准领料。

### 7.2 与 Inventory 集成

- 生产领料：
  - 发送 `ProductionMaterialIssuedEvent` 或直接调用 Inventory.IssueStock 接口。  
  - Inventory 负责扣减原材料库存。

- 生产完工：
  - 发送 `ProductionCompletedEvent` 或调用 Inventory.ReceiveStock 接口将成品入库。

### 7.3 与 Sales 集成

- Sales 订单某些行可标记为“由生产供货”：
  - Sales 将生产需求传递给 Production；  
  - Production 完工情况反过来影响 Sales 的可发货能力（后续联动）。

### 7.4 与 Finance 集成

- Finance 可基于生产订单的领料与报工数据进行成本核算：
  - 总投入数量（原材料/工时等）+ 产出数量 + 废品数。  
  - 本期在制品结转等（本版本仅提供数据，不实现财务逻辑）。

### 7.5 与 Identity 集成

- 所有接口需使用 Identity 的 JWT 鉴权。  
- 数据权限：
  - 生产计划员可查看所有生产线订单。  
  - 普通操作员仅看本生产线或本车间的订单（可通过 Department/Position + 数据域权限实现）。

---

## 8. 非功能需求

### 8.1 性能

- 生产订单列表查询（分页 20 条）在正常数据量下 ≤ 300ms。  
- 领料与报工接口在 ≤ 500ms 内完成（不含下游异步计算）。

### 8.2 安全

- 关键操作需要特定权限：
  - 创建/下达/取消/关闭生产订单。  
  - 领料与报工操作需具备对应生产线或车间的权限。

### 8.3 审计

- 记录每个生产订单状态变更。  
- 记录每次领料与报工操作（人、时间、数量）。

---

## 9. 实现优先级

### P0（当前迭代建议实现）

1. ProductionOrder 聚合及状态机：Created → Released → InProgress → Completed → Closed/Cancelled。  
2. 生产订单的基础 CRUD 与列表查询。  
3. 生产报工（合格品入库）与 Inventory 的简单集成。  
4. 在制品列表读模型（基于订单+报工）。

### P1（下一步）

1. 投料领料功能与 Inventory 的正式集成。  
2. 部分完工与强制完工逻辑优化。  
3. 与 Sales 的生产需求联动（从销售订单生成生产订单）。

### P2（后续）

1. 工艺路线/工序级进度管理。  
2. 委外加工/外协流程。  
3. 更细粒度的质量记录与不良原因分析。  
4. 与成本核算的深度集成（工单成本计算）。

---

> 有了该 PRD，你可以像前几个服务一样，先从 P0 开始实现：ProductionOrder 聚合与基本状态流转，再逐步打通报工→库存、领料→库存、及与 Sales/Finance 的联动逻辑。