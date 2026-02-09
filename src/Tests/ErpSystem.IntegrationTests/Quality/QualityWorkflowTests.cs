using FluentAssertions;
using ErpSystem.Quality.Domain;

namespace ErpSystem.IntegrationTests.Quality;

/// <summary>
/// Integration tests for Quality Control workflow
/// </summary>
public class QualityWorkflowTests
{
    [Fact]
    public void QualityPoint_Should_Defines_Requirements_Correctly()
    {
        // Issue
        Guid id = Guid.NewGuid();
        QualityPoint point = QualityPoint.Create(
            id,
            "tenant-1",
            "Incoming Inspection",
            "MAT-IRON-01",
            "RECEIPT",
            QualityCheckType.Visual,
            "Check for surface rust",
            true);

        // Assert
        point.Name.Should().Be("Incoming Inspection");
        point.MaterialId.Should().Be("MAT-IRON-01");
        point.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public void QualityCheck_Should_Handle_Pass_Workflow()
    {
        // Issue
        Guid qpId = Guid.NewGuid();
        Guid qcId = Guid.NewGuid();
        QualityCheck check = QualityCheck.Create(
            qcId,
            "tenant-1",
            qpId,
            "REC-2026-001",
            "RECEIPT",
            "MAT-IRON-01");

        // Act
        check.Pass("Looks good", "Inspector-Alpha");

        // Assert
        check.Status.Should().Be(QualityCheckStatus.Passed);
    }

    [Fact]
    public void QualityAlert_Should_Track_Issues()
    {
        // Issue
        Guid id = Guid.NewGuid();
        QualityAlert alert = QualityAlert.Create(
            id,
            "tenant-1",
            "Found excessive rust on Batch B12",
            "MAT-IRON-01",
            Guid.NewGuid(),
            QualityAlertPriority.High);

        // Act
        alert.Assign("MaintenanceTeam");
        alert.Resolve("Batch returned to supplier");

        // Assert
        alert.Status.Should().Be(QualityAlertStatus.Resolved);
        alert.Priority.Should().Be(QualityAlertPriority.High);
    }
}
