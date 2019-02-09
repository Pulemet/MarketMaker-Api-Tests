using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Statistics;
using MarketMaker.Api.Subscriptions;
using MarketMaker_Api_Tests.Helper;

namespace MarketMaker_Api_Tests.RealtimeTests
{
    public class CostEngine
    {
        public CostEngine()
        {
            ListExecutions = new List<ExecutionDto>();
        }
        public CostEngine(SubscriptionFactory subscriptions, AlgorithmInfo algo) : this()
        {
            Subscriptions = subscriptions;
            Algo = algo;
        }
        public AlgorithmInfo Algo { get; set; }
        public SubscriptionFactory Subscriptions { get; set; }
        public List<ExecutionDto> ListExecutions { get; set; }

        public void Start()
        {
            var executionsListener = Subscriptions.CreateExecutionsSubscription();
            executionsListener.Subscribe(Algo.AlgoId, SaveExecutions);
            Thread.Sleep(1000);

            var tradeStatisticListener = Subscriptions.CreateTradingStatisticsSubscription();
            tradeStatisticListener.Subscribe(CheckCost);

            Console.ReadLine();

            executionsListener.Unsubscribe(Algo.AlgoId, SaveExecutions);
            tradeStatisticListener.Unsubscribe(CheckCost);
        }

        protected List<ExecutionDto> GetExecutionsForCalcAvgPrice(List<ExecutionDto> executions, double curPosSize)
        {
            double tradeQty = 0;
            List<ExecutionDto> executionsForAvgPrice = new List<ExecutionDto>();
            foreach (var execution in executions.FindAll(a => new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(a.Timestamp).ToUniversalTime().Day ==
                                                              DateTime.UtcNow.Day).OrderByDescending(a => a.Timestamp))
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

        protected double CalculateCost(List<ExecutionDto> executions, double curPosSize)
        {
            var executionsForAvgPrice = GetExecutionsForCalcAvgPrice(executions, curPosSize);

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

        protected void CheckCost(AlgoInstrumentStatisticsDto[] statistics)
        {
            AlgoInstrumentStatisticsDto tradeStatistic = statistics.FirstOrDefault(a => a.AlgoId == Algo.AlgoId);
            if (tradeStatistic == null)
                return;
            Console.Clear();
            Console.WriteLine("Cost from statistic: {0}, Calculated cost: {1}", tradeStatistic.PositionCost, CalculateCost(ListExecutions, tradeStatistic.CurrentPositionSize));
        }

        protected void SaveExecutions(ExecutionDto[] executions)
        {
            if (executions.Length > 0)
                ListExecutions.AddRange(new List<ExecutionDto>(executions));
        }

    }
}
