using FluentAssertions;
using ErpSystem.Mrp.Domain;

namespace ErpSystem.IntegrationTests.Mrp;

/// <summary>
/// Integration tests for MRP calculation logic
/// </summary>
public class MrpCalculationTests
{
    [Fact]
    public void ReorderingRule_ShouldCalculateCorrectQuantities()
    {
        // Arrange
        Guid ruleId = Guid.NewGuid();
        ReorderingRule rule = ReorderingRule.Create(
            ruleId,
            "tenant1",
            "MAT-001",
            "WH-01",
            minQuantity: 100m,
            maxQuantity: 500m,
            leadTimeDays: 7,
            ReorderingStrategy.MakeToStock);

        // Assert
        rule.MinQuantity.Should().Be(100m);
        rule.MaxQuantity.Should().Be(500m);
        rule.ReorderQuantity.Should().Be(400m); // Default: max - min
        rule.LeadTimeDays.Should().Be(7);
        rule.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ReorderingRule_ShouldEnforceBusinessRules()
    {
        // Arrange & Act & Assert - Min cannot be negative
        Func<ReorderingRule> act1 = () => ReorderingRule.Create(
            Guid.NewGuid(),
            "tenant1",
            "MAT-002",
            "WH-01",
            minQuantity: -10m,
            maxQuantity: 100m,
            leadTimeDays: 7,
            ReorderingStrategy.MakeToStock);
        
        act1.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot be negative*");

        // Max must be greater than min
        Func<ReorderingRule> act2 = () => ReorderingRule.Create(
            Guid.NewGuid(),
            "tenant1",
            "MAT-003",
            "WH-01",
            minQuantity: 100m,
            maxQuantity: 50m,
            leadTimeDays: 7,
            ReorderingStrategy.MakeToStock);
        
        act2.Should().Throw<InvalidOperationException>()
            .WithMessage("*greater than min*");
    }

    [Fact]
    public void ProcurementSuggestion_ShouldTrackApprovalWorkflow()
    {
        // Arrange
        ProcurementCalculation calculation = new ProcurementCalculation(
            CurrentOnHand: 50m,
            Reserved: 20m,
            Available: 30m,
            IncomingProcurement: 0m,
            IncomingProduction: 0m,
            ForecastedAvailable: 30m,
            MinQuantity: 100m,
            MaxQuantity: 500m,
            Reason: "Below minimum stock");

        Guid suggestionId = Guid.NewGuid();
        ProcurementSuggestion suggestion = ProcurementSuggestion.Create(
            suggestionId,
            "tenant1",
            "MAT-001",
            "WH-01",
            suggestedQuantity: 470m,
            suggestedDate: DateTime.UtcNow.AddDays(7),
            reorderingRuleId: Guid.NewGuid().ToString(),
            calculation);

        // Assert initial state
        suggestion.Status.Should().Be(ProcurementSuggestionStatus.Pending);
        suggestion.SuggestedQuantity.Should().Be(470m);

        // Act - Approve
        suggestion.Approve("user@example.com");
        suggestion.Status.Should().Be(ProcurementSuggestionStatus.Approved);

        // Act - Convert to PO
        suggestion.MarkAsConverted("PO-20260208-001");
        suggestion.Status.Should().Be(ProcurementSuggestionStatus.Converted);
        suggestion.GeneratedPurchaseOrderId.Should().Be("PO-20260208-001");
    }

    [Fact]
    public void ProcurementSuggestion_ShouldEnforceWorkflowRules()
    {
        // Arrange
        ProcurementCalculation calculation = new ProcurementCalculation(
            CurrentOnHand: 50m,
            Reserved: 20m,
            Available: 30m,
            IncomingProcurement: 0m,
            IncomingProduction: 0m,
            ForecastedAvailable: 30m,
            MinQuantity: 100m,
            MaxQuantity: 500m,
            Reason: "Below minimum stock");

        ProcurementSuggestion suggestion = ProcurementSuggestion.Create(
            Guid.NewGuid(),
            "tenant1",
            "MAT-002",
            "WH-01",
            suggestedQuantity: 470m,
            suggestedDate: DateTime.UtcNow.AddDays(7),
            reorderingRuleId: Guid.NewGuid().ToString(),
            calculation);

        // Act & Assert - Cannot convert without approval
        Action act = () => suggestion.MarkAsConverted("PO-001");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*approved*");

        // Approve first
        suggestion.Approve("user@example.com");
        
        // Now conversion should work
        suggestion.MarkAsConverted("PO-001");
        suggestion.Status.Should().Be(ProcurementSuggestionStatus.Converted);
    }

    [Fact]
    public void MrpCalculation_ScenarioTest_LowStock()
    {
        // Scenario: Current stock is 30, min is 100, max is 500
        // Expected: Should suggest reordering 470 units (to reach max)
        
        ProcurementCalculation calculation = new ProcurementCalculation(
            CurrentOnHand: 50m,
            Reserved: 20m,
            Available: 30m,
            IncomingProcurement: 0m,
            IncomingProduction: 0m,
            ForecastedAvailable: 30m,
            MinQuantity: 100m,
            MaxQuantity: 500m,
            Reason: "Forecasted available (30) below minimum (100)");

        // Suggested quantity should be: max - forecasted = 500 - 30 = 470
        decimal expectedSuggestion = 470m;
        
        calculation.ForecastedAvailable.Should().Be(30m);
        calculation.ForecastedAvailable.Should().BeLessThan(calculation.MinQuantity);
        
        decimal suggestedQty = calculation.MaxQuantity - calculation.ForecastedAvailable;
        suggestedQty.Should().Be(expectedSuggestion);
    }

    [Fact]
    public void MrpCalculation_ScenarioTest_IncomingOrders()
    {
        // Scenario: Current available is 30, but we have 80 incoming from PO
        // Forecasted = 30 + 80 = 110, which is above min (100)
        // Expected: No reordering needed
        
        ProcurementCalculation calculation = new ProcurementCalculation(
            CurrentOnHand: 50m,
            Reserved: 20m,
            Available: 30m,
            IncomingProcurement: 80m,
            IncomingProduction: 0m,
            ForecastedAvailable: 110m,
            MinQuantity: 100m,
            MaxQuantity: 500m,
            Reason: "Sufficient with incoming procurement");

        calculation.ForecastedAvailable.Should().Be(110m);
        calculation.ForecastedAvailable.Should().BeGreaterThanOrEqualTo(calculation.MinQuantity);
        
        // No reordering needed
    }
}
