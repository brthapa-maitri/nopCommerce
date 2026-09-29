using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog.Cache;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Product popularity service
    /// </summary>
    public partial class ProductPopularityService : IProductPopularityService
    {
        #region Fields

        private readonly ICacheManager _cacheManager;
        private readonly IRepository<OrderItem> _orderItemRepository;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheManager">Cache manager</param>
        /// <param name="orderItemRepository">Order item repository</param>
        public ProductPopularityService(ICacheManager cacheManager,
            IRepository<OrderItem> orderItemRepository)
        {
            this._cacheManager = cacheManager;
            this._orderItemRepository = orderItemRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the number of times a product has been ordered (order items across
        /// all non-deleted orders). Cached under
        /// <see cref="ProductPopularityCacheEventConsumer.PRODUCT_ORDER_COUNT_KEY"/>;
        /// invalidation happens in <see cref="ProductPopularityCacheEventConsumer"/>,
        /// not here.
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <returns>
        /// The order count. Returns 0 for an unknown, invalid (&lt;= 0), or
        /// never-ordered product id rather than throwing.
        /// </returns>
        public virtual int GetProductOrderCount(int productId)
        {
            if (productId <= 0)
                return 0;

            var key = string.Format(ProductPopularityCacheEventConsumer.PRODUCT_ORDER_COUNT_KEY, productId);
            return _cacheManager.Get(key, () =>
                _orderItemRepository.Table
                    .Count(orderItem => orderItem.ProductId == productId && !orderItem.Order.Deleted));
        }

        #endregion
    }
}
