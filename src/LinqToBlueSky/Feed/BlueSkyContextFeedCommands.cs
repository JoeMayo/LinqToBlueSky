using LinqToBlueSky.Feed;
using LinqToBlueSky.OAuth;

using System.Dynamic;

namespace LinqToBlueSky;

public partial class BlueSkyContext
{
    /// <summary>
    /// Sends a new post
    /// </summary>
    /// <param name="text">The primary post content. May be an empty string, if there are embeds.</param>
    /// <param name="facets">Annotations of text (mentions, URLs, hashtags, etc).</param>
    /// <param name="reply">Post replying to.</param>
    /// <param name="embed">Image and video details.</param>
    /// <param name="langs">Indicates human language of post primary text content.</param>
    /// <param name="labels">Self-label values for this post. Effectively content warnings.</param>
    /// <param name="tags">Additional hashtags, in addition to any included in post text and facets.</param>
    /// <param name="createdAt">Client-declared timestamp when this post was originally created. LINQ to BlueSky will populate this if you omit.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>ID info on Post</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task<PostResponse?> PostAsync(
        string text,
        List<Facet>? facets = default,
        Ref? reply =  default,
        Embed? embed = default,
        List<string>? langs = default,
        List<string>? labels = default,
        List<string>? tags = default,
        DateTime createdAt = default,
        CancellationToken cancelToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, $"{nameof(text)} is required");

        string url = $"{BaseUrl}xrpc/com.atproto.repo.createRecord";

        dynamic postRequest = new ExpandoObject();

        postRequest.type = "app.bsky.feed.post";
        postRequest.text = text;
        postRequest.createdAt = createdAt == default ? DateTime.UtcNow : createdAt;

        if (facets is not null)
            postRequest.facets = facets;
        if (reply is not null)
            postRequest.reply = reply;
        if (embed is not null)
            postRequest.embed = embed;
        if (langs is not null)
            postRequest.langs = langs;
        if (labels is not null)
            postRequest.labels = labels;
        if (tags is not null)
            postRequest.tags = tags;

        PostResponse? response = 
            await BlueSkyExecutor.PostAsync<PostResponse>(
                new
                {
                    repo = (Authorizer?.CredentialStore as PasswordCredentials)?.Session?.Did,
                    collection = "app.bsky.feed.post",
                    Record = postRequest
                },
                new Dictionary<string, string>(),
                url,
                cancelToken);

        return response;
    }
}
