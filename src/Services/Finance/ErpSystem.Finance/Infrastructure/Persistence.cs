using Microsoft.EntityFrameworkCore;
using ErpSystem.BuildingBlocks.Domain;

namespace ErpSystem.Finance.Infrastructure;

public class FinanceEventStoreDbContext(DbContextOptions<FinanceEventStoreDbContext> options) : DbContext(options)
{
    public DbSet<EventStream> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventStream>(b =>
        {
            b.HasKey(e => new { e.AggregateId, e.Version });
            b.Property(e => e.Payload).HasColumnType("jsonb");
        });
    }
}

public class FinanceReadDbContext(DbContextOptions<FinanceReadDbContext> options) : DbContext(options)
{
    public DbSet<InvoiceReadModel> Invoices { get; set; } = null!;

    public DbSet<PaymentReadModel> Payments { get; set; } = null!;

    // GL
    public DbSet<AccountReadModel> Accounts { get; set; } = null!;
    public DbSet<JournalEntryReadModel> JournalEntries { get; set; } = null!;
    public DbSet<JournalEntryLineReadModel> JournalEntryLines { get; set; } = null!;
    public DbSet<FinancialPeriodReadModel> FinancialPeriods { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvoiceReadModel>(b => {
             b.HasKey(i => i.InvoiceId);
             b.Property(i => i.LinesJson).HasColumnType("jsonb");
        });
        modelBuilder.Entity<PaymentReadModel>().HasKey(p => p.PaymentId);

        // GL mappings
        modelBuilder.Entity<AccountReadModel>().HasKey(x => x.AccountId);
        modelBuilder.Entity<JournalEntryReadModel>().HasKey(x => x.JournalEntryId);
        modelBuilder.Entity<FinancialPeriodReadModel>().HasKey(x => x.PeriodId);
        modelBuilder.Entity<JournalEntryLineReadModel>(b => {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.JournalEntryId);
            b.HasIndex(x => x.AccountId); // Crucial for Account reporting
        });
    }
}

public class InvoiceReadModel
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int Type { get; set; } // 1=AR, 2=AP
    public string PartyId { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public string Currency { get; set; } = "CNY";
    public int Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string LinesJson { get; set; } = "[]";
}

public class PaymentReadModel
{
    public Guid PaymentId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public int Direction { get; set; } // 1=In, 2=Out
    public string PartyId { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal UnallocatedAmount { get; set; }
    public string Currency { get; set; } = "CNY";
    public DateTime PaymentDate { get; set; }
    public int Method { get; set; }
    public string? ReferenceNo { get; set; }
    public int Status { get; set; }
    public Guid? InvoiceId { get; set; } // Simple link for 1:1 or primary allocation
}

// --- General Ledger Read Models ---
public class AccountReadModel
{
    public Guid AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Type { get; set; }
    public int Class { get; set; }
    public Guid? ParentAccountId { get; set; }
    public int BalanceType { get; set; }
    public bool IsActive { get; set; }
    public string Currency { get; set; } = "CNY";
}

public class JournalEntryReadModel
{
    public Guid JournalEntryId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; }
    public DateTime PostingDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public int Source { get; set; }
    public string? ReferenceNo { get; set; }
}

public class JournalEntryLineReadModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

public class FinancialPeriodReadModel
{
    public Guid PeriodId { get; set; }
    public int Year { get; set; }
    public int PeriodNumber { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
}
