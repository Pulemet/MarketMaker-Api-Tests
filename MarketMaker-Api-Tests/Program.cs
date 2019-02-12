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
using MarketMaker_Api_Tests.Helper;
using MarketMaker_Api_Tests.RealtimeTests;

namespace MarketMaker_Api_Tests
{
    class Program
    {
        public static TestEventHandler _testEvent;
        private const int AlgoId1 = 240;
        private const int AlgoId2 = 424;

        public static List<OrderDto> Orders = new List<OrderDto>();

        public static void PrintSpread(L2PackageDto l2Book)
        {
            Console.WriteLine("spread = {0}", AlgorithmInfo.CalculateSpread(l2Book.Entries));
        }

        static void Main(string[] args)
        {
            string test = "Execution: ETHBTC BUY 0.7 @ 0.03318805 (DLTXMM)";
            Console.WriteLine(AlgorithmInfo.GetExecutionSizeFromAlert(test));
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
