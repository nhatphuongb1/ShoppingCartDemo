﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NT.Core.Events;
using NT.Infrastructure.AspNetCore;
using NT.Infrastructure.MessageBus.Event;
using NT.OrderService.Core;

namespace NT.WebApi.ShoppingCart
{
    [Route("api/checkout")]
    public class CheckoutApiController : BaseGatewayController
    {
        private readonly IEventBus _messageBus;

        public CheckoutApiController(RestClient restClient, IEventBus messageBus)
            : base(restClient)
        {
            _messageBus = messageBus;
        }

        [HttpPost("{orderId}/process")]
        public async Task<string> ProcessOrder(Guid orderId)
        {
            var order = await RestClient.GetAsync<Order>("order_service", $"/api/orders/{orderId}");
            if (order.OrderStatus == OrderStatus.Processing || order.OrderStatus == OrderStatus.Paid)
                return await Task.FromResult($"Order[#{orderId}] was in Processing or Paid status.");
            _messageBus.Publish(new CheckoutEvent(orderId));
            return await Task.FromResult($"Order[#{orderId}] is processing...");
        }

        [HttpPost]
        public async Task<bool> Post([FromBody] CartViewModel cartViewModel)
        {
            //TODO: hard code the customer id and employee id for now
            cartViewModel.CustomerId = new Guid("37EB08EF-E4C2-4211-B808-F64A81AE02FC");
            cartViewModel.EmployeeId = new Guid("d3b13f7e-8978-4364-96dd-978878de9fce");
            var order = await RestClient.PostAsync<Order>("order_service", "/api/orders", cartViewModel);
            if (order.OrderStatus == OrderStatus.Processing || order.OrderStatus == OrderStatus.Paid)
                return await Task.FromResult(false);
            _messageBus.Publish(new CheckoutEvent(order.Id));
            return await Task.FromResult(true);
        }
    }
}