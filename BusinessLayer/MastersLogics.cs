using CheckinPortal.Helpers;
using CheckinPortal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace CheckinPortal.BusinessLayer
{
    public class MastersLogics
    {
        public MastersLogics()
        {

        }
        public List<DataAccess.tbCountryMaster> GetCountryList()
        {
            List<DataAccess.tbCountryMaster> tbCountryMasters = new List<DataAccess.tbCountryMaster>();
            DataTable transaction = new DataTable();
            try
            {
                string Query = "select * from tbCountryMaster  order by Country_Full_name";
                var countryTable = SQLHelpers.Instance.ExecuteDataset(Query);
                tbCountryMasters = Helpers.DataTableHelper.DataTableToList<CheckinPortal.DataAccess.tbCountryMaster>(countryTable);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return tbCountryMasters;
        }

        public List<DataAccess.tbStateMaster> GetStateList()
        {
            List<DataAccess.tbStateMaster> tbstateMaster = new List<DataAccess.tbStateMaster>();
            try
            {

                var stateDatatable = SQLHelpers.Instance.ExecuteDataset($"SELECT * FROM tbStateMasters");

                tbstateMaster = Helpers.DataTableHelper.DataTableToList<CheckinPortal.DataAccess.tbStateMaster>(stateDatatable);
            }
            catch (Exception ex)
            {

            }

            return tbstateMaster;
        }

        public List<Models.StateMaster> GetStateListByCountryID(int CountryID)
        {

            List<Models.StateMaster> stateList = new List<Models.StateMaster>();

            DataTable transaction = new DataTable();
            try
            {
                string Query = $"select * from tbStateMaster where CountryMasterID = {CountryID}";
                var countryTable = SQLHelpers.Instance.ExecuteDataset(Query);
                stateList = Helpers.DataTableHelper.DataTableToList<Models.StateMaster>(countryTable);
            }
            catch (Exception ex)
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return stateList;// db.tbStateMasters.Where(x=>x.CountryMasterID == CountryID).ToList();
        }

        public DataTable validateDocumentIssueCountry(string docType,string issueCountry)
        {
            try
            {
                SqlParameter docTypeParameter = new SqlParameter()
                {
                    ParameterName = "@DocumentTypeCode",
                    Value = docType,
                    SqlDbType = SqlDbType.VarChar
                };

                SqlParameter issueCountryParameter = new SqlParameter()
                {
                    ParameterName = "@issueCountry",
                    Value = issueCountry,
                    SqlDbType = SqlDbType.VarChar
                };

                return SQLHelpers.Instance.ExecuteSP("Usp_validateIssueCountry", docTypeParameter, issueCountryParameter);
                
            }
            catch
            {
                //Helpers.EvenLogHelper.Instance.LogError($"Unhandled Exception while executing SP Usp_FetchTopOneRecordsforProcessing {ex.ToString()}");
            }
            return null;
        }
    }
}