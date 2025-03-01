using System.Threading.Tasks;
using PuppeteerSharp.Media;
using PuppeteerSharp.Tests.Attributes;
using PuppeteerSharp.Nunit;
using NUnit.Framework;

namespace PuppeteerSharp.Tests.EmulationTests
{
    public class EmulateMediaTypeTests : PuppeteerPageBaseTest
    {
        public EmulateMediaTypeTests(): base()
        {
        }

        [PuppeteerTest("emulation.spec.ts", "Page.emulateMediaType", "should work")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldWork()
        {
            Assert.True(await Page.EvaluateExpressionAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateExpressionAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaTypeAsync(MediaType.Print);
            Assert.False(await Page.EvaluateExpressionAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateExpressionAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaTypeAsync(MediaType.None);
            Assert.True(await Page.EvaluateExpressionAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateExpressionAsync<bool>("matchMedia('print').matches"));
        }
    }
}
