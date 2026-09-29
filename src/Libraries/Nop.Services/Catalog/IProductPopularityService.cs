namespace Nop.Services.Catalog
{
    /// <summary>
    /// Product popularity service interface. Exposes cheap, cached "how popular is
    /// this product" signals derived from order history (e.g. for a storefront
    /// "Popular" badge).
    /// </summary>
    public partial interface IProductPopularityService
    {
        /// <summary>
        /// Gets the number of times a product has been ordered (order items across
        /// all non-deleted orders). The result is cached; see
        /// <see cref="Cache.ProductPopularityCacheEventConsumer"/> for the cache key
        /// and its invalidation.
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>
        /// The order count. Returns 0 for an unknown, invalid (&lt;= 0), or
        /// never-ordered product id rather than throwing.
        /// </returns>
        int GetProductOrderCount(int productId);
    }
}
