using FashionPro.Models;
using Microsoft.EntityFrameworkCore;

namespace FashionPro.Data
{
	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<User> Users { get; set; }
		public DbSet<Party> Parties { get; set; }
		public DbSet<Expense> Expenses { get; set; }
		public DbSet<Fabric> Fabrics { get; set; }
		public DbSet<FabricPayment> FabricPayments { get; set; }
		public DbSet<ProductionTransaction> ProductionTransactions { get; set; }
		public DbSet<ProductionPayment> ProductionPayments { get; set; }
		public DbSet<Material> Materials { get; set; }
		public DbSet<Sender> Senders { get; set; }
		public DbSet<Receiver> Receivers { get; set; }
		public DbSet<BillItem> BillItems { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Indexes for performance
			modelBuilder.Entity<Party>()
				.HasIndex(p => p.PartyType)
				.HasDatabaseName("IX_Parties_Type");

			modelBuilder.Entity<ProductionTransaction>()
				.HasIndex(p => new { p.ModuleType, p.SubType })
				.HasDatabaseName("IX_Production_Module");

			modelBuilder.Entity<ProductionTransaction>()
				.HasIndex(p => p.PartyId)
				.HasDatabaseName("IX_Production_Party");

			// 🔥 Sender index with filter for nullable
			modelBuilder.Entity<Sender>()
				.HasIndex(p => p.PartyId)
				.HasDatabaseName("IX_Senders_Party");

			modelBuilder.Entity<Fabric>()
				.HasIndex(p => p.PartyId)
				.HasDatabaseName("IX_Fabrics_Party");

			// BillItems Index
			modelBuilder.Entity<BillItem>()
				.HasIndex(b => b.SenderId)
				.HasDatabaseName("IX_BillItems_SenderId");

			// Default values
			modelBuilder.Entity<User>()
				.Property(u => u.IsActive)
				.HasDefaultValue(true);

			modelBuilder.Entity<Party>()
				.Property(p => p.CurrentBalance)
				.HasDefaultValue(0);

			modelBuilder.Entity<ProductionTransaction>()
				.Property(p => p.Status)
				.HasDefaultValue("Pending");

			// BillItem relationships
			modelBuilder.Entity<BillItem>()
				.HasOne(b => b.Sender)
				.WithMany()
				.HasForeignKey(b => b.SenderId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}