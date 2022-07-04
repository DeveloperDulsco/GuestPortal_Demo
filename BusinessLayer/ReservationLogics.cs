using CheckinPortal.DataAccess;
using CheckinPortal.Helpers;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.PaymentDetails;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;

namespace CheckinPortal.BusinessLayer
{
    public class ReservationLogics
    {
        public int ReservationID { get; set; }
        public ReservationLogics()
        {
            var ConnectionString = ConfigurationManager.AppSettings["ConnectionString"].ToString();
            Helpers.SQLHelpers.Instance.SetConnectionString(ConnectionString);

        }

        public DataTable GetReservationDetailsDT(string ReservationNumber)
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

                transaction = SQLHelpers.Instance.ExecuteSP("usp_GetReservationDetails", reservationNoParameter);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return transaction;
        }


        //public async Task<List<usp_GetReservationDetails_Result>> GetReservationDetails1(string ConfirmationNo)
        //{
        //    using (var db = new DataAccess.SwissotelStamfordEntities())
        //    {
        //        return await Task.Run(() =>
        //        {
        //            return db.usp_GetReservationDetails(ConfirmationNo).ToList();
        //        });
        //    }
        //}


        public async Task<List<Usp_GetProfileInformationByReservationID_Result>> GetReservationProfileList(int ReservationID)
        {

            List<Usp_GetProfileInformationByReservationID_Result> Profiles = new List<DataAccess.Usp_GetProfileInformationByReservationID_Result>();
            try
            {
                SqlParameter reservationNoParameter = new SqlParameter()
                {
                    ParameterName = "@ReservationDetailID",
                    Value = ReservationID,
                    SqlDbType = SqlDbType.Int
                };

                var ProfilesDt = SQLHelpers.Instance.ExecuteSP("Usp_GetProfileInformationByReservationID", reservationNoParameter);
                Profiles = Helpers.DataTableHelper.DataTableToList<Usp_GetProfileInformationByReservationID_Result>(ProfilesDt);
            }
            catch (Exception ex)
            {

            }
            return Profiles;

        }

        public List<Models.Questions> GetQuestions()
        {
            List<Models.Questions> Questions = new List<Models.Questions>();
            try
            {
                var QuestionsDt = SQLHelpers.Instance.ExecuteSP("Usp_GetQuestionMaster");
                 Questions = Helpers.DataTableHelper.DataTableToList<Models.Questions>(QuestionsDt);
            }
            catch (Exception ex)
            {

            }
            return Questions;
        }


        public List<Models.PackageMasterModal> GetPackages(string RoomType)
        {
            List<Models.PackageMasterModal> Questions = new List<Models.PackageMasterModal>();
            try
            {
                SqlParameter reservationIDParameter = new SqlParameter()
                {
                    ParameterName = "@RoomTypeCode",
                    Value = RoomType,
                    SqlDbType = SqlDbType.VarChar
                };

                var QuestionsDt = SQLHelpers.Instance.ExecuteSP("Usp_GetPackageMaster", reservationIDParameter);

                Questions = Helpers.DataTableHelper.DataTableToList<Models.PackageMasterModal>(QuestionsDt);

            }
            catch 
            {

            }
            return Questions;
        }

        public void UpdateReservationByStage(string Stage, Models.UpdateReservationModel reservationModel)
        {

            List<SqlParameter> sqlParameters = new List<SqlParameter>();
            sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationID", Value = reservationModel.ReservationID, SqlDbType = SqlDbType.Int });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@ETA", Value = reservationModel.ETA, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@FlightNo", Value = reservationModel.FlightNo, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@ProfileDetailID", Value = reservationModel.ProfileDetailID, SqlDbType = SqlDbType.Int });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@MembershipNo", Value = reservationModel.MembershipNo, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@Email", Value = reservationModel.Email, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@Phone", Value = reservationModel.Phone, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@AddressLine1", Value = reservationModel.AddressLine1, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@AddressLine2", Value = reservationModel.AddressLine2, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@City", Value = reservationModel.City, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@StateMasterID", Value = reservationModel.StateMasterID, SqlDbType = SqlDbType.Int });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@PostalCode", Value = reservationModel.PostalCode, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@CountryMasterID", Value = reservationModel.CountryMasterID, SqlDbType = SqlDbType.Int });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@SignatureImage", Value = reservationModel.SignatureImage, SqlDbType = SqlDbType.VarBinary });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@DocumentImage1", Value = reservationModel.DocumentImage1, SqlDbType = SqlDbType.VarBinary });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@DocumentImage2", Value = reservationModel.DocumentImage2, SqlDbType = SqlDbType.VarBinary });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@DocumentImage3", Value = reservationModel.DocumentImage3, SqlDbType = SqlDbType.VarBinary });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@FaceImage", Value = reservationModel.FaceImage, SqlDbType = SqlDbType.VarBinary });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@Type", Value = Stage, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@IsMemberShipEnrolled", Value = reservationModel.IsMemberShipEnrolled, SqlDbType = SqlDbType.Bit });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@DocumentNumber", Value = reservationModel.DocumentNumber, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@DocumentTypeCode", Value = reservationModel.DocumentType, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@ExpiryDate", Value = reservationModel.ExpiryDate, SqlDbType = SqlDbType.Date });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@IssueDate", Value = reservationModel.IssueDate, SqlDbType = SqlDbType.DateTime });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@Gender", Value = reservationModel.Gender, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@IssueCountry", Value = reservationModel.IssueCountry, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@BirthDate", Value = reservationModel.BirthDate, SqlDbType = SqlDbType.DateTime });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@NationalityCharCode", Value = reservationModel.Nationality, SqlDbType = SqlDbType.VarChar });

            sqlParameters.Add(new SqlParameter() { ParameterName = "@FirstName", Value = reservationModel.FirstName, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@MiddleName", Value = reservationModel.MiddleName, SqlDbType = SqlDbType.VarChar });
            sqlParameters.Add(new SqlParameter() { ParameterName = "@LastName", Value = reservationModel.LastName, SqlDbType = SqlDbType.VarChar });

            Helpers.SQLHelpers.Instance.ExecuteSP("Usp_UpdateReservationByGuest", sqlParameters);
        }



        public bool InsertFeedback(int reservationID,int questionID,string answer)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationID", Value = reservationID, SqlDbType = SqlDbType.Int });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@QuestionID", Value = questionID, SqlDbType = SqlDbType.Int });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@Answer", Value = answer, SqlDbType = SqlDbType.VarChar });
                var ResultDt = Helpers.SQLHelpers.Instance.ExecuteSP("Usp_InsertFeedbackData", sqlParameters);

                if (ResultDt != null && ResultDt.Rows.Count > 0)
                {
                    return ResultDt.Rows[0][0].ToString() == "1";
                }
            }
            catch (Exception ex)
            {
                //log error
            }

            return false;
        }


        public bool InsertEvent(int reservationID, string evenSubModuleName)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationID", Value = reservationID, SqlDbType = SqlDbType.Int });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@EventSubModule", Value = evenSubModuleName, SqlDbType = SqlDbType.VarChar });
                var ResultDt = Helpers.SQLHelpers.Instance.ExecuteSP("Usp_InsertEventsTracking", sqlParameters);

                if (ResultDt != null && ResultDt.Rows.Count > 0)
                {
                    return ResultDt.Rows[0][0].ToString() == "1";
                }
            }
            catch (Exception ex)
            {
                //log error
            }

            return false;
        }


        public bool InsertReservationPackageDetails(int reservationID, string[] PackageList)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();

                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("ReservationDetailID");
                dataTable.Columns.Add("PackageID");

                foreach (var item in PackageList)
                {
                    DataRow dr = dataTable.NewRow();
                    dr["ReservationDetailID"] = reservationID;
                    dr["PackageID"] = item;
                    dataTable.Rows.Add(dr);
                }


                sqlParameters.Add(new SqlParameter() { ParameterName = "@TBReservationPackageType", Value = dataTable });

                var ResultDt = Helpers.SQLHelpers.Instance.ExecuteSP("Usp_InsertReservationPackageDetails", sqlParameters);
                return true;
                //if (ResultDt != null && ResultDt.Rows.Count > 0)
                //{
                //    return ResultDt.Rows[0][0].ToString() == "1";
                //}
            }
            catch (Exception ex)
            {
                //log error
            }

            return false;
        }

        public bool UpdateCheckoutFlag(int reservationID)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationDetailID", Value = reservationID, SqlDbType = SqlDbType.Int });
                var ResultDt = Helpers.SQLHelpers.Instance.ExecuteSP("Usp_UpdatePreCheckOutFlag", sqlParameters);

                if (ResultDt != null && ResultDt.Rows.Count > 0)
                {
                    return ResultDt.Rows[0][0].ToString() == "1";
                }
                return true;
            }
            catch (Exception ex)
            {
                //log error
            }

            return false;
        }

        public bool UpdatePaymentHeaderData(string transactionID, string ResultCode, string ResponseMessage,bool? isActive,string transactionType,decimal Amount)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionID", Value = transactionID, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ResultCode", Value = ResultCode, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ResponseMessage", Value = ResponseMessage, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@IsActive", Value = isActive, SqlDbType = SqlDbType.Bit });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionType", Value = transactionType, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@Amount", Value = Amount, SqlDbType = SqlDbType.Decimal });

                var ProfilesDt = SQLHelpers.Instance.ExecuteSP("Usp_UpdatePaymentHeader", sqlParameters);

                if (ProfilesDt != null && ProfilesDt.Rows.Count > 0)
                {
                    if (ProfilesDt.Rows[0]["Result"].ToString() != "0")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        public List<Models.ActiveTransctionsModel> GetActivePaymentTransctions(string ReservationNumber)
        {

            List<Models.ActiveTransctionsModel> ActiveTranscitons = new List<Models.ActiveTransctionsModel>();

            try
            {

                List<SqlParameter> sqlParameters = new List<SqlParameter>();

                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationNumber", Value = ReservationNumber, SqlDbType = SqlDbType.VarChar });

                var ResultDt = Helpers.SQLHelpers.Instance.ExecuteSP("Usp_GetActivePaymentTransctions", sqlParameters);

                ActiveTranscitons = Helpers.DataTableHelper.DataTableToList<Models.ActiveTransctionsModel>(ResultDt);

            }
            catch 
            {
                //log error
            }

            return ActiveTranscitons;
        }

        public async Task<bool> InsertPaymentData(PaymentResponse paymentDetailResponseModel, string ReservationNumber, string ReservationNameID,string TransactionID,string transactionType)
        {
            try
            {
                List<SqlParameter> sqlParameters = new List<SqlParameter>();
                string maskedCarNumber = paymentDetailResponseModel.MaskCardNumber != null ? paymentDetailResponseModel.MaskCardNumber : "";
                string fundingSource = paymentDetailResponseModel.FundingSource != null ? paymentDetailResponseModel.FundingSource : "";

                //bool isActive = true;
                //if(transactionType == "SALE")
                //{
                //    isActive = false;
                //}

                string amount = paymentDetailResponseModel.Amount.Value.ToString("0.00");

                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionID", Value = TransactionID, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationNumber", Value = ReservationNumber, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ReservationNameID", Value = ReservationNameID, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@MaskedCardNumber", Value = maskedCarNumber, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ExpiryDate", Value = paymentDetailResponseModel.CardExpiryDate, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@FundingSource", Value = fundingSource, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@AuthorisationCode", Value = paymentDetailResponseModel.AuthCode, SqlDbType = SqlDbType.VarChar });

                sqlParameters.Add(new SqlParameter() { ParameterName = "@Amount", Value = amount, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@Currency", Value = paymentDetailResponseModel.Currency, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@RecurringIdentifier", Value = paymentDetailResponseModel.PaymentToken, SqlDbType = SqlDbType.VarChar });
 
                sqlParameters.Add(new SqlParameter() { ParameterName = "@pspReferenceNumber", Value = paymentDetailResponseModel.PspReference, SqlDbType = SqlDbType.VarChar });

                string parentPspReference = paymentDetailResponseModel.PspReference;

                if (!string.IsNullOrEmpty(paymentDetailResponseModel.ParentPSPReferece))
                {
                    parentPspReference = paymentDetailResponseModel.ParentPSPReferece;
                }

                sqlParameters.Add(new SqlParameter() { ParameterName = "@ParentPspRefereceNumber", Value = parentPspReference, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@TransactionType", Value = transactionType, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ResultCode", Value = paymentDetailResponseModel.ResultCode, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@ResponseMessage", Value = paymentDetailResponseModel.RefusalReason, SqlDbType = SqlDbType.VarChar });
                sqlParameters.Add(new SqlParameter() { ParameterName = "@CardType ", Value = paymentDetailResponseModel.CardType, SqlDbType = SqlDbType.VarChar });
                //sqlParameters.Add(new SqlParameter() { ParameterName = "@IsActive ", Value = isActive, SqlDbType = SqlDbType.Bit });



                DataTable dataTable = new DataTable();
                dataTable.Columns.Add("KeyHeader", typeof(string));
                dataTable.Columns.Add("KeyValue", typeof(string));


                if (paymentDetailResponseModel.additionalInfos != null)
                {
                    foreach (var item in paymentDetailResponseModel.additionalInfos)
                    {
                        DataRow drow1 = dataTable.NewRow();
                        drow1["KeyHeader"] = item.key;
                        drow1["KeyValue"] = item.value;
                        dataTable.Rows.Add(drow1);
                    }
                }


                sqlParameters.Add(new SqlParameter() { ParameterName = "@TbAdditionalPaymentInfoType", Value = dataTable });

                var ProfilesDt = SQLHelpers.Instance.ExecuteSP("Usp_InsertPaymentDetails", sqlParameters);

                if (ProfilesDt != null && ProfilesDt.Rows.Count > 0)
                {
                    if (ProfilesDt.Rows[0]["Result"].ToString() != "0")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            return false;
        }

        public bool updateCheckoutFlag(string reservationNameID)
        {
            List<DataAccess.tbCountryMaster> tbCountryMasters = new List<DataAccess.tbCountryMaster>();
            DataTable transaction = new DataTable();
            try
            {
                string Query = $"update tbReservationDetails set IsCheckOutFlag = 1,IsPreCheckedOutPMS=1 where ReservationNameID = {reservationNameID}";
                var countryTable = SQLHelpers.Instance.ExecuteDataset(Query);
                tbCountryMasters = Helpers.DataTableHelper.DataTableToList<CheckinPortal.DataAccess.tbCountryMaster>(countryTable);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return true;
        }


        public DataTable UpdatePrimaryGuestEmail(string emailID, string ReservationID)
        {
            DataTable transaction = new DataTable();
            try
            {
                string Query = string.Empty;

                SqlParameter reservationIDParameter = new SqlParameter()
                {
                    ParameterName = "@ReservationDetailID",
                    Value = ReservationID,
                    SqlDbType = SqlDbType.Int
                };

                SqlParameter emailIDParameter = new SqlParameter()
                {
                    ParameterName = "@Email",
                    Value = emailID,
                    SqlDbType = SqlDbType.VarChar
                };

                transaction = SQLHelpers.Instance.ExecuteSP("Usp_UpdateEmailIDByReservationID", reservationIDParameter, emailIDParameter);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return transaction;
        }

        public List<ReservationPackageModel> GetReservationPackages(int ReservationDetailID)
        {
            List<ReservationPackageModel> ReservationPackagesList = new List<ReservationPackageModel>();
            try
            {
                string Query = string.Empty;

                SqlParameter reservationNoParameter = new SqlParameter()
                {
                    ParameterName = "@ReservationDetailID",
                    Value = ReservationDetailID,
                    SqlDbType = SqlDbType.Int
                };

                var transaction = SQLHelpers.Instance.ExecuteSP("usp_FetchReservationPackageByReservationDetailID", reservationNoParameter);
                ReservationPackagesList = Helpers.DataTableHelper.DataTableToList<ReservationPackageModel>(transaction);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return ReservationPackagesList;
        }

        public DataTable GetReservationByReservationID(int ReservationID)
        {
            DataTable dataTable = new DataTable();
            try
            {
                dataTable = SQLHelpers.Instance.ExecuteDataset($"SELECT * FROM tbReservationDetails where ReservationDetailID={ReservationID}");
                
            }
            catch (Exception ex)
            {
                //
            }

            return dataTable;
        }
    }

}