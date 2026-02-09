using FluentAssertions;
using ErpSystem.Finance.Domain;

namespace ErpSystem.IntegrationTests.Finance;

/// <summary>
/// Integration tests for moving average cost calculation
/// </summary>
public class MaterialCostValuationTests
{
    [Fact]
    public void ProcessReceipt_ShouldCalculateCorrectMovingAverage()
    {
        // Arrange
        Guid valuationId = Guid.NewGuid();
        MaterialCostValuation valuation = MaterialCostValuation.Create(
            valuationId,
            "tenant1",
            "MAT001",
            "WH01",
            initialCost: 100m);

        // Act - First receipt: 10 units @ 100
        valuation.ProcessReceipt("PO-001", "PO_RECEIPT", 10m, 100m, DateTime.UtcNow);
        
        // Assert - After first receipt
        valuation.CurrentAverageCost.Should().Be(100m);
        valuation.TotalQuantityOnHand.Should().Be(10m);
        valuation.TotalValue.Should().Be(1000m);

        // Act - Second receipt: 20 units @ 110
        valuation.ProcessReceipt("PO-002", "PO_RECEIPT", 20m, 110m, DateTime.UtcNow);
        
        // Assert - Moving average = (1000 + 2200) / (10 + 20) = 3200 / 30 = 106.67
        valuation.CurrentAverageCost.Should().BeApproximately(106.67m, 0.01m);
        valuation.TotalQuantityOnHand.Should().Be(30m);
        valuation.TotalValue.Should().Be(3200m);
    }

    [Fact]
    public void ProcessIssue_ShouldUseCurrentAverageCost()
    {
        // Arrange
        Guid valuationId = Guid.NewGuid();
        MaterialCostValuation valuation = MaterialCostValuation.Create(
            valuationId,
            "tenant1",
            "MAT002",
            "WH01",
            initialCost: 50m);

        valuation.ProcessReceipt("PO-001", "PO_RECEIPT", 100m, 50m, DateTime.UtcNow);
        valuation.ProcessReceipt("PO-002", "PO_RECEIPT", 100m, 60m, DateTime.UtcNow);
        
        // Current state: 200 units @ avg cost 55 = 11,000 total value

        // Act - Issue 50 units
        valuation.ProcessIssue("SO-001", "SO_SHIPMENT", 50m, DateTime.UtcNow);
        
        // Assert - Issue value = 50 * 55 = 2,750
        // Remaining: 150 units, value = 11,000 - 2,750 = 8,250
        valuation.TotalQuantityOnHand.Should().Be(150m);
        valuation.TotalValue.Should().Be(8250m);
        valuation.CurrentAverageCost.Should().Be(55m); // Average cost doesn't change on issue
    }

    [Fact]
    public void ProcessIssue_ShouldThrowException_WhenInsufficientQuantity()
    {
        // Arrange
        Guid valuationId = Guid.NewGuid();
        MaterialCostValuation valuation = MaterialCostValuation.Create(
            valuationId,
            "tenant1",
            "MAT003",
            "WH01",
            initialCost: 100m);

        valuation.ProcessReceipt("PO-001", "PO_RECEIPT", 10m, 100m, DateTime.UtcNow);

        // Act & Assert
        Action act = () => valuation.ProcessIssue("SO-001", "SO_SHIPMENT", 20m, DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Insufficient quantity*");
    }

    [Fact]
    public void ComplexScenario_ShouldMaintainCorrectValuation()
    {
        // Arrange
        Guid valuationId = Guid.NewGuid();
        MaterialCostValuation valuation = MaterialCostValuation.Create(
            valuationId,
            "tenant1",
            "MAT004",
            "WH01",
            initialCost: 0m);

        // Act - Simulate real-world scenario
        // Day 1: Receive 100 @ 10
        valuation.ProcessReceipt("PO-001", "PO_RECEIPT", 100m, 10m, DateTime.UtcNow);
        valuation.CurrentAverageCost.Should().Be(10m);
        valuation.TotalValue.Should().Be(1000m);

        // Day 2: Issue 30 for sales
        valuation.ProcessIssue("SO-001", "SO_SHIPMENT", 30m, DateTime.UtcNow);
        valuation.TotalQuantityOnHand.Should().Be(70m);
        valuation.TotalValue.Should().Be(700m);

        // Day 3: Receive 50 @ 12 (price increased)
        valuation.ProcessReceipt("PO-002", "PO_RECEIPT", 50m, 12m, DateTime.UtcNow);
        // New avg = (700 + 600) / (70 + 50) = 1300 / 120 = 10.833...
        valuation.CurrentAverageCost.Should().BeApproximately(10.833m, 0.001m);
        valuation.TotalQuantityOnHand.Should().Be(120m);
        valuation.TotalValue.Should().Be(1300m);

        // Day 4: Issue 40 for production
        valuation.ProcessIssue("PROD-001", "PROD_ISSUE", 40m, DateTime.UtcNow);
        // Issue value = 40 * 10.833 = 433.33
        // Remaining = 1300 - 433.33 = 866.67
        valuation.TotalQuantityOnHand.Should().Be(80m);
        valuation.TotalValue.Should().BeApproximately(866.67m, 0.01m);
        valuation.CurrentAverageCost.Should().BeApproximately(10.833m, 0.001m);
    }
}
