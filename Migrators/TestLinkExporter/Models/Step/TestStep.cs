namespace TestLinkExporter.Models.Step
{
    /// <summary>
    ///  represent a single test step in a test case
    /// </summary>
    public class TestStep
    {
        /// <summary>
        ///  string describing the actions
        /// </summary>
        public string actions;

        /// <summary>
        ///  flag whether this step is active
        /// </summary>
        public bool active;

        /// <summary>
        ///  1=manual or 2=automated
        /// </summary>
        public int execution_type;

        /// <summary>
        ///  string desribing the expected result in this step
        /// </summary>
        public string expected_results;

        /// <summary>
        ///  interenal primary key.
        /// </summary>
        public int id;

        /// <summary>
        ///  step number. Starts at 1
        /// </summary>
        public int step_number;

        public TestStep()
        {
        }
    }
}
