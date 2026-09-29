using System.Collections.Generic;
using System.Linq;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Nop.Services.Tests.Catalog
{
    [TestFixture]
    public class ProductPopularityServiceTests : ServiceTest
    {
        private IRepository<OrderItem> _orderItemRepo;
        private ICacheManager _cacheManager;
        private IProductPopularityService _productPopularityService;

        [SetUp]
        public new void SetUp()
        {
            var activeOrder1 = new Order { Id = 1, Deleted = false };
            var activeOrder2 = new Order { Id = 2, Deleted = false };
            var deletedOrder = new Order { Id = 3, Deleted = true };

            var orderItems = new List<OrderItem>
            {
                new OrderItem { Id = 1, ProductId = 100, Order = activeOrder1, OrderId = activeOrder1.Id },
                new OrderItem { Id = 2, ProductId = 100, Order = activeOrder2, OrderId = activeOrder2.Id },
                new OrderItem { Id = 3, ProductId = 100, Order = deletedOrder, OrderId = deletedOrder.Id },
                new OrderItem { Id = 4, ProductId = 200, Order = activeOrder1, OrderId = activeOrder1.Id },
            };

            _orderItemRepo = MockRepository.GenerateMock<IRepository<OrderItem>>();
            _orderItemRepo.Expect(x => x.Table).Return(orderItems.AsQueryable());

            _cacheManager = new NopNullCache();

            _productPopularityService = new ProductPopularityService(_cacheManager, _orderItemRepo);
        }

        [Test]
        public void Can_get_product_order_count()
        {
            //product 100 was ordered twice via non-deleted orders; the third order item
            //belongs to a deleted order and must not be counted
            _productPopularityService.GetProductOrderCount(100).ShouldEqual(2);
        }

        [Test]
        public void Can_get_product_order_count_for_single_order()
        {
            _productPopularityService.GetProductOrderCount(200).ShouldEqual(1);
        }

        [Test]
        public void Get_product_order_count_returns_zero_for_unknown_product_id()
        {
            _productPopularityService.GetProductOrderCount(999).ShouldEqual(0);
        }

        [Test]
        public void Get_product_order_count_returns_zero_rather_than_throwing_for_invalid_product_id()
        {
            _productPopularityService.GetProductOrderCount(0).ShouldEqual(0);
            _productPopularityService.GetProductOrderCount(-1).ShouldEqual(0);
        }
    }
}
