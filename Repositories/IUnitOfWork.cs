using FashionPro.Models;
using FashionPro.Models;

namespace FashionPro.Repositories
{
	public interface IUnitOfWork : IDisposable
	{
		IGenericRepository<User> Users { get; }
		IGenericRepository<Party> Parties { get; }
		IGenericRepository<Expense> Expenses { get; }
		IGenericRepository<Fabric> Fabrics { get; }
		IGenericRepository<FabricPayment> FabricPayments { get; }
		IGenericRepository<ProductionTransaction> ProductionTransactions { get; }
		IGenericRepository<ProductionPayment> ProductionPayments { get; }
		IGenericRepository<Material> Materials { get; }
		IGenericRepository<Sender> Senders { get; }
		IGenericRepository<Receiver> Receivers { get; }

		int Complete();
		Task<int> CompleteAsync();
	}
}