﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models.AdaptorAPIModels
{
    public enum TransactionType
    {
        PreAuth,
        Capture,
        Sale
    }

    public class GetOrginKeysRequestModel
    {
        public string MerchantAccount { get; set; }
        public string apiKey { get; set; }
        public List<string> RequestObject { get; set; }
    }

    public class GetpaymentMethodsRequestModel
    {
        public string MerchantAccount { get; set; }
        public string apiKey { get; set; }
    }

    public class CostEstimatorRequest
    {
        public string MerchantAccount { get; set; }
        public string apiKey { get; set; }
        public CostEstimatorObject RequestObject { get; set; }
    }



    public class CloudAPIRequestModel
    {
        public object RequestObject { get; set; }
    }
    public class RegulaRequest
    {

        public string Base64Image { get; set; }
        public string ImageFormat { get; set; }

    }


    public partial class CostEstimatorObject
    {
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("encryptedCard")]
        public string EncryptedCard { get; set; }

        [JsonProperty("mcc")]
        public long Mcc { get; set; }
    }
}