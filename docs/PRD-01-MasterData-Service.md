# 主数据管理服务 (MasterData Service) - 产品需求文档 (PRD)

## 1. 产品概述

### 1.1 服务定位
主数据管理服务是整个ERP系统的基础数据中心，负责管理所有业务实体的主数据，包括物料、供应商、客户、仓库、货币、计量单位等核心主数据。

### 1.2 业务价值
- 统一管理企业核心主数据，保证数据一致性
- 为其他业务服务提供可靠的基础数据支撑
- 支持主数据的版本管理和审计追踪
- 实现主数据的生命周期管理

### 1.3 核心用户
- 主数据管理员
- 物料工程师
- 采购专员
- 仓库管理员
- 系统管理员

---

## 2. 功能需求

### 2.1 物料主数据管理

#### 2.1.1 物料基础信息管理

**功能描述**
管理物料的完整生命周期，包括创建、修改、启用、停用、删除等操作。

**详细需求**

**FR-MD-001：创建物料**
- 输入字段：
  - 物料编码（必填，唯一，规则：MAT-YYYYMMDD-XXXX）
  - 物料名称（必填，中英文，最长100字符）
  - 物料类型（必填，枚举：原材料/半成品/成品/服务/工具/消耗品/备品备件）
  - 物料描述（选填，最长500字符）
  - 计量单位（必填，关联单位主数据）
  - 基本单位（必填）
  - 辅助单位（选填，支持多个）
  - 物料分类（必填，树形结构，最多5级）
  - 规格型号（选填，最长50字符）
  - 品牌（选填，最长50字符）
  - 制造商（选填，关联供应商）
- 业务规则：
  - 物料编码生成规则：MAT-日期(8位)-流水号(4位)
  - 同一物料分类下，物料名称+规格型号不能重复
  - 创建时默认状态为"草稿"
  - 必须审批通过后才能启用
- 触发事件：MaterialCreatedEvent

**FR-MD-002：编辑物料基础信息**
- 可编辑字段：
  - 物料名称
  - 物料描述
  - 规格型号
  - 品牌
  - 制造商
  - 物料分类（需审批）
- 业务规则：
  - 物料编码不可修改
  - 物料类型不可修改（需新建）
  - 已有业务单据的物料，修改需审批
  - 修改记录保留完整历史
- 触发事件：MaterialUpdatedEvent

**FR-MD-003：物料标准成本管理**
- 功能：
  - 设置物料标准成本
  - 标准成本历史追踪
  - 成本变更审批流程
  - 成本差异分析
- 字段：
  - 标准成本（必填，精度4位小数）
  - 生效日期（必填）
  - 失效日期（选填）
  - 成本构成明细（材料成本/人工成本/制造费用/管理费用）
  - 变更原因（必填，最长200字符）
  - 审批人
  - 审批时间
- 业务规则：
  - 同一物料的成本版本不能有日期重叠
  - 成本变更需要财务经理审批
  - 历史成本不可删除，只能新增版本
  - 成本差异超过10%需要总经理审批
- 触发事件：StandardCostUpdatedEvent

**FR-MD-004：物料启用/停用**
- 启用条件：
  - 物料信息完整
  - 已设置标准成本
  - 审批流程通过
- 停用条件：
  - 无在途采购订单
  - 无库存余额（或允许负库存）
  - 无在制工单
  - 无销售订单预留
- 业务规则：
  - 停用需要部门负责人审批
  - 停用后不能用于新建单据
  - 可以重新启用
  - 保留完整的启用/停用历史
- 触发事件：MaterialActivatedEvent / MaterialDeactivatedEvent

#### 2.1.2 物料扩展属性管理

**FR-MD-005：自定义属性管理**
- 功能：
  - 为不同物料类型配置自定义属性
  - 支持属性模板
  - 支持属性继承
- 属性类型：
  - 文本（单行/多行）
  - 数值（整数/小数）
  - 日期/日期时间
  - 布尔值
  - 枚举（单选/多选）
  - 文件附件
  - 关联选择（关联其他主数据）
- 示例扩展属性：
  - 化工原料：CAS号、危险品等级、保质期
  - 电子元器件：封装类型、电压范围、温度范围
  - 机械零件：材质、硬度、表面处理
  - 服务类：服务周期、服务范围、SLA等级

#### 2.1.3 物料分类管理

**FR-MD-006：物料分类树形结构**
- 功能：
  - 创建多级分类（最多5级）
  - 分类移动/合并
  - 分类属性继承
- 字段：
  - 分类编码（自动生成，层级编码）
  - 分类名称（必填）
  - 父级分类
  - 分类描述
  - 分类属性模板
  - 排序号
- 示例分类结构：
  ```
  ├── 原材料 (01)
  │   ├── 金属材料 (01.01)
  │   │   ├── 钢材 (01.01.01)
  │   │   └── 铝材 (01.01.02)
  │   └── 化工原料 (01.02)
  ├── 半成品 (02)
  └── 成品 (03)
      ├── 标准品 (03.01)
      └── 定制品 (03.02)
  ```

#### 2.1.4 物料多单位管理

**FR-MD-007：计量单位转换**
- 功能：
  - 定义基本单位和辅助单位
  - 配置单位转换关系
  - 支持动态转换率
- 示例：
  - 基本单位：个，辅助单位：箱，转换率：1箱=12个
  - 基本单位：公斤，辅助单位：吨，转换率：1吨=1000公斤
  - 基本单位：米，辅助单位：卷，转换率：动态（每卷长度不同）
- 业务规则：
  - 库存管理使用基本单位
  - 采购/销售可使用辅助单位
  - 自动换算并记录
  - 精度损失控制

#### 2.1.5 物料关联关系

**FR-MD-008：物料替代关系**
- 功能：
  - 定义主物料和替代物料
  - 设置替代优先级
  - 设置替代条件（库存不足/价格优势等）
- 字段：
  - 主物料
  - 替代物料
  - 替代类型（完全替代/条件替代）
  - 替代比例（1:1或其他比例）
  - 生效日期/失效日期
  - 替代原因

**FR-MD-009：物料BOM关联**
- 功能：
  - 记录物料的BOM（物料清单）
  - 支持多版本BOM
  - BOM成本汇总
- 字段：
  - BOM版本号
  - 生效日期
  - 子项物料列表
  - 用量/损耗率
  - 工序关联

### 2.2 供应商主数据管理

#### 2.2.1 供应商基础信息

**FR-MD-010：供应商档案管理**
- 字段：
  - 供应商编码（唯一，SUP-YYYYMMDD-XXX）
  - 供应商名称（中英文）
  - 供应商类型（枚举：原材料供应商/外协供应商/服务供应商）
  - 统一社会信用代码
  - 法定代表人
  - 注册资本
  - 成立日期
  - 注册地址
  - 经营范围
  - 联系人信息（支持多个）
    - 姓名
    - 职位
    - 手机
    - 邮箱
    - 微信
  - 银行账户信息（支持多个）
    - 开户行
    - 账号
    - 是否默认
  - 税务信息
    - 纳税人识别号
    - 税率
    - 开票信息
  - 资质证书（支持附件上传）
    - 营业执照
    - 生产许可证
    - ISO认证
    - 其他资质

**FR-MD-011：供应商分类分级**
- 供应商分类：
  - 按物料类别分类
  - 按地域分类
  - 按业务类型分类
- 供应商分级：
  - 战略供应商（A级）
  - 核心供应商（B级）
  - 普通供应商（C级）
  - 试用供应商（D级）
- 分级标准：
  - 供货质量
  - 交期准时率
  - 价格竞争力
  - 服务响应速度
  - 合作年限
  - 年度采购额

#### 2.2.2 供应商绩效管理

**FR-MD-012：供应商考核指标**
- KPI指标：
  - 质量合格率（权重30%）
  - 交期准时率（权重25%）
  - 价格竞争力（权重20%）
  - 服务响应速度（权重15%）
  - 配合度（权重10%）
- 考核周期：月度/季度/年度
- 考核结果：优秀/良好/合格/不合格
- 考核措施：
  - 优秀：增加订单份额、优先合作
  - 良好：保持合作
  - 合格：警告、要求改进
  - 不合格：减少订单、暂停合作、淘汰

**FR-MD-013：供应商黑名单管理**
- 黑名单原因：
  - 质量问题严重
  - 多次延期交货
  - 商业欺诈
  - 违反合同
- 黑名单效果：
  - 禁止新建采购订单
  - 系统自动拦截
  - 需高级管理层审批才能解除

### 2.3 客户主数据管理

#### 2.3.1 客户基础信息

**FR-MD-014：客户档案管理**
- 字段：
  - 客户编码（CUS-YYYYMMDD-XXX）
  - 客户名称（中英文）
  - 客户类型（个人客户/企业客户）
  - 客户分类（经销商/终端客户/集团客户）
  - 客户等级（VIP/普通/潜在）
  - 统一社会信用代码（企业客户）
  - 身份证号（个人客户）
  - 联系信息
  - 收货地址（支持多个）
  - 开票信息
  - 信用信息
    - 信用额度
    - 信用期限
    - 当前欠款
    - 历史信用记录
  - 销售区域
  - 所属行业
  - 客户来源
  - 跟进销售员

**FR-MD-015：客户信用管理**
- 功能：
  - 信用额度设置
  - 信用期限设置
  - 超信用额度控制
  - 信用评级
- 信用控制：
  - 软控制：超额提醒，允许继续
  - 硬控制：超额拦截，禁止下单
  - 弹性控制：超额需审批

#### 2.3.2 客户关系管理

**FR-MD-016：客户分组管理**
- 按销售区域分组
- 按行业分组
- 按客户价值分组（ABC分类）
- 自定义分组标签

**FR-MD-017：客户生命周期管理**
- 生命周期阶段：
  - 潜在客户
  - 新客户
  - 成长期客户
  - 成熟期客户
  - 衰退期客户
  - 流失客户
- 自动识别规则：
  - 根据首单时间、订单频次、订单金额自动判断
  - 系统自动提醒客户流失风险

### 2.4 仓库主数据管理

**FR-MD-018：仓库档案管理**
- 字段：
  - 仓库编码（WH-XXX）
  - 仓库名称
  - 仓库类型（原材料仓/半成品仓/成品仓/备品备件仓）
  - 仓库地址
  - 仓库面积
  - 仓库容量
  - 负责人
  - 是否启用库位管理
  - 是否启用批次管理
  - 是否启用序列号管理
  - 温湿度要求（特殊仓库）
  - 是否虚拟仓库

**FR-MD-019：库位管理**
- 库位结构：区域-货架-层-位
- 示例：A区-01架-03层-05位 → A-01-03-05
- 字段：
  - 库位编码
  - 库位类型（标准/大件/危险品）
  - 容量限制
  - 当前存储物料
  - 占用率
- 功能：
  - 库位分配策略（就近原则/ABC分类）
  - 库位利用率分析
  - 库位优化建议

### 2.5 其他主数据管理

**FR-MD-020：计量单位管理**
- 基础单位：个、件、台、套、公斤、克、米、升等
- 单位分类：数量单位/重量单位/长度单位/体积单位/时间单位
- 单位转换关系配置

**FR-MD-021：货币管理**
- 支持货币：CNY、USD、EUR、JPY等
- 汇率管理：
  - 手动维护汇率
  - 自动获取汇率（接入汇率API）
  - 历史汇率查询
- 汇率生效日期

**FR-MD-022：国家/地区管理**
- 国家列表
- 省份/州列表
- 城市列表
- 关联邮政编码

**FR-MD-023：支付方式管理**
- 支付方式：现金/银行转账/支票/承兑汇票/信用证/支付宝/微信
- 支付条件：预付/货到付款/月结/账期N天

**FR-MD-024：运输方式管理**
- 运输方式：陆运/海运/空运/快递/自提
- 承运商管理
- 运费计算规则

---

## 3. 业务流程

### 3.1 物料主数据创建流程

```
物料工程师创建物料 
  → 填写基础信息
  → 设置扩展属性
  → 提交审批
  → 部门负责人审批
  → 财务设置标准成本
  → 审批通过
  → 物料启用
  → 发布MaterialCreatedEvent
  → 其他服务订阅并同步
```

### 3.2 供应商准入流程

```
采购专员提交供应商信息
  → 填写基础档案
  → 上传资质证书
  → 供应商评估
    ├── 现场考察
    ├── 样品测试
    └── 信用调查
  → 供应商等级评定
  → 采购经理审批
  → 总经理审批（A级供应商）
  → 供应商入库
  → 开始试用期（3个月）
  → 转正/淘汰
```

### 3.3 客户信用额度申请流程

```
销售员提交信用额度申请
  → 填写客户信息
  → 提供客户资信证明
  → 设置申请额度
  → 销售经理审批
  → 财务经理审批（5万以下）
  → 总经理审批（5万以上）
  → 信用额度生效
  → 定期复核（每年）
```

---

## 4. 数据模型

### 4.1 Material（物料）核心字段

```csharp
public class Material : AggregateRoot<Material, MaterialId>
{
    // 基础信息
    public string MaterialCode { get; private set; }          // 物料编码
    public string MaterialName { get; private set; }          // 物料名称
    public MaterialType MaterialType { get; private set; }    // 物料类型
    public string UnitOfMeasure { get; private set; }         // 基本单位
    public string Specification { get; private set; }         // 规格型号
    public string Brand { get; private set; }                 // 品牌
    public string CategoryId { get; private set; }            // 分类ID
    
    // 成本信息
    public decimal StandardCost { get; private set; }         // 标准成本
    public DateTime CostEffectiveDate { get; private set; }   // 成本生效日期
    public List<CostHistory> CostHistory { get; private set; } // 成本历史
    
    // 库存信息
    public decimal SafetyStock { get; private set; }          // 安全库存
    public decimal MinOrderQty { get; private set; }          // 最小订购量
    public decimal LeadTime { get; private set; }             // 采购提前期（天）
    
    // 供应商信息
    public List<string> PreferredSuppliers { get; private set; } // 首选供应商列表
    
    // 状态信息
    public bool IsActive { get; private set; }                // 是否启用
    public DateTime CreatedAt { get; private set; }           // 创建时间
    public string CreatedBy { get; private set; }             // 创建人
    public DateTime? LastModifiedAt { get; private set; }     // 最后修改时间
    public string LastModifiedBy { get; private set; }        // 最后修改人
    
    // 扩展属性
    public Dictionary<string, object> ExtendedAttributes { get; private set; }
}
```

### 4.2 Supplier（供应商）核心字段

```csharp
public class Supplier : AggregateRoot<Supplier, SupplierId>
{
    public string SupplierCode { get; private set; }
    public string SupplierName { get; private set; }
    public SupplierType SupplierType { get; private set; }
    public SupplierLevel Level { get; private set; }          // A/B/C/D级
    public string CreditCode { get; private set; }            // 统一社会信用代码
    public List<ContactPerson> Contacts { get; private set; }
    public List<BankAccount> BankAccounts { get; private set; }
    public PerformanceMetrics Performance { get; private set; } // 绩效指标
    public bool IsBlacklisted { get; private set; }           // 是否黑名单
    public decimal AnnualPurchaseAmount { get; private set; } // 年度采购额
}
```

### 4.3 Customer（客户）核心字段

```csharp
public class Customer : AggregateRoot<Customer, CustomerId>
{
    public string CustomerCode { get; private set; }
    public string CustomerName { get; private set; }
    public CustomerType Type { get; private set; }
    public CustomerLevel Level { get; private set; }          // VIP/普通
    public decimal CreditLimit { get; private set; }          // 信用额度
    public int CreditPeriod { get; private set; }             // 信用期限（天）
    public decimal CurrentArrears { get; private set; }       // 当前欠款
    public List<ShippingAddress> Addresses { get; private set; }
    public string SalesPersonId { get; private set; }         // 负责销售员
    public CustomerLifecycleStage LifecycleStage { get; private set; }
}
```

---

## 5. API设计

### 5.1 物料管理API

#### 5.1.1 创建物料
```http
POST /api/v1/masterdata/materials
Content-Type: application/json

{
  "materialCode": "MAT-20260206-0001",
  "materialName": "304不锈钢板",
  "materialType": 1,
  "specification": "2mm*1000mm*2000mm",
  "unitOfMeasure": "张",
  "categoryId": "01.01.01",
  "standardCost": 1250.00,
  "safetyStock": 100,
  "minOrderQty": 10,
  "leadTime": 15,
  "extendedAttributes": {
    "material": "304不锈钢",
    "surfaceTreatment": "拉丝",
    "thickness": "2mm"
  }
}
```

#### 5.1.2 查询物料列表
```http
GET /api/v1/masterdata/materials?keyword=钢板&categoryId=01.01&isActive=true&pageIndex=1&pageSize=20
```

#### 5.1.3 获取物料详情
```http
GET /api/v1/masterdata/materials/{id}
```

#### 5.1.4 更新物料标准成本
```http
POST /api/v1/masterdata/materials/{id}/update-cost
Content-Type: application/json

{
  "newStandardCost": 1300.00,
  "effectiveDate": "2026-03-01",
  "reason": "原材料价格上涨",
  "costBreakdown": {
    "materialCost": 1000.00,
    "laborCost": 150.00,
    "overheadCost": 150.00
  }
}
```

#### 5.1.5 启用/停用物料
```http
POST /api/v1/masterdata/materials/{id}/activate
POST /api/v1/masterdata/materials/{id}/deactivate
```

#### 5.1.6 物料替代品查询
```http
GET /api/v1/masterdata/materials/{id}/substitutes
```

#### 5.1.7 物料成本历史
```http
GET /api/v1/masterdata/materials/{id}/cost-history?startDate=2025-01-01&endDate=2026-12-31
```

### 5.2 供应商管理API

#### 5.2.1 创建供应商
```http
POST /api/v1/masterdata/suppliers
```

#### 5.2.2 供应商绩效评分
```http
POST /api/v1/masterdata/suppliers/{id}/performance-rating
Content-Type: application/json

{
  "period": "2026-Q1",
  "qualityRate": 98.5,
  "onTimeDeliveryRate": 95.0,
  "priceCompetitiveness": 88.0,
  "serviceRating": 92.0,
  "overallScore": 94.0,
  "level": "A"
}
```

#### 5.2.3 供应商黑名单管理
```http
POST /api/v1/masterdata/suppliers/{id}/blacklist
DELETE /api/v1/masterdata/suppliers/{id}/blacklist
```

### 5.3 客户管理API

#### 5.3.1 创建客户
```http
POST /api/v1/masterdata/customers
```

#### 5.3.2 更新客户信用额度
```http
POST /api/v1/masterdata/customers/{id}/credit-limit
Content-Type: application/json

{
  "creditLimit": 100000.00,
  "creditPeriod": 30,
  "approvedBy": "财务经理"
}
```

#### 5.3.3 客户信用查询
```http
GET /api/v1/masterdata/customers/{id}/credit-status
```

### 5.4 仓库管理API

#### 5.4.1 创建仓库
```http
POST /api/v1/masterdata/warehouses
```

#### 5.4.2 库位管理
```http
GET /api/v1/masterdata/warehouses/{id}/locations
POST /api/v1/masterdata/warehouses/{id}/locations
```

---

## 6. 非功能需求

### 6.1 性能需求
- 物料查询响应时间 < 200ms
- 物料创建响应时间 < 500ms
- 支持10万级物料数据
- 支持5万级供应商/客户数据
- 并发用户数 > 200

### 6.2 安全需求
- 主数据修改需要审批
- 敏感字段（成本、信用额度）权限控制
- 操作日志完整记录
- 数据加密存储（敏感信息）

### 6.3 可用性需求
- 系统可用性 > 99.5%
- 支持主备切换
- 数据实时备份

### 6.4 审计需求
- 记录所有变更历史
- 支持任意时间点数据回溯
- 导出审计报表

---

## 7. 集成需求

### 7.1 与其他服务集成

**7.1.1 与Finance服务集成**
- 同步物料标准成本
- 供应商付款信息
- 客户收款信息

**7.1.2 与Procurement服务集成**
- 提供供应商主数据
- 提供物料主数据
- 接收采购价格反馈

**7.1.3 与Inventory服务集成**
- 提供物料主数据
- 提供仓库/库位数据
- 同步安全库存

**7.1.4 与Sales服务集成**
- 提供客户主数据
- 提供物料销售价格
- 同步客户信用信息

### 7.2 领域事件发布

```csharp
// 物料相关事件
- MaterialCreatedEvent          // 物料创建
- MaterialUpdatedEvent          // 物料更新
- StandardCostUpdatedEvent      // 成本更新
- MaterialActivatedEvent        // 物料启用
- MaterialDeactivatedEvent      // 物料停用

// 供应商相关事件
- SupplierCreatedEvent          // 供应商创建
- SupplierLevelChangedEvent     // 供应商等级变更
- SupplierBlacklistedEvent      // 供应商拉黑

// 客户相关事件
- CustomerCreatedEvent          // 客户创建
- CreditLimitChangedEvent       // 信用额度变更
- CustomerLifecycleChangedEvent // 客户生命周期变更
```

---

## 8. 实现优先级

### P0 - 核心功能（必须实现）
1. 物料基础信息CRUD
2. 物料标准成本管理
3. 物料启用/停用
4. 供应商档案管理
5. 客户档案管理
6. 仓库档案管理

### P1 - 重要功能（优先实现）
1. 物料分类管理
2. 物料多单位转换
3. 供应商绩效管理
4. 客户信用管理
5. 库位管理
6. 主数据审批流程

### P2 - 增强功能（后续实现）
1. 物料替代品管理
2. 物料BOM关联
3. 供应商黑名单
4. 客户生命周期管理
5. 主数据导入导出
6. 主数据质量检查

### P3 - 可选功能（未来规划）
1. 主数据版本管理
2. 主数据对账
3. 主数据分析报表
4. AI智能推荐
5. 移动端支持

---

## 9. 验收标准

### 9.1 功能验收
- [ ] 所有P0功能完整实现并通过测试
- [ ] 物料创建到启用完整流程可用
- [ ] 支持物料成本历史追踪
- [ ] 供应商/客户档案管理功能完整
- [ ] API接口符合设计规范
- [ ] 集成事件正常发布

### 9.2 性能验收
- [ ] 查询响应时间达标
- [ ] 并发测试通过
- [ ] 大数据量测试通过

### 9.3 安全验收
- [ ] 权限控制生效
- [ ] 审批流程有效
- [ ] 审计日志完整

---

## 10. 风险与挑战

### 10.1 主要风险
1. **数据一致性风险**：主数据在多个服务间同步可能不一致
2. **性能风险**：大量物料查询可能影响性能
3. **审批流程复杂性**：多级审批可能影响效率

### 10.2 应对措施
1. 采用最终一致性模型，通过领域事件同步
2. 实现缓存机制，优化查询性能
3. 支持灵活配置审批流程，特殊情况可快速通道

---

## 附录

### A. 术语表
- **主数据**：企业核心业务实体的基础数据
- **标准成本**：预先设定的产品成本标准
- **信用额度**：允许客户赊账的最大金额
- **安全库存**：为防止缺货而设置的最低库存量
- **提前期**：从下单到收货的时间周期

### B. 参考文档
- EventFlow技术文档
- DDD领域驱动设计
- ERP主数据管理最佳实践
