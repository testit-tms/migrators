namespace TestLinkExporter.Models;

public class TestLinkSuite
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ParentId { get; set; }
    public List<TestLinkSuite> Suites { get; set; }
}
