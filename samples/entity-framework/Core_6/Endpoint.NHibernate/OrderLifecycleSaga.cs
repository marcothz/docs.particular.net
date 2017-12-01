﻿using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class OrderLifecycleSaga :
    Saga<OrderLifecycleSagaData>,
    IAmStartedByMessages<OrderSubmitted>,
    IHandleTimeouts<OrderTimeout>
{
    static ILog log = LogManager.GetLogger<OrderLifecycleSaga>();

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderLifecycleSagaData> mapper)
    {
        mapper.ConfigureMapping<OrderSubmitted>(m => m.OrderId)
            .ToSaga(s => s.OrderId);
    }

    public async Task Handle(OrderSubmitted message, IMessageHandlerContext context)
    {
        var orderTimeout = new OrderTimeout();
        await RequestTimeout(context, TimeSpan.FromSeconds(5), orderTimeout)
            .ConfigureAwait(false);

        var orderAccepted = new OrderAccepted
        {
            OrderId = message.OrderId,
        };

        log.Info($"Order process {message.OrderId} started.");

        await context.Reply(orderAccepted)
            .ConfigureAwait(false);
    }

    public Task Timeout(OrderTimeout state, IMessageHandlerContext context)
    {
        var completeOrder = new CompleteOrder
        {
            OrderId = Data.OrderId
        };
        return context.SendLocal(completeOrder);
    }
}