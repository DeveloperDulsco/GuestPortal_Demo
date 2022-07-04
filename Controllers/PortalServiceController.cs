﻿using CheckinPortal.BusinessLayer;
using CheckinPortal.DataAccess;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.PaymentDetails;
using Microsoft.Reporting.WebForms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebGrease.Configuration;

namespace CheckinPortal.Controllers 
{
    public class PortalServiceController : ApiController
    {
        public List<Models.StateMaster> GetStateByCountryID(int CountryID)
        {
            BusinessLayer.MastersLogics mastersLogics = new BusinessLayer.MastersLogics();
            return mastersLogics.GetStateListByCountryID(CountryID);
        }

        [HttpPost]
        [ActionName("GetPaymentmethods")]
        [Route("api/portalservice/GetPaymentmethods")]
        public async Task<string> GetPaymentmethods()
        {
            HttpClient httpClient = new HttpClient();

            string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
            string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
            string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

            Models.AdaptorAPIModels.GetpaymentMethodsRequestModel getpaymentMethodsRequestModel = new Models.AdaptorAPIModels.GetpaymentMethodsRequestModel()
            {
                apiKey = APIKey,
                MerchantAccount = MerchantAccount
            };

            httpClient.BaseAddress = new Uri(BaseURL);

            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(getpaymentMethodsRequestModel);// "{\"merchantAccount\":\"" + merchantAccount + "\"}";

            HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentmethods", requestContent);

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string responsestr = await response.Content.ReadAsStringAsync();
                    var Adyenresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.AdaptorAPIResponseModel>(responsestr);
                    return Adyenresponse.ResponseObject.ToString();
                }
            }
            return "";
        }
    
        [HttpPost]
        [ActionName("orginkey")]
        [Route("api/portalservice/orginkey")]
        public async Task<string> orginkey(string domainName)
        {
            HttpClient httpClient = new HttpClient();
            string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
            string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
            string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

            httpClient.BaseAddress = new Uri(BaseURL);

            Models.AdaptorAPIModels.GetOrginKeysRequestModel getOrginKeysRequestModel = new Models.AdaptorAPIModels.GetOrginKeysRequestModel()
            {
                apiKey = APIKey,
                MerchantAccount = MerchantAccount,
                RequestObject = new List<string>()  { domainName }
            };


            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(getOrginKeysRequestModel);// "{\"originDomains\": [\"" + domainName + "\"]}";

            //httpClient.DefaultRequestHeaders.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");

            HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await httpClient.PostAsync($"GetOrginKey", requestContent);

            if (response != null)
            {
                if (response.IsSuccessStatusCode)
                {
                    string responsestr = await response.Content.ReadAsStringAsync();
                    var Adyenresponse = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.AdaptorAPIResponseModel>(responsestr);

                    return Adyenresponse.ResponseObject.ToString();

                }
                //{
                //    //"originKeys": {
                //    //    "https:\/\/localhost:44356\/": "pub.v2.8015887802321585.aHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNTY.klg4CMjQaabQJWhz4xzTFnuM-Y8x9f8jftr1015wnvw"
                //    //}
                //}
            }
            return "";
        }




        [HttpPost]
        [ActionName("getCostEstimater")]
        [Route("api/portalservice/GetCostEstimater")]
        public async Task<CostEstimatorAPIResponseModel> GetCostEstimater(CostEstimatorObject model)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();

                model.Mcc = Convert.ToInt32(ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString());

                Models.AdaptorAPIModels.CostEstimatorRequest costEstimatorRequest = new Models.AdaptorAPIModels.CostEstimatorRequest()
                {
                    apiKey = APIKey,
                    MerchantAccount = MerchantAccount,
                    RequestObject = model
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(costEstimatorRequest);// "{\"merchantAccount\":\"" + merchantAccount + "\"}";

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"GetCostEstimater", requestContent);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        var ResponseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.CostEstimatorAPIResponseModel>(responsestr);


                        if(ResponseObj != null && ResponseObj.ResponseObject != null && ResponseObj.ResponseObject.CardBin != null)
                        {
                            ResponseObj.FundingSource = ResponseObj.ResponseObject.CardBin.FundingSource;
                            ResponseObj.PaymentMethod = ResponseObj.ResponseObject.CardBin.PaymentMethod;
                        }

                        return ResponseObj;
                    }
                    else
                    {
                        return new CostEstimatorAPIResponseModel()
                        {
                            Result = false,
                            ResponseMessage = response.ReasonPhrase
                        };
                    }
                }
                return new CostEstimatorAPIResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to get cost estimator"
                };
            }
            catch (Exception ex)
            {
                return new CostEstimatorAPIResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to get cost estimator"
                };
            }
        }


        [HttpPost]
        [ActionName("savePaymentDetails")]
        [Route("api/portalservice/savePaymentDetails")]
        public async Task<string> savePaymentDetails(PaymentDetailsModel paymentDetailsModel)
        {


            return "";
        }



        [HttpPost]
        [ActionName("makePaymentDetails")]
        [Route("api/portalservice/makePaymentDetails")]
        public async Task<string> makePaymentDetails(MakePaymentDetailRequestModel paymentDetailsModel)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri("https://checkout-test.adyen.com");

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(paymentDetailsModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"/v52/payments/details", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    string Test = response.Content.ReadAsStringAsync().Result;
                    return await response.Content.ReadAsStringAsync();
                }

            }

            return "";
        }



        [HttpPost]
        [ActionName("SendEmail")]
        [Route("api/portalservice/SendEmail")]
        public IHttpActionResult SendEmail(SendEmailModel sendEmail)
        {
            try
            {
                ReservationLogics reservationLogics = new ReservationLogics();
                var reservationsDt = reservationLogics.UpdatePrimaryGuestEmail(sendEmail.emailID, sendEmail.reservationID);
            }
            catch
            {
            }
           
            return Ok(new { result = true });
        }


        [HttpPost]
        [ActionName("ValidateDocument")]
        [Route("api/portalservice/ValidateDocument")]
        public async Task<ReadDocumentResponseModel> ValidateDocument(ValidateDocumentModel validateDocument)
        {
            HttpClient httpClient = new HttpClient();

            try
            {
                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();

                CloudAPIRequestModel validateDocRequest = new CloudAPIRequestModel()
                {
                    RequestObject = new RegulaRequest()
                    {
                        Base64Image = validateDocument.imageBase64,
                        ImageFormat = validateDocument.extension
                    }
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(validateDocRequest);

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"ProcessDocument", requestContent);

                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responsestr = await response.Content.ReadAsStringAsync();
                        var ResponseObj = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadDocumentResponseModel>(responsestr);

                        BusinessLayer.MastersLogics mastersLogics = new MastersLogics();
                        
                        if (ResponseObj.responseData != null)
                        {

                            Helpers.HttpClientHelper httpClientHelper = new Helpers.HttpClientHelper(BaseURL);

                            string requestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjDocImg = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedImage", requestJson);

                            if (ResponseObjDocImg != null && ResponseObjDocImg.responseData != null)
                            {
                                var ResponseObjDocImgData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GetDocumentImageResponseData>>(ResponseObjDocImg.responseData.ToString());
                                if(ResponseObjDocImgData != null && ResponseObjDocImgData.Count > 0)
                                {
                                    ResponseObj.responseData.fullImage = ResponseObjDocImgData[0].Base64ImageString;
                                }
                            }

                            string docTypeRequestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjDocType = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedDocumentType", docTypeRequestJson);

                            if(ResponseObjDocType != null)
                            {
                                var ResponseObjDocTypeData = Newtonsoft.Json.JsonConvert.DeserializeObject<RegulaDocumentTypeResponseModel>(ResponseObjDocType.responseData.ToString());
                                if(ResponseObjDocTypeData != null)
                                {
                                    ResponseObj.responseData.idType = ResponseObjDocTypeData.documentTypeCode;
                                    ResponseObj.responseData.issueCountry = ResponseObjDocTypeData.issueCountryCode;
                                }
                            }


                            string faceImageRequestJson = "{\"RequestObject\":\"" + ResponseObj.responseData.TransactionID + "\"}";
                            var ResponseObjFaceImage = await httpClientHelper.PostAsync<RegulaAPIResponseModel>("GetProcessedDocumentFaceImage", docTypeRequestJson);

                            if (ResponseObjFaceImage != null)
                            {
                               // var ResponseObjDocTypeData = Newtonsoft.Json.JsonConvert.DeserializeObject<RegulaDocumentTypeResponseModel>(ResponseObjDocType.responseData.ToString());
                                if (ResponseObjFaceImage.responseData != null)
                                {
                                    ResponseObj.responseData.faceImage = ResponseObjFaceImage.responseData.ToString();
                                }
                            }

                            var ResponseDataTable = mastersLogics.validateDocumentIssueCountry(ResponseObj.responseData.idType, ResponseObj.responseData.issueCountry);

                            if (ResponseDataTable != null && ResponseDataTable.Rows.Count > 0)
                            {
                                if (ResponseDataTable.Rows[0][0].ToString() == "1")
                                {
                                    return ResponseObj;
                                }
                                else
                                {
                                    ResponseObj.Result = false;
                                    return ResponseObj;
                                }
                            }
                            else
                            {
                                ResponseObj.Result = false;
                                return ResponseObj;
                            }
                        }

                        if(ResponseObj.responseData == null)
                        {
                            ResponseObj.responseData = new ReadDocumentModel();
                            ResponseObj.responseData.fullImage = validateDocument.imageBase64;
                        }
                        return new ReadDocumentResponseModel()
                        {
                            responseData = ResponseObj.responseData,
                            ResponseMessage = "Invalid document",
                            Result = false
                        };
                    }
                    else
                    {
                        return new ReadDocumentResponseModel()
                        {
                            Result = false,
                            ResponseMessage = response.ReasonPhrase
                        };
                    }
                }
                return new ReadDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
            catch(Exception ex)
            {
                return new ReadDocumentResponseModel()
                {
                    Result = false,
                    ResponseMessage = "Unable to read document"
                };
            }
        }



    }
}
