# Testing Strategy

<cite>
**Referenced Files in This Document**
- [ErpSystem.IntegrationTests.csproj](file://src/Tests/ErpSystem.IntegrationTests/ErpSystem.IntegrationTests.csproj)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs)
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs)
- [HRToIdentityTests.cs](file://src/tests/ErpSystem.IntegrationTests/HRToIdentityTests.cs)
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs)
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs)
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs)
- [PredictiveAnalyticsTests.cs](file://src/tests/ErpSystem.IntegrationTests/Analytics/PredictiveAnalyticsTests.cs)
- [MaterialCostValuationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Finance/MaterialCostValuationTests.cs)
- [MrpCalculationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Mrp/MrpCalculationTests.cs)
- [QualityWorkflowTests.cs](file://src/tests/ErpSystem.IntegrationTests/Quality/QualityWorkflowTests.cs)
- [DaprEventBus.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/EventBus/DaprEventBus.cs)
- [OutboxProcessor.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Outbox/OutboxProcessor.cs)
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
This document defines a comprehensive testing strategy for the ERP microservices system. It focuses on integration testing across multiple services, event-driven workflows, and cross-service interactions. The current suite includes 21 passing tests organized by functional domains and services, validating major business workflows such as financial journal entries, procurement-to-inventory goods receipt, production material issue/completion, sales order confirmation and reservation, HR lifecycle to identity account creation/lock, predictive analytics forecasting, material cost valuation, MRP calculations, and quality control workflows. The strategy covers test organization, base test classes, mocking strategies, event bus simulation, continuous integration guidance, performance and load testing considerations, and debugging techniques.

## Project Structure
The integration tests are organized under a dedicated test project that references all business services. Each test class targets a specific workflow or domain, leveraging a shared base class to bootstrap service-specific web hosts and replace persistence and event bus dependencies for deterministic, isolated testing.

Key characteristics:
- Centralized test project referencing all service projects
- Per-service bootstrapping using WebApplicationFactory
- In-memory databases replacing persistent stores
- Event bus replacement with a test-friendly HTTP-based event bus
- Domain event dispatching and outbox processing supported by building blocks

```mermaid
graph TB
IT["IntegrationTestBase.cs"]
GL["GLTests.cs"]
HRID["HRToIdentityTests.cs"]
PINV["ProcurementToInventoryTests.cs"]
PPROD["ProductionToInventoryTests.cs"]
SALINV["SalesToInventoryTests.cs"]
ANA["PredictiveAnalyticsTests.cs"]
FIN["MaterialCostValuationTests.cs"]
MRP["MrpCalculationTests.cs"]
QUAL["QualityWorkflowTests.cs"]
IT --> GL
IT --> HRID
IT --> PINV
IT --> PPROD
IT --> SALINV
IT --> ANA
IT --> FIN
IT --> MRP
IT --> QUAL
```

**Diagram sources**
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L1-L187)
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs#L1-L89)
- [HRToIdentityTests.cs](file://src/tests/ErpSystem.IntegrationTests/HRToIdentityTests.cs#L1-L97)
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L1-L80)
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs#L1-L126)
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs#L1-L87)
- [PredictiveAnalyticsTests.cs](file://src/tests/ErpSystem.IntegrationTests/Analytics/PredictiveAnalyticsTests.cs#L1-L47)
- [MaterialCostValuationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Finance/MaterialCostValuationTests.cs#L1-L126)
- [MrpCalculationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Mrp/MrpCalculationTests.cs#L1-L195)
- [QualityWorkflowTests.cs](file://src/tests/ErpSystem.IntegrationTests/Quality/QualityWorkflowTests.cs#L1-L75)

**Section sources**
- [ErpSystem.IntegrationTests.csproj](file://src/Tests/ErpSystem.IntegrationTests/ErpSystem.IntegrationTests.csproj#L1-L44)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L1-L187)

## Core Components
- IntegrationTestBase: Provides per-service WebApplicationFactory bootstrapping, in-memory database registration, Dapr client mocking, and event bus substitution with a test HTTP-based event bus.
- TestEventBus: A lightweight event bus implementation that posts events to a configured HTTP endpoint, enabling cross-service integration verification without external pub/sub.
- DomainEventDispatcher and OutboxProcessor: Building block components that ensure domain events are dispatched after persistence and processed asynchronously via outbox messages.

Key testing patterns:
- Service isolation: Each test spins up only the involved services and replaces persistence to avoid external dependencies.
- Event-driven verification: Events are captured by posting to local HTTP endpoints and validated through queries against the receiving service’s read model.
- Deterministic delays: Controlled waits are used to allow asynchronous projections and outbox processing to complete before assertions.

**Section sources**
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L19-L187)
- [DaprEventBus.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/EventBus/DaprEventBus.cs#L6-L31)
- [DomainEventDispatcher.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Domain/DomainEventDispatcher.cs#L12-L72)
- [OutboxProcessor.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/Outbox/OutboxProcessor.cs#L8-L72)

## Architecture Overview
The integration tests simulate a realistic environment by:
- Bootstrapping multiple services behind a single test host
- Replacing persistence with in-memory databases
- Substituting the event bus with a test HTTP bus that routes events to the receiving service
- Using MediatR handlers to orchestrate commands and queries across services

```mermaid
sequenceDiagram
participant Test as "IntegrationTestBase"
participant ProcApp as "Procurement App"
participant InvApp as "Inventory App"
participant EventBus as "TestEventBus"
participant Dapr as "Dapr Client"
Test->>ProcApp : "CreateProcurementApp(TestEventBus)"
Test->>InvApp : "CreateInventoryApp()"
ProcApp->>Dapr : "Publish event (goods-received)"
EventBus->>InvApp : "HTTP POST /integration/goods-received"
InvApp->>InvApp : "Handle event and update read model"
InvApp-->>Test : "Query inventory and assert"
```

**Diagram sources**
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L109-L153)
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L25-L59)
- [DaprEventBus.cs](file://src/BuildingBlocks/ErpSystem.BuildingBlocks/EventBus/DaprEventBus.cs#L11-L21)

## Detailed Component Analysis

### Financial Journal Entry and Trial Balance
Validates end-to-end accounting workflow: account definition, financial period setup, draft journal entry creation, posting, and trial balance verification.

```mermaid
sequenceDiagram
participant Test as "GLTests"
participant FinApp as "Finance App"
participant Mediator as "IMediator"
participant Read as "Finance Read Model"
Test->>FinApp : "CreateFinanceApp(TestEventBus)"
Test->>Mediator : "Define accounts"
Test->>Mediator : "Define financial period"
Test->>Mediator : "Create journal entry (draft)"
Mediator-->>Test : "Verify draft status"
Test->>Mediator : "Post journal entry"
Mediator-->>Test : "Wait for projection"
Test->>Mediator : "Get trial balance"
Mediator-->>Test : "Assert balances"
```

**Diagram sources**
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs#L11-L87)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L155-L176)

**Section sources**
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs#L1-L89)

### HR Lifecycle to Identity Account Management
End-to-end workflow from employee hiring to termination, verifying identity account creation and locking via integration events.

```mermaid
sequenceDiagram
participant Test as "HRToIdentityTests"
participant HRApp as "HR App"
participant IDApp as "Identity App"
participant EventBus as "TestEventBus"
Test->>IDApp : "CreateIdentityApp()"
Test->>HRApp : "CreateHRApp(TestEventBus)"
Test->>HRApp : "Hire employee"
EventBus->>IDApp : "POST /integration/employee-hired"
IDApp-->>Test : "Query user and assert created"
Test->>HRApp : "Terminate employee"
EventBus->>IDApp : "POST /integration/employee-terminated"
IDApp-->>Test : "Query user and assert locked"
```

**Diagram sources**
- [HRToIdentityTests.cs](file://src/tests/ErpSystem.IntegrationTests/HRToIdentityTests.cs#L14-L95)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L21-L61)

**Section sources**
- [HRToIdentityTests.cs](file://src/tests/ErpSystem.IntegrationTests/HRToIdentityTests.cs#L1-L97)

### Procurement to Inventory Goods Receipt
Validates procurement purchase order progression and resulting inventory stock increase via the goods-received integration event.

```mermaid
sequenceDiagram
participant Test as "ProcurementToInventoryTests"
participant POApp as "Procurement App"
participant INVApp as "Inventory App"
participant EventBus as "TestEventBus"
Test->>INVApp : "CreateInventoryApp()"
Test->>POApp : "CreateProcurementApp(TestEventBus)"
Test->>POApp : "Create PO"
Test->>POApp : "Submit/Approve/Send PO"
Test->>POApp : "Record goods receipt"
EventBus->>INVApp : "POST /integration/goods-received"
INVApp-->>Test : "Query inventory and assert stock"
```

**Diagram sources**
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L14-L79)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L109-L153)

**Section sources**
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L1-L80)

### Production to Inventory Material Issue and Completion
Validates material consumption and finished goods receipt workflows, ensuring inventory updates on production events.

```mermaid
sequenceDiagram
participant Test as "ProductionToInventoryTests"
participant PRODApp as "Production App"
participant INVApp as "Inventory App"
participant EventBus as "TestEventBus"
Test->>INVApp : "CreateInventoryApp()"
Test->>PRODApp : "CreateProductionApp(TestEventBus)"
Test->>INVApp : "Initialize stock"
Test->>PRODApp : "Create and release production order"
Test->>PRODApp : "Consume material"
EventBus->>INVApp : "POST /integration/production-material-issued"
INVApp-->>Test : "Assert reduced stock"
Test->>PRODApp : "Report production completion"
EventBus->>INVApp : "POST /integration/production-completed"
INVApp-->>Test : "Assert received stock"
```

**Diagram sources**
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs#L13-L125)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L63-L84)

**Section sources**
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs#L1-L126)

### Sales to Inventory Order Confirmation Reservation
Validates sales order confirmation and stock reservation behavior in the inventory service.

```mermaid
sequenceDiagram
participant Test as "SalesToInventoryTests"
participant SALESApp as "Sales App"
participant INVApp as "Inventory App"
participant EventBus as "TestEventBus"
Test->>INVApp : "CreateInventoryApp()"
Test->>SALESApp : "CreateSalesApp(TestEventBus)"
Test->>INVApp : "Initialize stock"
Test->>SALESApp : "Create sales order"
Test->>SALESApp : "Confirm sales order"
EventBus->>INVApp : "POST /integration/order-confirmed"
INVApp-->>Test : "Assert reserved and available quantities"
```

**Diagram sources**
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs#L15-L86)
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L86-L107)

**Section sources**
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs#L1-L87)

### Predictive Analytics Engine
Validates demand forecasting engine behavior with historical trends and empty datasets.

```mermaid
flowchart TD
Start(["Test Entry"]) --> Setup["Generate historical series"]
Setup --> Predict["Call PredictDemand()"]
Predict --> Assert1["Assert trend-following prediction"]
Assert1 --> Empty["Empty history test"]
Empty --> PredictEmpty["Call PredictDemand(empty)"]
PredictEmpty --> Assert2["Assert zero prediction"]
Assert2 --> End(["Test Exit"])
```

**Diagram sources**
- [PredictiveAnalyticsTests.cs](file://src/tests/ErpSystem.IntegrationTests/Analytics/PredictiveAnalyticsTests.cs#L11-L46)

**Section sources**
- [PredictiveAnalyticsTests.cs](file://src/tests/ErpSystem.IntegrationTests/Analytics/PredictiveAnalyticsTests.cs#L1-L47)

### Material Cost Valuation
Validates moving average cost calculations for receipts and issues, including insufficient quantity scenarios.

```mermaid
flowchart TD
Start(["Test Entry"]) --> Create["Create valuation with initial cost"]
Create --> Receipt1["Process first receipt"]
Receipt1 --> Assert1["Assert average cost and totals"]
Assert1 --> Receipt2["Process second receipt"]
Receipt2 --> Assert2["Assert updated average cost"]
Assert2 --> Issue["Process issue"]
Issue --> Assert3["Assert remaining quantities/value"]
Assert3 --> Insufficient["Attempt insufficient issue"]
Insufficient --> Assert4["Assert exception thrown"]
Assert4 --> End(["Test Exit"])
```

**Diagram sources**
- [MaterialCostValuationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Finance/MaterialCostValuationTests.cs#L12-L125)

**Section sources**
- [MaterialCostValuationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Finance/MaterialCostValuationTests.cs#L1-L126)

### MRP Calculation and Workflow
Validates reordering rules, procurement suggestions, and workflow enforcement.

```mermaid
flowchart TD
Start(["Test Entry"]) --> Rule["Create reordering rule"]
Rule --> AssertRule["Assert rule defaults and validations"]
AssertRule --> Suggestion["Create procurement suggestion"]
Suggestion --> Approve["Approve suggestion"]
Approve --> Convert["Convert to PO"]
Convert --> AssertWF["Assert workflow transitions"]
AssertWF --> Scenarios["Run scenario tests"]
Scenarios --> End(["Test Exit"])
```

**Diagram sources**
- [MrpCalculationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Mrp/MrpCalculationTests.cs#L12-L194)

**Section sources**
- [MrpCalculationTests.cs](file://src/tests/ErpSystem.IntegrationTests/Mrp/MrpCalculationTests.cs#L1-L195)

### Quality Control Workflow
Validates quality point definitions, pass/fail workflows, and alert tracking.

```mermaid
flowchart TD
Start(["Test Entry"]) --> Point["Create quality point"]
Point --> AssertPoint["Assert point properties"]
AssertPoint --> Check["Create quality check and pass"]
Check --> AssertCheck["Assert passed status"]
AssertCheck --> Alert["Create quality alert and resolve"]
Alert --> AssertAlert["Assert resolved status"]
AssertAlert --> End(["Test Exit"])
```

**Diagram sources**
- [QualityWorkflowTests.cs](file://src/tests/ErpSystem.IntegrationTests/Quality/QualityWorkflowTests.cs#L12-L74)

**Section sources**
- [QualityWorkflowTests.cs](file://src/tests/ErpSystem.IntegrationTests/Quality/QualityWorkflowTests.cs#L1-L75)

## Dependency Analysis
The integration test project depends on:
- Service projects for the workflows being tested
- Testing frameworks: xUnit, FluentAssertions
- ASP.NET Core testing utilities and in-memory Entity Framework provider
- Moq for mocking Dapr client
- MediatR for command/query orchestration

```mermaid
graph TB
TestProj["IntegrationTests.csproj"]
HR["HR.csproj"]
ID["Identity.csproj"]
FIN["Finance.csproj"]
INV["Inventory.csproj"]
PROD["Production.csproj"]
PROC["Procurement.csproj"]
MR["Mrp.csproj"]
AN["Analytics.csproj"]
Q["Quality.csproj"]
TestProj --> HR
TestProj --> ID
TestProj --> FIN
TestProj --> INV
TestProj --> PROD
TestProj --> PROC
TestProj --> MR
TestProj --> AN
TestProj --> Q
```

**Diagram sources**
- [ErpSystem.IntegrationTests.csproj](file://src/Tests/ErpSystem.IntegrationTests/ErpSystem.IntegrationTests.csproj#L27-L41)

**Section sources**
- [ErpSystem.IntegrationTests.csproj](file://src/Tests/ErpSystem.IntegrationTests/ErpSystem.IntegrationTests.csproj#L1-L44)

## Performance Considerations
- Asynchronous projections and outbox processing: The system relies on outbox processing and domain event dispatchers. In tests, explicit delays are used to allow projections to complete. In performance/load tests, consider:
  - Measuring end-to-end latency from command submission to read-model availability
  - Monitoring outbox batch processing throughput and error rates
  - Validating domain event dispatch timing and concurrency
- Database contention: In-memory databases reduce IO overhead but do not reflect disk-bound performance. For realistic performance testing, use lightweight ephemeral databases or containerized instances.
- Event bus overhead: The test event bus posts events over HTTP. In performance tests, measure event serialization, network latency, and receiver-side processing time.

[No sources needed since this section provides general guidance]

## Troubleshooting Guide
Common issues and debugging techniques:
- Missing or delayed projections: Add controlled delays around event publishing and assertion points. Verify that domain events are dispatched and outbox messages are processed.
- Cross-service event routing: Ensure the TestEventBus endpoint matches the receiving controller route. Validate HTTP status codes and payload shape.
- Inconsistent read model state: Confirm that the receiving service’s read model is updated after event handling. Use scoped contexts to query the read database directly.
- Flaky tests due to timing: Prefer deterministic waits or polling with bounded retries. Avoid hard-coded sleeps; consider retry policies or explicit readiness checks.
- Assertion failures: Capture and log exception details, stack traces, and inner exceptions for failing tests to accelerate diagnosis.

**Section sources**
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L179-L187)
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs#L78-L87)
- [HRToIdentityTests.cs](file://src/tests/ErpSystem.IntegrationTests/HRToIdentityTests.cs#L84-L95)
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L66-L79)
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs#L59-L70)
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs#L73-L86)

## Conclusion
The testing strategy leverages a robust integration test framework that simulates cross-service workflows using in-memory databases and a test event bus. The 21 passing tests cover critical business scenarios spanning finance, procurement, inventory, production, sales, HR, identity, analytics, MRP, and quality. The approach provides strong confidence in event-driven integrations and domain logic correctness, with clear patterns for extending coverage and maintaining reliability during continuous integration and performance testing.

[No sources needed since this section summarizes without analyzing specific files]

## Appendices

### Writing New Integration Tests
- Use IntegrationTestBase to bootstrap the minimal set of services required for the scenario.
- Replace persistence with in-memory databases and inject the TestEventBus pointing to the receiving service endpoint.
- Orchestrate commands via MediatR and assert outcomes using queries or direct read model access.
- Add controlled delays only when asynchronous processing is expected; otherwise, rely on synchronous handlers in tests.

**Section sources**
- [IntegrationTestBase.cs](file://src/tests/ErpSystem.IntegrationTests/IntegrationTestBase.cs#L21-L187)
- [GLTests.cs](file://src/tests/ErpSystem.IntegrationTests/GLTests.cs#L11-L87)
- [ProcurementToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProcurementToInventoryTests.cs#L14-L79)

### Test Data Management
- Prefer deterministic identifiers and controlled timestamps for reproducibility.
- Initialize read models explicitly when needed (e.g., inventory stock) before asserting downstream effects.
- Use scoped service containers to query read databases directly for verification.

**Section sources**
- [ProductionToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/ProductionToInventoryTests.cs#L37-L57)
- [SalesToInventoryTests.cs](file://src/tests/ErpSystem.IntegrationTests/SalesToInventoryTests.cs#L39-L71)

### Continuous Integration Testing
- Run the integration test project with coverage collection enabled.
- Ensure CI environments provide sufficient resources for concurrent service bootstrapping.
- Use containerized infrastructure for services requiring external dependencies (e.g., pub/sub) when not using the test event bus.

**Section sources**
- [ErpSystem.IntegrationTests.csproj](file://src/Tests/ErpSystem.IntegrationTests/ErpSystem.IntegrationTests.csproj#L10-L21)

### Performance and Load Testing Guidance
- Measure end-to-end latency for each major workflow and track error rates.
- Monitor outbox processing metrics and adjust batch sizes and intervals.
- Validate event bus throughput and payload sizes; consider compression or batching for high-volume scenarios.

[No sources needed since this section provides general guidance]