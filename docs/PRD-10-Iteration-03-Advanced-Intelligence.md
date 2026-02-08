# ERP 系统迭代计划 (Iteration 03) - 产品需求文档 (PRD)

## 1. 迭代目标：从“自动化”迈向“预测与治理”

Iteration 02 实现了核心业务的自动化与实时核算。本迭代 (Iteration 03) 将参考 **Odoo 18** 的前沿特性，重点发力**数据预测**、**质量控制**与**设备治理**，将系统能力提升至工业 4.0 的预备阶段。

### 迭代核心关键词：
- **AI 需求预测**：基于历史数据的智能补货建议。
- **全链路质量控制 (QC)**：嵌入库存与生产的质量检查。
- **预防性维护 (Maintenance)**：设备状态监控与维护计划。
- **实时 BI 大屏**：基于 TimescaleDB 的多维度分析仪表盘。

---

## 2. 核心功能模块

### 2.1 AI 驱动的需求预测 (AI Demand Forecasting)
> 利用 TimescaleDB 中的历史流水数据，构建初步的预测模型。

**FR-PREDICT-001：销售需求预测模型**
- **功能描述**：分析过去 12 个月的销售/出库趋势，预测未来 30 天的需求量。
- **核心逻辑**：
  - 导出 `inventory_transactions_ts` 的时序特征。
  - 应用移动平均或线性回归算法（初期支持简单算法，提供 ML 接入点）。
  - **预测输出**：直接反馈至 MRP 引擎的 `Safety Stock` 计算。

**FR-PREDICT-002：财务现金流预测**
- **功能描述**：基于应收/应付账款和销售预测，提供未来 3 个月的现金流趋势图。

---

### 2.2 全链路质量管理 (Quality Control 2.0)
> 参考 Odoo 18 的 "On-demand Control"。

**FR-QUALITY-001：生产关键点质检 (Control Points)**
- **功能描述**：在多级 BOM 的特定工序设置质检点。
- **业务规则**：
  - 质检未通过，生产订单自动挂起或拆分出不良品。
  - 支持“随机抽检”与“全检”配置。

**FR-QUALITY-002：进料/出货检验 (IQC/OQC)**
- **功能描述**：采购收货或销售发货前，强制生成质检单。

---

### 2.3 预防性设备维护 (Asset Maintenance)
> 实现生产力的持久保障。

**FR-MAINT-001：预防性维护计划**
- **功能描述**：根据设备运行时间或周期，自动生成维护保养工单。
- **业务价值**：降低非计划停机时间（Downtime）。

**FR-MAINT-002：仪表盘集成 (IoT Ready)**
- **功能描述**：提供设备状态看板，预留传感器数据接入接口。

---

### 2.4 BI 实时分析大屏 (Advanced Analytics)
> 释放 TimescaleDB 的时序数据价值。

**FR-BI-001：库存流转率看板**
- **功能描述**：实时显示库龄分析、周转率趋势、呆滞料预警。
- **技术实现**：直接在 `daily_inventory_summary` 聚合视图上构建查询。

**FR-BI-002：生产效率看板 (OEE)**
- **功能描述**：展示生产进度、良率、设备利用率等核心指标。

---

## 3. 技术架构演进

### 3.1 预测模型抽象层
- 建立 `PredictiveAnalytics` 微服务。
- 使用 Python (FastAPI) 或 .NET ML 库进行算法实现。
- 与 Dapr State Store 结合存储模型权重。

### 3.2 质检状态机集成
- 在 `Production` 和 `Inventory` 服务中引入 `QualityState` 转换逻辑。

---

## 4. 实施阶段计划

1. **Stage 1: 质量与治理** (Quality & Maintenance Foundations)
2. **Stage 2: 预测能力** (AI & Forecasting Models)
3. **Stage 3: 视觉化决策** (BI Dashboards & OEE)
