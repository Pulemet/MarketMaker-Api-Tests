using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Statistics;
using MarketMaker.Api.Rest;
using MarketMaker.Api.Subscriptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public class TestEventHandler
    {
        public TestEventHandler()
        {
            Algorithms = new List<AlgorithmInfo>();
            TestResult = true;
            _startTestTime = DateTime.Now;
        }

        private DateTime _startTestTime;
        // CurrentPositionSize can be partially updated in the message for trade statistics while all the executions are filled.
        // Sometimes the next message for trade statistics should be waited for.
        private DateTime _receivedExecutionsTime = DateTime.MaxValue;
        private const int WaitCurPosSizeSecs = 3;
        public List<AlgorithmInfo> Algorithms { get; set; }
        public bool TestResult { get; set; }
        public void CompareQuoteAgainstParams(AlgorithmInfo algo)
        {
            double originalSellQty = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double originalBuyQty = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double sellQty = 0.0, buyQty = 0.0;

            int originalSellLevels = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Length;
            int originalBuyLevels = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Length;

            int sellLevels = 0, buyLevels = 0;
            foreach (var ens in algo.AlgoDictionary[AlgorithmInfo.BookType.QUOTE].Entries)
            {
                if (ens.Side == Side.SELL)
                {
                    sellQty += ens.Quantity;
                    sellLevels++;
                }
                if (ens.Side == Side.BUY)
                {
                    buyQty += ens.Quantity;
                    buyLevels++;
                }
            }

            Debug.WriteLine("Sell levels: {0}", sellLevels);
            if (Util.CompareDouble(sellQty, originalSellQty))
                Debug.WriteLine("OK! Sell Qty: {0}; Original: {1}", sellQty, originalSellQty);
            else
                Debug.WriteLine("Error! Sell Qty: {0}; Original: {1}", sellQty, originalSellQty);

            Debug.WriteLine("Buy levels: {0}", buyLevels);
            if (Util.CompareDouble(buyQty, originalBuyQty))
                Debug.WriteLine("OK! Buy Qty: {0}; Original: {1}", buyQty, originalBuyQty);
            else
                Debug.WriteLine("Error! Buy Qty: {0}; Original: {1}", buyQty, originalBuyQty);

            CompareTestValues<bool>(true, Util.CompareDouble(sellQty, originalSellQty),
                                    String.Format("Sell qty: {0} is not equal to original qty: {1}", sellQty, originalSellQty));
            CompareTestValues<bool>(true, Util.CompareDouble(buyQty, originalBuyQty),
                                    String.Format("Buy qty: {0} is not equal to original qty: {1}", buyQty, originalBuyQty));
            CompareTestValues<int>(originalSellLevels, sellLevels,
                                    String.Format("Sell levels: {0} are not equal to original levels: {1}", sellLevels, originalSellLevels));
            CompareTestValues<int>(originalBuyLevels, buyLevels,
                                    String.Format("Buy levels: {0} are not equal to original levels: {1}", buyLevels, originalBuyLevels));
        }

        public void CompareSpread(AlgorithmInfo algo)
        {
            double spread = AlgorithmInfo.CalculateSpread(algo.AlgoDictionary[AlgorithmInfo.BookType.QUOTE].Entries);
            double paramSpread = AlgorithmInfo.GetSpreadFromParams(Algorithms[1].AlgoDictionary[AlgorithmInfo.BookType.QUOTE].Entries, algo.PricerConfigInfo.MinSpread);
            Debug.WriteLine("Spread: {0}, Parse spread: {1}, MinSpread: {2}", spread, paramSpread, algo.PricerConfigInfo.MinSpread);
            CompareTestValues(false, Double.IsNaN(paramSpread), "Spread is NAN - no subscription to Source book.");

            CompareTestValues<bool>(true, spread - paramSpread >= 0,
                                    String.Format("Spread from book: {0} less than original spread: {1}", spread, paramSpread));
        }

        public void CompareSourceAgainstTargetBook(AlgorithmInfo algo)
        {
            CompareTestValues<bool>(true, algo.AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.SOURCE) &&
                                          Algorithms[0].AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.TARGET),
                                          "Book is NULL!");
            CompareBooks(algo.AlgoDictionary[AlgorithmInfo.BookType.SOURCE],
                         Algorithms[0].AlgoDictionary[AlgorithmInfo.BookType.TARGET]); 
        }

        public void CompareStatisticAgainstTargetBook(AlgorithmInfo algo)
        {
            CompareTestValues<bool>(true, algo.AlgoDictionary.ContainsKey(AlgorithmInfo.BookType.TARGET) && algo.TradeStatistic != null,
                                    "Data is NULL");

            double sellQty = 0.0, buyQty = 0.0;
            foreach (var ens in algo.AlgoDictionary[AlgorithmInfo.BookType.TARGET].Entries)
            {
                if (ens.Side == Side.SELL)
                {
                    sellQty += ens.Quantity;
                }
                if (ens.Side == Side.BUY)
                {
                    buyQty += ens.Quantity;
                }
            }
            Debug.WriteLine("Trade Statistic Sell Qty: {0}, Target book: {1}", algo.TradeStatistic.OpenSellQty, sellQty);
            Debug.WriteLine("Trade Statistic Buy Qty: {0}, Target book: {1}", algo.TradeStatistic.OpenBuyQty, buyQty);

            CompareTestValues<bool>(true, Util.CompareDouble(algo.TradeStatistic.OpenBuyQty, buyQty),
                            String.Format("Buy Qty doesn't matched. OpenSellQty: {0}, Sell Qty from book: {1}", algo.TradeStatistic.OpenBuyQty, buyQty));
            CompareTestValues<bool>(true, Util.CompareDouble(algo.TradeStatistic.OpenSellQty, sellQty),
                            String.Format("Sell Qty doesn't matched. OpenBuyQty: {0}, Buy Qty from book: {1}", algo.TradeStatistic.OpenSellQty, sellQty));
        }

        public void CompareBooks(L2PackageDto sourceBook, L2PackageDto targetBook)
        {
            Debug.WriteLine("Number Source: {0}, Target: {1}", sourceBook.SequenceNumber, targetBook.SequenceNumber);
            Debug.WriteLine("Count of levels. Source {0}, Target: {1}", sourceBook.Entries.Count, targetBook.Entries.Count);
            CompareTestValues<int>(sourceBook.Entries.Count, targetBook.Entries.Count,
                                   String.Format("Count of levels is not equal. Source {0}, Quotes: {1}", sourceBook.Entries.Count, targetBook.Entries.Count));

            foreach (var level in sourceBook.Entries)
            {
                var targetLevel = targetBook.Entries.FirstOrDefault(l => l.Side == level.Side && l.Level == level.Level);

                CompareTestValues<bool>(true, targetLevel != null, String.Format("Level {0} doesn't exist for {1}", level.Level, level.Side));

                Debug.WriteLine("Side {3} Level {0}. Source {1}; Target: {2}", level.Level, level.Quantity, targetLevel.Quantity, targetLevel.Side);

                CompareTestValues(true, Util.CompareDouble(targetLevel.Quantity, level.Quantity),
                                        String.Format("Qty is not the same at level {0} for {1}. Source {2}; Target: {3}",
                                        level.Level, level.Side, level.Quantity, targetLevel.Quantity));
            }
        }

        public void InitialisePositionSize(AlgorithmInfo algo)
        {
            if (algo.TradeStatistic != null && algo.InitTradeStatistic == null)
                algo.InitTradeStatistic = (AlgoInstrumentStatisticsDto)algo.TradeStatistic.Clone(); ;
        }

        public void MonitorChangesPosition(AlgorithmInfo algo)
        {
            InitialisePositionSize(algo);
            Debug.WriteLine("MonitorChangesPosition, Initial Position Size: {0}, Seconds after received executions {1}",
                            algo.InitTradeStatistic.CurrentPositionSize, (DateTime.Now - _receivedExecutionsTime).TotalSeconds);
            if (algo.TradeStatistic != null && !Util.CompareDouble(algo.ChangePositionSize, 0.0) &&
               (DateTime.Now - _receivedExecutionsTime).TotalSeconds > WaitCurPosSizeSecs)
            {
                bool isBuyOrder = algo.OrderToSend.Side == Side.BUY;
                algo.ChangePositionSize = isBuyOrder ? -algo.ChangePositionSize : algo.ChangePositionSize;

                Debug.WriteLine("Enter inside. Initial position size: {0}, Change size: {1}, Current position size: {2} " +
                                "TradeSellQty: {3}, TradeBuyQty {4}", algo.InitTradeStatistic.CurrentPositionSize,
                                 algo.ChangePositionSize, algo.TradeStatistic.CurrentPositionSize,
                                 algo.TradeStatistic.TradeSellQty, algo.TradeStatistic.TradeBuyQty);

                CompareTestValues(true, CompareChangesStatistic(algo.InitTradeStatistic.CurrentPositionSize,
                                  algo.TradeStatistic.CurrentPositionSize, algo.ChangePositionSize),
                                  "Position Size is different from expected");

                if (isBuyOrder)
                    CompareTestValues(true,
                        CompareChangesStatistic(algo.InitTradeStatistic.TradeSellQty, algo.TradeStatistic.TradeSellQty,
                            Math.Abs(algo.ChangePositionSize)),
                        "Trade Sell Qty is different");
                else
                    CompareTestValues(true,
                        CompareChangesStatistic(algo.InitTradeStatistic.TradeBuyQty, algo.TradeStatistic.TradeBuyQty,
                            Math.Abs(algo.ChangePositionSize)),
                        "Trade Buy Qty is different");

                algo.InitTradeStatistic = (AlgoInstrumentStatisticsDto)algo.TradeStatistic.Clone();
                algo.ChangePositionSize = 0.0;
            }
        }

        public bool CompareChangesStatistic(double init, double current, double change)
        {
            return Util.CompareDouble(init + change, current);
        }

        public void CalculateSizeExecutions(AlgorithmInfo algo)
        {
            Debug.WriteLine("CalculateSizeExecutions, Executions count: {0}", algo.Executions.Count);

            algo.ChangePositionSize = 0.0;
            DateTime newTime = DateTime.MinValue;
            foreach (var exec in algo.Executions)
            {
                DateTime execTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(exec.Timestamp).ToLocalTime();
                if (execTime > _startTestTime)
                {
                    algo.ChangePositionSize += exec.ExecutionSize;
                    newTime = execTime > newTime ? execTime : newTime;
                    //Debug.WriteLine(exec.OrderCorrelationId);
                }
            }
            Debug.WriteLine("Summary executions size: {0}", algo.ChangePositionSize);

            if (Util.CompareDouble(algo.ChangePositionSize, 0.0))
                return;

            _startTestTime = newTime;
            double maxExecutionsSizeFromParams = algo.OrderToSend.Side == Side.BUY ? AlgorithmInfo.GetQuoteSize(algo.PricerConfigInfo.SellQuoteSizes)
                                                                        : AlgorithmInfo.GetQuoteSize(algo.PricerConfigInfo.BuyQuoteSizes);
            double maxExecutionsSize = algo.OrderToSend.Quantity <= maxExecutionsSizeFromParams ?
                                       algo.OrderToSend.Quantity : maxExecutionsSizeFromParams;
            CompareTestValues(true, Math.Abs(maxExecutionsSize - algo.ChangePositionSize) < Util.delta,
                                    String.Format("Size of executions exceeds the allowable. Summary size of executions: {0}, Allowable size: {1}",
                                    algo.ChangePositionSize, maxExecutionsSize));
            _receivedExecutionsTime = DateTime.Now;
        }

        public void CheckWebSocketStatus(string status)
        {
            CompareTestValues("200", status, String.Format("Error! Status {0}", status));
        }

        public void CompareTestValues<T>(T expected, T actual, string message)
        {
            TestResult = expected.Equals(actual) && TestResult;
            Assert.AreEqual(expected, actual, message);
        }
    }
}
