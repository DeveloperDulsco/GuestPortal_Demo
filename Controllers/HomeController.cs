using CheckinPortal.BusinessLayer;
using CheckinPortal.DataAccess;
using CheckinPortal.Models;
using CheckinPortal.Models.AdaptorAPIModels;
using CheckinPortal.Models.PaymentDetails;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using static QRCoder.PayloadGenerator;

namespace CheckinPortal.Controllers
{
    public class HomeController : Controller
    {
        ReservationLogics reservationLogics = new ReservationLogics();
        public async Task<ActionResult> Index(string id)
        {
            var test = Url.Encode(Helpers.EncryptionHelper.EncryptString("4910392"));
            if (string.IsNullOrEmpty(id))
            {
                return View("ReservationNotFound");
            }

            string confirmationNo = Helpers.EncryptionHelper.DecryptString(id.ToString());

            if (string.IsNullOrEmpty(confirmationNo))
            {
                Helpers.LogHelper.Instance.Warn($"Unable to decrypt the confirmation no {id.ToString()}", "0", "Index", "Pre-Checkin");
                return View("ReservationNotFound");
            }

            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;

            Models.ReservationModel reservationModel = new Models.ReservationModel();
            reservationModel.IsDepositAvailable = false;

            MastersLogics mastersLogics = new MastersLogics();

            var CountryList = mastersLogics.GetCountryList();

            var reservationsDt = reservationLogics.GetReservationDetailsDT(confirmationNo);

            var reservations = Helpers.DataTableHelper.DataTableToList<usp_GetReservationDetails_Result>(reservationsDt);

            if (reservations != null && reservations.Count > 0)
            {
                bool isredirectfromPaymentPage = false;
                bool IsPaymentSuccess = false;
                string PaymentFailureMessage = "";

                if (TempData["IsredirectedfromPaymentPage"] != null)
                {
                    isredirectfromPaymentPage = Convert.ToBoolean(TempData["IsredirectedfromPaymentPage"].ToString());
                    IsPaymentSuccess = Convert.ToBoolean(TempData["IsPaymentSuccess"].ToString());
                    PaymentFailureMessage = TempData["PaymentFailureMessage"].ToString();
                }

                ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                ViewBag.IsPaymentSuccess = IsPaymentSuccess;
                ViewBag.PaymentFailureMessage = PaymentFailureMessage;




                if (reservations[0].IsPreCheckedInPMS.HasValue && !reservations[0].IsPreCheckedInPMS.Value || isredirectfromPaymentPage)
                {
                    ViewBag.ReservationFound = true;


                    #region Get the existing selected package and Upsells

                    var ReservationPackagesList = reservationLogics.GetReservationPackages(reservations[0].ReservationDetailID);

                    if (ReservationPackagesList != null && ReservationPackagesList.Count > 0)
                    {
                        Helpers.LogHelper.Instance.Log($"Existing reservation packages found.", $"{confirmationNo}", "Index", "Pre-Checkin");

                        var RoomUpsellPackages = ReservationPackagesList.Where(x => x.IsRoomUpsell).Select(x => x.PackageID).ToArray();
                        var SpecialsPackages = ReservationPackagesList.Where(x => !x.IsRoomUpsell).Select(x => x.PackageID).ToArray();

                        ViewBag.RoomUpsellPackages = string.Join(",", RoomUpsellPackages);
                        ViewBag.SpecialPackages = string.Join(",", SpecialsPackages);
                        ViewBag.ReservationPackagesList = ReservationPackagesList;
                    }
                    else
                    {
                        Helpers.LogHelper.Instance.Log($"Existing reservation packages not found.", $"{confirmationNo}", "Index", "Pre-Checkin");
                        ViewBag.RoomUpsellPackages = string.Empty;
                        ViewBag.SpecialPackages = string.Empty;
                        ViewBag.ReservationPackagesList = new List<ReservationPackageModel>();
                    } 
                    #endregion


                    if ((reservations[0].EcomPaymentStatus != null && reservations[0].EcomPaymentStatus.Value) || isredirectfromPaymentPage)
                    {
                        Helpers.LogHelper.Instance.Log($"Reservation #{confirmationNo} payment already done.", $"{confirmationNo}", "Index", "Pre-Checkin");
                        ViewBag.PaymentProcessed = true;
                        ViewBag.IsPaymentSuccess = true;
                    }

                    var Questions = reservationLogics.GetQuestions();
                    var PackageLists = reservationLogics.GetPackages(reservations[0].RoomType);
                    ViewBag.PackageList = PackageLists;
                    ViewBag.Questions = Questions;

                    //push events to DB
                    reservationLogics.InsertEvent(reservations[0].ReservationDetailID, "Email Link Click");

                    Helpers.LogHelper.Instance.Log($"Getting prfile details", $"{confirmationNo}", "Index", "Pre-Checkin");
                    var ProfileList = await reservationLogics.GetReservationProfileList(reservations[0].ReservationDetailID);

                    ViewBag.Profiles = ProfileList;
                    ViewBag.CountryList = new SelectList(CountryList, "CountryMasterID", "Country_Full_name", ProfileList[0].CountryMasterID);

                    if (ProfileList[0].CountryMasterID != null)
                    {
                        var StateList = mastersLogics.GetStateListByCountryID(ProfileList[0].CountryMasterID.Value);
                        ViewBag.StateList = new SelectList(StateList, "StateMasterID", "Statename", ProfileList[0].StateMasterID);
                    }
                    else
                    {
                        List<Models.StateMaster> tbstateMasters = new List<Models.StateMaster>();
                        tbstateMasters.Add(new Models.StateMaster()
                        {
                            Statename = "Please select country",
                            StateMasterID = -1

                        });
                        ViewBag.StateList = new SelectList(tbstateMasters, "StateMasterID", "Statename", "Select State");
                    }

                    List<Models.Profile> profiles = new List<Models.Profile>();

                    foreach (var profile in ProfileList)
                    {
                        profiles.Add(new Models.Profile()
                        {
                            AddressLine1 = profile.AddressLine1,
                            AddressLine2 = profile.AddressLine2,
                            City = profile.City,
                            CountryID = profile.CountryMasterID,
                            Email = profile.Email,
                            FirstName = profile.FirstName,
                            LastName = profile.LastName,
                            Phone = profile.Phone,
                            PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                            StateID = profile.StateMasterID,
                            ProfileDetailID = profile.ProfileDetailID,
                            MiddleName = profile.MiddleName,

                        });
                    }

                    List<string> AuthoRuleTwoReservationTypes = new List<string>();
                    AuthoRuleTwoReservationTypes.Add("DP");
                    AuthoRuleTwoReservationTypes.Add("TA");
                    AuthoRuleTwoReservationTypes.Add("VO");
                    AuthoRuleTwoReservationTypes.Add("GG");
                    decimal TotalRoomRate = 0;
                    
                    if (AuthoRuleTwoReservationTypes.Any(x => x.Contains(reservations[0].ReservationSource)))
                    {
                        Helpers.LogHelper.Instance.Log($"Make room rate 0 for reservation type {reservations[0].ReservationSource}", $"{confirmationNo}", "Index", "Pre-Checkin");
                        TotalRoomRate = 0;
                    }
                    else
                    {
                        TotalRoomRate = reservations[0].TotalAmount.HasValue ? reservations[0].TotalAmount.Value : 0;
                    }

                    string ExpectedTimeofArrival = "";

                    if (reservations[0].ETA.HasValue && !(reservations[0].ETA.Value.ToString("HH:mm") == "00:00"))
                    {
                        Helpers.LogHelper.Instance.Log($"Estimate time of arrival already exist {reservations[0].ETA.Value.ToString("HH:mm")} ", $"{confirmationNo}", "Index", "Pre-Checkin");
                        DateTime timeUtc = reservations[0].ETA.Value;
                        ExpectedTimeofArrival = Helpers.DateTimeHelper.ConvertFromUTC(timeUtc);
                    }
                    
                    reservationModel = new Models.ReservationModel()
                    {
                        ReservationNameID = reservations[0].ReservationNameID,
                        ReservationID = reservations[0].ReservationDetailID,
                        AdultCount = reservations[0].Adultcount.HasValue ? reservations[0].Adultcount.Value : 0,
                        ArrivalDate = reservations[0].ArrivalDate.HasValue ? reservations[0].ArrivalDate.Value.ToString("dd MMM yyyy") : "",
                        AverageRoomRate = reservations[0].AverageRoomRate.HasValue ? reservations[0].AverageRoomRate.Value : 0,
                        ChildCount = reservations[0].Childcount.HasValue ? reservations[0].Childcount.Value : 0,
                        DepartureDate = reservations[0].DepartureDate.HasValue ? reservations[0].DepartureDate.Value.ToString("dd MMM yyyy") : "",
                        ExpectedTimeofArrival = ExpectedTimeofArrival,
                        FlightNo = reservations[0].FlightNo,
                        IsMembershipRequested = reservations[0].IsMemberShipEnrolled.HasValue ? reservations[0].IsMemberShipEnrolled.Value : false,
                        IsTermsAndConditions = false,
                        MembershipNo = reservations[0].MembershipNo,
                        ReservationNumber = reservations[0].ReservationNumber,
                        Profiles = profiles,
                        Questions = Questions,
                        RoomType = reservations[0].RoomType,
                        RoomTypeDescription = reservations[0].RoomTypeDescription,
                        TotalRoomRate = TotalRoomRate,
                        IsDepositAvailable = reservations[0].IsDepositAvailable != null ? reservations[0].IsDepositAvailable.Value : false,
                        IsBreakFastAvailable = reservations[0].IsBreakFastAvailable != null ? reservations[0].IsBreakFastAvailable.Value : false,
                    };

                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(confirmationNo, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(20);

                    System.IO.MemoryStream ms = new MemoryStream();
                    qrCodeImage.Save(ms, ImageFormat.Jpeg);
                    byte[] byteImage = ms.ToArray();
                    var QRCodeBase64 = Convert.ToBase64String(byteImage);

                    ViewBag.QRcode = QRCodeBase64;

                    ViewBag.AdaptorAPIBaseURL= ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                    ViewBag.OCRAPIBaseURL = ConfigurationManager.AppSettings["OCRAPIBaseURL"].ToString();
                    ViewBag.CalQRcode = "";
                    Helpers.LogHelper.Instance.Log($"Reservation  {confirmationNo} successfully returned", $"{confirmationNo}", "Index", "Pre-Checkin");
                    return View(reservationModel);

                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Reservation {confirmationNo} already completed the pre-checkin, return reservation not found page", $"{confirmationNo}", "Index", "Pre-Checkin");
                    // Reservation not found page
                    return View("ReservationNotFound");
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Warn($"Reservation not found for given confirmation no {confirmationNo}", $"{confirmationNo}", "Index", "Pre-Checkin");
                // Reservation not found page
                return View("ReservationNotFound");
            }
        }
        public async Task<ActionResult> IndexPayment(string id)
        {
           // var test = Helpers.EncryptionHelper.EncryptString("41443868");
            if (string.IsNullOrEmpty(id))
            {
                return View("ReservationNotFound");
            }

            string confirmationNo = id;


            if (string.IsNullOrEmpty(confirmationNo))
            {
                return View("ReservationNotFound");
            }

            ViewBag.ReservationFound = false;
            ViewBag.PaymentProcessed = false;

            Models.ReservationModel reservationModel = new Models.ReservationModel();
            BusinessLayer.MastersLogics mastersLogics = new BusinessLayer.MastersLogics();
            var CountryList = mastersLogics.GetCountryList();
            var reservationsDt = reservationLogics.GetReservationDetailsDT(confirmationNo);
            var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);
            //var reservations = await reservationLogics.GetReservationDetails("123456");

            if (reservations != null && reservations.Count > 0)
            {
                bool isredirectfromPaymentPage = false;
                bool IsPaymentSuccess = false;
                string PaymentFailureMessage = "";
                
                if (TempData["IsredirectedfromPaymentPage"] != null)
                {
                    isredirectfromPaymentPage = Convert.ToBoolean(TempData["IsredirectedfromPaymentPage"].ToString());
                    IsPaymentSuccess = Convert.ToBoolean(TempData["IsPaymentSuccess"].ToString());
                    PaymentFailureMessage = TempData["PaymentFailureMessage"].ToString();
                }

                ViewBag.IsredirectedfromPaymentPage = isredirectfromPaymentPage;
                ViewBag.IsPaymentSuccess = reservations[0].EcomPaymentStatus;// IsPaymentSuccess;
                ViewBag.PaymentFailureMessage = PaymentFailureMessage;




                if (reservations[0].IsPreCheckedInPMS.HasValue && !reservations[0].IsPreCheckedInPMS.Value || isredirectfromPaymentPage)
                {

                    ViewBag.ReservationFound = true;

                    #region Get Existing Packages and Upsell

                    var ReservationPackagesList = reservationLogics.GetReservationPackages(reservations[0].ReservationDetailID);

                    if (ReservationPackagesList != null && ReservationPackagesList.Count > 0)
                    {
                        var RoomUpsellPackages = ReservationPackagesList.Where(x => x.IsRoomUpsell).Select(x => x.PackageID).ToArray();
                        var SpecialsPackages = ReservationPackagesList.Where(x => !x.IsRoomUpsell).Select(x => x.PackageID).ToArray();

                        ViewBag.RoomUpsellPackages = string.Join(",", RoomUpsellPackages);
                        ViewBag.SpecialPackages = string.Join(",", SpecialsPackages);
                        ViewBag.ReservationPackagesList = ReservationPackagesList;
                    }
                    else
                    {
                        ViewBag.RoomUpsellPackages = string.Empty;
                        ViewBag.SpecialPackages = string.Empty;
                        ViewBag.ReservationPackagesList = new List<ReservationPackageModel>();
                    } 
                    #endregion

                    if ((reservations[0].EcomPaymentStatus != null && reservations[0].EcomPaymentStatus.Value) || isredirectfromPaymentPage)
                    {
                        ViewBag.PaymentProcessed = true;
                    }

                    var Questions = reservationLogics.GetQuestions();
                    var PackageLists = reservationLogics.GetPackages(reservations[0].RoomType);
                    ViewBag.PackageList = PackageLists;
                    ViewBag.Questions = Questions;

                   
                    var ProfileList = await reservationLogics.GetReservationProfileList(reservations[0].ReservationDetailID);

                    ViewBag.Profiles = ProfileList;
                    ViewBag.CountryList = new SelectList(CountryList, "CountryMasterID", "Country_Full_name", ProfileList[0].CountryMasterID);

                    if (ProfileList[0].CountryMasterID != null)
                    {
                        var StateList = mastersLogics.GetStateListByCountryID(ProfileList[0].CountryMasterID.Value);
                        ViewBag.StateList = new SelectList(StateList, "StateMasterID", "Statename", ProfileList[0].StateMasterID);
                    }
                    else
                    {
                        List<Models.StateMaster> tbstateMasters = new List<Models.StateMaster>();
                        tbstateMasters.Add(new Models.StateMaster()
                        {
                            Statename = "Please select country",
                            StateMasterID = -1

                        });
                        ViewBag.StateList = new SelectList(tbstateMasters, "StateMasterID", "Statename", "Select State");
                    }

                    List<Models.Profile> profiles = new List<Models.Profile>();

                    foreach (var profile in ProfileList)
                    {
                        profiles.Add(new Models.Profile()
                        {
                            AddressLine1 = profile.AddressLine1,
                            AddressLine2 = profile.AddressLine2,
                            City = profile.City,
                            CountryID = profile.CountryMasterID,
                            Email = profile.Email,
                            FirstName = profile.FirstName,
                            LastName = profile.LastName,
                            Phone = profile.Phone,
                            PostalCode = profile.PostalCode != null ? profile.PostalCode : "",
                            StateID = profile.StateMasterID,
                            ProfileDetailID = profile.ProfileDetailID,
                            MiddleName = profile.MiddleName,

                        });
                    }

                    reservationModel = new Models.ReservationModel()
                    {
                        ReservationNameID = reservations[0].ReservationNameID,
                        ReservationID = reservations[0].ReservationDetailID,
                        AdultCount = reservations[0].Adultcount.HasValue ? reservations[0].Adultcount.Value : 0,
                        ArrivalDate = reservations[0].ArrivalDate.HasValue ? reservations[0].ArrivalDate.Value.ToString("dd MMM yyyy") : "",
                        AverageRoomRate = reservations[0].AverageRoomRate.HasValue ? reservations[0].AverageRoomRate.Value : 0,
                        ChildCount = reservations[0].Childcount.HasValue ? reservations[0].Childcount.Value : 0,
                        DepartureDate = reservations[0].DepartureDate.HasValue ? reservations[0].DepartureDate.Value.ToString("dd MMM yyyy") : "",
                        ExpectedTimeofArrival = reservations[0].ETA.HasValue ? reservations[0].ETA.Value.ToString("HH:mm") : "",
                        FlightNo = reservations[0].FlightNo,
                        // IsMembershipRequested = reservations[0].ISMembershipRequested.HasValue ? reservations[0].ISMembershipRequested.Value : false,
                        IsTermsAndConditions = false,
                        MembershipNo = reservations[0].MembershipNo,
                        ReservationNumber = reservations[0].ReservationNumber,
                        Profiles = profiles,
                        Questions = Questions,
                        RoomType = reservations[0].RoomType,
                        RoomTypeDescription = reservations[0].RoomTypeDescription,
                        TotalRoomRate = reservations[0].TotalAmount.HasValue ? reservations[0].TotalAmount.Value : 0,
                        IsBreakFastAvailable = reservations[0].IsBreakFastAvailable != null ? reservations[0].IsBreakFastAvailable.Value : false
                    };
                    ViewBag.AdaptorAPIBaseURL = ConfigurationManager.AppSettings["AdaptorAPIBaseURL"].ToString();
                    ViewBag.OCRAPIBaseURL = ConfigurationManager.AppSettings["OCRAPIBaseURL"].ToString();

                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(confirmationNo, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(20);

                    System.IO.MemoryStream ms = new MemoryStream();
                    qrCodeImage.Save(ms, ImageFormat.Jpeg);
                    byte[] byteImage = ms.ToArray();
                    var QRCodeBase64 = Convert.ToBase64String(byteImage);

                    ViewBag.QRcode = QRCodeBase64;

                    CalendarEvent generator = new CalendarEvent("Hotel Checkin", $"Your Confirmation No {confirmationNo}", "51.26118,6.6717", reservations[0].ArrivalDate.Value, reservations[0].DepartureDate.Value, true);
                    string payload = generator.ToString();

                    QRCodeGenerator calqrGenerator = new QRCodeGenerator();
                    QRCodeData calqrCodeData = calqrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
                    QRCode calqrCode = new QRCode(calqrCodeData);
                    var qrCodeAsBitmap = calqrCode.GetGraphic(20);

                    ms = new MemoryStream();
                    qrCodeAsBitmap.Save(ms, ImageFormat.Jpeg);
                    byte[] byteImage1 = ms.ToArray();
                    var CalenderBase64 = Convert.ToBase64String(byteImage1);
                    ViewBag.CalQRcode = CalenderBase64;


                    Helpers.LogHelper.Instance.Log($"Reservation {confirmationNo} already completed the pre-checkin, return reservation not found page", "", "IndexPayment", "Pre-Checkin");

                    return View("Index",reservationModel);

                }
                else
                {
                    Helpers.LogHelper.Instance.Warn($"Reservation {confirmationNo} already completed the pre-checkin, return reservation not found page", "", "IndexPayment", "Pre-Checkin");
                    // Reservation not found page
                    return View("ReservationNotFound");
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Warn($"Reservation not found for given confirmation no {confirmationNo}", "", "IndexPayment", "Pre-Checkin");
                // Reservation not found page
                return View("ReservationNotFound");
            }

        }

        public ActionResult UpdateReservation(Models.ReservationModel  reservationModel)
        {
            int count = 0;
            ReservationLogics reservationLogics = new ReservationLogics();

            //push events to DB
            reservationLogics.InsertEvent(reservationModel.ReservationID, "Guest Details");

            Helpers.LogHelper.Instance.Log($"Updating reservation/guest details,", $"{reservationModel.ReservationNumber}", "UpdateReservation", "Pre-Checkin");

            foreach (var profile in reservationModel.Profiles)
            {               
                var resUpdateModel = new UpdateReservationModel()
                {
                    ReservationID = reservationModel.ReservationID,
                    AddressLine1 = profile.AddressLine1,
                    AddressLine2 = profile.AddressLine2,
                    City = profile.City,
                    CountryMasterID = profile.CountryID,
                    Email = profile.Email,
                    ETA = Helpers.DateTimeHelper.ConvertToUTC(reservationModel.ExpectedTimeofArrival),// reservationModel.ExpectedTimeofArrival,
                    FlightNo = reservationModel.FlightNo,
                    MembershipNo = reservationModel.MembershipNo,
                    Phone = profile.Phone,
                    PostalCode = profile.PostalCode,
                    ProfileDetailID = profile.ProfileDetailID,
                    StateMasterID = profile.StateID,
                    //SignatureImage = Convert.FromBase64String(reservationModel.SignatureBase64),
                    IsMemberShipEnrolled = reservationModel.IsMembershipRequested,
                };
                
                Helpers.LogHelper.Instance.Debug($"Profile info Json : {Newtonsoft.Json.JsonConvert.SerializeObject(resUpdateModel)}", $"{reservationModel.ReservationNumber}", "UpdateReservation", "Pre-Checkin");

                reservationLogics.UpdateReservationByStage("Guest Details", resUpdateModel);
            }
            return Json(new { result = count > 0 });
        }

        public ActionResult UpdatePolicies(PoliciesModel policiesModel)
        {
            int count = 0;
            //push events to DB
            reservationLogics.InsertEvent(policiesModel.ReservationID, "Registration Card");

            Helpers.LogHelper.Instance.Log($"Updating Signature,", $"{policiesModel.ReservationID}", "UpdatePolicies", "Pre-Checkin");
            
            reservationLogics.UpdateReservationByStage("Policies", new UpdateReservationModel()
            {
                SignatureImage = Convert.FromBase64String(policiesModel.Base64Signature),
                ReservationID = policiesModel.ReservationID
            });

            return Json(new { result = count > 0 });
        }

        public ActionResult savePackages(int ReservationID, string Packages)
        {
            int count = 1;

            var packageList = Packages.Split(',');

            ReservationLogics reservationLogics = new ReservationLogics();

            Helpers.LogHelper.Instance.Log($"Updating Specials and Upsell packages,", $"{ReservationID}", "savePackages", "Pre-Checkin");

            Helpers.LogHelper.Instance.Debug($"Specials and Pacage Json : {Newtonsoft.Json.JsonConvert.SerializeObject(packageList)}", $"{ReservationID}", "savePackages", "Pre-Checkin");

            if (packageList.Count() > 0)
            {
                reservationLogics.InsertReservationPackageDetails(ReservationID, packageList);
            }
            

            return Json(new { result = count > 0 });
        }

        public ActionResult UploadDocument(UploadGuestDocumentModel uploadGuestDocumentModel)
        {
            var files = Request.Files;

            int count = 0;

            //push events to DB
            reservationLogics.InsertEvent(uploadGuestDocumentModel.ReservationID, "DocumentUploadTry");
            Helpers.LogHelper.Instance.Log($"Uploading guest document,", $"{uploadGuestDocumentModel.ReservationID}", "UploadDocument", "Pre-Checkin");


            var documentModel = new UpdateReservationModel();

            if (uploadGuestDocumentModel.documentInformation != null)
            {
                var docInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<DocumentInformation>(uploadGuestDocumentModel.documentInformation);
                if (docInfo != null)
                {
                    DateTime expiryDate = new DateTime(1900, 01, 01);
                    DateTime issueDate = new DateTime(1900, 01, 01);
                    DateTime birthDate = new DateTime(1900, 01, 01);

                    if (!DateTime.TryParseExact(docInfo.issueDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out issueDate))
                    {
                        issueDate = new DateTime(1900, 01, 01);
                    }
                    if (!DateTime.TryParseExact(docInfo.expiryDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out expiryDate))
                    {
                        expiryDate = new DateTime(1900, 01, 01);
                    }
                    if (!DateTime.TryParseExact(docInfo.birthDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out birthDate))
                    {
                        birthDate = new DateTime(1900, 01, 01);
                    }

                    documentModel.Gender = docInfo.gender;
                    documentModel.IssueCountry = docInfo.issueCountry;
                    documentModel.ExpiryDate = expiryDate;
                    documentModel.IssueDate = issueDate;
                    documentModel.DocumentType = docInfo.documentType;
                    documentModel.DocumentNumber = docInfo.documentNumber;
                    documentModel.BirthDate = birthDate;
                    documentModel.Nationality = docInfo.nationality;

                    documentModel.FirstName = docInfo.firstName;
                    documentModel.MiddleName = docInfo.middleName;
                    documentModel.LastName = docInfo.lastName;

                    if (!string.IsNullOrEmpty(docInfo.faceImage))
                    {
                        documentModel.FaceImage = Convert.FromBase64String(docInfo.faceImage);
                    }

                }
            }

            documentModel.ReservationID = uploadGuestDocumentModel.ReservationID;
            documentModel.ProfileDetailID = uploadGuestDocumentModel.ProfileDetailID;


            //documentModel.DocumentImage1 = Request.Files[0].InputStream


            if (!string.IsNullOrEmpty(uploadGuestDocumentModel.Doc1Base64))
            {
                documentModel.DocumentImage1 = Convert.FromBase64String(uploadGuestDocumentModel.Doc1Base64);
            }

            if (!string.IsNullOrEmpty(uploadGuestDocumentModel.Doc2Base64))
            {
                documentModel.DocumentImage2 = Convert.FromBase64String(uploadGuestDocumentModel.Doc2Base64);
            }

            Helpers.LogHelper.Instance.Debug($"Uploading Document Json : {Newtonsoft.Json.JsonConvert.SerializeObject(documentModel)}", $"{uploadGuestDocumentModel.ReservationID}", "UploadDocument", "Pre-Checkin");

            reservationLogics.UpdateReservationByStage("Upload", documentModel);
            reservationLogics.UpdateReservationByStage("Finish", new UpdateReservationModel()
            {
                ReservationID = uploadGuestDocumentModel.ReservationID
            });
            return Json(new { result = count > 0 });
        }

        [HttpPost]
        public async Task<ActionResult> SaveDeclaration(Answers answers,int ReservationID)
        {
            int count = 0;


            var Questions = reservationLogics.GetQuestions();


            Helpers.LogHelper.Instance.Log($"Updating Disclaimer", $"{ReservationID}", "SaveDeclaration", "Pre-Checkin");

            foreach (var question in Questions)
            {
                if (Request.Form["Answer[" + question.QuestionID + "]"] != null)
                {
                    var questionAns = Request.Form["Answer[" + question.QuestionID + "]"].ToString();
                    if (!string.IsNullOrEmpty(questionAns))
                    {
                        reservationLogics.InsertFeedback(ReservationID, question.QuestionID, questionAns);
                    }

                }
            }
            //push events to DB
            reservationLogics.InsertEvent(ReservationID, "Disclaimer");
            return Json(new { result = true });
        }
        
        public ActionResult CompletePreCheckin(int ReservationID)
        {

            Helpers.LogHelper.Instance.Log($"PreCheckin Completed", $"{ReservationID}", "CompletePreCheckin", "Pre-Checkin");

            reservationLogics.UpdateReservationByStage("Finish", new UpdateReservationModel()
            {
                ReservationID = ReservationID
            });
            var redirectURL = ConfigurationManager.AppSettings["PreCheckinCompleteRedirectURL"].ToString();
            return Redirect(redirectURL);
        }

        public ActionResult InsertEvent(string EventName,int reservationid)
        {
            reservationLogics.InsertEvent(reservationid, EventName);
            return Json(new { result = true });
        }

        //This is the function where adyen response will be send back
        public async Task<ActionResult> PaymentResponseFromGateway(string ConfirmationNo,string TransactionID)
        {
            Helpers.LogHelper.Instance.Log($"Payment response from PG after 3ds.", $"", "PaymentResponseFromGateway", "Pre-Checkin");

            PaymentLogics paymentLogics = new PaymentLogics();

            var paymentHistrory = paymentLogics.GetPaymentHistory(ConfirmationNo);

            //payment Detail call
            var details = new Dictionary<string, string>();
            details.Add("MD", Request.Params["MD"]);
            details.Add("PaRes", Request.Params["PaRes"]);

            MakePaymentDetailRequestModel paymentDetailsModel = new MakePaymentDetailRequestModel()
            {
                details = details,
                 paymentData = paymentHistrory.Rows.Count > 0 ? paymentHistrory.Rows[0]["PData"].ToString() : "",// "Ab02b4c0!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"
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
                //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //httpClient.DefaultRequestHeaders.Add("x-api-key", "AQE1hmfuXNWTK0Qc+iSDk2UuvsaOW4JDCIBZa3xF0n2mjVZdiutiFFJB8m+HZPXmoKVywMgI/xQQwV1bDb7kfNy1WIxIIkxgBw==-rODJO2F2/g0t6SNBtX135za8qsAPMapU1bIGmWrLDP8=-:=GN%5%nV5Tpj*=W");

                HttpContent requestContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                Helpers.LogHelper.Instance.Log($"Getting Payment Details from PG", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                HttpResponseMessage response = await httpClient.PostAsync($"GetPaymentDetails", requestContent);

                if (response.IsSuccessStatusCode)
                {

                    string Test = await response.Content.ReadAsStringAsync();

                    Helpers.LogHelper.Instance.Debug($"Got Payment Response from PG : {Test}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                    var resposneObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AdaptorAPIModels.MakePaymentResponseModel>(Test);

                    if (resposneObj != null && resposneObj.Result)
                    {
                        Helpers.LogHelper.Instance.Log($"Updating payment details to DB", $"", "PaymentResponseFromGateway", "Pre-Checkin");

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
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}, Updating payment status to BD", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                            //Make payment sucess flag.
                            await paymentLogics.UpdateReservationPaymentStatus(ConfirmationNo);

                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = true;
                            TempData["PaymentFailureMessage"] = "";
                        }
                        else
                        {
                            Helpers.LogHelper.Instance.Log($"Payment {resposneObj.ResponseObject.ResultCode}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                            //log error
                            TempData["IsredirectedfromPaymentPage"] = true;
                            TempData["IsPaymentSuccess"] = false;
                            TempData["PaymentFailureMessage"] = resposneObj.ResponseObject.RefusalReason;

                        }
                        //return Json(new { result = true, content = Test });
                    }
                    else
                    {
                        string FailureMessage = string.Empty;
                        if(resposneObj != null)
                        {
                            FailureMessage = resposneObj.ResponseMessage;
                        }

                        Helpers.LogHelper.Instance.Log($"Payment failed {FailureMessage}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                        TempData["IsredirectedfromPaymentPage"] = true;
                        TempData["IsPaymentSuccess"] = false;
                        TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                    }
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Payment failed {response.IsSuccessStatusCode}", $"", "PaymentResponseFromGateway", "Pre-Checkin");

                    TempData["IsredirectedfromPaymentPage"] = true;
                    TempData["IsPaymentSuccess"] = false;
                    TempData["PaymentFailureMessage"] = "Unable to complete the payment, Please try again";
                }
            }

            //redirect back to registraton document processing tab
            string encConfirmationNo = Helpers.EncryptionHelper.EncryptString(ConfirmationNo);

            
            return RedirectToAction("IndexPayment", new { id = ConfirmationNo });
            
        }

        public async Task<ActionResult> InsertPaymentResponseHeader(PaymentResponse paymentResponse, string ConfirmationNo,string ReservationNameID,string TransactionID,string TransactionType)
        {
            PaymentLogics paymentLogics = new PaymentLogics();
            Helpers.LogHelper.Instance.Log($"Updating payment details non 3ds", $"", "InsertPaymentResponseHeader", "Pre-Checkin");
            if (paymentResponse != null)
            {
                Helpers.LogHelper.Instance.Log($"Updating payment response {Newtonsoft.Json.JsonConvert.SerializeObject(paymentResponse)}", $"", "InsertPaymentResponseHeader", "Pre-Checkin");
            }
            await reservationLogics.InsertPaymentData(paymentResponse, ConfirmationNo, ReservationNameID, TransactionID, TransactionType);

            #region  Precheckin Completed
            var reservationsDt = reservationLogics.GetReservationDetailsDT(ConfirmationNo);
            var reservations = Helpers.DataTableHelper.DataTableToList<DataAccess.usp_GetReservationDetails_Result>(reservationsDt);

            //reservationLogics.UpdateReservationByStage("Finish", new UpdateReservationModel()
            //{
            //    ReservationID = reservations[0].ReservationDetailID
            //});

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
        public ActionResult SaveAdyenPaymentDetails(MakePaymentDetailModel makePaymentDetailModel,string ConfirmationNo ,long TransactionID,string ReservationNameID,string TransactionType)
        {
            Helpers.LogHelper.Instance.Log($"Updating payment PData", $"", "SaveAdyenPaymentDetails", "Pre-Checkin");

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

        public ActionResult CalculatePreAuthAmount(string ReservationNumber,string FundingSource,string PaymentMethod)
        {

            //Get the reservation details for room rate
            var reservationsDt = reservationLogics.GetReservationDetailsDT(ReservationNumber);
            var reservations = Helpers.DataTableHelper.DataTableToList<GetReservationDetailsModel>(reservationsDt);

            Helpers.LogHelper.Instance.Log($"Calculating Pre-Auth Amount", $"", "CalculatePreAuthAmount", "Pre-Checkin");

            if (reservations != null && reservations.Count > 0)
            {
                var reservation = reservations.FirstOrDefault();
                var ReservationPackages = reservationLogics.GetReservationPackages(reservation.ReservationDetailID);
                if(ReservationPackages != null && ReservationPackages.Count > 0)
                {
                    ReservationPackages = ReservationPackages.Where(x => !x.IsRoomUpsell).ToList();
                }
                

                decimal packageAmount = 0;
                if(ReservationPackages != null)
                {
                    foreach (var item in ReservationPackages)
                    {
                        if (item.PackageAmount != null)
                        {
                            packageAmount += Convert.ToDecimal(item.PackageAmount.ToString());
                        }
                    }
                }

                decimal PreuthAmount = 0;
                
                decimal roomrate = reservation.TotalAmount != null ? reservation.TotalAmount.Value : 0;
                int incidentalCap = -1;
                decimal IncidentialCharge = 100;
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["IncidentalCapValue"].ToString()))
                {
                    incidentalCap = Int32.Parse(ConfigurationManager.AppSettings["IncidentalCapValue"].ToString());
                }
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["IncidentalCharge"].ToString()))
                {
                    IncidentialCharge = Int32.Parse(ConfigurationManager.AppSettings["IncidentalCharge"].ToString());
                }
                int NoofNights = (reservation.DepartureDate.Value - reservation.ArrivalDate.Value).Days;
                
                if(NoofNights == 0)
                {
                    NoofNights = 1;
                }


                List<string> AuthoRuleOneReservationTypes = new List<string>();
                AuthoRuleOneReservationTypes.Add("CC");
                AuthoRuleOneReservationTypes.Add("DR");
                AuthoRuleOneReservationTypes.Add("CO");
                AuthoRuleOneReservationTypes.Add("NG");



                List<string> AuthoRuleTwoReservationTypes = new List<string>();
                AuthoRuleTwoReservationTypes.Add("DP");
                AuthoRuleTwoReservationTypes.Add("TA");
                AuthoRuleTwoReservationTypes.Add("GG");
                

                string ReservationType = reservation.ReservationSource != null ? reservation.ReservationSource : "";


                if (AuthoRuleOneReservationTypes.Any(x => x.Contains(ReservationType)))
                {
                    //Rule 1

                    if (FundingSource == "CREDIT")
                    {
                        if (!string.IsNullOrEmpty(PaymentMethod) && (PaymentMethod.ToUpper().Contains("JCB") || PaymentMethod.ToUpper().Contains("CUP")))
                        {
                            PreuthAmount = roomrate;// No incidental charge for JCB and CUP
                            IncidentialCharge = 0;
                        }
                        else
                        {

                            if (incidentalCap != -1)
                            {
                                if ((NoofNights * IncidentialCharge) > incidentalCap)
                                    PreuthAmount = roomrate + incidentalCap;
                                else
                                    PreuthAmount = roomrate + (NoofNights * IncidentialCharge);
                            }
                            else
                                PreuthAmount = roomrate + (NoofNights * IncidentialCharge);

                        }
                    }
                    else
                    {
                        PreuthAmount = roomrate; // No incidental charge for DEBIT card
                        IncidentialCharge = 0;
                    }

                }
                else if (AuthoRuleTwoReservationTypes.Any(x => x.Contains(reservation.ReservationSource)))
                {
                    //Rule 2

                    if (FundingSource == "CREDIT")
                    {
                        if (!string.IsNullOrEmpty(PaymentMethod) && (PaymentMethod.ToUpper() == "JCB" || PaymentMethod.ToUpper() == "CUP"))
                        {
                            PreuthAmount = 0;// No incidental charge for JCB and CUP
                            IncidentialCharge = 0;
                            roomrate = 0;
                        }
                        else
                        {
                            roomrate = 0;
                            if (incidentalCap != -1)
                            {
                                if ((NoofNights * IncidentialCharge) > incidentalCap)
                                    PreuthAmount = roomrate + incidentalCap;
                                else
                                    PreuthAmount = roomrate + (NoofNights * IncidentialCharge);
                            }
                            else
                                PreuthAmount = roomrate + (NoofNights * IncidentialCharge);

                        }
                    }
                    else
                    {
                        roomrate = 0;
                        PreuthAmount = 0;// No incidental charge for DEBIT card
                        IncidentialCharge = 0;
                    }

                    //roomrate = 0;

                }
                else
                {
                   
                    roomrate = 0;
                    PreuthAmount = 0;//
                    IncidentialCharge = 0;
                }


                if (PreuthAmount > 3500)
                {
                    PreuthAmount = 3500;
                }

                //if any packages selected in previously 
                PreuthAmount += packageAmount;

                Helpers.LogHelper.Instance.Log($"Calculated pre-auth Amount  {PreuthAmount}", $"", "CalculatePreAuthAmount", "Pre-Checkin");

                return Json(new { result = true, PreuthAmount = PreuthAmount, IncidentialCharge = IncidentialCharge, RoomRate = (roomrate + packageAmount), NoofNights = NoofNights });
            }
            return Json(new { result = false, AmountToCharge = 0 });
        }

        public ActionResult ValidateDocumentIssueCountry(string idType , string issueCountry)
        {
            Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} ", $"", "ValidateDocumentIssueCountry", "Pre-Checkin");

            MastersLogics mastersLogics = new MastersLogics();
            var ResponseDataTable = mastersLogics.validateDocumentIssueCountry(idType, issueCountry);
            if (ResponseDataTable != null && ResponseDataTable.Rows.Count > 0)
            {
                if (ResponseDataTable.Rows[0][0].ToString() == "1")
                {
                    Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is valid", $"", "ValidateDocumentIssueCountry", "Pre-Checkin");
                    return Json(new { result = true });
                }
                else
                {
                    Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is not valid", $"", "ValidateDocumentIssueCountry", "Pre-Checkin");
                    return Json(new { result = false });
                }
            }
            else
            {
                Helpers.LogHelper.Instance.Log($"Validating Document Type {idType} and Issue Country {issueCountry} is not valid", $"", "ValidateDocumentIssueCountry", "Pre-Checkin");
                return Json(new { result = false });
            }
        }
    
    }
}