using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CryptoCortex.Models;

namespace MarketMaker_Api_Tests.CryptoCortex
{
    public class TraderSubscription : SubscriptionFactory
    {
        private const string CreateOrder = "/app/v1/orders/create";
        private const string BalanceDestination = "/app/v1/accounts";
        private const string OrdersSubscribe = "/user/v1/orders";
        private const string DeleteOrder = "/app/v1/orders/cancel";

        public TraderSubscription(string url, string token) : base(url, token)
        {

        }

        public void SendOrder(OrderCrypto order, Action<string> action)
        {
            WebSocketService.SendMessage("", CreateOrder, order, action);
        }

        public void CheckBalance(Action<string> action)
        {
            WebSocketService.SendMessage("", BalanceDestination, "", action);
        }

        public void CancelOrder(string orderId, Action<string> action)
        {
            string idHeader = String.Format("X-Deltix-Order-ID:{0}\r\n", orderId);
            WebSocketService.SendMessage(idHeader, DeleteOrder, "", action);
        }

        public void OrderBookSubcribe(string topic, Action<string> action)
        {
            WebSocketService.Subscribe(topic, action);
        }

        public void OrdersReceiver(Action<string> action)
        {
            WebSocketService.Subscribe(OrdersSubscribe, action);
        }

        public void OrderBookUnsubscribe(string topic, Action<string> action)
        {
            WebSocketService.Unsubscribe(topic, action);
        }

        public void OrdersUnsubscribe(Action<string> action)
        {
            WebSocketService.Unsubscribe(OrdersSubscribe, action);
        }
    }
}
