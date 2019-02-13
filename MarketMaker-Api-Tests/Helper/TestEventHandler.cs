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
            StartTestTime = DateTime.Now;
        }

        public DateTime StartTestTime { get; set; }
        // CurrentPositionSize can be partially updated in the message for trade statistics while all the executions are filled.
        // Sometimes the next message for trade statistics should be waited for.
        private DateTime _receivedTime = DateTime.MaxValue;
        private const int WaitAction = 3;
        public List<AlgorithmInfo> Algorithms { get; set; }
        public bool TestResult { get; set; }
        public void CompareQuoteAgainstParams(AlgorithmInfo algo)
        {
            double originalSellQty = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double originalBuyQty = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double sellQty = 0, buyQty = 0;

            int originalSellLevels = algo.PricerConfigInfo.SellQuoteSizes.Split(' ').Length;
            int originalBuyLevels = algo.PricerConfigInfo.BuyQuoteSizes.Split(' ').Length;

            int sellLevels = 0, buyLevels = 0;
            foreach (var ens in algo.AlgoDictionary[BookType.QUOTE].Entries)
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
            if((DateTime.Now - StartTestTime).TotalSeconds < WaitAction || !algo.AlgoDictionary.ContainsKey(BookType.TARGET))
                return;
            double quoteSpread = AlgorithmInfo.CalculateSpread(algo.AlgoDictionary[BookType.QUOTE].Entries);
            double targetSpread = AlgorithmInfo.CalculateSpread(algo.AlgoDictionary[BookType.TARGET].Entries);
            double paramSpread = AlgorithmInfo.GetSpreadFromParams(null, algo.PricerConfigInfo.MinSpread);
            Debug.WriteLine("Quote Spread: {0}, Target Spread: {1}, Parse spread: {2}, MinSpread: {3}",
                            quoteSpread, targetSpread, paramSpread, algo.PricerConfigInfo.MinSpread);

            CompareTestValues(true, quoteSpread - paramSpread >= -Util.Delta && targetSpread - paramSpread >= -Util.Delta,
                                    String.Format("Spreads from Quotes book: {0} and Target book: {1} are less than original spread: {2}", quoteSpread, targetSpread, paramSpread));
        }

        public void CompareSourceAgainstTargetBook(AlgorithmInfo algo)
        {
            if ((DateTime.Now - StartTestTime).TotalSeconds < WaitAction)
                return;
            CompareTestValues(true, algo.AlgoDictionary.ContainsKey(BookType.SOURCE) &&
                                          Algorithms[0].AlgoDictionary.ContainsKey(BookType.TARGET),
                                          "Book is NULL!");
            CompareBooks(algo.AlgoDictionary[BookType.SOURCE],
                         Algorithms[0].AlgoDictionary[BookType.TARGET]); 
        }

        public void CompareStatisticAgainstTargetBook(AlgorithmInfo algo)
        {
            CompareTestValues(true, algo.AlgoDictionary.ContainsKey(BookType.TARGET) && algo.TradeStatistic != null,
                                    "Data is NULL");

            double sellQty = 0, buyQty = 0;
            foreach (var ens in algo.AlgoDictionary[BookType.TARGET].Entries)
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

            CompareTestValues(true, Util.CompareDouble(algo.TradeStatistic.OpenBuyQty, buyQty),
                            String.Format("Buy Qty doesn't matched. OpenSellQty: {0}, Sell Qty from book: {1}", algo.TradeStatistic.OpenBuyQty, buyQty));
            CompareTestValues(true, Util.CompareDouble(algo.TradeStatistic.OpenSellQty, sellQty),
                            String.Format("Sell Qty doesn't matched. OpenBuyQty: {0}, Buy Qty from book: {1}", algo.TradeStatistic.OpenSellQty, sellQty));
        }

        public void CompareBooks(L2PackageDto sourceBook, L2PackageDto targetBook)
        {
            Debug.WriteLine("Number Source: {0}, Target: {1}", sourceBook.SequenceNumber, targetBook.SequenceNumber);
            Debug.WriteLine("Count of levels. Source {0}, Target: {1}", sourceBook.Entries.Count, targetBook.Entries.Count);
            CompareTestValues(sourceBook.Entries.Count, targetBook.Entries.Count,
                                   String.Format("Count of levels is not equal. Source {0}, Quotes: {1}", sourceBook.Entries.Count, targetBook.Entries.Count));

            foreach (var level in sourceBook.Entries)
            {
                var targetLevel = targetBook.Entries.FirstOrDefault(l => l.Side == level.Side && l.Level == level.Level);

                CompareTestValues(true, targetLevel != null, String.Format("Level {0} doesn't exist for {1}", level.Level, level.Side));

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
                            algo.InitTradeStatistic.CurrentPositionSize, (DateTime.Now - _receivedTime).TotalSeconds);
            if (algo.TradeStatistic != null && !Util.CompareDouble(algo.ChangePositionSize, 0.0) &&
               (DateTime.Now - _receivedTime).TotalSeconds > WaitAction)
            {
                bool isBuyOrder = algo.OrderToSend.Side == Side.BUY;
                algo.ChangePositionSize = isBuyOrder ? -algo.ChangePositionSize : algo.ChangePositionSize;
                Console.WriteLine(algo.OrderToSend.Side);
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
                        "Trade Sell Qty is different from expected");
                else
                    CompareTestValues(true,
                        CompareChangesStatistic(algo.InitTradeStatistic.TradeBuyQty, algo.TradeStatistic.TradeBuyQty,
                            Math.Abs(algo.ChangePositionSize)),
                        "Trade Buy Qty is different from expected");

                algo.InitTradeStatistic = (AlgoInstrumentStatisticsDto)algo.TradeStatistic.Clone();
                algo.ChangePositionSize = 0;
            }
        }

        public bool CompareChangesStatistic(double init, double current, double change)
        {
            return Util.CompareDouble(init + change, current);
        }

        public void CalculateExecutions(AlgorithmInfo algo)
        {
            Debug.WriteLine("CalculateExecutions, Number of executions: {0}", algo.Executions.Count);

            algo.ChangePositionSize = 0;

            DateTime newTime = DateTime.MinValue;
            foreach (var exec in algo.Executions)
            {
                DateTime execTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(exec.Timestamp).ToLocalTime();
                if (execTime > StartTestTime)
                {
                    algo.ChangePositionSize += exec.ExecutionSize;
                    newTime = execTime > newTime ? execTime : newTime;
                }
            }

            CalculateSizeExecutions(algo, newTime);
        }

        public void CalculateExecutionsFromAlerts(AlgorithmInfo algo)
        {
            Debug.WriteLine("CalculateExecutionsFromAlerts, Number of alerts for executions : {0}", algo.Alerts.Count);

            algo.ChangePositionSize = 0;

            DateTime newTime = DateTime.MinValue;
            foreach (var alert in algo.Alerts)
            {
                DateTime execTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(alert.Timestamp).ToLocalTime();
                if (execTime > StartTestTime)
                {
                    algo.ChangePositionSize += AlgorithmInfo.GetExecutionSizeFromAlert(alert.Description);
                    newTime = execTime > newTime ? execTime : newTime;
                }
            }

            CalculateSizeExecutions(algo, newTime);
        }

        public void CalculateSizeExecutions(AlgorithmInfo algo, DateTime newTime)
        {
            Debug.WriteLine("Summary executions size: {0}", algo.ChangePositionSize);

            if (Util.CompareDouble(algo.ChangePositionSize, 0))
                return;

            StartTestTime = newTime;
            double maxExecutionsSizeFromParams = algo.OrderToSend.Side == Side.BUY ? AlgorithmInfo.GetQuoteSize(algo.PricerConfigInfo.SellQuoteSizes)
                                                                        : AlgorithmInfo.GetQuoteSize(algo.PricerConfigInfo.BuyQuoteSizes);
            double maxExecutionsSize = algo.OrderToSend.Quantity <= maxExecutionsSizeFromParams ?
                                       algo.OrderToSend.Quantity : maxExecutionsSizeFromParams;
            CompareTestValues(true, Math.Abs(algo.ChangePositionSize - algo.ChangePositionSize) < Util.Delta,
                                    String.Format("Size of executions doesn't match with expected. Summary size of executions: {0}, Allowable size: {1}",
                                    algo.ChangePositionSize, maxExecutionsSize));
            _receivedTime = DateTime.Now;
        }

        public void CompareOrdersAgainstOpenQty(AlgorithmInfo algo)
        {
            DateTime time = DateTime.Now;
            Debug.WriteLine("CompareOrdersAgainstOpenQty, Seconds after received executions {0}, Current Time: {1}",
                            (time - _receivedTime).TotalSeconds, time.Hour + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond);
            Debug.WriteLine("Open Buy Qty: {0}, Orders Buy Qty: {1}, Open Sell Qty: {2}, Orders Sell Qty: {3}",
                algo.TradeStatistic.OpenBuyQty, algo.OrdersBuyQty, algo.TradeStatistic.OpenSellQty, algo.OrdersSellQty);

            if ((DateTime.Now - _receivedTime).TotalSeconds < WaitAction)
                return;

            Debug.WriteLine("Enter inside");

            CompareTestValues(true, Util.CompareDouble(algo.TradeStatistic.OpenBuyQty, algo.OrdersBuyQty),
                              String.Format("Open Buy Qty is different: {0}, Orders Buy Qty: {1}", algo.TradeStatistic.OpenBuyQty, algo.OrdersBuyQty));
            CompareTestValues(true, Util.CompareDouble(algo.TradeStatistic.OpenSellQty, algo.OrdersSellQty),
                              String.Format("Open Sell Qty is different: {0}, Orders Sell Qty: {1}", algo.TradeStatistic.OpenSellQty, algo.OrdersSellQty));
            _receivedTime = DateTime.Now;
        }

        public void CalculateSizeOrders(AlgorithmInfo algo)
        {
            Debug.WriteLine(algo.IsChangedOrders);
            if (!algo.IsChangedOrders)
                return;
            DateTime time = DateTime.Now;
            Debug.WriteLine("Orders are received: " + time.Hour + ":" + time.Minute + ":" + time.Second + ":" + time.Millisecond);
            algo.CalculateOrdersQty();
            _receivedTime = time;
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
