# 财务管理服务 (Finance Service) - 详细版 PRD

> 版本：v1.0  
> 模块范围：应收 / 应付 / 发票 / 收付款 / 账龄与逾期分析（当前迭代重点），预留总账、成本、资金、税务、固定资产等扩展能力。

---

## 1. 产品概述

### 1.1 服务定位

财务管理服务是 ERP 的核心后端服务，用于统一管理企业的**应收应付、发票、收付款记录、账龄分析**，并为后续的总账、成本核算、资金管理和财务报表提供基础数据。

当前迭代主要目标：
- √ 建立完整的 **应收 / 应付发票模型**（Invoice + InvoiceLine）。
- √ 支持 **收款/付款记录** 与发票联动（PaymentRecord）。
- √ 提供 **账龄分析/逾期统计** 接口，供前端和报表使用。
- ❌ 尚未实现：总账凭证、期末结账、报表引擎，仅在 PRD 中预留设计。

### 1.2 使用角色

- **财务会计（AR/AP）**：录入、管理应收/应付发票和收付款。
- **财务主管**：审批坏账、查看账龄和逾期报表。
- **销售人员**：查看客户应收余额（只读）。
- **采购人员**：查看供应商应付余额（只读）。
- **管理层/审计**：查看汇总财务报表与明细查询（只读）。

---

## 2. 业务范围与模块

### 2.1 本迭代覆盖范围

- **发票模块（Invoice）**：
  - 应收发票（AR Invoice）
  - 应付发票（AP Invoice）
  - 发票生命周期管理：草稿 → 已开票 → 部分收/付 → 结清 → 坏账/作废

- **收付款模块（Payment）**：
  - 发票级别的收款/付款记录
  - 付款方式、银行信息、参考号记录

- **账龄与逾期分析（Aging & Overdue）**：
  - 按客户/供应商计算应收/应付账龄
  - 按时间区间分档，统计余额和逾期

- **预留模块（只做领域设计，不做实现）**：
  - 总账（GL）：凭证、科目、月结
  - 成本核算：生产/采购成本归集
  - 资金管理：现金/银行日记账
  - 税务管理：增值税/所得税申报
  - 固定资产：资产卡片、折旧

### 2.2 不在范围内（当前版本）

- 复杂的多账簿/多会计准则（如同时支持中国准则和 IFRS）。
- 跨币种重估、外汇损益详细处理（仅留汇率字段）。
- 自动与银行系统对接（仅保留 BankReference 字段）。

---

## 3. 核心业务概念与状态

### 3.1 发票（Invoice）

#### 3.1.1 发票类型

- `InvoiceType`
  - `1 = AccountsReceivable`（应收）
  - `2 = AccountsPayable`（应付）

#### 3.1.2 发票状态

- `InvoiceStatus`
  - `0 = Draft`：草稿，未生效，可自由修改/删除。
  - `1 = Issued`：已开票/确认，计入应收/应付，进入账龄统计。
  - `2 = PartiallyPaid`：部分收/付，仍有未结余额。
  - `3 = FullyPaid`：已完全结清，OutstandingAmount = 0。
  - `4 = WrittenOff`：已坏账/核销（一般对应应收）。
  - `5 = Cancelled`：作废，不再参与账龄和统计。

**状态流转规则**：

- Draft → Issued  
- Issued → PartiallyPaid / FullyPaid / Cancelled / WrittenOff  
- PartiallyPaid → FullyPaid / WrittenOff  
- FullyPaid → （终态，不可变更除备注）  
- WrittenOff → （终态，不可收付款）  
- Cancelled → （终态，不可收付款）

### 3.2 发票金额字段

- `TotalAmount`：发票总金额（含税或不含税，由配置决定）。
- `PaidAmount`：累积收/付金额（所有 PaymentRecord 之和）。
- `OutstandingAmount`：未收/付金额 = TotalAmount - PaidAmount。
- `Currency`：币种（CNY、USD…）。

### 3.3 收付款记录（PaymentRecord）

- 对应某一张发票的一笔收款或付款操作。
- **不直接修改发票金额**，只通过事件更新发票状态和已收/付金额。

字段：
- PaymentId：唯一标识。
- InvoiceId：关联发票。
- PaymentDirection：`In = 收款` / `Out = 付款`。
- Amount：金额，>0，≤ OutstandingAmount。
- PaymentDate：记账日期。
- PaymentMethod：现金/转账/支票/电子支付等。
- BankAccountId（选填）：对应 Finance 内部定义的账户。
- ReferenceNo：银行流水号或凭证号。
- Comment：备注。

### 3.4 账龄与逾期

- **账龄基准日期**：通常为 `查询日` 或 `系统当前日期`。
- **账龄区间（默认）**：
  - 0–30 天
  - 31–60 天
  - 61–90 天
  - >90 天

**计算逻辑**：
- 对于应收：
  - `天数 = 基准日 - 发票日期 (InvoiceDate)` 或 `基准日 - 到期日 (DueDate)`（可配置）。
  - 未结清金额分配到对应区间。
- 对于应付：逻辑类似，只是角色变为供应商。

---

## 4. 功能需求（FR 列表）

### 4.1 发票功能

#### FR-INV-001 创建草稿发票（Create Draft Invoice）

**目标**：创建一张暂未生效的 AR 或 AP 发票。

**输入字段**：
- InvoiceType（必填）：1=AR, 2=AP。
- ExternalInvoiceNumber（选填）：外部发票号（税票号等）。
- PartyId（必填）：客户Id / 供应商Id（由类型决定）。
- PartyName（必填）：展示名称，冗余方便查询。
- InvoiceDate（必填）：开票日期。
- DueDate（选填）：到期日，若为空可根据付款条件推算。
- Currency（必填）：默认 CNY。
- Lines（必填，至少一行）：
  - LineNumber：行号（1..n）。
  - MaterialId（选填）：关联物料。
  - Description（必填）：摘要。
  - Quantity（必填，>0）。
  - UnitPrice（必填，≥0）。
  - TaxRate（选填，0–1，例如 0.13）。

**业务规则**：
- `TotalAmount = Σ(Quantity * UnitPrice)`（可再加税额逻辑）。
- 草稿发票不计入应收/应付账龄。
- 创建来源可为：Sales/Procurement 服务发来的 Command 或 API 调用。

#### FR-INV-002 编辑/删除草稿发票

- 仅当 `Status = Draft` 时：
  - 允许修改所有关键字段（Party, Lines 等）。
  - 允许删除发票（逻辑删除或物理删除，由实现决定）。

#### FR-INV-003 发票确认（IssueInvoice）

**目标**：将草稿发票正式生效，计入应收/应付。

**输入**：InvoiceId。

**校验**：
- Status 必须为 Draft。
- TotalAmount > 0。
- PartyId 不为空。

**结果**：
- Status: Draft → Issued。
- 触发事件：`InvoiceIssuedEvent`。
- 读模型中标记计入应收/应付余额。

#### FR-INV-004 发票作废（CancelInvoice）

**条件**：
- Status ∈ { Draft, Issued }。
- 发票没有任何 PaymentRecord。

**结果**：
- Status → Cancelled。
- 不再计入应收/应付余额、账龄统计。

#### FR-INV-005 发票坏账核销（WriteOffInvoice）

**场景（应收）**：
- 客户长期欠款无法收回，需要计提坏账并核销。

**条件**：
- Status ∈ { Issued, PartiallyPaid }。
- OutstandingAmount > 0。

**输入**：
- Reason（必填）：核销原因。
- WriteOffDate（必填）。

**结果**：
- 业务上视为 OutstandingAmount 被核销，Status → WrittenOff。
- 触发 `InvoiceWrittenOffEvent`，供总账/报表处理坏账准备（未来）。

### 4.2 收款 / 付款功能

#### FR-PAY-001 创建收款/付款记录（RecordPayment）

**目标**：为某张发票记录一笔收款/付款，并更新已收/付金额和状态。

**输入**：
- InvoiceId（必填）。
- Amount（必填，>0）。
- PaymentDate（必填）。
- PaymentMethod（必填，枚举：Cash/BankTransfer/Cheque/E-Payment/Other）。
- BankAccountId（选填）。
- ReferenceNo（选填）。
- Comment（选填）。

**校验**：
- 发票 Status ∉ { Cancelled, WrittenOff }。
- `Amount <= OutstandingAmount`。

**结果**：
- 新增一条 PaymentRecord。
- 发票：
  - NewPaidAmount = PaidAmount + Amount。
  - NewOutstanding = TotalAmount - NewPaidAmount。
  - 若 NewOutstanding == 0 → Status = FullyPaid。
  - 若 NewOutstanding > 0 且原 Status = Issued → Status = PartiallyPaid。

#### FR-PAY-002 查询发票收付款明细

- 接口：`GET /api/v1/finance/invoices/{id}/payments`。
- 返回该发票对应的所有 PaymentRecord，按日期排序。

### 4.3 账龄与逾期分析功能

#### FR-AGE-001 应收账龄分析

**接口**：
- `GET /api/v1/finance/invoices/aging-analysis?type=AR&asOf=2026-02-06&customerId=...`

**输出结构示例**：
```json
{
  "asOfDate": "2026-02-06",
  "type": "AR",
  "buckets": [
    { "name": "0-30",  "fromDays": 0,  "toDays": 30,  "amount": 120000.50 },
    { "name": "31-60", "fromDays": 31, "toDays": 60, "amount": 80000.00 },
    { "name": "61-90", "fromDays": 61, "toDays": 90, "amount": 15000.00 },
    { "name": ">90",  "fromDays": 91, "toDays": null, "amount": 5000.00 }
  ],
  "total": 220000.50
}
```

**计算规则**：
- 仅统计 Status ∈ { Issued, PartiallyPaid }。
- 取每张发票 OutstandingAmount，根据 `asOfDate - InvoiceDate` 的天数放入相应区间。

#### FR-AGE-002 逾期发票列表

**接口**：
- `GET /api/v1/finance/invoices/overdue?type=AR&customerId=...`

**规则**：
- 逾期定义：`asOfDate > DueDate` 且 OutstandingAmount > 0。
- 返回数据包括：InvoiceNumber、Customer、InvoiceDate、DueDate、OutstandingAmount、DaysOverdue。

---

## 5. 业务流程（端到端）

### 5.1 应收流程（销售收款）

```text
Sales 创建销售订单 → Sales 服务生成发货记录 →
Finance 创建 AR 发票 (Draft) → 确认发票 (Issued) →
客户分期或一次付款 → 多次 RecordPayment →
PaidAmount 累加，OutstandingAmount 归零 → Status = FullyPaid →
参与账龄统计，客户应收余额下降
```

### 5.2 应付流程（采购付款）

```text
采购订单创建 → 收货 (Inventory 记入库存) →
供应商开具发票 → Finance 创建 AP 发票 (Draft) → Issue →
内部付款审批流程（可扩展） → 实际付款 → RecordPayment →
OutstandingAmount 归零 → 应付结清
```

---

## 6. 数据模型（领域模型视角）

### 6.1 Invoice 聚合根（精简字段定义）

```csharp
public class Invoice : SnapshotAggregateRoot<Invoice, InvoiceId, InvoiceSnapshot>
{
    // 标识
    public string InvoiceNumber { get; private set; }
    public InvoiceType InvoiceType { get; private set; } // AR / AP

    // 往来对象
    public string PartyId { get; private set; }          // 客户或供应商
    public string PartyName { get; private set; }

    // 金额与日期
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string Currency { get; private set; }

    // 状态
    public InvoiceStatus Status { get; private set; }

    // 明细
    public IReadOnlyList<InvoiceLine> Lines => _state.Lines; // 在 State 中维护

    // 历史记录，如 PaymentHistory 由 ReadModel 层维护
}
```

### 6.2 InvoiceLine 值对象

```csharp
public class InvoiceLine
{
    public string LineNumber { get; private set; }
    public string MaterialId { get; private set; }
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; } // Quantity * UnitPrice
    public decimal? TaxRate { get; private set; }
}
```

### 6.3 PaymentRecord 读模型（或单独聚合）

读取视图中 PaymentHistory 已实现，你可以进一步形式化为 read model：

```csharp
public class PaymentRecord
{
    public string Id { get; set; }
    public string InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; }
    public string BankAccountId { get; set; }
    public string ReferenceNo { get; set; }
    public string Comment { get; set; }
}
```

---

## 7. API 设计（实现导向）

### 7.1 发票 API

1. **创建发票（草稿）**

```http
POST /api/v1/finance/invoices
Content-Type: application/json

{
  "invoiceType": 1,               // 1=AR,2=AP
  "externalInvoiceNumber": "FP2026-0001",
  "partyId": "CUS-0001",
  "partyName": "上海某客户",
  "invoiceDate": "2026-02-06",
  "dueDate": "2026-03-08",
  "currency": "CNY",
  "lines": [
    {
      "lineNumber": "1",
      "materialId": "MAT-001",
      "description": "产品A",
      "quantity": 10,
      "unitPrice": 100.00,
      "taxRate": 0.13
    }
  ]
}
```

2. **确认发票**

```http
POST /api/v1/finance/invoices/{id}/issue
```

3. **作废发票**

```http
POST /api/v1/finance/invoices/{id}/cancel
```

4. **坏账核销**

```http
POST /api/v1/finance/invoices/{id}/write-off
Content-Type: application/json
{
  "reason": "客户破产清算",
  "writeOffDate": "2026-12-31"
}
```

5. **查询发票**

```http
GET /api/v1/finance/invoices?
    type=AR&partyId=CUS-0001&status=Issued&fromDate=2026-01-01&toDate=2026-12-31
```

### 7.2 收付款 API

1. **记录收款/付款**

```http
POST /api/v1/finance/invoices/{id}/payments
Content-Type: application/json

{
  "amount": 500.00,
  "paymentDate": "2026-02-20",
  "paymentMethod": "BankTransfer",
  "bankAccountId": "BANK-001",
  "referenceNo": "20260220-000123",
  "comment": "首期付款"
}
```

2. **查询某发票收付款明细**

```http
GET /api/v1/finance/invoices/{id}/payments
```

### 7.3 账龄与逾期 API

1. **账龄分析**

```http
GET /api/v1/finance/invoices/aging-analysis?type=AR&asOf=2026-02-06&partyId=CUS-0001
```

2. **逾期发票列表**

```http
GET /api/v1/finance/invoices/overdue?type=AR&asOf=2026-02-06
```

---

## 8. 集成设计

### 8.1 与 Sales 服务集成

- 销售订单完成发货后，由 Sales 通过 Command 或 API 请求 Finance 创建 AR 发票：
  - 传入：客户、订单号、金额、行项目。
  - Finance 创建 Draft Invoice，自动设置类型为 AR。

### 8.2 与 Procurement 服务集成

- 收货完成后，由 Procurement 创建 AP 发票：
  - 传入：供应商、PO 号、收货行金额。

### 8.3 与 MasterData 服务集成

- PartyId/Name 由 MasterData 提供（客户/供应商）。
- 物料编码、名称由 MasterData 提供（仅冗余到行项）。
- 后续成本相关可从 MasterData 读取标准成本。

---

## 9. 非功能与合规需求

### 9.1 性能

- 常规发票列表查询（分页 20 条）响应时间：≤ 300ms（正常数据量下）。
- 账龄分析接口：≤ 1s（在 10 万张发票级别，需合理索引）。

### 9.2 安全

- 所有 API 必须通过 Identity 服务验证 JWT。
- 发票与收付款操作必须记录操作人（从 Token 中解析）。
- 高风险操作（写 off、取消）需后续增加审批/二次确认。

### 9.3 审计

- 每个发票的状态变更需保留事件记录（ES 已天然满足）。
- 读模型层应提供“变更历史查询”接口（后续迭代）。

---

## 10. 实现优先级（落地指南）

### P0（当前必须实现）

1. Invoice 聚合根（AR/AP）+ 事件：Issued / PaymentRecorded / WrittenOff / Cancelled。  
2. 基本 CRUD API：创建草稿 → 确认 → 列表查询。  
3. Payment 记录 + 发票状态联动。  
4. 账龄分析 & 逾期统计读模型实现。

### P1（下一个迭代）

1. 简单的 AP 应付流程（与采购集成）。
2. 付款审批占位（可先手工通过，后续接审批流）。
3. 银行账户基础数据，与 Payment 关联。

### P2（后续）

1. 总账、凭证、期末结账。
2. 成本核算、库存成本结转。
3. 税务计算与报表。
4. 固定资产与折旧。

---

> 你后续在实现时，可以严格按本 PRD 的章节顺序来：先把 Invoice 的状态机和 Payment 写完整，再补 Aging/Overdue 的读模型和 API，然后再往 AP/总账方向扩展。
