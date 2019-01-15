using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Book;
using MarketMaker.Api.Models.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public class WebSocketEvent
    {
        public WebSocketEvent()
        {
            Algorithms = new List<AlgorithmInfo>();
        }

        public List<AlgorithmInfo> Algorithms { get; set; }

        public void OnQuoteMessage(L2PackageDto l2Book)
        {
            if (l2Book.Type != L2PackageType.SNAPSHOT_FULL_REFRESH)
                return;

            double originalSellCount = Algorithms[0].PricerConfigInfo.SellQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double originalBuyCount = Algorithms[0].PricerConfigInfo.BuyQuoteSizes.Split(' ').Sum(x => Double.Parse(x));
            double sellCount = 0.0, buyCount = 0.0;

            int originalSellLevels = Algorithms[0].PricerConfigInfo.SellQuoteSizes.Split(' ').Length;
            int originalBuyLevels = Algorithms[0].PricerConfigInfo.BuyQuoteSizes.Split(' ').Length;

            int sellLevels = 0, buyLevels = 0;
            foreach (var ens in l2Book.Entries)
            {
                if (ens.Side == Side.SELL)
                {
                    sellCount += ens.Quantity;
                    sellLevels++;
                }
                if (ens.Side == Side.BUY)
                {
                    buyCount += ens.Quantity;
                    buyLevels++;
                }
            }
            // DEBUG
            /*
            if (sellCount <= originalSellCount)
                Console.WriteLine("OK! Sell Qty: {0}; Original: {1}", sellCount, originalSellCount);
            else
                Console.WriteLine("Error! Sell Qty: {0}; Original: {1}", sellCount, originalSellCount);
            Console.WriteLine("Sell levels: {0}", sellLevels);
            if (buyCount <= originalBuyCount)
                Console.WriteLine("OK! Buy Qty: {0}; Original: {1}", buyCount, originalBuyCount);
            else
                Console.WriteLine("Error! Buy Qty: {0}; Original: {1}", buyCount, originalBuyCount);
            Console.WriteLine("Buy levels: {0}", buyLevels);
            */
            Assert.AreEqual(sellCount <= originalSellCount, true, "Sell qty is not equal to original qty");
            Assert.AreEqual(buyCount <= originalBuyCount, true, "Buy qty is not equal to original qty");
            Assert.AreEqual(sellLevels <= originalSellLevels, true, "Sell levels are not equal to original levels");
            Assert.AreEqual(buyLevels <= originalBuyLevels, true, "Buy levels are not equal to original levels");
        }

        public void OnStatisticsMessage(AlgoInstrumentStatisticsDto[] statistics)
        {
            foreach (var st in statistics)
            {
                var foundAlgo = Algorithms.FirstOrDefault(a => a.AlgoId == st.AlgoId);
                if (foundAlgo != null)
                {
                    this.CheckStatistics(foundAlgo, st);
                }
            }
        }

        private void CheckStatistics(AlgorithmInfo algo, AlgoInstrumentStatisticsDto st)
        {
            if (st.OpenBuyQty <= algo.RiskLimitConfigInfo.MaxLongExposure)
                Console.WriteLine("OK! AlgoId: {0} BuyQty, Expected = {1}; Actual: {2}", algo.AlgoId,
                    algo.RiskLimitConfigInfo.MaxLongExposure, st.OpenBuyQty);
            else
                Console.WriteLine("Error! AlgoId: {0} BuyQty, Expected = {1}; Actual: {2}", algo.AlgoId,
                    algo.RiskLimitConfigInfo.MaxLongExposure, st.OpenBuyQty);
            if (st.OpenSellQty <= algo.RiskLimitConfigInfo.MaxShortExposure)
                Console.WriteLine("OK! AlgoId: {0} SellQty, Expected = {1}; Actual: {2}", algo.AlgoId,
                    algo.RiskLimitConfigInfo.MaxShortExposure, st.OpenSellQty);
            else
                Console.WriteLine("Error! AlgoId: {0} SellQty, Expected = {1}; Actual: {2}", algo.AlgoId,
                    algo.RiskLimitConfigInfo.MaxShortExposure, st.OpenSellQty);
        }
    }
}
