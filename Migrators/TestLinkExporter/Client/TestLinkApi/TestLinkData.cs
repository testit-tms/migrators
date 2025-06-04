using CookComputing.XmlRpc;
using TestLinkExporter.Models.Attachment;
using TestLinkExporter.Models.Project;
using TestLinkExporter.Models.Suite;
using TestLinkExporter.Models.Step;
using TestLinkExporter.Models.TestCase;

namespace TestLinkExporter.Client.TestLinkApi
{
    public abstract class TestLinkData
    {
        internal static TestLinkErrorMessage ToTestLinkErrorMessage(XmlRpcStruct data)
        {
            var item = new TestLinkErrorMessage();
            item.code = ToInt(data, "code");
            item.message = (string) data["message"];
            return item;
        }

        internal static Attachment ToAttachment(XmlRpcStruct data)
        {
            var item = new Attachment();
            item.id = ToInt(data, "id");
            item.file_type = (string) data["file_type"];
            item.name = (string) data["name"];
            item.title = (string) data["title"];
            item.date_added = ToDate(data, "date_added");
            var s = (string) data["content"];
            item.content = Convert.FromBase64String(s);

            return item;
        }

        internal static TestCaseFromTestSuite ToTestCaseFromTestSuite(XmlRpcStruct data)
        {
            var item = new TestCaseFromTestSuite();
            item.active = int.Parse((string) data["active"]) == 1;
            item.id = ToInt(data, "id");
            item.name = (string) data["name"];
            item.version = ToInt(data, "version");
            item.tcversion_id = ToInt(data, "tcversion_id");
            //steps = (string)data["steps"];
            //expected_results = (string)data["expected_results"];
            item.external_id = (string) data["tc_external_id"];
            item.testSuite_id = ToInt(data, "parent_id");
            item.is_open = int.Parse((string) data["is_open"]) == 1;
            item.modification_ts = ToDate(data, "modification_ts");
            item.updater_id = ToInt(data, "updater_id");
            item.execution_type = ToInt(data, "execution_type");
            item.summary = (string) data["summary"];
            if (data.ContainsKey("details"))
                item.details = (string) data["details"];
            else
                item.details = "";
            item.author_id = ToInt(data, "author_id");
            item.creation_ts = ToDate(data, "creation_ts");
            item.importance = ToInt(data, "importance");
            item.parent_id = ToInt(data, "parent_id");
            item.node_type_id = ToInt(data, "node_type_id");
            item.node_order = ToInt(data, "node_order");
            item.node_table = (string) data["node_table"];
            item.layout = (string) data["layout"];
            item.status = ToInt(data, "status");
            item.preconditions = (string) data["preconditions"];

            return item;
        }

        /// <summary>
        ///  constructor used by XMLRPC interface on decoding the function return
        /// </summary>
        /// <param name="data">data returned by Testlink</param>
        internal static TestProject ToTestProject(XmlRpcStruct data)
        {
            var item = new TestProject();
            item.id = ToInt(data, "id");
            item.notes = (string) data["notes"];
            item.color = (string) data["color"];
            item.active = ToInt(data, "active") == 1;
            //changed option encoding sinc V 1.9
            var opt = data["opt"] as XmlRpcStruct;
            item.option_reqs = (int) opt["requirementsEnabled"] == 1;
            item.option_priority = (int) opt["testPriorityEnabled"] == 1;
            item.option_automation = (int) opt["automationEnabled"] == 1;
            item.option_inventory = (int) opt["inventoryEnabled"] == 1;
            item.prefix = (string) data["prefix"];
            item.tc_counter = ToInt(data, "tc_counter");
            item.name = (string) data["name"];

            return item;
        }

        /// <summary>
        ///  constructor used by the XML Rpc return
        /// </summary>
        /// <param name="data"></param>
        internal static TestStep ToTestStep(XmlRpcStruct data)
        {
            var item = new TestStep();
            item.id = ToInt(data, "id");
            item.step_number = ToInt(data, "step_number");
            item.actions = (string) data["actions"];
            item.expected_results = (string) data["expected_results"];
            item.active = ToInt(data, "active") == 1;
            item.execution_type = ToInt(data, "execution_type");

            return item;
        }

        /// <summary>
        ///  constructor used by XMLRPC interface on decoding the function return
        /// </summary>
        /// <param name="data">data returned by Testlink</param>
        internal static TestSuite ToTestSuite(XmlRpcStruct data)
        {
            var item = new TestSuite();
            item.name = (string) data["name"];
            item.id = ToInt(data, "id");
            item.parentId = ToInt(data, "parent_id");
            item.nodeTypeId = ToInt(data, "node_type_id");
            item.nodeOrder = ToInt(data, "node_order");

            return item;
        }

        internal static TestCase ToTestCase(XmlRpcStruct data)
        {
            var tc = new TestCase
            {
                active = int.Parse((string) data["active"]) == 1,
                externalid = (string) data["tc_external_id"],
                id = ToInt(data, "id"),
                updater_login = (string) data["updater_login"],
                author_login = (string) data["author_login"],
                name = (string) data["name"],
                node_order = ToInt(data, "node_order"),
                testsuite_id = ToInt(data, "testsuite_id"),
                testcase_id = ToInt(data, "testcase_id"),
                version = ToInt(data, "version"),
                layout = (string) data["layout"],
                status = ToInt(data, "status"),
                summary = (string) data["summary"],
                preconditions = (string) data["preconditions"],
                importance = ToInt(data, "importance"),
                author_id = ToInt(data, "author_id"),
                updater_id = ToInt(data, "updater_id"),
                modification_ts = ToDate(data, "modification_ts"),
                creation_ts = ToDate(data, "creation_ts"),
                is_open = int.Parse((string) data["is_open"]) == 1,
                execution_type = ToInt(data, "execution_type"),
                author_first_name = (string) data["author_first_name"],
                author_last_name = (string) data["author_last_name"],
                updater_first_name = (string) data["updater_first_name"],
                updater_last_name = (string) data["updater_last_name"],
                steps = new List<TestStep>()
            };

            var stepData = data["steps"] as object[];
            if (stepData != null)
                foreach (XmlRpcStruct aStepDatum in stepData)
                    tc.steps.Add(ToTestStep(aStepDatum));

            return tc;
        }

        protected static int ToInt(XmlRpcStruct data, string name)
        {
            if (data.ContainsKey(name))
            {
                var val = data[name];
                switch (val)
                {
                    case string s:
                        if (int.TryParse(s, out var n)) return n;
                        break;
                    case int _:
                        return (int) val;
                }
            }

            return 0;
        }

        protected static DateTime ToDate(XmlRpcStruct data, string name)
        {
            if (data.ContainsKey(name) && DateTime.TryParse((string) data[name], out var n)) return n;
            return DateTime.MinValue;
        }
    }
}
