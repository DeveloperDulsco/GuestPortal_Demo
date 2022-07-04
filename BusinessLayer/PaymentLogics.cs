using CheckinPortal.DataAccess;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CheckinPortal.BusinessLayer
{
    public class PaymentLogics
    {

        public PaymentLogics()
        {

        }
        public DataTable SaveTransactionHistory(InsertPaymentHistoryUspModel model)
        {
            DataTable transaction = new DataTable();
            try
            {
                string Query = string.Empty;

                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionID", Value = model.TransactionID, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationNameID", Value = model.ReservationNameID, SqlDbType = SqlDbType.VarChar });

                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationNumber", Value = model.ReservationNumber, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@PData", Value = model.PData, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@MDData", Value = model.MDData, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@PaRes", Value = model.PaRes, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@PSPReference", Value = model.PSPReference, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ResultCode", Value = model.ResultCode, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@RefusalReason", Value = model.RefusalReason, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionType", Value = model.TransactionType, SqlDbType = SqlDbType.VarChar });
                transaction = SQLHelpers.Instance.ExecuteSP("usp_InsertPaymentHistory", sqlParameters);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }

            return transaction;
        }

        public DataTable GetPaymentHistory(string ReservationNumber)
        {


            DataTable transaction = new DataTable();
            try
            {
                string Query = string.Empty;

                SqlParameter reservationNoParameter = new SqlParameter()
                {
                    ParameterName = "@ReservationNumber",
                    Value = ReservationNumber,
                    SqlDbType = SqlDbType.VarChar
                };

                transaction = SQLHelpers.Instance.ExecuteSP("Usp_GetPaymentHistory", reservationNoParameter);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return transaction;

            //var PaymentHistory = db.tbPaymentHistories.Where(x => x.TransactionID == TransactionID).OrderByDescending(x => x.PaymentHistoryID).ToList();

            //return PaymentHistory.FirstOrDefault();
        }

        public async Task<bool> UpdateReservationPaymentStatus(string ReservationNumber)
        {

            var transaction = await SQLHelpers.Instance.ExecuteNonQuery($"UPDATE tbReservationDetails SET  EcomPaymentStatus = 1 WHERE ReservationNumber='{ReservationNumber}'");

            if (transaction> 0)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }


        public async Task<MakePaymentResponseModel> CaptureTransaction(TopupTransctionModels model)
        {
            using (var httpClient = new HttpClient())
            {
                Models.AdaptorAPIModels.makePaymentModel makePaymentModel = new Models.AdaptorAPIModels.makePaymentModel();

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();
                httpClient.BaseAddress = new Uri(BaseURL);
                makePaymentModel.ApiKey = APIKey;
                makePaymentModel.MerchantAccount = MerchantAccount;
                makePaymentModel.RequestObject = new Models.AdaptorAPIModels.RequestObject()
                {
                    Amount = model.AmountToCharge.ToString("0.00"),
                    orginalPSPRefernce = model.PspReferenceNumber,
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", ConfigurationManager.AppSettings["APIKey"].ToString());

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"PaymentCapture", requestContent);
                if (response != null)
                {
                    var test = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<MakePaymentResponseModel>(test);
                        return resposneObj;
                    }
                    else
                    {
                        return new MakePaymentResponseModel()
                        {
                            ResponseMessage = "",
                            Result = false
                        };
                    }
                }
                else
                {
                    return new MakePaymentResponseModel()
                    {
                        ResponseMessage = "",
                        Result = false
                    };
                }
            }
        }

        public async Task<MakePaymentResponseModel> TopupTransaction(TopupTransctionModels model)
        {
            using (var httpClient = new HttpClient())
            {
                Models.AdaptorAPIModels.makePaymentModel makePaymentModel = new Models.AdaptorAPIModels.makePaymentModel();

                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();
                httpClient.BaseAddress = new Uri(BaseURL);
                makePaymentModel.ApiKey = APIKey;
                makePaymentModel.MerchantAccount = MerchantAccount;
                makePaymentModel.RequestObject = new Models.AdaptorAPIModels.RequestObject()
                {
                    Amount = model.AmountToCharge.ToString("0.00"),
                    orginalPSPRefernce = model.PspReferenceNumber,
                    AdjustAuthorisationData = !string.IsNullOrEmpty(model.AdjustAuthorisationData) ? model.AdjustAuthorisationData : null
                };

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentModel);

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Add("x-api-key", ConfigurationManager.AppSettings["APIKey"].ToString());

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"PaymentTopUp", requestContent);
                if (response != null)
                {
                    var test = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<MakePaymentResponseModel>(test);
                        return resposneObj;
                    }
                    else
                    {
                        return new MakePaymentResponseModel()
                        {
                            ResponseMessage = "",
                            Result = false
                        };
                    }
                }
                else
                {
                        return new MakePaymentResponseModel()
                        {
                            ResponseMessage = "",
                            Result = false
                        };
                }
            }
        }
    }
}