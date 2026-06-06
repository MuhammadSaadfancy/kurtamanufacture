using Microsoft.EntityFrameworkCore;
using FashionPro.Data;
using System.Linq.Expressions;

namespace FashionPro.Repositories
{
	public class GenericRepository<T> : IGenericRepository<T> where T : class
	{
		protected readonly ApplicationDbContext _context;
		protected readonly DbSet<T> _dbSet;

		public GenericRepository(ApplicationDbContext context)
		{
			_context = context;
			_dbSet = context.Set<T>();
		}

		public T GetById(int id)
		{
			return _dbSet.Find(id);
		}

		public async Task<T> GetByIdAsync(int id)
		{
			return await _dbSet.FindAsync(id);
		}

		public IEnumerable<T> GetAll()
		{
			return _dbSet.ToList();
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			return await _dbSet.ToListAsync();
		}

		public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
		{
			return _dbSet.Where(predicate).ToList();
		}

		public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet.Where(predicate).ToListAsync();
		}

		public T SingleOrDefault(Expression<Func<T, bool>> predicate)
		{
			return _dbSet.SingleOrDefault(predicate);
		}

		public async Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet.SingleOrDefaultAsync(predicate);
		}

		public void Add(T entity)
		{
			_dbSet.Add(entity);
		}

		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
		}

		public void AddRange(IEnumerable<T> entities)
		{
			_dbSet.AddRange(entities);
		}

		public void Remove(T entity)
		{
			_dbSet.Remove(entity);
		}

		public void RemoveRange(IEnumerable<T> entities)
		{
			_dbSet.RemoveRange(entities);
		}

		public void Update(T entity)
		{
			_dbSet.Update(entity);
		}
	}
}