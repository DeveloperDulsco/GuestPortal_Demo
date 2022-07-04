using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CheckinPortal.Models
{
    public class StateMaster
    {
        public int StateMasterID { get; set; }
        public string Statename { get; set; }
        public int? CountryMasterID { get; set; }
    }

    public class PoliciesModel
    {
        public string Base64Signature { get; set; }
        public bool IsTermsAndConditionAccepted { get; set; }
        public int ReservationID { get; set; }
    }

    public class UploadGuestDocumentModel
    {
        public string Doc1Base64 { get; set; }
        public string Doc2Base64 { get; set; }
        public string Doc3Base64 { get; set; }

        public int ReservationID { get; set; }

        public int ProfileDetailID { get; set; }

        public string documentInformation { get; set; }
    }

    public class DocumentInformation
    {
        public string documentType { get; set; }
        public string documentNumber { get; set; }
        public string issueDate { get; set; }
        public string expiryDate { get; set; }
        public string gender { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string lastName { get; set; }
        public string nationality { get; set; }
        public string issueCountry { get; set; }
        public string birthDate { get; set; }
        public string faceImage { get; set; }
    }
}