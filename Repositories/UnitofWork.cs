using FashionPro.Data;
using FashionPro.Models;
using FashionPro.Models;

namespace FashionPro.Repositories
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationDbContext _context;

		public UnitOfWork(ApplicationDbContext context)
		{
			_context = context;
			Users = new GenericRepository<User>(_context);
			Parties = new GenericRepository<Party>(_context);
			Expenses = new GenericRepository<Expense>(_context);
			Fabrics = new GenericRepository<Fabric>(_context);
			FabricPayments = new GenericRepository<FabricPayment>(_context);
			ProductionTransactions = new GenericRepository<ProductionTransaction>(_context);
			ProductionPayments = new GenericRepository<ProductionPayment>(_context);
			Materials = new GenericRepository<Material>(_context);
			Senders = new GenericRepository<Sender>(_context);
			Receivers = new GenericRepository<Receiver>(_context);
		}

		public IGenericRepository<User> Users { get; private set; }
		public IGenericRepository<Party> Parties { get; private set; }
		public IGenericRepository<Expense> Expenses { get; private set; }
		public IGenericRepository<Fabric> Fabrics { get; private set; }
		public IGenericRepository<FabricPayment> FabricPayments { get; private set; }
		public IGenericRepository<ProductionTransaction> ProductionTransactions { get; private set; }
		public IGenericRepository<ProductionPayment> ProductionPayments { get; private set; }
		public IGenericRepository<Material> Materials { get; private set; }
		public IGenericRepository<Sender> Senders { get; private set; }
		public IGenericRepository<Receiver> Receivers { get; private set; }

		public int Complete()
		{
			return _context.SaveChanges();
		}

		public async Task<int> CompleteAsync()
		{
			return await _context.SaveChangesAsync();
		}

		public void Dispose()
		{
			_context.Dispose();
		}
	}
}