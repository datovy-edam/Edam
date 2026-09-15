using Edam.Data.AssetReport;
using Edam.Text;
namespace Edam.Test.AssetReports
{
   [TestClass]
   public class TestReportHeader
   {
      public const string REPORTS_PATH =
         "C:\\prjs\\Edam\\Edam.Resources\\Edam.Studio\\Edam.App.Data\\Reports";
      [TestMethod]
      public void LoadHeader()
      {
         string reportFullPath = REPORTS_PATH + "/AssetReportHeaderStandard.json";
         TableRowHeaderInfo headers = 
            TableRowHeaderInfo.FromJson(reportFullPath);
         Assert.IsNotNull(headers);
      }
   }
}