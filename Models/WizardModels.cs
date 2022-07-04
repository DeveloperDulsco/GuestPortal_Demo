﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Web;

namespace CheckinPortal.Models
{

    public class Answers
    {
        List<string> Answer { get; set; }
    }

    public class WizardModels
    {
    }

    public class SendEmailModel
    {
        public string reservationID { get; set; }
        public string emailID { get; set; }

    }
    public class ValidateDocumentModel
    {
        public string extension { get; set; }
        public string imageBase64 { get; set; }

    }

    public class ReservationPackageModel
    {
        //PM.PackageID,RD.ReservationNameID,PM.PackageCode,PM.PackageName,PM.PackageDesc,PM.PackageAmount
        public int PackageID { get; set; }
        public int ReservationNameID { get; set; }
        public string PackageCode { get; set; }
        public string PackageName { get; set; }
        public string PackageDesc { get; set; }
        public string PackageAmount { get; set; }

        public bool IsRoomUpsell { get; set; }
    }

    public class ReservationModel
    {
        public string ReservationNameID { get; set; }
        public int ReservationID { get; set; }
        public string ReservationNumber { get; set; }
        public decimal AverageRoomRate { get; set; }
        public string ArrivalDate { get; set; }
        public string DepartureDate { get; set; }
        public int AdultCount { get; set; }
        public int ChildCount { get; set; }
        public List<Profile> Profiles { get; set; }
        public string MembershipNo { get; set; }
        public bool IsMembershipRequested { get; set; }
        public string FlightNo { get; set; }
        public string ExpectedTimeofArrival { get; set; }
        public string SignatureBase64 { get; set; }
        public bool IsTermsAndConditions { get; set; }
        public List<Upsell> SelectedUpsells { get; set; }
        public List<Questions> Questions { get; set; }

        public string RoomType { get; set; }

        public string RoomTypeDescription { get; set; }

        public decimal TotalRoomRate { get; set; }

        public bool IsDepositAvailable { get; set; }

        public bool IsBreakFastAvailable { get; set; }
    }

    public class Upsell
    {
        public string UpsellCode { get; set; }
    }

    public class Profile
    {
        public int ProfileDetailID { get; set; }
        public int ProfileID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public int? CountryID { get; set; }
        public int? StateID { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string DocumentImage1 { get; set; }
        public string DocumentImage2 { get; set; }
        public string DocumentImage3 { get; set; }

        public string Salutation { get; set; }

    }

    public class Questions
    {
        public int QuestionID { get; set; }
        public string Question { get; set; }
        public string QuestionHint { get; set; }
        public string QuestionType { get; set; }
    }

    public class PackageMasterModal
    {
        public string PackageCode { get; set; }
        public string PackageName { get; set; }
        public decimal? PackageAmount { get; set; }
        public string PackageDesc { get; set; }
        public byte[] PackageImage { get; set; }
        public bool? isActive { get; set; }
        public int PackageID { get; set; }
        public bool IsRoomUpsell { get; set; }

        public string Units { get; set; }
    }

    public class UpdateReservationModel
    {
        public int ReservationID { get; set; }
        public string ETA { get; set; }
        public string FlightNo { get; set; }
        public int ProfileDetailID { get; set; }
        public string MembershipNo { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int? StateMasterID { get; set; }
        public string PostalCode { get; set; }
        public int? CountryMasterID { get; set; }
        public byte[] SignatureImage { get; set; }
        public byte[] DocumentImage1 { get; set; }
        public byte[] DocumentImage2 { get; set; }
        public byte[] DocumentImage3 { get; set; }
        public byte[] FaceImage { get; set; }
        public string Type { get; set; }
        public bool IsMemberShipEnrolled { get; set; }
        public string Gender { get; set; }
        public string IssueCountry { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? BirthDate { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentType { get; set; }
        public string Nationality { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
    }


    public class ActiveTransctionsModel
    {
        public int TransactionID  { get; set; }
		public string  ReservationNumber{ get; set; }
		public string  ReservationNameID{ get; set; }
		public string    MaskedCardNumber { get; set; }
		public string    ExpiryDate { get; set; } 
		public string   FundingSource { get; set; }
		public string   Amount { get; set; }
		public string   Currency{ get; set; }
		public string   RecurringIdentifier{ get; set; }
		public string   AuthorisationCode{ get; set; }
		public string   pspReferenceNumber{ get; set; }
		public string   ParentPspRefereceNumber{ get; set; } 
		public string   TransactionType{ get; set; }
		public string   ResultCode{ get; set; }
		public string   ResponseMessage{ get; set; }
		public bool   IsActive{ get; set; }
		public string   StatusType{ get; set; }
		public string   CardType{ get; set; }

        public string AdjustAuthorisationData { get; set; }
    }
}