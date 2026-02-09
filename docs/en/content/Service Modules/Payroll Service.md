# Payroll Service

<cite>
**Referenced Files in This Document**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs)
- [appsettings.json](file://src/Services/Payroll/ErpSystem.Payroll/appsettings.json)
- [EventStore.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DDDBase.cs)
- [DomainEventDispatcher.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DomainEventDispatcher.cs)
</cite>

## Table of Contents
1. [Introduction](#introduction)
2. [Project Structure](#project-structure)
3. [Core Components](#core-components)
4. [Architecture Overview](#architecture-overview)
5. [Detailed Component Analysis](#detailed-component-analysis)
6. [Dependency Analysis](#dependency-analysis)
7. [Performance Considerations](#performance-considerations)
8. [Troubleshooting Guide](#troubleshooting-guide)
9. [Conclusion](#conclusion)
10. [Appendices](#appendices)

## Introduction
This document describes the Payroll service responsible for comprehensive payroll processing and compliance within the ERP system. It covers the payroll calculation engine (salary computations, components, deductions), tax and withholding management, benefit administration, pay slip generation, direct deposit processing, tax document preparation, compliance reporting, API endpoints, integrations with HR, Finance, and Tax services, and batch processing capabilities including year-end reporting.

## Project Structure
The Payroll service follows a clean architecture with separate concerns for API controllers, domain aggregates, infrastructure persistence and projections, and building blocks for event sourcing and domain events.

```mermaid
graph TB
subgraph "Payroll Service"
Controllers["Controllers<br/>SalaryStructuresController<br/>PayrollRunsController<br/>PayslipsController"]
Domain["Domain Aggregates<br/>SalaryStructure<br/>PayrollRun<br/>Payslip"]
Infra["Infrastructure<br/>EventStoreDbContext<br/>PayrollReadDbContext<br/>Projections"]
Settings["Configuration<br/>appsettings.json"]
end
Controllers --> Domain
Domain --> Infra
Controllers --> Infra
Infra --> Settings
```

**Diagram sources**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs#L1-L45)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L1-L278)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L1-L429)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L1-L121)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L1-L171)
- [appsettings.json](file://src/Services/Payroll/ErpSystem.Payroll/appsettings.json#L1-L12)

**Section sources**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs#L1-L45)
- [appsettings.json](file://src/Services/Payroll/ErpSystem.Payroll/appsettings.json#L1-L12)

## Core Components
- Event-sourced domain model with aggregates for salary structures, payroll runs, and payslips.
- Controllers exposing REST endpoints for salary structures, payroll runs, and payslips.
- Event store and read model projections for durable event sourcing and efficient querying.
- Configuration for database connections and Swagger API documentation.

Key responsibilities:
- Manage salary structures with components and deductions.
- Create and process payroll runs with status transitions.
- Generate payslips and track payment status.
- Provide read-side queries for analytics and reporting.

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L183-L429)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L1-L278)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L1-L121)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L1-L171)

## Architecture Overview
The Payroll service uses event sourcing to capture state changes as a sequence of domain events. Controllers orchestrate commands against aggregates, which emit events captured by the event store. Projections update read models for efficient querying.

```mermaid
graph TB
Client["Client"]
C1["SalaryStructuresController"]
C2["PayrollRunsController"]
C3["PayslipsController"]
ES["EventStore (IEventStore)"]
ESDb["EventStoreDbContext"]
RDb["PayrollReadDbContext"]
P1["SalaryStructure Projection Handler"]
P2["PayrollRun Projection Handler"]
Client --> C1
Client --> C2
Client --> C3
C1 --> ES
C2 --> ES
C3 --> ES
ES --> ESDb
ES --> P1
ES --> P2
P1 --> RDb
P2 --> RDb
```

**Diagram sources**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs#L10-L26)
- [EventStore.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DDDBase.cs#L53-L87)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L8-L60)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L9-L171)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L1-L278)

## Detailed Component Analysis

### Payroll Calculation Engine
The calculation engine centers around aggregates and value objects:
- SalaryStructure computes total earnings from base salary and components, supporting fixed and percentage-based additions.
- PayrollRun aggregates payslips and tracks totals and counts.
- Payslip holds per-employee earnings, deductions, and payment metadata.

```mermaid
classDiagram
class SalaryStructure {
+string Name
+string? Description
+decimal BaseSalary
+string Currency
+bool IsActive
+SalaryComponent[] Components
+Deduction[] Deductions
+DateTime CreatedAt
+decimal TotalEarnings
+Create(...)
+AddComponent(...)
+AddDeduction(...)
}
class PayrollRun {
+string RunNumber
+int Year
+int Month
+DateTime PaymentDate
+string? Description
+PayrollRunStatus Status
+Payslip[] Payslips
+DateTime CreatedAt
+DateTime? ApprovedAt
+string? ApprovedByUserId
+decimal TotalGrossAmount
+decimal TotalDeductions
+decimal TotalNetAmount
+int EmployeeCount
+int PaidCount
+Create(...)
+AddPayslip(...)
+StartProcessing()
+SubmitForApproval()
+Approve(approvedByUserId)
+MarkPayslipAsPaid(payslipId, paymentMethod, transactionRef)
+Cancel(reason)
}
class Payslip {
+Guid Id
+string PayslipNumber
+string EmployeeId
+string EmployeeName
+decimal GrossAmount
+decimal TotalDeductions
+decimal NetAmount
+PayslipStatus Status
+DateTime? PaidAt
+string? PaymentMethod
+string? TransactionRef
+PayslipLine[] Lines
}
class SalaryComponent {
+Guid Id
+string Name
+SalaryComponentType Type
+decimal Amount
+bool IsPercentage
+bool IsTaxable
}
class Deduction {
+Guid Id
+string Name
+DeductionType Type
+decimal Amount
+bool IsPercentage
}
class PayslipLine {
+string Description
+decimal Amount
+bool IsDeduction
}
SalaryStructure --> SalaryComponent : "has many"
SalaryStructure --> Deduction : "has many"
PayrollRun --> Payslip : "contains many"
```

**Diagram sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L183-L429)

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L183-L429)

### Tax Calculation and Withholding Management
- Components and deductions support tax-related entries via enums for income tax and social insurance types.
- Net amount computation occurs during payslip creation by subtracting total deductions from gross amount.
- Future enhancements can integrate external tax services for dynamic tax rates and brackets.

Implementation notes:
- Income tax and social security are modeled as deduction types.
- Percentage-based components/deductions enable scalable tax modeling.

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L7-L47)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L298-L301)

### Benefit Administration
- Benefits such as health insurance, retirement contributions, and paid time off can be represented as salary components or deductions.
- Components support percentage-based allocations for benefits like pension contributions.
- Deductions accommodate mandatory benefit deductions.

Operational guidance:
- Define benefit components in salary structures.
- Track benefit-related lines in payslip lines for transparency.

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L158-L179)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L196-L201)

### Pay Slip Generation and Direct Deposit
- Pay slips are generated within a payroll run with computed gross, total deductions, and net amounts.
- Payment tracking includes method and optional transaction reference for direct deposit reconciliation.

```mermaid
sequenceDiagram
participant Client as "Client"
participant Ctrl as "PayrollRunsController"
participant ES as "EventStore"
participant Run as "PayrollRun Aggregate"
participant Proj as "Projection Handlers"
Client->>Ctrl : POST /api/v1/payroll/payroll-runs/{id}/payslips
Ctrl->>ES : LoadAggregateAsync(PayrollRun)
ES-->>Ctrl : PayrollRun
Ctrl->>Run : AddPayslip(number, employee, gross, deductions, lines)
Run-->>Ctrl : PayslipGeneratedEvent
Ctrl->>ES : SaveAggregateAsync(Run)
ES-->>Proj : Publish event
Proj-->>Proj : Update read models
Ctrl-->>Client : {payslipId, payslipNumber}
```

**Diagram sources**
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L126-L145)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L287-L302)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L103-L133)

**Section sources**
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L126-L145)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L287-L302)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L103-L133)

### Compliance Management and Reporting
- Payroll runs maintain status transitions and approvals for audit trails.
- Read models expose statistics and filters for compliance reporting.
- Year-end reporting can leverage monthly aggregations and employee-level payslip history.

```mermaid
flowchart TD
Start(["Start Payroll Run"]) --> Draft["Draft"]
Draft --> Processing["Processing"]
Processing --> PendingApproval["Pending Approval"]
PendingApproval --> Approved["Approved"]
Approved --> Paid["Paid"]
Draft --> Cancelled["Cancelled (if needed)"]
Processing --> Cancelled
PendingApproval --> Cancelled
Approved --> Cancelled
```

**Diagram sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L30-L47)
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L304-L349)

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L30-L47)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L191-L209)

### API Endpoints
The Payroll service exposes the following REST endpoints:

- Salary Structures
  - GET /api/v1/payroll/salary-structures
  - GET /api/v1/payroll/salary-structures/{id}
  - POST /api/v1/payroll/salary-structures
  - POST /api/v1/payroll/salary-structures/{id}/components
  - POST /api/v1/payroll/salary-structures/{id}/deductions

- Payroll Runs
  - GET /api/v1/payroll/payroll-runs
  - GET /api/v1/payroll/payroll-runs/{id}
  - POST /api/v1/payroll/payroll-runs
  - POST /api/v1/payroll/payroll-runs/{id}/payslips
  - POST /api/v1/payroll/payroll-runs/{id}/start-processing
  - POST /api/v1/payroll/payroll-runs/{id}/submit
  - POST /api/v1/payroll/payroll-runs/{id}/approve
  - POST /api/v1/payroll/payroll-runs/{id}/cancel
  - GET /api/v1/payroll/payroll-runs/statistics

- Payslips
  - GET /api/v1/payroll/payslips
  - GET /api/v1/payroll/payslips/{id}
  - GET /api/v1/payroll/payslips/employee/{employeeId}

Request DTOs:
- CreateSalaryStructureRequest
- AddComponentRequest
- AddDeductionRequest
- CreatePayrollRunRequest
- AddPayslipRequest
- ApprovePayrollRequest
- CancelPayrollRequest

**Section sources**
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L1-L278)

### Integration Patterns
- HR Integration: Employee data and employment details can feed salary structures and payroll run composition. Use HR service APIs to enrich employee profiles and job roles.
- Finance Integration: Approved payroll runs trigger disbursements; integrate with Finance service for payment execution and bank feeds.
- Tax Services: Integrate with external tax services for dynamic tax calculations, withholding tables, and regulatory updates.

[No sources needed since this section provides general integration guidance]

### Scheduling, Batch Processing, and Year-End Reporting
- Scheduling: Use external schedulers to trigger payroll run creation and processing at month-end.
- Batch Processing: Payroll runs aggregate multiple payslips; projections update read models atomically per run.
- Year-End Reporting: Leverage statistics endpoint and payslip queries to generate YTD reports and tax documents.

**Section sources**
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L87-L99)
- [PayrollControllers.cs](file://src/Services/Payroll/ErpSystem.Payroll/API/PayrollControllers.cs#L191-L209)

## Dependency Analysis
The Payroll service depends on building blocks for event sourcing and domain event dispatching. Controllers depend on the event store and read databases. Projections depend on domain events to update read models.

```mermaid
graph TB
Controllers["PayrollControllers"]
Aggregate["PayrollAggregate"]
EventStore["IEventStore (EventStore)"]
Dispatcher["DomainEventDispatcher"]
ESDb["PayrollEventStoreDbContext"]
RDb["PayrollReadDbContext"]
ProjSS["SalaryStructureProjectionHandler"]
ProjPR["PayrollRunProjectionHandler"]
Controllers --> EventStore
Controllers --> Aggregate
Controllers --> RDb
Aggregate --> Dispatcher
EventStore --> ESDb
ProjSS --> RDb
ProjPR --> RDb
```

**Diagram sources**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs#L10-L26)
- [EventStore.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DDDBase.cs#L53-L87)
- [DomainEventDispatcher.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DomainEventDispatcher.cs#L37-L87)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L8-L60)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L9-L171)

**Section sources**
- [Program.cs](file://src/Services/Payroll/ErpSystem.Payroll/Program.cs#L10-L26)
- [EventStore.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DDDBase.cs#L53-L87)
- [DomainEventDispatcher.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DomainEventDispatcher.cs#L37-L87)
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L1-L121)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L1-L171)

## Performance Considerations
- Event store writes are append-only; ensure indexing on event stream keys and payload serialization efficiency.
- Read model projections serialize JSONB fields; consider projection batching for high-volume runs.
- Use pagination and filtering in controllers to limit response sizes for large datasets.
- Offload heavy analytics to background jobs or dedicated reporting service.

[No sources needed since this section provides general guidance]

## Troubleshooting Guide
Common issues and resolutions:
- Invalid status transitions: Ensure runs progress through Draft → Processing → PendingApproval → Approved → Paid.
- Empty payroll submission: Submissions require at least one payslip.
- Duplicate run numbers: Generated run numbers are unique per run; verify generation logic.
- Database connectivity: Verify connection string configuration and PostgreSQL availability.

Operational checks:
- Confirm event store and read database migrations are applied.
- Validate domain event publishing and projection handler execution.

**Section sources**
- [PayrollAggregate.cs](file://src/Services/Payroll/ErpSystem.Payroll/Domain/PayrollAggregate.cs#L304-L349)
- [Projections.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Projections.cs#L93-L101)
- [appsettings.json](file://src/Services/Payroll/ErpSystem.Payroll/appsettings.json#L9-L12)

## Conclusion
The Payroll service provides a robust, event-sourced foundation for salary structures, payroll runs, and payslips. It supports extensibility for tax calculations, benefits administration, and compliance reporting. Integrations with HR, Finance, and Tax services enable end-to-end payroll processing, while projections and statistics facilitate operational insights and year-end reporting.

## Appendices

### Data Models and Read Models
```mermaid
erDiagram
SALARY_STRUCTURE_READ_MODEL {
uuid id PK
string name
string description
decimal base_salary
string currency
boolean is_active
decimal total_earnings
int component_count
int deduction_count
jsonb components
jsonb deductions
timestamp created_at
}
PAYROLL_RUN_READ_MODEL {
uuid id PK
string run_number UK
int year
int month
timestamp payment_date
string description
string status
int employee_count
int paid_count
decimal total_gross_amount
decimal total_deductions
decimal total_net_amount
timestamp created_at
timestamp approved_at
string approved_by_user_id
}
PAYSLIP_READ_MODEL {
uuid id PK
uuid payroll_run_id FK
string payslip_number
string employee_id
string employee_name
int year
int month
decimal gross_amount
decimal total_deductions
decimal net_amount
string status
timestamp paid_at
string payment_method
string transaction_ref
jsonb lines
}
SALARY_STRUCTURE_READ_MODEL ||--o{ PAYSLIP_READ_MODEL : "referenced by"
PAYROLL_RUN_READ_MODEL ||--o{ PAYSLIP_READ_MODEL : "contains"
```

**Diagram sources**
- [Persistence.cs](file://src/Services/Payroll/ErpSystem.Payroll/Infrastructure/Persistence.cs#L66-L118)