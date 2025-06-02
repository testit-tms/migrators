using CookComputing.XmlRpc;

namespace TestLinkExporter.Client.TestLinkApi
{
    /// <summary>
    /// the interface mapping required for the XmlRpc api of testlink. 
    /// This interface is used by the TestLink class. 
    /// </summary>
    /// <remarks>This class makes use of XML-RPC.NET Copyright (c) 2006 Charles Cook</remarks>
    [XmlRpcUrl("")]
    public interface ITestLink : IXmlRpcProxy
    {
        #region TestProject

        [XmlRpcMethod("tl.getTestProjectByName", StructParams = true)]
        object getTestProjectByName(string devKey, string testprojectname);

        #endregion

        #region TestCase


        [XmlRpcMethod("tl.getTestCaseAttachments", StructParams = true)]
        object getTestCaseAttachments(string devKey, int testcaseid);

        /// <summary>
        /// get test case specification using external or internal id. returns last version
        /// </summary>
        /// <param name="devKey"></param>
        /// <param name="testcaseid"></param>
        /// <returns></returns>
        [XmlRpcMethod("tl.getTestCase", StructParams = true)]
        object getTestCase(string devKey, int testcaseid);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="devKey"></param>
        /// <param name="testcaseid"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        [XmlRpcMethod("tl.getTestCase", StructParams = true)]
        object getTestCase(string devKey, int testcaseid, int version);

        [XmlRpcMethod("tl.getTestCasesForTestSuite", StructParams = true)]
        object getTestCasesForTestSuite(string devKey, int testsuiteid, bool deep, bool getkeywords);

        [XmlRpcMethod("tl.getTestCasesForTestSuite", StructParams = true)]
        object getTestCasesForTestSuite(string devKey, int testsuiteid, bool deep, string details);

        [XmlRpcMethod("tl.getTestCasesForTestSuite", StructParams = true)]
        object getTestCasesForTestSuite(string devKey, int testsuiteid, bool deep, string details, bool getkeywords);

        [XmlRpcMethod("tl.getTestCaseKeywords", StructParams = true)]
        object getTestCaseKeywords(string devKey, int testcaseid);

        #endregion

        #region TestSuite

        [XmlRpcMethod("tl.getFirstLevelTestSuitesForTestProject", StructParams = true)]
        object[] getFirstLevelTestSuitesForTestProject(string devKey, int testprojectid);

        [XmlRpcMethod("tl.getTestSuitesForTestSuite", StructParams = true)]
        object getTestSuitesForTestSuite(string devKey, int testsuiteid);

        #endregion
    }
}
