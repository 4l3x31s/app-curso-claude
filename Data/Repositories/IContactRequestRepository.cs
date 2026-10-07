using app_curso_claude.Models;

namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Storage of the contact requests.
    /// </summary>
    public interface IContactRequestRepository
    {
        /// <summary>
        /// Returns how many requests were created in the given year (UTC).
        /// </summary>
        /// <param name="year">Year to count.</param>
        Task<int> CountByYearAsync(int year);

        /// <summary>
        /// Stores a new request.
        /// </summary>
        /// <param name="request">Request to store.</param>
        Task AddAsync(ContactRequest request);
    }
}
