namespace app_curso_claude.Data.Repositories
{
    /// <summary>
    /// Groups several repository calls so they are all saved or none is.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Runs the operation inside one transaction: it is committed when the operation
        /// returns and rolled back when it throws. What the operation reads stays unchanged
        /// by others until the transaction ends.
        /// </summary>
        /// <typeparam name="T">Type of the value the operation returns.</typeparam>
        /// <param name="operation">Repository calls to run together.</param>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
    }
}
