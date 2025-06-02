using System.Collections;
using System.Net;
using CookComputing.XmlRpc;
using TestLinkExporter.Models.Attachment;
using TestLinkExporter.Models.Project;
using TestLinkExporter.Models.Suite;
using TestLinkExporter.Models.TestCase;

namespace TestLinkExporter.Client.TestLinkApi
{
    /// <summary>
    /// this is the proxy class to connect to TestLink.
    /// It provides a list of functions that map straight into the Tstlink API as it stands at V 1.9.2
    /// </summary>
    /// <remarks>This class makes use of XML-RPC.NET Copyright (c) 2006 Charles Cook</remarks>
    public class TestLink
    {
        private string devkey = "";
        private ITestLink proxy;


        #region constructors

        /// <summary>
        /// default constructor 
        /// </summary>
        /// <param name="apiKey">developer key as provided by testlink</param>
        /// <param name="url">url of testlink API. Something like http://localhost/testlink/lib/api/xmlrpc.php</param>
        public TestLink(string apiKey, string url)
            : this(apiKey, url, false)
        {
        }

        ///// <summary>
        ///// default constructor without URL. Uses default localhost url
        ///// </summary>
        ///// <param name="apiKey"></param>
        //public TestLink(string apiKey) : this(apiKey, "", false) { }

        ///// <summary>
        ///// constructor with debug key
        ///// </summary>
        ///// <param name="apiKey"></param>
        ///// <param name="log">if true then keep last request and last response</param>
        //public TestLink(string apiKey, bool log): this(apiKey,"",log)
        //{
        //}
        /// <summary>
        /// constructor with URL and log flag
        /// </summary>
        /// <param name="apiKey">developer key as provided by testlink</param>
        /// <param name="url">url of testlink API. Something like http://localhost/testlink/lib/api/xmlrpc.php </param>
        /// <param name="log">enable capture of lastRequest and lastResponse for debugging</param>
        public TestLink(string apiKey, string url, bool log)
        {
            devkey = apiKey;
            proxy = XmlRpcProxyGen.Create<ITestLink>();
            ServicePointManager.ServerCertificateValidationCallback += (o, c, ch, er) => true;
            if (log)
            {
                proxy.RequestEvent += myHandler;
                proxy.ResponseEvent += proxy_ResponseEvent;
            }

            if (!string.IsNullOrEmpty(url))
                proxy.Url = url;
        }

        #endregion

        #region logging

        /// <summary>
        /// last xmlrpc request sent to testlink. only works if constructor was called with argument log set to true
        /// </summary>
        public string LastRequest { get; private set; }

        /// <summary>
        /// debug last response reseved from testlink xmlrpc call. Only works if constructor was called with argument log set to true
        /// </summary>
        public string LastResponse { get; private set; }

        private void proxy_ResponseEvent(object sender, XmlRpcResponseEventArgs args)
        {
            args.ResponseStream.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(args.ResponseStream);
            LastResponse = sr.ReadToEnd();
        }

        private void myHandler(object sender, XmlRpcRequestEventArgs args)
        {
            var l = args.RequestStream.Length;
            args.RequestStream.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(args.RequestStream);
            LastRequest = sr.ReadToEnd();
        }

        #endregion

        #region error handling

        /// <summary>
        ///  process the response object returned by the Tstlink API for error messages. 
        ///  if it finds one or more error messages it throws a TLINK Exception        ///  
        /// </summary>
        /// <param name="errorMessage">the actual message returned by testlink</param>
        /// <returns>true if it found an error message that matches an errorCodes list
        /// false if there were no errors</returns>returns>
        private bool handleErrorMessage(object errorMessage)
        {
            if (errorMessage is object[])
                return handleErrorMessage(errorMessage as object[], new int[0]);
            return false;
        }

        /// <summary>
        ///  process the response object returned by the Tstlink API for error messages. 
        ///  if it finds one or more error messages it throws a TLINK Exception        ///  
        /// </summary>
        /// <param name="errorMessage">the actual message returned by testlink</param>
        /// <returns>true if it found an error message that matches an errorCodes list
        /// false if there were no errors</returns>returns>
        /// <param name="exceptedErrorCodes">a list of integers that should not result in an exception to be processed</param>
        private bool handleErrorMessage(object errorMessage, params int[] exceptedErrorCodes)
        {
            if (errorMessage is object[])
                return handleErrorMessage(errorMessage as object[], exceptedErrorCodes);
            return false;
        }

        /// <summary>
        ///  process the response object returned by the Tstlink API for error messages. 
        ///  if it finds one or more error messages it throws a TLINK Exception        ///  
        /// </summary>
        /// <param name="errorMessage">the actual message returned by testlink</param>
        /// <returns>true if it found an error message that matches an errorCodes list
        /// false if there were no errors</returns>returns>
        /// <param name="exceptedErrorCodes">a list of integers that should not result in an exception to be processed</param>
        private bool handleErrorMessage(object[] errorMessage, int[] exceptedErrorCodes)
        {
            var errs = decodeErrors(errorMessage);
            if (errs.Count > 0)
            {
                foreach (var foundError in errs)
                {
                    var matched = false;
                    foreach (var exceptedErrorCode in exceptedErrorCodes)
                        if (foundError.code == exceptedErrorCode)
                        {
                            matched = true;
                            break;
                        }

                    if (!matched) // all must match or we throw an exception
                    {
                        var msg = string.Format("{0}:{1}", errs[0].code, errs[0].message);
                        throw new TestLinkException(msg, errs);
                    }
                }

                return true; // we have matched the errors to the exceptions
            }

            return false; // there were no errors
        }

        private List<TestLinkErrorMessage> decodeErrors(object messageList)
        {
            return decodeErrors(messageList as object[]);
        }

        /// <summary>
        /// internal. try to conver the struct to an error message. Return null if it wasn't one
        /// </summary>
        /// <param name="messageList"></param>
        /// <returns>a TLErrorMessage or null</returns>
        private List<TestLinkErrorMessage> decodeErrors(object[] messageList)
        {
            var result = new List<TestLinkErrorMessage>();
            if (messageList == null)
                return result;
            foreach (XmlRpcStruct message in messageList)
                if (message.ContainsKey("code") && message.ContainsKey("message"))
                    result.Add(TestLinkData.ToTestLinkErrorMessage(message));

            return result;
        }

        private TestLinkErrorMessage decodeSingleError(XmlRpcStruct message)
        {
            if (message.ContainsKey("code") && message.ContainsKey("message"))
                return TestLinkData.ToTestLinkErrorMessage(message);
            return null;
        }

        /// <summary>
        /// check that the state of the interface 
        /// </summary>
        private void stateIsValid()
        {
            if (devkey == null)
                throw new TestLinkException("Devkey is null. You must supply a development key");
            if (devkey == string.Empty)
                throw new TestLinkException("Devkey is empty. You must supply a development key");
        }

        #endregion

        #region TestCase

        /// <summary>
        /// get a test case by its id
        /// </summary>
        /// <param name="testcaseid">Id of the test case</param>
        /// <param name="versionId">(optional) the version id. If not given the latest version is returned</param>
        /// <returns></returns>
        public TestCase GetTestCase(int testcaseid, int versionId = 0)
        {
            stateIsValid();
            object o = null;
            if (versionId == 0)
                o = proxy.getTestCase(devkey, testcaseid);
            else
                o = proxy.getTestCase(devkey, testcaseid, versionId);
            handleErrorMessage(o);
            var c = o as object[];
            var rList = c[0] as XmlRpcStruct;
            return TestLinkData.ToTestCase(rList);
        }

        /// <summary>
        /// get test cases contained in a test suite
        /// </summary>
        /// <param name="testSuiteid">id of the test suite</param>
        /// <param name="deep">Set the deep flag to false if you only want test cases in the test suite provided and no child test cases.</param>
        /// <returns>A list of Test Cases</returns>
        public List<TestCaseFromTestSuite> GetTestCasesForTestSuite(int testSuiteid, bool deep, bool getkeywords)
        {
            stateIsValid();
            var result = new List<TestCaseFromTestSuite>();
            var response1 = proxy.getTestCasesForTestSuite(devkey, testSuiteid, deep, getkeywords);
            var response = proxy.getTestCasesForTestSuite(devkey, testSuiteid, deep, "full", getkeywords);
            if (response is string && (string) response == string.Empty) // equals null return
                return result;
            handleErrorMessage(response);
            var list = response as object[];
            if (list != null)
                foreach (XmlRpcStruct data in list)
                {
                    var tc = TestLinkData.ToTestCaseFromTestSuite(data);
                    result.Add(tc);
                }

            return result;
        }

        /// <summary>
        /// get a list of testcase ids of test cases contained in a test suite
        /// </summary>
        /// <param name="testSuiteid">id of the test suite</param>
        /// <param name="deep">Set the deep flag to false if you only want test cases in the test suite provided and no child test cases.</param>
        /// <returns></returns>
        public List<int> GetTestCaseIdsForTestSuite(int testSuiteid, bool deep)
        {
            stateIsValid();
            var o = proxy.getTestCasesForTestSuite(devkey, testSuiteid, deep, "simple");
            var result = new List<int>();

            handleErrorMessage(o);

            if (o is object[] list)
                foreach (var item in list)
                {
                    var val = (string) (item as XmlRpcStruct)?["id"];
                    //string val = "2";// (string)item.Keys[0];//["id"];
                    result.Add(int.Parse(val));
                }

            return result;
        }

        /// <summary>
        /// retrieve the attachments for a test case
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <returns></returns>
        public List<Attachment> GetTestCaseAttachments(int testCaseId)
        {
            stateIsValid();
            var o = proxy.getTestCaseAttachments(devkey, testCaseId);
            handleErrorMessage(o);
            var result = new List<Attachment>();
            var olist = o as XmlRpcStruct;
            if (olist != null)
                foreach (XmlRpcStruct item in olist.Values)
                {
                    var a = TestLinkData.ToAttachment(item);
                    result.Add(a);
                }

            return result;
        }

        /// <summary>
        /// Get keywords assigned to a test case
        /// </summary>
        /// <param name="testcaseid">ID of the test case</param>
        /// <returns>A list of keywords assigned to the test case</returns>
        public List<string> GetTestCaseKeywords(int testcaseid)
        {
            stateIsValid();
            var response = proxy.getTestCaseKeywords(devkey, testcaseid);
            var result = new List<string>();
            
            handleErrorMessage(response);
            
            if (response is XmlRpcStruct keywords)
            {
                foreach (DictionaryEntry entry in keywords)
                {
                    if (entry.Value is XmlRpcStruct keywordData)
                    {
                        foreach (DictionaryEntry childEntry in keywordData)
                        {
                            var keyword = childEntry.Value;
                            if (keyword != null)
                            {
                                result.Add((string)keyword);
                            }
                        }
                    }
                }
            }
            
            return result;
        }

        #endregion

        #region test project
        /// <summary>
        /// get a single Project
        /// </summary>
        /// <param name="projectname"></param>
        /// <returns>a Project or Null</returns>
        public TestProject GetProject(string projectname)
        {
            TestProject result = null;
            stateIsValid();
            var o = proxy.getTestProjectByName(devkey, projectname);
            handleErrorMessage(o);
            if (o is object[])
            {
                var olist = o as object[];
                result = TestLinkData.ToTestProject(olist[0] as XmlRpcStruct);
            }
            else
            {
                result = TestLinkData.ToTestProject(o as XmlRpcStruct);
            }

            return result;
        }

        #endregion

        #region Test Suite

        /// <summary>
        /// get all top level test suites for a test project
        /// </summary>
        /// <param name="testprojectid"></param>
        /// <returns></returns>
        public List<TestSuite> GetFirstLevelTestSuitesForTestProject(int testprojectid)
        {
            stateIsValid();
            var response = proxy.getFirstLevelTestSuitesForTestProject(devkey, testprojectid);
            var errors = decodeErrors(response);
            var result = new List<TestSuite>();
            if (errors.Count > 0)
            {
                if (errors[0].code != 7008) // project has no test suites, we return an emptu result
                    handleErrorMessage(response);
            }
            else
            {
                foreach (XmlRpcStruct data in response)
                    result.Add(TestLinkData.ToTestSuite(data));
            }

            return result;
        }

        public List<TestSuite> GetTestSuitesForTestSuite(int testsuiteid)
        {
            var Result = new List<TestSuite>();
            stateIsValid();
            var o = proxy.getTestSuitesForTestSuite(devkey, testsuiteid);
            // Testlink returns an empty string if a test suite has no child test suites
            if (o is string)
                return Result;

            // just in case this gets fixed, then this should work.
            if (handleErrorMessage(o, 7008))
                return Result; // empty list
            var response = o as XmlRpcStruct;
            foreach (var data in response.Values)
                if (data is XmlRpcStruct d)
                    Result.Add(TestLinkData.ToTestSuite(d));

            return Result;
        }

        #endregion
    }
}
