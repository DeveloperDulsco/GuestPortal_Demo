using CheckinPortal.BusinessLayer;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.PaymentDetails;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace CheckinPortal.Controllers
{
    public class CheckoutController : Controller
    {
        ReservationLogics reservationLogics = new ReservationLogics();
        // GET: Checkout
        public async Task<ActionResult> Index(string id)
        {

            var test = Url.Encode(Helpers.EncryptionHelper.EncryptString("321664"));
            if (string.IsNullOrEmpty(id))
            {
                return View("ReservationNotFound");
            }

            string ConfirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());


            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                Helpers.LogHelper.Instance.Log($"Unable to decrypt the confirmation no {id.ToString()}", "", "Checkout/Index", "Pre-Checkout");
                return View("ReservationNotFound");
            }

            Helpers.FileHelpers.DeleteTempFiles();
            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            ViewBag.IsPaymentSuccess = false;
            ViewBag.EcomStatus = false;

            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();

            var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);

            if (reservationsDt != null)
            {

                var reservationList = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);

                var reservation = reservationList.FirstOrDefault();
                bool isPreCheckoutComplete = false;
                if(reservation  != null)
                {
                    var ReservationDt = reservationLogics.GetReservationByReservationID(reservation.ReservationDetailID);
                    if(ReservationDt != null && ReservationDt.Rows.Count > 0 && ReservationDt.Rows[0]["IsPreCheckedOutPMS"] != null)
                    {
                        isPreCheckoutComplete = Convert.ToBoolean(ReservationDt.Rows[0]["IsPreCheckedOutPMS"].ToString());
                        ViewBag.EcomStatus = Convert.ToBoolean(ReservationDt.Rows[0]["EcomPaymentStatus"].ToString());
                    }
                }


                if (reservation != null && reservation.FolioDocument != null && !isPreCheckoutComplete)
                {
                    ViewBag.ReservationFound = true;

                    //save the folio PDF locally and send link to front end

                    string path = Server.MapPath($"~/temp/{reservation.ReservationNumber}.pdf");

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    System.IO.File.WriteAllBytes(path, reservation.FolioDocument);

                    checkoutReservation.ReservationID = reservation.ReservationDetailID;
                    checkoutReservation.ReservationNumber = reservation.ReservationNumber;
                    checkoutReservation.ReservationNameID = reservation.ReservationNameID;
                    
                    checkoutReservation.TotalAmount = reservation.TotalAmount != null ? reservation.TotalAmount.Value : 0;
                    checkoutReservation.BalanecAmount = reservation.BalanceAmount != null ? reservation.BalanceAmount.Value : 0;
                    checkoutReservation.PaidAmount = reservation.PaidAmount != null ? reservation.PaidAmount.Value : 0;
                    
                    var profile = await reservationLogics.GetReservationProfileList(reservation.ReservationDetailID);

                    if (profile != null && profile.Count > 0)
                    {
                        checkoutReservation.FullName = $"{profile[0].LastName}";
                        checkoutReservation.EmailID = profile[0].Email;
                    }
                    else
                    {
                        checkoutReservation.FullName = $"Guest";
                        checkoutReservation.EmailID = "";
                    }

                    var activeTransactions = reservationLogics.GetActivePaymentTransctions(reservation.ReservationNumber).Where(x=>x.IsActive).ToList(); //List only Active pre-auth

                    decimal preAuthAmount = 0;
                    foreach (var preauth in activeTransactions)
                    {
                        if (preauth.IsActive)
                        {
                            preAuthAmount += Convert.ToDecimal(preauth.Amount);
                        }
                    }

                    Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} opening precheckout link", "", "Checkout/Index", "Pre-Checkout");
                    ViewBag.PreauthAmount = preAuthAmount;
                    ViewBag.ActiveTransactions = activeTransactions;                  
                    return View(checkoutReservation);
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} not valid for checkout", "", "Checkout/Index", "Pre-Checkout");
                    return View("ReservationNotFound");
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Unable to find the reservation no {ConfirmationNo}", "", "Checkout/Index", "Pre-Checkout");
                return View("ReservationNotFound");
            }

        }


        public async Task<ActionResult> IndexPayment(string id,string paymentResponse="")
        {

            if (string.IsNullOrEmpty(id))
            {
                return View("ReservationNotFound");
            }

            string ConfirmationNo = id;

            if (string.IsNullOrEmpty(ConfirmationNo))
            {
                return View("ReservationNotFound");
            }

            Helpers.FileHelpers.DeleteTempFiles();
            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;
            bool IsPaymentSuccess = false;
            bool EcomStatus = false;

            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();

            var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);

            if (reservationsDt != null && reservationsDt.Rows.Count > 0)
            {

                bool isredirectfromPaymentPage = true;

                EcomStatus = Convert.ToBoolean(reservationsDt.Rows[0]["EcomPaymentStatus"].ToString());

                ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                ViewBag.IsPaymentSuccess = EcomStatus;
                ViewBag.PaymentFailureMessage = paymentResponse;
                ViewBag.EcomStatus = EcomStatus;


                ViewBag.ReturnFromPayment = true;



                var reservationList = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);

                var reservation = reservationList.FirstOrDefault();

                if (reservation != null)
                {
                    ViewBag.PaymentProcessed = true;




                    ViewBag.ReservationFound = true;

                    //save the folio PDF locally and send link to front end

                    string path = Server.MapPath($"~/temp/{reservation.ReservationNumber}.pdf");

                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }

                    System.IO.File.WriteAllBytes(path, reservation.FolioDocument);

                    checkoutReservation.ReservationID = reservation.ReservationDetailID;
                    checkoutReservation.ReservationNumber = reservation.ReservationNumber;
                    checkoutReservation.ReservationNameID = reservation.ReservationNameID;

                    checkoutReservation.TotalAmount = reservation.TotalAmount != null ? reservation.TotalAmount.Value : 0;
                    checkoutReservation.BalanecAmount = reservation.BalanceAmount != null ? reservation.BalanceAmount.Value : 0;
                    checkoutReservation.PaidAmount = reservation.PaidAmount != null ? reservation.PaidAmount.Value : 0;

                    var profile = await reservationLogics.GetReservationProfileList(reservation.ReservationDetailID);

                    if (profile != null && profile.Count > 0)
                    {
                        checkoutReservation.FullName = $"{profile[0].LastName}";
                        checkoutReservation.EmailID = profile[0].Email;
                    }
                    else
                    {
                        checkoutReservation.FullName = $"Guest";
                        checkoutReservation.EmailID = "";
                    }




                    var activeTransactions = reservationLogics.GetActivePaymentTransctions(reservation.ReservationNumber).Where(x => x.IsActive).ToList(); //List only Active pre-auth

                    decimal preAuthAmount = 0;
                    foreach (var preauth in activeTransactions)
                    {
                        if (preauth.IsActive)
                        {
                            preAuthAmount += Convert.ToDecimal(preauth.Amount);
                        }
                    }




                    ViewBag.PreauthAmount = preAuthAmount;
                    ViewBag.ActiveTransactions = activeTransactions;

                    return View("Index", checkoutReservation);
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Reservation {ConfirmationNo} not valid for checkout or foilo not found", "", "Index", "Pre-Checkout");
                    return View("ReservationNotFound");
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Unable to find the reservation no {ConfirmationNo}", "", "Index", "Pre-Checkout");
                return View("ReservationNotFound");
            }

        }


        public ActionResult CompletePreCheckout(int ReservationID)
        {
            //var reservations = reservationLogics.UpdateCheckoutFlag(ReservationID);
            return Json(new { result = true });
        }

        public ActionResult UpdateCheckoutFlag(int ReservationID)
        {
            Helpers.LogHelper.Instance.Log($"Updating  checkout flag.", "", "Checkout/UpdateSignature", "Pre-Checkout");
            Models.CheckoutReservationModel checkoutReservation = new Models.CheckoutReservationModel();
            //var reservations = reservationLogics.UpdateCheckoutFlag(ReservationID);
            return Json(new { result = true });
        }


        public ActionResult UpdateSignature(PoliciesModel policiesModel)
        {
            int count = 0;
            //push events to DB
            reservationLogics.InsertEvent(policiesModel.ReservationID, "Folio Sign");

            Helpers.LogHelper.Instance.Log($"Updating folio signature", "", "Checkout/UpdateSignature", "Pre-Checkout");

            reservationLogics.UpdateReservationByStage("Policies", new UpdateReservationModel()
            {
                SignatureImage = Convert.FromBase64String(policiesModel.Base64Signature),
                ReservationID = policiesModel.ReservationID
            });
            return Json(new { result = count > 0 });
        }


        //This is the function where adyen response will be send back
        public async Task<ActionResult> PaymentResponseFromGateway(string ConfirmationNo, string TransactionID)
        {

            Helpers.LogHelper.Instance.Log($"Payment response from PG after 3ds.", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");

            PaymentLogics paymentLogics = new PaymentLogics();

            var paymentHistrory = paymentLogics.GetPaymentHistory(ConfirmationNo);

            //payment Detail call
            var details = new Dictionary<string, string>();
            details.Add("MD", Request.Params["MD"]);
            details.Add("PaRes", Request.Params["PaRes"]);

            MakePaymentDetailRequestModel paymentDetailsModel = new MakePaymentDetailRequestModel()
            {
                details = details,
                paymentData = paymentHistrory.Rows.Count > 0 ? paymentHistrory.Rows[0]["PData"].ToString() : "",
            };

            using (var httpClient = new HttpClient())
            {
                string BaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                string APIKey = ConfigurationManager.AppSettings["APIKey"].ToString();
                string MerchantAccount = ConfigurationManager.AppSettings["MerchantAccount"].ToString();
                string CostEstimator_MCC = ConfigurationManager.AppSettings["CostEstimator_MCC"].ToString();

                MakePaymentDetailRequestModelRoot makePaymentDetailRequestModelRoot = new MakePaymentDetailRequestModelRoot()
                {
                    apiKey = APIKey,
                    MerchantAccount = MerchantAccount,
                    RequestObject = paymentDetailsModel
                };

                httpClient.BaseAddress = new Uri(BaseURL);

                string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(makePaymentDetailRequestModelRoot);

                httpClient.DefaultRequestHeaders.Clear();

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                Helpers.LogHelper.Instance.Log($"Getting Payment Details from PG", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");

                HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentDetails", requestContent);

                if (response.IsSuccessStatusCode)
                {
                    string Test = await response.Content.ReadAsStringAsync();

                    Helpers.LogHelper.Instance.Debug($"Got Payment Response from PG : {Test}", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");

                    var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(Test);

                    if (resposneObj != null && resposneObj.Result)
                    {
                        Helpers.LogHelper.Instance.Log($"Updating payment details to DB", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");

                        await reservationLogics.InsertPaymentData(resposneObj.ResponseObject, ConfirmationNo, paymentHistrory.Rows[0]["ReservationNameID"].ToString(), paymentHistrory.Rows[0]["TransactionID"].ToString(), paymentHistrory.Rows[0]["TransactionType"].ToString());

                        paymentLogics.SaveTransactionHistory(new InsertPaymentHistoryUspModel()
                        {
                            ReservationNameID = resposneObj.ResponseObject.MerchantRefernce.Split('-')[1],
                            PData = paymentHistrory.Rows[0]["PData"].ToString(),
                            PaRes = Request.Params["PaRes"],
                            MDData = Request.Params["MD"],
                            PSPReference = resposneObj.ResponseObject.PspReference,
                            RefusalReason = resposneObj.ResponseObject.RefusalReason,
                            ReservationNumber = resposneObj.ResponseObject.MerchantRefernce.Split('-')[0],
                            ResultCode = resposneObj.ResponseObject.ResultCode,
                            TransactionID = TransactionID,
                            TransactionType = paymentHistrory.Rows[0]["TransactionType"].ToString()
                        });

                        if (resposneObj.ResponseObject.ResultCode == "Authorised")
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}, Updating payment status to BD", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");
                            await paymentLogics.UpdateReservationPaymentStatus(ConfirmationNo);

                            var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
                            if (reservationsDt != null && reservationsDt.Rows.Count > 0)
                            {
                                var reservations = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);
                                if (reservationLogics != null)
                                {
                                   // reservationLogics.updateCheckoutFlag(reservations[0].ReservationNameID);
                                }
                            }


                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = true;
                            TempData["PaymentFailureMessage"] = "";
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");
                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = false;
                            TempData["PaymentFailureMessage"] = resposneObj.ResponseObject.RefusalReason;
                        }
                    }
                    else
                    {
                        string FailureMessage = string.Empty;
                        if (resposneObj != null)
                        {
                            FailureMessage = resposneObj.ResponseMessage;
                        }

                        Helpers.LogHelper.Instance.Log($"Payment failed {FailureMessage}", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");
                        TempData["IsredirectedfromPaymentPage"] = true;
                        TempData["IsPaymentSuccess"] = false;
                        TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Payment failed {response.IsSuccessStatusCode}", $"{ConfirmationNo}", "PaymentResponseFromGateway", "Pre-Checkout");
                    TempData["IsredirectedfromPaymentPage"] = true;
                    TempData["IsPaymentSuccess"] = false;
                    TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                }
            }

            string encConfirmationNo = Helpers.EncryptionHelper.EncryptString(ConfirmationNo);

            return RedirectToAction("IndexPayment", new { id = ConfirmationNo, paymentResponse = TempData["PaymentFailureMessage"] });

        }

        public async Task<ActionResult> InsertPaymentResponseHeader(PaymentResponse paymentResponse, string ConfirmationNo, string ReservationNameID, string TransactionID, string TransactionType)
        {
            Helpers.LogHelper.Instance.Log($"Adding payment details to DB.", "", "Checkout/InsertPaymentResponseHeader", "Pre-Checkout");

            PaymentLogics paymentLogics = new PaymentLogics();

            await reservationLogics.InsertPaymentData(paymentResponse, ConfirmationNo, ReservationNameID, TransactionID, TransactionType);

            #region  Precheckin Completed
            

            var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
            var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);

            Helpers.LogHelper.Instance.Log($"Updating precheckout complete flag.", "", "Checkout/InsertPaymentResponseHeader", "Pre-Checkout");

            //reservationLogics.UpdateCheckoutFlag(reservations[0].ReservationDetailID);

            #endregion


            paymentLogics.SaveTransactionHistory(new InsertPaymentHistoryUspModel()
            {
                ReservationNameID = paymentResponse.MerchantRefernce.Split('-')[1],
                // PData = paymentDetailResponseModel.r,
                PaRes = Request.Params["PaRes"],
                MDData = Request.Params["MD"],
                PSPReference = paymentResponse.PspReference,
                RefusalReason = paymentResponse.RefusalReason,
                ReservationNumber = paymentResponse.MerchantRefernce.Split('-')[0],
                ResultCode = paymentResponse.ResultCode,
                TransactionID = TransactionID,
                TransactionType = TransactionType
            });


            return Json(new { result = true });
        }

        [HttpPost]
        public ActionResult SaveAdyenPaymentDetails(MakePaymentDetailModel makePaymentDetailModel, string ConfirmationNo, long TransactionID, string ReservationNameID, string TransactionType)
        {
            Helpers.LogHelper.Instance.Log($"Adding payment details to DB.", "", "Checkout/SaveAdyenPaymentDetails", "Pre-Checkout");

            PaymentLogics paymentLogics = new PaymentLogics();
            paymentLogics.SaveTransactionHistory(new InsertPaymentHistoryUspModel()
            {
                PData = makePaymentDetailModel.paymentData,
                PaRes = "",
                MDData = "",
                PSPReference = "",
                RefusalReason = "",
                ReservationNumber = ConfirmationNo,
                ResultCode = "",
                TransactionID = TransactionID.ToString(),
                ReservationNameID = ReservationNameID,
                TransactionType = TransactionType
            });

            return Json(new { result = true });
        }

        public ActionResult GetActiveTransctions(string ReservationNumber)
        {
            Helpers.LogHelper.Instance.Log($"Getting active transactions.", "", "Checkout/GetActiveTransctions", "Pre-Checkout");
            var reservations = reservationLogics.GetActivePaymentTransctions(ReservationNumber);

            return Json(new { result = true });
        }


        public async Task<ActionResult> processExistingTransction(ProcessExistingTransction model)
        {
            Helpers.LogHelper.Instance.Log($"Processing existing transactions.", "", "Checkout/processExistingTransction", "Pre-Checkout");

            var reservationsDt = reservationLogics.GetReservationDetailsDT(model.ReservationNo);

            var reservations = Helpers.DataTableHelper.DataTableToList<Models.GetReservationDetailsModel>(reservationsDt);

            var activeTransactions = reservationLogics.GetActivePaymentTransctions(model.ReservationNo).Where(x => x.IsActive).ToList(); //List only Active pre-auth
            PaymentLogics paymentLogics = new PaymentLogics();
            decimal preAuthAmount = 0;
            string ParentPSPReference = string.Empty;
            string AdjustAuthorisationData = string.Empty;
            foreach (var preauth in activeTransactions)//for now only one transaction
            {
                if (preauth.IsActive)
                {
                    preAuthAmount += Convert.ToDecimal(preauth.Amount);
                    ParentPSPReference = preauth.ParentPspRefereceNumber;
                    AdjustAuthorisationData = preauth.AdjustAuthorisationData;
                }
            }


            if(reservations[0].BalanceAmount <= preAuthAmount)
            {
                Helpers.LogHelper.Instance.Log($"Balance < Preauth .", "", "Checkout/processExistingTransction", "Pre-Checkout");
                Helpers.LogHelper.Instance.Log($"Capturing preauth.", "", "Checkout/processExistingTransction", "Pre-Checkout");
                //Directly Capture
                var captureResponse = await paymentLogics.CaptureTransaction(new TopupTransctionModels()
                {
                    AmountToCharge = reservations[0].BalanceAmount.Value,
                    PspReferenceNumber = ParentPSPReference
                });

                if (captureResponse != null && captureResponse.Result)
                {
                    Helpers.LogHelper.Instance.Log($"Preauth capture success.", "", "Checkout/processExistingTransction", "Pre-Checkout");

                    string transactionType = Models.TransactionType.PreAuth.ToString();
                    var updatecaptureTransctionToDB = reservationLogics.UpdatePaymentHeaderData(activeTransactions[0].TransactionID.ToString(), transactionType, "Modified", false, transactionType, reservations[0].BalanceAmount.Value);

                    string TransactionID = DateTime.Now.ToString("yyMMddHHss");
                    transactionType = Models.TransactionType.Capture.ToString();

                    PaymentResponse paymentResponse = new PaymentResponse()
                    {
                        additionalInfos = new System.Collections.Generic.List<AdditionalInfo>(),
                        Amount = reservations[0].BalanceAmount.Value,
                        PspReference = captureResponse.ResponseObject.PspReference,
                        ParentPSPReferece = ParentPSPReference,
                        AuthCode = activeTransactions[0].AuthorisationCode,
                        CardExpiryDate = activeTransactions[0].ExpiryDate,
                        CardToken = activeTransactions[0].RecurringIdentifier,
                        CardType = activeTransactions[0].CardType,
                        Currency = activeTransactions[0].Currency,
                        FundingSource = activeTransactions[0].FundingSource,
                        MaskCardNumber = activeTransactions[0].MaskedCardNumber,
                        MerchantRefernce = activeTransactions[0].ReservationNumber + "-" + activeTransactions[0].ReservationNameID,
                        PaymentToken = activeTransactions[0].RecurringIdentifier,
                        RefusalReason = "",
                        ResultCode = "Capture"
                    };

                    var saveTopupTransctionToDB = await reservationLogics.InsertPaymentData(paymentResponse, reservations[0].ReservationNumber, reservations[0].ReservationNameID, TransactionID, transactionType);
                    if (saveTopupTransctionToDB)
                    {
                        //reservationLogics.updateCheckoutFlag(reservations[0].ReservationNameID.ToString());
                        return Json(new { result = true, message = "success" });
                    }
                    else
                    {
                        //return failure
                        Helpers.LogHelper.Instance.Warn($"Unable to save payment details to DB.", "", "Checkout/processExistingTransction", "Pre-Checkout");
                        return Json(new { result = false, message = "Unable to process payment" });
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Unable to capture pre-auth", "", "Checkout/processExistingTransction", "Pre-Checkout");
                    //return failure
                    return Json(new { result = false, message = "Unable to process payment" });
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Balance > Preauth .", "", "Checkout/processExistingTransction", "Pre-Checkout");
                //Topup balance amount
                decimal AmountToTopup =  reservations[0].BalanceAmount.Value;

                var topUpResponse = await paymentLogics.TopupTransaction(new TopupTransctionModels()
                {
                    AmountToCharge = AmountToTopup,
                    PspReferenceNumber = ParentPSPReference,
                    AdjustAuthorisationData = AdjustAuthorisationData
                });

                if(topUpResponse != null && topUpResponse.Result)
                {
                    Helpers.LogHelper.Instance.Log($"Transaction topup successfully for {AmountToTopup}", "", "Checkout/processExistingTransction", "Pre-Checkout");

                    //Save the transaction to DB

                    ReservationLogics reservationLogics = new ReservationLogics();

                    PaymentResponse paymentResponse = new PaymentResponse()
                    {
                        additionalInfos = new System.Collections.Generic.List<AdditionalInfo>(),
                        Amount = Convert.ToDecimal(activeTransactions[0].Amount),
                        PspReference = topUpResponse.ResponseObject.PspReference,
                        ParentPSPReferece = ParentPSPReference,
                        AuthCode = activeTransactions[0].AuthorisationCode,
                        CardExpiryDate = activeTransactions[0].ExpiryDate,
                        CardToken = activeTransactions[0].RecurringIdentifier,
                        CardType = activeTransactions[0].CardType,
                        Currency = activeTransactions[0].Currency,
                        FundingSource = activeTransactions[0].FundingSource,
                        MaskCardNumber = activeTransactions[0].MaskedCardNumber,
                        MerchantRefernce = activeTransactions[0].ReservationNumber + "-" + activeTransactions[0].ReservationNameID,
                        PaymentToken = activeTransactions[0].RecurringIdentifier,
                        RefusalReason = "",
                        ResultCode = "Completed"                
                    };

                    //call update sp insted

                    string transctionType = activeTransactions[0].TransactionType;

                    var updateTransctionToDB = reservationLogics.UpdatePaymentHeaderData(activeTransactions[0].TransactionID.ToString(), "Modified", "Transction modified", false, transctionType, Convert.ToDecimal(activeTransactions[0].Amount));
                   
                    paymentResponse.PspReference = topUpResponse.ResponseObject.PspReference;
                    paymentResponse.Amount = AmountToTopup;
                    paymentResponse.ResultCode = "Modification";

                    string TransactionID = DateTime.Now.ToString("yyMMddHHss");
                    string transactionType = Models.TransactionType.PreAuth.ToString();

                    var saveTopupTransctionToDB = await reservationLogics.InsertPaymentData(paymentResponse, reservations[0].ReservationNumber, reservations[0].ReservationNameID, TransactionID, transactionType);

                    if (saveTopupTransctionToDB)
                    {
                        var captureResponse = await paymentLogics.CaptureTransaction(new TopupTransctionModels()
                        {
                            AmountToCharge = reservations[0].BalanceAmount.Value,
                            PspReferenceNumber = ParentPSPReference
                        });

                        if (captureResponse != null && captureResponse.Result)
                        {
                            Helpers.LogHelper.Instance.Log($"Transaction captured successfully for {reservations[0].BalanceAmount.Value}", "", "Checkout/processExistingTransction", "Pre-Checkout");

                            transctionType = Models.TransactionType.Capture.ToString();
                            var updatecaptureTransctionToDB = reservationLogics.UpdatePaymentHeaderData(TransactionID, transctionType, "Captured", null, transctionType, reservations[0].BalanceAmount.Value);//Pass null for isactive flag to mark as active transaction
                            // Update trasaction to captured
                            //return success

                          //  reservationLogics.updateCheckoutFlag(reservations[0].ReservationNameID.ToString());
                            return Json(new { result = true, message = "success" });
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Warn($"Unable to captured transaction", "", "Checkout/processExistingTransction", "Pre-Checkout");

                            //return failure
                            return Json(new { result = false, message = "Unable to process payment" });
                        }
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Warn($"Unable to save topup details to DB", "", "Checkout/processExistingTransction", "Pre-Checkout");

                        //return failure
                        return Json(new { result = false, message = "Unable to process payment" });
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Unable to topup transacation", "", "Checkout/processExistingTransction", "Pre-Checkout");

                    //return failure
                    return Json(new { result = false, message = "Unable to process payment" });
                }

            }

        }

    }
}