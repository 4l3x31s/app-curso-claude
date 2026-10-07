using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Read access to the customers.
    /// </summary>
    public interface ICustomerRepository
    {
        /// <summary>
        /// Returns every customer, ordered by id.
        /// </summary>
        Task<IReadOnlyList<Customer>> GetAllAsync();

        /// <summary>
        /// Returns the customer with the given id, or <c>null</c> when there is none.
        /// </summary>
        /// <param name="id">Customer id to look for.</param>
        Task<Customer?> GetAsync(int id);
    }
}
