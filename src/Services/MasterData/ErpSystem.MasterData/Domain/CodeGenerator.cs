namespace ErpSystem.MasterData.Domain;

public interface ICodeGenerator
{
    string GenerateMaterialCode();
    string GenerateSupplierCode();
    string GenerateCustomerCode();
    string GenerateWarehouseCode();
}

public class DefaultCodeGenerator : ICodeGenerator
{
    // In production, this would use a distributed sequence or DB counter
    // For this implementation, we simulate the date + sequence pattern from PRD
    private static int _sequence = 1;

    public string GenerateMaterialCode() => $"MAT-{DateTime.UtcNow:yyyyMMdd}-{_sequence++:D4}";
    public string GenerateSupplierCode() => $"SUP-{DateTime.UtcNow:yyyyMMdd}-{_sequence++:D4}";
    public string GenerateCustomerCode() => $"CUS-{DateTime.UtcNow:yyyyMMdd}-{_sequence++:D4}";
    public string GenerateWarehouseCode() => $"WH-{_sequence++:D3}";
}
