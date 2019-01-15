using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketMaker.Api.Models.Config;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace MarketMaker_Api_Tests.Helper
{
    public class AlgorithmInfo
    {
        public AlgorithmInfo()
        {
        }

        public AlgorithmInfo(string fileInstrument, string filePricer, string fileHeadger, string fileRiskLimit)
        {
            AlgoId = -1;
            InstrumentConfigInfo = JsonConvert.DeserializeObject<InstrumentConfigDto>(Util.ReadFile(Util.paramsFolder + fileInstrument));
            PricerConfigInfo = JsonConvert.DeserializeObject<PricerConfigDto>(Util.ReadFile(Util.paramsFolder + filePricer));
            HedgerConfigInfo = JsonConvert.DeserializeObject<HedgerConfigDto>(Util.ReadFile(Util.paramsFolder + fileHeadger));
            RiskLimitConfigInfo = JsonConvert.DeserializeObject<RiskLimitsConfigDto>(Util.ReadFile(Util.paramsFolder + fileRiskLimit));
        }

        public long AlgoId { get; set; }
        public InstrumentConfigDto InstrumentConfigInfo { get; set; }
        public PricerConfigDto PricerConfigInfo { get; set; }
        public HedgerConfigDto HedgerConfigInfo { get; set; }
        public RiskLimitsConfigDto RiskLimitConfigInfo { get; set; }

        public static InstrumentConfigDto CreateInstrumentConfig(string fileName)
        {
            return JsonConvert.DeserializeObject<InstrumentConfigDto>(Util.ReadFile(Util.paramsFolder + fileName));
        }
        public static PricerConfigDto CreatePricerConfig(string fileName)
        {
            return JsonConvert.DeserializeObject<PricerConfigDto>(Util.ReadFile(Util.paramsFolder + fileName));
        }
        public static HedgerConfigDto CreateHedgerConfig(string fileName)
        {
            return JsonConvert.DeserializeObject<HedgerConfigDto>(Util.ReadFile(Util.paramsFolder + fileName));
        }
        public static RiskLimitsConfigDto CreateRiskLimitConfig(string fileName)
        {
            return JsonConvert.DeserializeObject<RiskLimitsConfigDto>(Util.ReadFile(Util.paramsFolder + fileName));
        }

        public void SetAlgoId(long? id)
        {
            long algoId = id != null ? (long)id : 0;
            AlgoId = algoId;
            InstrumentConfigInfo.AlgoId = algoId;
            PricerConfigInfo.AlgoId = algoId;
            HedgerConfigInfo.AlgoId = algoId;
            RiskLimitConfigInfo.AlgoId = algoId;
        }

        public bool Equals(FullInstrumentConfigDto allConfiguration)
        {
            return this.EqualInstrument(allConfiguration.InstrumentConfig, true) &&
                   this.EqualPricer(allConfiguration.PricerConfig, true) &&
                   this.EqualHedger(allConfiguration.HedgerConfig, true) &&
                   this.EqualRiskLimit(allConfiguration.RiskLimitsConfig, true);
        }

        public bool EqualInstrument(InstrumentConfigDto instrumentConfig, bool printMessage)
        {
            bool isEqual = this.InstrumentConfigInfo.AlgoId == instrumentConfig.AlgoId &&
                   this.InstrumentConfigInfo.AlgoKey == instrumentConfig.AlgoKey &&
                   this.InstrumentConfigInfo.Exchange == instrumentConfig.Exchange &&
                   this.InstrumentConfigInfo.SourceExchange == instrumentConfig.SourceExchange &&
                   this.InstrumentConfigInfo.Instrument == instrumentConfig.Instrument &&
                   this.InstrumentConfigInfo.FxLeg == instrumentConfig.FxLeg &&
                   this.InstrumentConfigInfo.Underlyings == instrumentConfig.Underlyings;
            if(!isEqual && printMessage)
                Console.WriteLine("Instrument doesn't match with added.");
            return isEqual;
        }

        public bool EqualPricer(PricerConfigDto pricerConfig, bool printMessage)
        {
            bool isEqual = this.PricerConfigInfo.AlgoId == pricerConfig.AlgoId &&
                   this.PricerConfigInfo.AlgoKey == pricerConfig.AlgoKey &&
                   this.PricerConfigInfo.SellQuoteSizes == pricerConfig.SellQuoteSizes &&
                   this.PricerConfigInfo.BuyQuoteSizes == pricerConfig.BuyQuoteSizes &&
                   this.PricerConfigInfo.BuyMargins == pricerConfig.BuyMargins &&
                   this.PricerConfigInfo.SellMargins == pricerConfig.SellMargins &&
                   this.PricerConfigInfo.AggregationMethod == pricerConfig.AggregationMethod &&
                   Util.CompareDouble(this.PricerConfigInfo.MinPriceChange, pricerConfig.MinPriceChange);
            if (!isEqual && printMessage)
                Console.WriteLine("Pricer doesn't match with added.");
            return isEqual;
        }

        public bool EqualHedger(HedgerConfigDto hedgerConfig, bool printMessage)
        {
            bool isEqual = this.HedgerConfigInfo.AlgoId == hedgerConfig.AlgoId &&
                   this.HedgerConfigInfo.AlgoKey == hedgerConfig.AlgoKey &&
                   this.HedgerConfigInfo.HedgeInstrument == hedgerConfig.HedgeInstrument &&
                   this.HedgerConfigInfo.HedgeStrategy == hedgerConfig.HedgeStrategy &&
                   this.HedgerConfigInfo.TimeInForce == hedgerConfig.TimeInForce &&
                   this.HedgerConfigInfo.PositionMaxNormSize == hedgerConfig.PositionMaxNormSize &&
                   this.HedgerConfigInfo.VenuesList == hedgerConfig.VenuesList &&
                   Util.CompareDouble(this.HedgerConfigInfo.MaxOrderSize, hedgerConfig.MaxOrderSize);
            if (!isEqual && printMessage)
                Console.WriteLine("Hedger doesn't match with added.");
            return isEqual;
        }

        public bool EqualRiskLimit(RiskLimitsConfigDto riskLimitConfig, bool printMessage)
        {
            bool isEqual = this.RiskLimitConfigInfo.AlgoId == riskLimitConfig.AlgoId &&
                   this.RiskLimitConfigInfo.AlgoKey == riskLimitConfig.AlgoKey &&
                   this.RiskLimitConfigInfo.MinBuyQuoteActiveTime == riskLimitConfig.MinBuyQuoteActiveTime &&
                   this.RiskLimitConfigInfo.MinSellQuoteActiveTime == riskLimitConfig.MinSellQuoteActiveTime &&
                   Util.CompareDouble(this.RiskLimitConfigInfo.MaxLongExposure, riskLimitConfig.MaxLongExposure) &&
                   Util.CompareDouble(this.RiskLimitConfigInfo.MaxShortExposure, riskLimitConfig.MaxShortExposure);
            if (!isEqual && printMessage)
                Console.WriteLine("Risk Limits don't match with added.");
            return isEqual;
        }
    }
}
