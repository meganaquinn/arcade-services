using System.Threading.Tasks;
using NUnit.Framework;

namespace Maestro.ScenarioTests
{
    class ScenarioTests_AzDoFlow : MaestroScenarioTestBase
    {
        private TestParameters _parameters;
        private EndToEndFlowLogic testLogic;

        public ScenarioTests_AzDoFlow()
        {
        }

        [SetUp]
        public async Task InitializeAsync()
        {
            _parameters = await TestParameters.GetAsync();
            SetTestParameters(_parameters);

            testLogic = new EndToEndFlowLogic(_parameters);
        }

        [TearDown]
        public Task DisposeAsync()
        {
            _parameters.Dispose();
            return Task.CompletedTask;
        }

        [Test]
        [Category("ScenarioTest")]
        public async Task Darc_AzDoFlow_Batched()
        {
            TestContext.WriteLine("Azure DevOps Dependency Flow, batched");
            await testLogic.DarcBatchedFlowTestBase(true);
        }

        [Test]
        [Category("ScenarioTest")]
        public async Task Darc_AzDoFlow_NonBatched_AllChecksSuccessful()
        {
            TestContext.WriteLine("AzDo Dependency Flow, non-batched, all checks successful");
            await testLogic.NonBatchedAllChecksTestBase(false).ConfigureAwait(true);
        }

        [Test]
        [Category("ScenarioTest")]
        public async Task Darc_AzDoFlow_NonBatched()
        {
            TestContext.WriteLine("AzDo Dependency Flow, non-batched");
            await testLogic.NonBatchedFlowTestBase(false).ConfigureAwait(false);
        }
    }
}