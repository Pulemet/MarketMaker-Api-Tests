using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CriptoCortex;

namespace MarketMaker_Api_Tests.CryptoCortex
{
    public class SubscriptionFactory
    {
        protected const string ResponsesDestination = "/user/v1/responses";
        protected StompWebSocketServiceCrypto WebSocketService;
        private readonly string _url;
        private readonly string _token;

        public SubscriptionFactory(string url, string token)
        {
            _url = url;
            _token = token;
            WebSocketService = new StompWebSocketServiceCrypto(_url, _token);
        }

        public void ResponsesSubscribe()
        {
            WebSocketService.Subscribe(ResponsesDestination, CheckWebSocketStatus);
        }

        protected void CheckWebSocketStatus(string message)
        {
            if (message.Length > 3 && message.Substring(0, 3) != "200")
            {
                Console.WriteLine("Error! Status: {0}", message.Substring(0, 3));
            }
        }

        public void StopResponses()
        {
            WebSocketService.Unsubscribe(ResponsesDestination, CheckWebSocketStatus);
        }

        public void Close()
        {
            WebSocketService.Close();
        }
    }
}
