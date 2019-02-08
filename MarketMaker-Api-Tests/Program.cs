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

namespace MarketMaker_Api_Tests
{
    class Program
    {
        private const string _url = "https://18.218.146.41:8990";
        private const string _authorization = "Basic bW13ZWJ1aTptbQ==";

        private static IMarketMakerRestService _mmRest;
        private static SubscriptionFactory _wsFactory;
        private static TestEventHandler _testEvent;
        private const int AlgoId = 351;

        public static List<ExecutionDto> ListExecutions = new List<ExecutionDto>();
        public static List<OrderDto> Orders = new List<OrderDto>();

        public static void Initialize()
        {
            _mmRest = MakerMakerRestServiceFactory.CreateMakerRestService(_url, "/oauth/token",
                _authorization);
            _mmRest.Authorize("admin", "admin");
            _wsFactory = new SubscriptionFactory("wss://18.218.146.41:8990/websocket/v0", _mmRest.Token);

            _testEvent = new TestEventHandler();
        }

        public static void PrintSpread(L2PackageDto l2Book)
        {
            Console.WriteLine("spread = {0}", AlgorithmInfo.CalculateSpread(l2Book.Entries));
        }

        public static void PrintOrders(OrderDto[] orders)
        {
            foreach (var order in orders)
            {
                bool isExists = false;
                for (int i = 0; i < Orders.Count; i++)
                {
                    if (order.CorrelationId == Orders[i].CorrelationId)
                    {
                        if (!Orders[i].EqualsExceptPrice(order))
                        {
                            Console.WriteLine(order.CorrelationId + " " + order.OrderStatus + " " + Orders[i].OrderStatus + " " + order.ExecutedSize + " " + Orders[i].ExecutedSize);
                        }
                        Orders[i] = (OrderDto)order.Clone();
                        isExists = true;
                        break;
                    }
                }
                if (!isExists)
                {
                    Console.WriteLine("Added new order: {0}", order.CorrelationId);
                    Orders.Add(order);
                }
            }
            Console.WriteLine("--------------------------------------------------------");
        }

        public static void SaveExecutions(ExecutionDto[] executions)
        {
            if(executions.Length > 0)
                ListExecutions.AddRange(new List<ExecutionDto>(executions));
        }

        // ---------- For Calculate Cost ---------------------------------------------------

        public static List<ExecutionDto> GetExecutionsForAvgPrice(List<ExecutionDto> executions, double curPosSize)
        {
            double tradeQty = 0;
            List<ExecutionDto> executionsForAvgPrice = new List<ExecutionDto>();
            foreach (var execution in executions.OrderByDescending(a => a.Timestamp))
            {
                if (Math.Abs(curPosSize - tradeQty) > Util.Delta)
                {
                    tradeQty += execution.Side == Side.BUY ? execution.ExecutionSize : -execution.ExecutionSize;
                    executionsForAvgPrice.Add(execution);
                }
                else
                    break;
            }

            if (Math.Abs(curPosSize - tradeQty) > Util.Delta)
            {
                Console.WriteLine("Impossible to calculate Avg Price. Reason: These executions are not enough.");
                return new List<ExecutionDto>();
            }

            return executionsForAvgPrice;
        }

        public static double CalculateCost(List<ExecutionDto> executions, double curPosSize)
        {
            var executionsForAvgPrice = GetExecutionsForAvgPrice(executions, curPosSize);

            double tradeQty = 0, avgPrice = 0;
            foreach (var execution in executionsForAvgPrice.OrderBy(a => a.Timestamp))
            {
                double executionSize =
                    execution.Side == Side.BUY ? execution.ExecutionSize : -execution.ExecutionSize;

                if ((curPosSize > 0 && executionSize > 0) || (curPosSize < 0 && executionSize < 0))
                {
                    double currentCost = avgPrice * tradeQty;
                    double totalCost = currentCost + executionSize * execution.ExecutionPrice;
                    tradeQty += executionSize;
                    avgPrice = totalCost / tradeQty;
                }
                else
                    tradeQty += executionSize;
            }

            Console.WriteLine("Calc tradeQty: {0}, Avg Price {1}", tradeQty, avgPrice);
            return Math.Round(tradeQty * avgPrice, Util.OrderPricePrecision);
        }

        // ---------------------------------------------------------------------------------

        public static void CheckCost(AlgoInstrumentStatisticsDto[] statistics)
        {
            AlgoInstrumentStatisticsDto tradeStatistic = statistics.FirstOrDefault(a => a.AlgoId == AlgoId);
            if(tradeStatistic == null)
                return;
            Console.WriteLine("Cost from statistic: {0}, Calculated cost: {1}", tradeStatistic.PositionCost, CalculateCost(ListExecutions, tradeStatistic.CurrentPositionSize));
        }

        static void Main(string[] args)
        {
            Initialize();
            _testEvent.Algorithms.Add(new AlgorithmInfo(_mmRest.GetInstrument(AlgoId)));
            _testEvent.Algorithms[0].AlgoId = AlgoId;

            var ordersListener = _wsFactory.CreaOrdersSubscription();
            ordersListener.Subscribe(AlgoId, PrintOrders);

            //var quotesBookLister = _wsFactory.CreateQuotesSubscription();
            //quotesBookLister.Subscribe(AlgoId, PrintSpread);

            /*
            var executionsListener = _wsFactory.CreateExecutionsSubscription();
            executionsListener.Subscribe(AlgoId, SaveExecutions);
            Thread.Sleep(1000);
            var tradeStatisticListener = _wsFactory.CreateTradingStatisticsSubscription();
            tradeStatisticListener.Subscribe(CheckCost);
            */

            Console.ReadLine();
            //quotesBookLister.Unsubscribe(AlgoId, PrintSpread);

            ordersListener.Unsubscribe(AlgoId, PrintOrders);

            //executionsListener.Unsubscribe(AlgoId, SaveExecutions);
            //tradeStatisticListener.Unsubscribe(CheckCost);
        }
    }
}
