//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CheckinPortal.DataAccess
{
    using System;
    
    public partial class usp_FetchProfileDocumentsforLocalPush_Result
    {
        public string ReservationNameID { get; set; }
        public string ProfileID { get; set; }
        public Nullable<int> DocumentMasterID { get; set; }
        public string DocumentNumber { get; set; }
        public Nullable<System.DateTime> ExpiryDate { get; set; }
        public Nullable<System.DateTime> IssueDate { get; set; }
        public byte[] DocumentImage1 { get; set; }
        public byte[] DocumentImage2 { get; set; }
        public byte[] DocumentImage3 { get; set; }
        public byte[] FaceImage { get; set; }
    }
}
