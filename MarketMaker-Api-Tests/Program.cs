using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Statistics;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.CriptoCortex;
using MarketMaker_Api_Tests.CryptoCortex.Models;
using MarketMaker_Api_Tests.Helper;
using MarketMaker_Api_Tests.RealtimeTests;

namespace MarketMaker_Api_Tests
{
    class Program
    {
        private const string _ordersDestination = "/app/v1/orders/create";
        public static TestEventHandler _testEvent;
        private const int AlgoId1 = 240;
        private const int AlgoId2 = 424;

        public static List<OrderDto> Orders = new List<OrderDto>();

        private static IMarketMakerRestService _restCrypto;
        private static StompWebSocketServiceCrypto _wsCrypto;

        public static void PrintSpread(L2PackageDto l2Book)
        {
            Console.WriteLine("spread = {0}", AlgorithmInfo.CalculateSpread(l2Book.Entries));
        }

        public static void ConnectToCrypto()
        {
            _testEvent = new TestEventHandler();
            string authorization = "Basic d2ViOg==";
            string urlCrypto = "http://18.218.20.9";
            _restCrypto = MarketMakerRestServiceFactory.CreateMakerRestService(urlCrypto, "/oauth/token",
                authorization);
            _restCrypto.Authorize("Tester 1", "password");
            _wsCrypto = new StompWebSocketServiceCrypto("ws://18.218.20.9/websocket/v1?trader_0", _restCrypto.Token);
            _wsCrypto.Subscribe("/user/v1/responses", _testEvent.CheckWebSocketStatus);
        }

        static void Main(string[] args)
        {
            string test = "Execution: ETHBTC BUY 0.7 @ 0.03318805 (DLTXMM)";
            Console.WriteLine(AlgorithmInfo.GetExecutionSizeFromAlert(test));
            int i = 0;
            ConnectToCrypto();
            Thread.Sleep(1000);
            while (i < 10)
            {
                OrderCrypto orderToSend = new OrderCrypto() { Destination = "DLTXMM", Quantity = 1, Side = Side.BUY, Type = OrderType.MARKET, SecurityId = "ETHBTC" };
                _wsCrypto.SendMessage(_ordersDestination, orderToSend, _testEvent.OrderRequest);
                Thread.Sleep(2000);
                i++;
                if (i == 5)
                {
                    _wsCrypto.Unsubscribe(_ordersDestination, _testEvent.CheckWebSocketStatus);
                    _wsCrypto.Close();
                }
                    
            }

            //MarketMakerRest restEngine = new MarketMakerRest();
            //_testEvent = new TestEventHandler();
            //_testEvent.Algorithms.Add(new AlgorithmInfo(restEngine.RestService.GetInstrument(AlgoId1)));
            //_testEvent.Algorithms[0].AlgoId = AlgoId1;

            /* Start spreadObserver
            _testEvent.Algorithms.Add(new AlgorithmInfo(_mmRest.GetInstrument(AlgoId2)));
            _testEvent.Algorithms[1].AlgoId = AlgoId1;
            SpreadObserver spreadObserver = new SpreadObserver(_wsFactory, _testEvent.Algorithms[0], _testEvent.Algorithms[1]);
            spreadObserver.Observe();
            */

            //CostEngine costEngine = new CostEngine(restEngine.WebSocketService, _testEvent.Algorithms[0]);
            //costEngine.Start();

            //var quotesBookLister = _wsFactory.CreateQuotesSubscription();
            //quotesBookLister.Subscribe(AlgoId, PrintSpread);

            Console.ReadLine();

            //quotesBookLister.Unsubscribe(AlgoId, PrintSpread);
        }
    }
}
