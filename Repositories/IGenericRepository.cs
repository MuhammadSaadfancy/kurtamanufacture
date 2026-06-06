using System.Linq.Expressions;

namespace FashionPro.Repositories
{
	public interface IGenericRepository<T> where T : class
	{
		T GetById(int id);
		Task<T> GetByIdAsync(int id);
		IEnumerable<T> GetAll();
		Task<IEnumerable<T>> GetAllAsync();
		IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
		Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
		T SingleOrDefault(Expression<Func<T, bool>> predicate);
		Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> predicate);
		void Add(T entity);
		Task AddAsync(T entity);
		void AddRange(IEnumerable<T> entities);
		void Remove(T entity);
		void RemoveRange(IEnumerable<T> entities);
		void Update(T entity);
	}
}