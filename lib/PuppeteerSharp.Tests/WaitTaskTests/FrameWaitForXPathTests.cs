#pragma warning disable CS0618 // WaitForXPathAsync is obsolete but we test the funcionatlity anyway
using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PuppeteerSharp.Helpers;
using PuppeteerSharp.Nunit;
using PuppeteerSharp.Tests.Attributes;
using PuppeteerSharp.Transport;

namespace PuppeteerSharp.Tests.WaitTaskTests
{
    public sealed class FrameWaitForXPathTests : PuppeteerPageBaseTest
    {
        private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";
        private PollerInterceptor _pollerInterceptor;

        public FrameWaitForXPathTests()
        {
            DefaultOptions = TestConstants.DefaultBrowserOptions();

            // Set up a custom TransportFactory to intercept sent messages
            // Some of the tests require making assertions after a WaitForFunction has
            // started, but before it has resolved. We detect that reliably by
            // listening to the message that is sent to start polling.
            // This might not be an issue in upstream puppeteer.js, or may be highly unlikely,
            // due to differences between node.js's task scheduler and .net's.
            DefaultOptions.TransportFactory = async (url, options, cancellationToken) =>
            {
                _pollerInterceptor = new PollerInterceptor(await WebSocketTransport.DefaultTransportFactory(url, options, cancellationToken));
                return _pollerInterceptor;
            };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _pollerInterceptor.Dispose();
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should support some fancy xpath")]
        [PuppeteerTimeout]
        public async Task ShouldSupportSomeFancyXpath()
        {
            await Page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
            var waitForXPath = Page.WaitForXPathAsync("//p[normalize-space(.)=\"hello world\"]");
            Assert.AreEqual("hello  world  ", await Page.EvaluateFunctionAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should run in specified frame")]
        [PuppeteerTimeout]
        public async Task ShouldRunInSpecifiedFrame()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            await FrameUtils.AttachFrameAsync(Page, "frame2", TestConstants.EmptyPage);
            var frame1 = Page.Frames.First(f => f.Name == "frame1");
            var frame2 = Page.Frames.First(f => f.Name == "frame2");
            var waitForXPathPromise = frame2.WaitForXPathAsync("//div");
            await frame1.EvaluateFunctionAsync(AddElement, "div");
            await frame2.EvaluateFunctionAsync(AddElement, "div");
            var eHandle = await waitForXPathPromise;
            Assert.AreEqual(frame2, eHandle.Frame);
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should throw when frame is detached")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldThrowWhenFrameIsDetached()
        {
            await FrameUtils.AttachFrameAsync(Page, "frame1", TestConstants.EmptyPage);
            var frame = Page.FirstChildFrame();
            var waitPromise = frame.WaitForXPathAsync("//*[@class=\"box\"]");
            await FrameUtils.DetachFrameAsync(Page, "frame1");
            var exception = Assert.ThrowsAsync<WaitTaskTimeoutException>(() => waitPromise);
            StringAssert.Contains("waitForFunction failed: frame got detached.", exception.Message);
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "hidden should wait for display: none")]
        [PuppeteerTimeout]
        public async Task HiddenShouldWaitForDisplayNone()
        {
            var divHidden = false;
            var startedPolling = _pollerInterceptor.WaitForStartPollingAsync();
            await Page.SetContentAsync("<div style='display: block;'></div>");
            var waitForXPath = Page.WaitForXPathAsync("//div", new WaitForSelectorOptions { Hidden = true })
                .ContinueWith(_ => divHidden = true);
            await startedPolling;
            Assert.False(divHidden);
            await Page.EvaluateExpressionAsync("document.querySelector('div').style.setProperty('display', 'none')");
            Assert.True(await waitForXPath.WithTimeout());
            Assert.True(divHidden);
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should return the element handle")]
        [PuppeteerTimeout]
        public async Task ShouldReturnTheElementHandle()
        {
            var waitForXPath = Page.WaitForXPathAsync("//*[@class=\"zombo\"]");
            await Page.SetContentAsync("<div class='zombo'>anything</div>");
            Assert.AreEqual("anything", await Page.EvaluateFunctionAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should allow you to select a text node")]
        [PuppeteerTimeout]
        public async Task ShouldAllowYouToSelectATextNode()
        {
            await Page.SetContentAsync("<div>some text</div>");
            var text = await Page.WaitForXPathAsync("//div/text()");
            Assert.AreEqual(3 /* Node.TEXT_NODE */, await (await text.GetPropertyAsync("nodeType")).JsonValueAsync<int>());
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should allow you to select an element with single slash")]
        [PuppeteerTimeout]
        public async Task ShouldAllowYouToSelectAnElementWithSingleSlash()
        {
            await Page.SetContentAsync("<div>some text</div>");
            var waitForXPath = Page.WaitForXPathAsync("/html/body/div");
            Assert.AreEqual("some text", await Page.EvaluateFunctionAsync<string>("x => x.textContent", await waitForXPath));
        }

        [PuppeteerTest("waittask.spec.ts", "Frame.waitForXPath", "should respect timeout")]
        [PuppeteerTimeout]
        public void ShouldRespectTimeout()
        {
            const int timeout = 10;

            var exception = Assert.ThrowsAsync<WaitTaskTimeoutException>(()
                    => Page.WaitForXPathAsync("//div", new WaitForSelectorOptions { Timeout = timeout }));

            StringAssert.Contains($"Waiting failed: {timeout}ms exceeded", exception.Message);
        }
    }
}
#pragma warning restore CS0618
