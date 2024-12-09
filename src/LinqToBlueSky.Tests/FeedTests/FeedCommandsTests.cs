using LinqToBlueSky.Feed;
using LinqToBlueSky.OAuth;
using LinqToBlueSky.Provider;
using LinqToBlueSky.Tests.Common;

using Moq;

namespace LinqToBlueSky.Tests.FeedTests;

// TODO: Facets
// TODO: Reply
// TODO: Embed
// TODO: Langs
// TODO: Labels
// TODO: Tags
// TODO: CreatedAt

[TestClass]
public class FeedCommandsTests
{
    public FeedCommandsTests()
    {
        TestCulture.SetCulture();
    }

    async Task<BlueSkyContext> InitializeBlueSkyContextAsync(PostResponse result)
    {
        Mock<IAuthorizer> authMock = new();
        Mock<IBlueSkyExecute> execMock = new();

        TaskCompletionSource<IAuthorizer> tcsAuth = new();
        tcsAuth.SetResult(authMock.Object);

        TaskCompletionSource<PostResponse> tcsResponse = new();
        tcsResponse.SetResult(result);

        execMock.SetupGet(exec => exec.Authorizer).Returns(authMock.Object);
        execMock.Setup(exec =>
            exec.PostAsync<PostResponse>(
                It.IsAny<object>(),
                It.IsAny<Dictionary<string, string>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(tcsResponse.Task);

        BlueSkyContext ctx = new(execMock.Object);

        return await Task.FromResult(ctx);
    }

    [TestMethod]
    public async Task PostAsync_WithText_PopulatesResponse()
    {
        const string ExpectedCid = "42";
        const string ExpectedUri = "https://api.bluesky.com/post";
        PostResponse response = new()
        {
            Cid = ExpectedCid,
            Uri = ExpectedUri
        };
        BlueSkyContext ctx = await InitializeBlueSkyContextAsync(response);

        PostResponse? actual = await ctx.PostAsync("test");

        Assert.IsNotNull(actual);
        Assert.AreEqual(ExpectedCid, actual.Cid);
        Assert.AreEqual(ExpectedUri, actual.Uri);
    }

    [TestMethod]
    public async Task PostAsync_WithNullText_Throws()
    {
        BlueSkyContext ctx = await InitializeBlueSkyContextAsync(new());

        ArgumentNullException ex =
            await L2BSkyAssert.Throws<ArgumentNullException>(async () =>
                await ctx.PostAsync(null!));

        Assert.AreEqual("text is required", ex.ParamName);
    }
}
