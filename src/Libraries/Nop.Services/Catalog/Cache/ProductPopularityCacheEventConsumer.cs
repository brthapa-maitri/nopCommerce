using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Services.Events;

namespace Nop.Services.Catalog.Cache
{
    /// <summary>
    /// Product popularity cache event consumer (used for caching of
    /// <see cref="ProductPopularityService"/> order counts).
    /// </summary>
    /// <remarks>
    /// This is the ONLY place that evicts the product order-count cache. Services
    /// must not call <see cref="ICacheManager.Remove"/> / RemoveByPattern for this
    /// key ad hoc — they publish entity events instead and let this consumer react.
    /// </remarks>
    public partial class ProductPopularityCacheEventConsumer :
        //products
        IConsumer<EntityUpdated<Product>>,
        //orders (an order's line items are what the count is derived from)
        IConsumer<EntityInserted<Order>>,
        IConsumer<EntityUpdated<Order>>,
        IConsumer<EntityDeleted<Order>>
    {
        /// <summary>
        /// Key for a product's order count
        /// </summary>
        /// <remarks>
        /// {0} : product id
        /// </remarks>
        public const string PRODUCT_ORDER_COUNT_KEY = "Nop.product.ordercount-{0}";
        /// <summary>
        /// Key pattern to clear the cached product order counts
        /// </summary>
        public const string PRODUCT_ORDER_COUNT_PATTERN_KEY = "Nop.product.ordercount";

        private readonly ICacheManager _cacheManager;

        public ProductPopularityCacheEventConsumer()
        {
            //TODO inject static cache manager using constructor
            this._cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("nop_cache_static");
        }

        //products
        public void HandleEvent(EntityUpdated<Product> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_ORDER_COUNT_PATTERN_KEY);
        }

        //orders
        public void HandleEvent(EntityInserted<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_ORDER_COUNT_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdated<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_ORDER_COUNT_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeleted<Order> eventMessage)
        {
            _cacheManager.RemoveByPattern(PRODUCT_ORDER_COUNT_PATTERN_KEY);
        }
    }
}
