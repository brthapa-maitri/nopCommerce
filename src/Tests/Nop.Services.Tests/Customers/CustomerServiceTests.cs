using System;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Tests;
using NUnit.Framework;
using Rhino.Mocks;

namespace Nop.Services.Tests.Customers
{
    [TestFixture]
    public class CustomerServiceTests : ServiceTest
    {
        private IDateTimeHelper _dateTimeHelper;
        private ICustomerService _customerService;

        [SetUp]
        public new void SetUp()
        {
            _dateTimeHelper = MockRepository.GenerateMock<IDateTimeHelper>();

            //none of the repositories/services below are exercised by
            //GetCustomerLastLoginDate, so they're left null
            _customerService = new CustomerService(new NopNullCache(),
                null, null, null, null, null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, _dateTimeHelper);
        }

        [Test]
        public void Can_get_last_login_date_should_return_null_when_customer_never_logged_in()
        {
            var customer = new Customer
            {
                Id = 1,
                LastLoginDateUtc = null
            };

            var result = _customerService.GetCustomerLastLoginDate(customer);

            Assert.IsFalse(result.HasValue);
        }

        [Test]
        public void Can_get_last_login_date_should_convert_to_store_timezone_when_customer_has_logged_in()
        {
            var lastLoginUtc = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
            //a distinct, deterministic offset (not TimeZoneInfo.Utc) so the
            //assertion below can tell the source (UTC) and destination
            //(store timezone) arguments apart
            var storeTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                "UTC+02:00 (test)", TimeSpan.FromHours(2), "UTC+02:00 (test)", "UTC+02:00 (test)");
            var expected = lastLoginUtc.AddHours(2);

            _dateTimeHelper.Expect(x => x.DefaultStoreTimeZone).Return(storeTimeZone);
            //asserts the AC directly: conversion source must be UTC and the
            //destination must be the store's timezone, not e.g. CurrentTimeZone
            _dateTimeHelper.Expect(x => x.ConvertToUserTime(
                Arg<DateTime>.Is.Equal(lastLoginUtc),
                Arg<TimeZoneInfo>.Is.Equal(TimeZoneInfo.Utc),
                Arg<TimeZoneInfo>.Is.Equal(storeTimeZone))).Return(expected);

            var customer = new Customer
            {
                Id = 2,
                LastLoginDateUtc = lastLoginUtc
            };

            var result = _customerService.GetCustomerLastLoginDate(customer);

            Assert.IsTrue(result.HasValue);
            result.Value.ShouldEqual(expected);
        }
    }
}
