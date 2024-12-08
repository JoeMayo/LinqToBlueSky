using LinqToBlueSky.Feed;
using LinqToBlueSky.Provider;
using LinqToBlueSky.Tests.Common;

using System.Globalization;
using System.Linq.Expressions;

namespace LinqToBlueSky.Tests.FeedTests;

[TestClass]
public class FeedRequestProcessorTests
{
    const string BaseUrl = "https://api.bsky.app/";

    public FeedRequestProcessorTests()
    {
        TestCulture.SetCulture();
    }

    [TestMethod]
    public void GetParametersTest()
    {
        FeedRequestProcessor<FeedQuery> reqProc = new();

        var endTime = new DateTime(2020, 8, 30);
        var startTime = new DateTime(2020, 8, 1);
        Expression<Func<FeedQuery, bool>> expression =
            feed =>
                feed.Type == FeedType.Timeline &&
                feed.Algorithm == "123" &&
                feed.Limit == 25 &&
                feed.Cursor == "456";

        var lambdaExpression = expression as LambdaExpression;

        Dictionary<string, string> queryParams = reqProc.GetParameters(lambdaExpression);

        Assert.IsTrue(
            queryParams.Contains(
                new KeyValuePair<string, string>(nameof(FeedQuery.Type), ((int)FeedType.Timeline).ToString(CultureInfo.InvariantCulture))));
        Assert.IsTrue(
            queryParams.Contains(
                new KeyValuePair<string, string>(nameof(FeedQuery.Algorithm), "123")));
        Assert.IsTrue(
            queryParams.Contains(
                new KeyValuePair<string, string>(nameof(FeedQuery.Limit), "25")));
        Assert.IsTrue(
            queryParams.Contains(
                new KeyValuePair<string, string>(nameof(FeedQuery.Cursor), "456")));
    }

    [TestMethod]
    public void BuildUrl_ForTimeline_IncludesParameters()
    {
        const string ExpectedUrl =
            BaseUrl + "xrpc/app.bsky.feed.getTimeline?" +
            "algorithm=123&" +
            "limit=25&" +
            "cursor=456";
        FeedRequestProcessor<FeedQuery> reqProc = new() { BaseUrl = BaseUrl };
        Dictionary<string, string> parameters =
            new()
            {
                    { nameof(FeedQuery.Type), FeedType.Timeline.ToString() },
                    { nameof(FeedQuery.Algorithm), "123" },
                    { nameof(FeedQuery.Limit), "25" },
                    { nameof(FeedQuery.Cursor), "456" }
           };

        Request req = reqProc.BuildUrl(parameters);

        Assert.AreEqual(ExpectedUrl, req.FullUrl);
    }

    [TestMethod]
    public void BuildUrl_WithNullParameters_Throws()
    {
        FeedRequestProcessor<FeedQuery> reqProc = new() { BaseUrl = BaseUrl };

        L2BSkyAssert.Throws<NullReferenceException>(() =>
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            reqProc.BuildUrl(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        });
    }

    [TestMethod]
    public void BuildUrl_WithLimitOver100_Throws()
    {
        var tweetReqProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };
        var parameters =
            new Dictionary<string, string>
            {
                    { nameof(FeedQuery.Type), FeedType.Timeline.ToString() },
                    { nameof(FeedQuery.Limit), "101" },
           };

        ArgumentException ex =
            L2BSkyAssert.Throws<ArgumentOutOfRangeException>(() =>
                tweetReqProc.BuildUrl(parameters));

        Assert.AreEqual(nameof(FeedQuery.Limit), ex.ParamName);
    }

    [TestMethod]
    public void BuildUrl_WithLimitUnder1_Throws()
    {
        var tweetReqProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };
        var parameters =
            new Dictionary<string, string>
            {
                    { nameof(FeedQuery.Type), FeedType.Timeline.ToString() },
                    { nameof(FeedQuery.Limit), "0" },
           };

        ArgumentException ex =
            L2BSkyAssert.Throws<ArgumentOutOfRangeException>(() =>
                tweetReqProc.BuildUrl(parameters));

        Assert.AreEqual(nameof(FeedQuery.Limit), ex.ParamName);
    }

    [TestMethod]
    public void ProcessResults_WithSinglePost_Populates()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };

        List<FeedQuery> results = tweetProc.ProcessResults(SinglePost);

        Assert.IsNotNull(results);
        FeedQuery? feedQuery = results.SingleOrDefault();
        Assert.IsNotNull(feedQuery);
        List<FeedItem>? feedItems = feedQuery.Feed;
        Assert.IsNotNull(feedItems);
        Assert.AreEqual(1, feedItems.Count);
        FeedItem? feedItem = feedItems.FirstOrDefault();
        Assert.IsNotNull(feedItem);
        Post? post = feedItem.Post;
        Assert.IsNotNull(post);
        Record? record = post.Record;
        Assert.IsNotNull(record);
        Assert.AreEqual("Person 1's Post Text", record.Text);
        Reply? reply = feedItem.Reply;
    }
	
    [TestMethod]
    public void ProcessResults_ForFullTimeline_Processes()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };

        List<FeedQuery> results = tweetProc.ProcessResults(TimelinePosts);

        Assert.IsNotNull(results);
        FeedQuery? feedQuery = results.SingleOrDefault();
        Assert.IsNotNull(feedQuery);
        List<FeedItem>? feedItems = feedQuery.Feed;
        Assert.IsNotNull(feedItems);
        Assert.AreEqual(50, feedItems.Count);
        FeedItem? feedItem = feedItems.FirstOrDefault();
        Assert.IsNotNull(feedItem);
        Post? post = feedItem.Post;
        Assert.IsNotNull(post);
        Record? record = post.Record;
        Assert.IsNotNull(record);
        Assert.AreEqual("Sounds like your wrist/arm feeling better?", record.Text);
        Reply? reply = feedItem.Reply;
    }

    [TestMethod]
    public void ProcessResults_Handles_Response_With_No_Results()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };

        List<FeedQuery> results = tweetProc.ProcessResults(ErrorTweet);

        //         Assert.IsNotNull(results);
        //FeedQuery tweetQuery = results.SingleOrDefault();
        //         Assert.IsNotNull(tweetQuery);
        //         List<Post> tweets = tweetQuery.Tweets;
        //         Assert.IsNull(tweets);
    }

    [TestMethod]
    public void ProcessResults_PopulatesInputParameters()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery>()
        {
            BaseUrl = BaseUrl,
            Type = FeedType.Timeline,
            Algorithm = "123",
            Limit = 25,
            Cursor = "456"
        };

        var results = tweetProc.ProcessResults(SinglePost);

        Assert.IsNotNull(results);
        Assert.AreEqual(1, results.Count);
        var feedQuery = results.Single();
        Assert.IsNotNull(feedQuery);
        Assert.AreEqual(FeedType.Timeline, feedQuery.Type);
        //Assert.AreEqual(new DateTime(2020, 12, 31), feedQuery.EndTime);
        //Assert.AreEqual("123", feedQuery.Algorithm);
        //Assert.AreEqual("25", feedQuery.Limit);
        //Assert.AreEqual("456", feedQuery.Cursor);
    }

    [TestMethod]
    public void ProcessResults_With400Error_PopulatesErrorList()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };

        List<FeedQuery> results = tweetProc.ProcessResults(ErrorTweet);

        Assert.IsNotNull(results);
        FeedQuery tweetQuery = results.SingleOrDefault();
        Assert.IsNotNull(tweetQuery);
        //List<BlueSkyError> errors = tweetQuery.Errors;
        //Assert.IsNotNull(errors);
        //Assert.AreEqual(1, errors.Count);
        //BlueSkyError error = errors.FirstOrDefault();
        //Assert.IsNotNull(error);
        //Assert.AreEqual("InvalidRequest", error.Error);
        //Assert.AreEqual("bla bla bla.", error.Message);
    }

    [TestMethod]
    public void ProcessResults_With401Error_PopulatesErrorList()
    {
        var tweetProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };

        List<FeedQuery> results = tweetProc.ProcessResults(ErrorTweet);

        Assert.IsNotNull(results);
        FeedQuery tweetQuery = results.SingleOrDefault();
        Assert.IsNotNull(tweetQuery);
        //List<BlueSkyError> errors = tweetQuery.Errors;
        //Assert.IsNotNull(errors);
        //Assert.AreEqual(1, errors.Count);
        //BlueSkyError error = errors.FirstOrDefault();
        //Assert.IsNotNull(error);
        //Assert.AreEqual("bla bla bla.", error.Message);
    }

    const string SinglePost = @"{
    ""feed"": [
        {
            ""post"": {
                ""uri"": ""at://did:plc:mbpk2abdxtk2j2lctwfpclvv/app.bsky.feed.post/3lclxspblnk2p"",
                ""cid"": ""bafyreidkaflhw4tqnqgceusgdcv6oxrprbynfbkrvjmxkwwe52rsuvn52i"",
                ""author"": {
                    ""did"": ""did:plc:mbpk2abdxtk2j2lctwfpclvv"",
                    ""handle"": ""person1.com"",
                    ""displayName"": ""Person 1"",
                    ""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:mbpk2abdxtk2j2lctwfpclvv/bafkreieivo7h5yqhmore5vd2cu3a64k6fqjsmmz4ghy3b6jdnaepcwurre@jpeg"",
                    ""viewer"": {
                        ""muted"": false,
                        ""blockedBy"": false,
                        ""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardkkptqn2y"",
                        ""followedBy"": ""at://did:plc:mbpk2abdxtk2j2lctwfpclvv/app.bsky.graph.follow/3laqg54mour2j""
                    },
                    ""labels"": [],
                    ""createdAt"": ""2023-04-23T12:04:24.705Z""
                },
                ""record"": {
                    ""$type"": ""app.bsky.feed.post"",
                    ""createdAt"": ""2024-12-06T00:53:27.014Z"",
                    ""langs"": [
                        ""en""
                    ],
                    ""reply"": {
                        ""parent"": {
                            ""cid"": ""bafyreidkjlzicu6k35do6nke4fzbs7dvvl4ov5evzzbalyksrbql7gkrim"",
                            ""uri"": ""at://did:plc:h4qem3f3cz6yvs3r3xvs634g/app.bsky.feed.post/3lclwanfnc22k""
                        },
                        ""root"": {
                            ""cid"": ""bafyreidkjlzicu6k35do6nke4fzbs7dvvl4ov5evzzbalyksrbql7gkrim"",
                            ""uri"": ""at://did:plc:h4qem3f3cz6yvs3r3xvs634g/app.bsky.feed.post/3lclwanfnc22k""
                        }
                    },
                    ""text"": ""Person 1's Post Text""
                },
                ""replyCount"": 0,
                ""repostCount"": 0,
                ""likeCount"": 0,
                ""quoteCount"": 0,
                ""indexedAt"": ""2024-12-06T00:53:27.312Z"",
                ""viewer"": {
                    ""threadMuted"": false,
                    ""embeddingDisabled"": false
                },
                ""labels"": []
            },
            ""reply"": {
                ""root"": {
                    ""$type"": ""app.bsky.feed.defs#postView"",
                    ""uri"": ""at://did:plc:h4qem3f3cz6yvs3r3xvs634g/app.bsky.feed.post/3lclwanfnc22k"",
                    ""cid"": ""bafyreidkjlzicu6k35do6nke4fzbs7dvvl4ov5evzzbalyksrbql7gkrim"",
                    ""author"": {
                        ""did"": ""did:plc:h4qem3f3cz6yvs3r3xvs634g"",
                        ""handle"": ""person2.in"",
                        ""displayName"": ""Person 2"",
                        ""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:h4qem3f3cz6yvs3r3xvs634g/bafkreig7fzbknganveoij56depkm4dxax3yo2cudxf6mase23vccmqn5xa@jpeg"",
                        ""associated"": {
                            ""chat"": {
                                ""allowIncoming"": ""all""
                            }
                        },
                        ""viewer"": {
                            ""muted"": false,
                            ""blockedBy"": false
                        },
                        ""labels"": [],
                        ""createdAt"": ""2023-04-25T01:50:45.503Z""
                    },
                    ""record"": {
                        ""$type"": ""app.bsky.feed.post"",
                        ""createdAt"": ""2024-12-06T00:25:27.328Z"",
                        ""langs"": [
                            ""en""
                        ],
                        ""text"": ""Person 2's original post.""
                    },
                    ""replyCount"": 4,
                    ""repostCount"": 1,
                    ""likeCount"": 13,
                    ""quoteCount"": 0,
                    ""indexedAt"": ""2024-12-06T00:25:27.510Z"",
                    ""viewer"": {
                        ""threadMuted"": false,
                        ""embeddingDisabled"": false
                    },
                    ""labels"": []
                },
                ""parent"": {
                    ""$type"": ""app.bsky.feed.defs#postView"",
                    ""uri"": ""at://did:plc:h4qem3f3cz6yvs3r3xvs634g/app.bsky.feed.post/3lclwanfnc22k"",
                    ""cid"": ""bafyreidkjlzicu6k35do6nke4fzbs7dvvl4ov5evzzbalyksrbql7gkrim"",
                    ""author"": {
                        ""did"": ""did:plc:h4qem3f3cz6yvs3r3xvs634g"",
                        ""handle"": ""person2.in"",
                        ""displayName"": ""Person 2"",
                        ""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:h4qem3f3cz6yvs3r3xvs634g/bafkreig7fzbknganveoij56depkm4dxax3yo2cudxf6mase23vccmqn5xa@jpeg"",
                        ""associated"": {
                            ""chat"": {
                                ""allowIncoming"": ""all""
                            }
                        },
                        ""viewer"": {
                            ""muted"": false,
                            ""blockedBy"": false
                        },
                        ""labels"": [],
                        ""createdAt"": ""2023-04-25T01:50:45.503Z""
                    },
                    ""record"": {
                        ""$type"": ""app.bsky.feed.post"",
                        ""createdAt"": ""2024-12-06T00:25:27.328Z"",
                        ""langs"": [
                            ""en""
                        ],
                        ""text"": ""Person 2's original post.""
                    },
                    ""replyCount"": 4,
                    ""repostCount"": 1,
                    ""likeCount"": 13,
                    ""quoteCount"": 0,
                    ""indexedAt"": ""2024-12-06T00:25:27.510Z"",
                    ""viewer"": {
                        ""threadMuted"": false,
                        ""embeddingDisabled"": false
                    },
                    ""labels"": []
                }
            },
            ""reason"": {
                ""$type"": ""app.bsky.feed.defs#reasonRepost"",
                ""by"": {
                    ""did"": ""did:plc:ecz4yeln5u44knbej3aphym2"",
                    ""handle"": ""person3.com"",
                    ""displayName"": ""Person 3"",
                    ""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ecz4yeln5u44knbej3aphym2/bafkreibyp74l5ieedl6xjkymqbol6tbimdnz2kp2zaxhpvwmjbrg4ikcgi@jpeg"",
                    ""associated"": {
                        ""chat"": {
                            ""allowIncoming"": ""following""
                        }
                    },
                    ""viewer"": {
                        ""muted"": false,
                        ""blockedBy"": false,
                        ""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lcbe2uj3rm2y"",
                        ""followedBy"": ""at://did:plc:ecz4yeln5u44knbej3aphym2/app.bsky.graph.follow/3lcb55ftovh2p""
                    },
                    ""labels"": [
                        {
                            ""src"": ""did:plc:ecz4yeln5u44knbej3aphym2"",
                            ""uri"": ""at://did:plc:ecz4yeln5u44knbej3aphym2/app.bsky.actor.profile/self"",
                            ""cid"": ""bafyreia4a37n6banx2u4jrtw5t62h43vqpdakpf6xanrkkub6xnvquyrge"",
                            ""val"": ""!no-unauthenticated"",
                            ""cts"": ""1970-01-01T00:00:00.000Z""
                        }
                    ],
                    ""createdAt"": ""2023-11-08T18:47:48.957Z""
                },
                ""indexedAt"": ""2024-12-06T00:27:53.913Z""
            }
        }
	]
}";

    const string TimelinePosts = @"{
	""feed"": [
		{
			""post"": {
				""uri"": ""at://did:plc:wiz2tgxt3qvkl6ihuj5v4bzg/app.bsky.feed.post/3lcrdswxyok2z"",
				""cid"": ""bafyreid6oewl5fwepledegib4shhpxbpemmywu52ycwt4s5zhv2abokoee"",
				""author"": {
					""did"": ""did:plc:wiz2tgxt3qvkl6ihuj5v4bzg"",
					""handle"": ""shazwazza.bsky.social"",
					""displayName"": """",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:wiz2tgxt3qvkl6ihuj5v4bzg/bafkreicohrj4n2vw4tlda5sxfhsif5dpeh72xwcprg4ikxcc3okxdsuqeq@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardcqicip2h"",
						""followedBy"": ""at://did:plc:wiz2tgxt3qvkl6ihuj5v4bzg/app.bsky.graph.follow/3larbpmxxy42z""
					},
					""labels"": [],
					""createdAt"": ""2024-11-09T17:52:11.517Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T04:11:38.682Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreig5birru2zohxybhfjfxg2yrg4jkpoqksryx3xvy5zjhkcs7emk7e"",
							""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcra6zjbes2k""
						},
						""root"": {
							""cid"": ""bafyreigjp5n37i3vrnizl4yjdnforsskfrohxw6eerhktrtkx6eeodlm6y"",
							""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcr7xofgok2k""
						}
					},
					""text"": ""Sounds like your wrist/arm feeling better?""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T04:11:39.417Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcr7xofgok2k"",
					""cid"": ""bafyreigjp5n37i3vrnizl4yjdnforsskfrohxw6eerhktrtkx6eeodlm6y"",
					""author"": {
						""did"": ""did:plc:2zzpkvoegaautni7cct2nw52"",
						""handle"": ""james-jackson-south.me"",
						""displayName"": ""James Jackson-South"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2zzpkvoegaautni7cct2nw52/bafkreiei3gdbv6vrwoj26yiv52rglrdof7ohh7apybnh5f63rdr4e6tlz4@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-08T05:47:42.126Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T03:02:42.489Z"",
						""facets"": [
							{
								""$type"": ""app.bsky.richtext.facet"",
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#mention"",
										""did"": ""did:plc:wumt2f7qywo2yrx6jiimu2rj""
									}
								],
								""index"": {
									""byteEnd"": 74,
									""byteStart"": 60
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""dotnet""
									}
								],
								""index"": {
									""byteEnd"": 159,
									""byteStart"": 152
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""oss""
									}
								],
								""index"": {
									""byteEnd"": 164,
									""byteStart"": 160
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Made some major progress handling ICC profile transforms in @sixlabors.com ImageSharp this week. Fingers crossed V4 will allow normalisation on decode. #dotnet #oss""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 3,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T03:02:44.421Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcra6zjbes2k"",
					""cid"": ""bafyreig5birru2zohxybhfjfxg2yrg4jkpoqksryx3xvy5zjhkcs7emk7e"",
					""author"": {
						""did"": ""did:plc:2zzpkvoegaautni7cct2nw52"",
						""handle"": ""james-jackson-south.me"",
						""displayName"": ""James Jackson-South"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2zzpkvoegaautni7cct2nw52/bafkreiei3gdbv6vrwoj26yiv52rglrdof7ohh7apybnh5f63rdr4e6tlz4@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-08T05:47:42.126Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T03:06:49.032Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreibuzjadteojuf6zb4nzzfkhodkahf22ydqsutrxjcbvm3ka7hcpza"",
								""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcra3pjeec2k""
							},
							""root"": {
								""cid"": ""bafyreigjp5n37i3vrnizl4yjdnforsskfrohxw6eerhktrtkx6eeodlm6y"",
								""uri"": ""at://did:plc:2zzpkvoegaautni7cct2nw52/app.bsky.feed.post/3lcr7xofgok2k""
							}
						},
						""text"": ""The great thing is this will be built into the ColorProfileConverter type which means you can use the functions in your own code. This type also handles non ICC profile transforms in a really neat way.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T03:06:50.113Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:2zzpkvoegaautni7cct2nw52"",
					""handle"": ""james-jackson-south.me"",
					""displayName"": ""James Jackson-South"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2zzpkvoegaautni7cct2nw52/bafkreiei3gdbv6vrwoj26yiv52rglrdof7ohh7apybnh5f63rdr4e6tlz4@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-06-08T05:47:42.126Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:zea7wubyosyb5wpclkhgwjtz/app.bsky.feed.post/3lcrdqfar6k2b"",
				""cid"": ""bafyreibjekjnn6dxan4f3iihcrdy36fkeukkvgzfaiyzzwbczymsc7o3g4"",
				""author"": {
					""did"": ""did:plc:zea7wubyosyb5wpclkhgwjtz"",
					""handle"": ""jonsagara.com"",
					""displayName"": ""Jon Sagara"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:zea7wubyosyb5wpclkhgwjtz/bafkreibifefcr3mjlk753unes27gbxqgcntim4hyg3vgdghplkneo235f4@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la5neibf7f2w"",
						""followedBy"": ""at://did:plc:zea7wubyosyb5wpclkhgwjtz/app.bsky.graph.follow/3la5lonirc323""
					},
					""labels"": [],
					""createdAt"": ""2023-08-18T17:39:28.047Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T04:10:12.987Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""ALT: a close up of a man 's face with the website datgif.com written on the bottom"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreigzis7gkcsfzjzusjtdiuma6ebvh4zxml2i4b4anl5p63rbagzgkq""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 61028
							},
							""title"": ""a close up of a man 's face with the website datgif.com written on the bottom"",
							""uri"": ""https://media.tenor.com/3FW6ID5vvlgAAAAC/ahtf-schadenfreude.gif?hh=242&ww=280""
						}
					},
					""langs"": [
						""en""
					],
					""text"": ""When you don’t win the championship, but the team that knocked you out loses in the next round""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://media.tenor.com/3FW6ID5vvlgAAAAC/ahtf-schadenfreude.gif?hh=242&ww=280"",
						""title"": ""a close up of a man 's face with the website datgif.com written on the bottom"",
						""description"": ""ALT: a close up of a man 's face with the website datgif.com written on the bottom"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:zea7wubyosyb5wpclkhgwjtz/bafkreigzis7gkcsfzjzusjtdiuma6ebvh4zxml2i4b4anl5p63rbagzgkq@jpeg""
					}
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T04:10:15.010Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:wtwi6qsnimx7eu2s7leb3gny/app.bsky.feed.post/3lcnmdfc3vk2t"",
				""cid"": ""bafyreihc3r5fvc7fakdei7msdtphq2psgtznrdcil2jvf72omulatmrn5e"",
				""author"": {
					""did"": ""did:plc:wtwi6qsnimx7eu2s7leb3gny"",
					""handle"": ""markmorow.com"",
					""displayName"": ""Mark Morowczynski "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:wtwi6qsnimx7eu2s7leb3gny/bafkreihhtwjer4bdnnck4fyftldp4a6eyhccn4qgik5ohgfs3jpd4l2wd4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-05-02T14:51:59.443Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-06T16:33:21.395Z"",
					""facets"": [
						{
							""$type"": ""app.bsky.richtext.facet"",
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://isc.sans.edu/diary/Credential+Guard+and+Kerberos+delegation/31488/""
								}
							],
							""index"": {
								""byteEnd"": 96,
								""byteStart"": 68
							}
						},
						{
							""$type"": ""app.bsky.richtext.facet"",
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://learn.microsoft.com/en-us/windows/security/identity-protection/credential-guard/""
								}
							],
							""index"": {
								""byteEnd"": 205,
								""byteStart"": 170
							}
						},
						{
							""$type"": ""app.bsky.richtext.facet"",
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#mention"",
									""did"": ""did:plc:2h5yo2dhkdo6r3tokotdqu3v""
								}
							],
							""index"": {
								""byteEnd"": 227,
								""byteStart"": 216
							}
						},
						{
							""$type"": ""app.bsky.richtext.facet"",
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""infosec""
								}
							],
							""index"": {
								""byteEnd"": 286,
								""byteStart"": 278
							}
						}
					],
					""langs"": [
						""en""
					],
					""tags"": [],
					""text"": ""A good write up on how Credential Guard prevented an common attack. isc.sans.edu/diary/Creden.... If you haven't looked at this in a while, now is a great time to start. learn.microsoft.com/en-us/window.... Kudos to @syfuhs.net and the team for doing all the hard work on this. #infosec""
				},
				""replyCount"": 1,
				""repostCount"": 12,
				""likeCount"": 36,
				""quoteCount"": 1,
				""indexedAt"": ""2024-12-06T16:33:21.625Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:m3p5j3o66yghzlkbwnbgmcsi"",
					""handle"": ""merill.net"",
					""displayName"": ""Merill Fernando 💚"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:m3p5j3o66yghzlkbwnbgmcsi/bafkreid4iexettc7kwclgh52jj4p5tbxmu6v4zywicdp2berr4jfvdiibm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3l7s4txisow2m"",
						""followedBy"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.graph.follow/3l7rtl3s4h42u""
					},
					""labels"": [],
					""createdAt"": ""2023-04-23T22:25:56.230Z""
				},
				""indexedAt"": ""2024-12-08T04:09:17.319Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.feed.post/3lcrbvizspc2j"",
				""cid"": ""bafyreicuognuqoiuj5qbqz4cbbgqg2kz365ip7tphnhlpghthgmd7gpnf4"",
				""author"": {
					""did"": ""did:plc:chh3plzyhfyffr6wdpwsqli7"",
					""handle"": ""wilcantrell.bsky.social"",
					""displayName"": ""Wil Cantrell"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:chh3plzyhfyffr6wdpwsqli7/bafkreid4h3l7fvpfqsw6umq4g45zp74geqrlgdmlg7q4bmfbgn3n3mkzxy@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbahsn2bdb2y"",
						""followedBy"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.graph.follow/3lbah5kbaw32g""
					},
					""labels"": [],
					""createdAt"": ""2024-11-12T19:23:56.144Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T03:37:17.242Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreido3mnac47nao7yfvpvg2rgmxuyem4ruaabmozljtqmkaaezzck2i"",
							""uri"": ""at://did:plc:fau6fbov72slptdpb2vxqi32/app.bsky.feed.post/3lcnriwqzcc2v""
						},
						""root"": {
							""cid"": ""bafyreido3mnac47nao7yfvpvg2rgmxuyem4ruaabmozljtqmkaaezzck2i"",
							""uri"": ""at://did:plc:fau6fbov72slptdpb2vxqi32/app.bsky.feed.post/3lcnriwqzcc2v""
						}
					},
					""text"": ""Loving this work!""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T03:37:18.112Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:fau6fbov72slptdpb2vxqi32/app.bsky.feed.post/3lcnriwqzcc2v"",
					""cid"": ""bafyreido3mnac47nao7yfvpvg2rgmxuyem4ruaabmozljtqmkaaezzck2i"",
					""author"": {
						""did"": ""did:plc:fau6fbov72slptdpb2vxqi32"",
						""handle"": ""theprydonian.bsky.social"",
						""displayName"": ""James Johnson"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreiev3i56lbdwb5phaqhnvo44rn6pw2oxmcmc27fkmdke65hhhfb7sm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-09-20T09:20:31.147Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-06T18:05:55.810Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 786548
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 862628
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 762655
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Time Lord Victorious Daleks!\n\nExecutioner, Strategist and Drone.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 5,
					""likeCount"": 49,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-06T18:06:08.824Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:fau6fbov72slptdpb2vxqi32/app.bsky.feed.post/3lcnriwqzcc2v"",
					""cid"": ""bafyreido3mnac47nao7yfvpvg2rgmxuyem4ruaabmozljtqmkaaezzck2i"",
					""author"": {
						""did"": ""did:plc:fau6fbov72slptdpb2vxqi32"",
						""handle"": ""theprydonian.bsky.social"",
						""displayName"": ""James Johnson"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreiev3i56lbdwb5phaqhnvo44rn6pw2oxmcmc27fkmdke65hhhfb7sm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-09-20T09:20:31.147Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-06T18:05:55.810Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 786548
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 862628
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 762655
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Time Lord Victorious Daleks!\n\nExecutioner, Strategist and Drone.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreihqbobzgljksqwcfuakppcxk2bybg7qfjystill66nfjjyuz4gp7m@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibfiektkz73mgnouruhblikovqgcxf2pzjiwlaftnusdebdzo3uja@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:fau6fbov72slptdpb2vxqi32/bafkreibkxewvppqapi46aewx4nsg3jyjfeen2ixtiqlimv7r3laxezqc44@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 5,
					""likeCount"": 49,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-06T18:06:08.824Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.feed.post/3lcrbtkrtk22j"",
				""cid"": ""bafyreicoeg4vimn66hbz37twksbgyxnjj5y26jc5mqqjhpf7j6jglsobwa"",
				""author"": {
					""did"": ""did:plc:chh3plzyhfyffr6wdpwsqli7"",
					""handle"": ""wilcantrell.bsky.social"",
					""displayName"": ""Wil Cantrell"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:chh3plzyhfyffr6wdpwsqli7/bafkreid4h3l7fvpfqsw6umq4g45zp74geqrlgdmlg7q4bmfbgn3n3mkzxy@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbahsn2bdb2y"",
						""followedBy"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.graph.follow/3lbah5kbaw32g""
					},
					""labels"": [],
					""createdAt"": ""2024-11-12T19:23:56.144Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T03:36:11.967Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""YouTube video by BBC"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreiauwmwj7i6cphkwsmqittr4k37xejdazr7cfk76q2oj7hpypwfzmu""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 380751
							},
							""title"": ""Doctor Who BBC One Christmas Ident 2009 with David Tennant"",
							""uri"": ""https://youtu.be/_kXvs0aKSt0?si=_mfBwX8c88HqPR7y""
						}
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://youtu.be/_kXvs0aKSt0?si=_mfBwX8c88HqPR7y""
								}
							],
							""index"": {
								""byteEnd"": 199,
								""byteStart"": 175
							}
						}
					],
					""langs"": [
						""en""
					],
					""text"": ""Can anyone recall when an American broadcast network featured a sci fi franchise in a Christmas ident? I vaguely recall NBC doing something with \""V\"", but I'm probably wrong.\n\nyoutu.be/_kXvs0aKSt0?...""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://youtu.be/_kXvs0aKSt0?si=_mfBwX8c88HqPR7y"",
						""title"": ""Doctor Who BBC One Christmas Ident 2009 with David Tennant"",
						""description"": ""YouTube video by BBC"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:chh3plzyhfyffr6wdpwsqli7/bafkreiauwmwj7i6cphkwsmqittr4k37xejdazr7cfk76q2oj7hpypwfzmu@jpeg""
					}
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T03:36:15.111Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcrbohzenc27"",
				""cid"": ""bafyreihvo6c2zp5t5ky5psbcvxoekpgzlysieczez4rfe2kt6pk4bgzmgq"",
				""author"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T03:33:21.298Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiahwe6pzphj3hsghqhw6pqenqwxklyej6x4dhamu7psdqsg6s4xxm"",
							""uri"": ""at://did:plc:wepubn5jiq74vznap2ia2osh/app.bsky.feed.post/3lcqp4n4a6c2w""
						},
						""root"": {
							""cid"": ""bafyreia7hj2q5ddthn6btvfzpovvxosrnomydsiurwe4a2wx3jzut2o2oi"",
							""uri"": ""at://did:plc:khckhie63wli6k7qf2szn7zh/app.bsky.feed.post/3lcqmub3nn22t""
						}
					},
					""text"": ""This whole thread was a rollercoaster.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T03:33:21.117Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:khckhie63wli6k7qf2szn7zh/app.bsky.feed.post/3lcqmub3nn22t"",
					""cid"": ""bafyreia7hj2q5ddthn6btvfzpovvxosrnomydsiurwe4a2wx3jzut2o2oi"",
					""author"": {
						""did"": ""did:plc:khckhie63wli6k7qf2szn7zh"",
						""handle"": ""donnietaylor.bsky.social"",
						""displayName"": ""Donnie Taylor [MVP - Azure/PowerShell]"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:khckhie63wli6k7qf2szn7zh/bafkreibluemjy5itsaprwn5t32f3z5iymqbzoeq62l4qwpd3bm6h7ed7mq@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-19T05:37:24.438Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T21:20:46.780Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 527,
										""width"": 946
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreifht43bf2k2gosajpzppwf3qfqktg2vvmw6jfhhm36qle73epqdom""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 225906
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""pwsh""
									}
								],
								""index"": {
									""byteEnd"": 90,
									""byteStart"": 85
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""powershell""
									}
								],
								""index"": {
									""byteEnd"": 102,
									""byteStart"": 91
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Friendly reminder - Get is optional....  \n\nUnless someone else will see your code. \n\n#pwsh #powershell""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:khckhie63wli6k7qf2szn7zh/bafkreifht43bf2k2gosajpzppwf3qfqktg2vvmw6jfhhm36qle73epqdom@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:khckhie63wli6k7qf2szn7zh/bafkreifht43bf2k2gosajpzppwf3qfqktg2vvmw6jfhhm36qle73epqdom@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 527,
									""width"": 946
								}
							}
						]
					},
					""replyCount"": 4,
					""repostCount"": 2,
					""likeCount"": 8,
					""quoteCount"": 2,
					""indexedAt"": ""2024-12-07T21:20:50.013Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:wepubn5jiq74vznap2ia2osh/app.bsky.feed.post/3lcqp4n4a6c2w"",
					""cid"": ""bafyreiahwe6pzphj3hsghqhw6pqenqwxklyej6x4dhamu7psdqsg6s4xxm"",
					""author"": {
						""did"": ""did:plc:wepubn5jiq74vznap2ia2osh"",
						""handle"": ""posh.guru"",
						""displayName"": ""Justin Grote"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:wepubn5jiq74vznap2ia2osh/bafkreiafflnubq54biew7t3akku6c62cmi5cbtxvyoqwljui7imr5x4c64@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-09-24T15:16:02.877Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:01:15.300Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 207,
										""width"": 437
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreicwvkaqfmfs6fccdoflqg4m2ermb7vyujczv7uxllacgw6ylht7hu""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 55176
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreigs2dyqh4b6wbnnd4ly32bugv32h2ldmk7envakgpud2wm7pwyas4"",
								""uri"": ""at://did:plc:wqg3xumuue3v5nyd7fokoon3/app.bsky.feed.post/3lcqohcoikk2f""
							},
							""root"": {
								""cid"": ""bafyreia7hj2q5ddthn6btvfzpovvxosrnomydsiurwe4a2wx3jzut2o2oi"",
								""uri"": ""at://did:plc:khckhie63wli6k7qf2szn7zh/app.bsky.feed.post/3lcqmub3nn22t""
							}
						},
						""text"": ""Meh, I'll take the 5ms for occasional prompt and interactive usage :)""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:wepubn5jiq74vznap2ia2osh/bafkreicwvkaqfmfs6fccdoflqg4m2ermb7vyujczv7uxllacgw6ylht7hu@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:wepubn5jiq74vznap2ia2osh/bafkreicwvkaqfmfs6fccdoflqg4m2ermb7vyujczv7uxllacgw6ylht7hu@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 207,
									""width"": 437
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 4,
					""quoteCount"": 1,
					""indexedAt"": ""2024-12-07T22:01:17.513Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:wqg3xumuue3v5nyd7fokoon3"",
					""handle"": ""mikefrobbins.com"",
					""displayName"": ""Mike F. Robbins"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:wqg3xumuue3v5nyd7fokoon3/bafkreibqaudtaidzf3p2na5tfd7bky7dlfhqxzehoanahqdykle44s6iqa@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-07-01T20:15:25.777Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcrbijdzi227"",
				""cid"": ""bafyreicj5hivt7jfihjhxdxzzxy2okajndbmkuxebqcvmk5gx3brjdvg5e"",
				""author"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T03:30:01.368Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreih4nnbrap3dfvk34nytquhba77hx4d2exid7k6hjoic7ze2ny7jlu"",
							""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.feed.post/3lcq6xsjpc22c""
						},
						""root"": {
							""cid"": ""bafyreih4nnbrap3dfvk34nytquhba77hx4d2exid7k6hjoic7ze2ny7jlu"",
							""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.feed.post/3lcq6xsjpc22c""
						}
					},
					""text"": ""Is that food for ants?""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T03:30:00.812Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.feed.post/3lcq6xsjpc22c"",
					""cid"": ""bafyreih4nnbrap3dfvk34nytquhba77hx4d2exid7k6hjoic7ze2ny7jlu"",
					""author"": {
						""did"": ""did:plc:gbvytj44fa43qafbzckqw4o5"",
						""handle"": ""mikeelgan.bsky.social"",
						""displayName"": ""Mike Elgan"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreigs5lkatjdsrjusxbplnusdyl6txzbvxeisaxzmcpemvcnvjlsn3q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:gbvytj44fa43qafbzckqw4o5"",
								""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.actor.profile/self"",
								""cid"": ""bafyreiasmj2xb6n5tn2thxo24v4og5qkbiusgecmd7vbhssnsxnexpwjkm"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-05-15T10:54:23.312Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T17:12:13.342Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 809892
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#link"",
										""uri"": ""https://gastronomad.net/oaxaca""
									}
								],
								""index"": {
									""byteEnd"": 93,
									""byteStart"": 71
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""The Oaxaca Gastronomad Experience starts today!! (Wish you were here!) gastronomad.net/oaxaca""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 11,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T17:12:47.226Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.feed.post/3lcq6xsjpc22c"",
					""cid"": ""bafyreih4nnbrap3dfvk34nytquhba77hx4d2exid7k6hjoic7ze2ny7jlu"",
					""author"": {
						""did"": ""did:plc:gbvytj44fa43qafbzckqw4o5"",
						""handle"": ""mikeelgan.bsky.social"",
						""displayName"": ""Mike Elgan"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreigs5lkatjdsrjusxbplnusdyl6txzbvxeisaxzmcpemvcnvjlsn3q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:gbvytj44fa43qafbzckqw4o5"",
								""uri"": ""at://did:plc:gbvytj44fa43qafbzckqw4o5/app.bsky.actor.profile/self"",
								""cid"": ""bafyreiasmj2xb6n5tn2thxo24v4og5qkbiusgecmd7vbhssnsxnexpwjkm"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-05-15T10:54:23.312Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T17:12:13.342Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 809892
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#link"",
										""uri"": ""https://gastronomad.net/oaxaca""
									}
								],
								""index"": {
									""byteEnd"": 93,
									""byteStart"": 71
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""The Oaxaca Gastronomad Experience starts today!! (Wish you were here!) gastronomad.net/oaxaca""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:gbvytj44fa43qafbzckqw4o5/bafkreiarqua6lkuicv3b2hfljmp34l6mmstsf2muatlgrtyrnftwejkulm@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 11,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T17:12:47.226Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:7iqavrhpeznxztbhopzuhxlz/app.bsky.feed.post/3lcpqhms6ok2j"",
				""cid"": ""bafyreicnn22j2me2m34vplatoe2n4565hxhhbeg7wu5uqbwhnlq62gpnve"",
				""author"": {
					""did"": ""did:plc:7iqavrhpeznxztbhopzuhxlz"",
					""handle"": ""woodruff.dev"",
					""displayName"": ""Chris Woody Woodruff"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:7iqavrhpeznxztbhopzuhxlz/bafkreiapztdlckbs5k5uhbl6l6wy7msmuuwjq5j5dv5mdzdm5ystmcmeme@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jzfdwufikh2q"",
						""followedBy"": ""at://did:plc:7iqavrhpeznxztbhopzuhxlz/app.bsky.graph.follow/3jz3qyjoyos27""
					},
					""labels"": [],
					""createdAt"": ""2023-04-30T20:18:01.721Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T12:52:38.074Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""YouTube video by NDC Conferences"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreiefn3ten2rvfcj2jt7qtwqosqw73l52q3cjxoe2sh4zwjrv4j2rze""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 254497
							},
							""title"": ""Rust-ifying Your C# Codebase: A Tale of Adventure and Transformation - Chris Woody Woodruff"",
							""uri"": ""https://youtu.be/K4DP21OlktM?si=Eu7Eix3FRTBOi1Bj""
						}
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://youtu.be/K4DP21OlktM?si=Eu7Eix3FRTBOi1Bj""
								}
							],
							""index"": {
								""byteEnd"": 276,
								""byteStart"": 252
							}
						},
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""RustLang""
								}
							],
							""index"": {
								""byteEnd"": 286,
								""byteStart"": 277
							}
						},
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""DotNet""
								}
							],
							""index"": {
								""byteEnd"": 294,
								""byteStart"": 287
							}
						}
					],
					""langs"": [
						""en""
					],
					""text"": ""My journey from C# to Rust! At the Copenhagen Developers Festival, I shared \""Rust-ifying Your C# Codebase: A Tale of Adventure and Transformation.\"" Dive into the safety, concurrency, and performance Rust offers and see how it expands your dev toolkit! youtu.be/K4DP21OlktM?... #RustLang #DotNet""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://youtu.be/K4DP21OlktM?si=Eu7Eix3FRTBOi1Bj"",
						""title"": ""Rust-ifying Your C# Codebase: A Tale of Adventure and Transformation - Chris Woody Woodruff"",
						""description"": ""YouTube video by NDC Conferences"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:7iqavrhpeznxztbhopzuhxlz/bafkreiefn3ten2rvfcj2jt7qtwqosqw73l52q3cjxoe2sh4zwjrv4j2rze@jpeg""
					}
				},
				""replyCount"": 1,
				""repostCount"": 3,
				""likeCount"": 14,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-07T12:52:39.416Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:d4fvkojrjtuj6mlfq5fneccu"",
					""handle"": ""savranweb.bsky.social"",
					""displayName"": ""Hasan Savran"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:d4fvkojrjtuj6mlfq5fneccu/bafkreia7kujus5njzhiq37uui7sxkjngbrl67tv6pzdx6mqfyqsdaxlwby@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3k55yljjoex2c"",
						""followedBy"": ""at://did:plc:d4fvkojrjtuj6mlfq5fneccu/app.bsky.graph.follow/3k53crvdgh22b""
					},
					""labels"": [],
					""createdAt"": ""2023-08-14T13:06:06.562Z""
				},
				""indexedAt"": ""2024-12-08T03:13:16.611Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:jjld7ws2qgyls32p5efdu4qm/app.bsky.feed.post/3lcpxntbzp22f"",
				""cid"": ""bafyreifxi53thno2kjibuuigeydc7ltt7j7bahnt4yulgazkumbbgptacm"",
				""author"": {
					""did"": ""did:plc:jjld7ws2qgyls32p5efdu4qm"",
					""handle"": ""quinnypig.com"",
					""displayName"": ""Corey Quinn"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:jjld7ws2qgyls32p5efdu4qm/bafkreihvkzrycs4u475fonrremytlzzbp6fxbza2nepcnziizirphllqge@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-04-27T22:30:25.852Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T15:01:22.405Z"",
					""langs"": [
						""en""
					],
					""text"": ""Make no mistake, if United Health Group’s stock had gone *up* this week, corporate reports would start including what time the CEO goes to the gym and what route they take to get there.""
				},
				""replyCount"": 2,
				""repostCount"": 8,
				""likeCount"": 72,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-07T15:01:22.711Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:h4ythqntzatvpfzgvdfwc422"",
					""handle"": ""dyathinkesaurus.bsky.social"",
					""displayName"": ""dyathinkhesaurus"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:h4ythqntzatvpfzgvdfwc422/bafkreibw34mbsxzy7f5fafs2raltv4ltgbber7x2l6tmdgxzln5lsq2ri4@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3laymdlzx252a"",
						""followedBy"": ""at://did:plc:h4ythqntzatvpfzgvdfwc422/app.bsky.graph.follow/3layjopi4p42v""
					},
					""labels"": [
						{
							""src"": ""did:plc:h4ythqntzatvpfzgvdfwc422"",
							""uri"": ""at://did:plc:h4ythqntzatvpfzgvdfwc422/app.bsky.actor.profile/self"",
							""cid"": ""bafyreifqxfvegeurobhd27acjq4kztmbjnzjxisbxoemdvm7f4yov5t7se"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-14T10:03:24.863Z""
						}
					],
					""createdAt"": ""2024-11-14T10:03:26.314Z""
				},
				""indexedAt"": ""2024-12-08T03:03:27.919Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.feed.post/3lcr7w5rvdk2l"",
				""cid"": ""bafyreia2kfgxovfx6nwkdnwnhg7cpxswtowx26ixt2l7a5vipm6zzbg4kq"",
				""author"": {
					""did"": ""did:plc:ozekxfdo5a4zwv2rm6u6so26"",
					""handle"": ""lance.boston"",
					""displayName"": ""ᒪᗩᑎᑕE"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreihnqxqjocizjvebfrvyjespaj4gqhql7dcpyxfzymrugeoc2kl5nm@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jytx5sdbey2u"",
						""followedBy"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.graph.follow/3jyrsbywfdx2f""
					},
					""labels"": [],
					""createdAt"": ""2023-04-25T19:11:37.468Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T03:01:51.516Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.images"",
						""images"": [
							{
								""alt"": """",
								""aspectRatio"": {
									""height"": 1904,
									""width"": 2000
								},
								""image"": {
									""$type"": ""blob"",
									""ref"": {
										""$link"": ""bafkreibfeb27sokwmlp2xbkr4qlg6xsumwyxgi3umzhxgxxngo42g443bm""
									},
									""mimeType"": ""image/jpeg"",
									""size"": 696777
								}
							}
						]
					},
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiaxcw2zqibiz3jpldr4dq5jfpuubmoordksomoyxco2sgo2e7xq3i"",
							""uri"": ""at://did:plc:gi6angq6pvstdm6jly5ql3yc/app.bsky.feed.post/3lcr5ops5uk23""
						},
						""root"": {
							""cid"": ""bafyreifqr6qdm6scfduazdqakx657ccoun25m2si53atqp4v3mkb4i4fxq"",
							""uri"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.feed.post/3lc4tg3fqxs2o""
						}
					},
					""text"": ""I took it to the next level and did this...""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.images#view"",
					""images"": [
						{
							""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreibfeb27sokwmlp2xbkr4qlg6xsumwyxgi3umzhxgxxngo42g443bm@jpeg"",
							""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreibfeb27sokwmlp2xbkr4qlg6xsumwyxgi3umzhxgxxngo42g443bm@jpeg"",
							""alt"": """",
							""aspectRatio"": {
								""height"": 1904,
								""width"": 2000
							}
						}
					]
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T03:01:52.920Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.feed.post/3lc4tg3fqxs2o"",
					""cid"": ""bafyreifqr6qdm6scfduazdqakx657ccoun25m2si53atqp4v3mkb4i4fxq"",
					""author"": {
						""did"": ""did:plc:ozekxfdo5a4zwv2rm6u6so26"",
						""handle"": ""lance.boston"",
						""displayName"": ""ᒪᗩᑎᑕE"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreihnqxqjocizjvebfrvyjespaj4gqhql7dcpyxfzymrugeoc2kl5nm@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jytx5sdbey2u"",
							""followedBy"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.graph.follow/3jyrsbywfdx2f""
						},
						""labels"": [],
						""createdAt"": ""2023-04-25T19:11:37.468Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-11-30T00:24:52.480Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""A UniFi UDM Pro and UniFi 16 port PoE rack unit with new wiring. The patch cables are nicely aligned so the seem vertical wherever possible. It's not perfect by a professional's standard, but this homelab guy is pretty happy. "",
									""aspectRatio"": {
										""height"": 1697,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreia7kntp2enatjvzxcn53kdxdxliufbeofwnbt2oijprdwkberovi4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 606365
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""Homelab""
									}
								],
								""index"": {
									""byteEnd"": 76,
									""byteStart"": 68
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""Unifi""
									}
								],
								""index"": {
									""byteEnd"": 83,
									""byteStart"": 77
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""udmpro""
									}
								],
								""index"": {
									""byteEnd"": 91,
									""byteStart"": 84
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Is it too nerdy that I decided to rewire a part of my rack tonight? #Homelab #Unifi #udmpro""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreia7kntp2enatjvzxcn53kdxdxliufbeofwnbt2oijprdwkberovi4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreia7kntp2enatjvzxcn53kdxdxliufbeofwnbt2oijprdwkberovi4@jpeg"",
								""alt"": ""A UniFi UDM Pro and UniFi 16 port PoE rack unit with new wiring. The patch cables are nicely aligned so the seem vertical wherever possible. It's not perfect by a professional's standard, but this homelab guy is pretty happy. "",
								""aspectRatio"": {
									""height"": 1697,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 4,
					""repostCount"": 0,
					""likeCount"": 18,
					""quoteCount"": 1,
					""indexedAt"": ""2024-11-30T00:24:54.315Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:gi6angq6pvstdm6jly5ql3yc/app.bsky.feed.post/3lcr5ops5uk23"",
					""cid"": ""bafyreiaxcw2zqibiz3jpldr4dq5jfpuubmoordksomoyxco2sgo2e7xq3i"",
					""author"": {
						""did"": ""did:plc:gi6angq6pvstdm6jly5ql3yc"",
						""handle"": ""emphaticallyfrank.bsky.social"",
						""displayName"": ""Frank"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:gi6angq6pvstdm6jly5ql3yc/bafkreidv2yfkolds5q6lnoeblgo3g7ziln24c4zdbblgt7xfse7lwfuhwm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:gi6angq6pvstdm6jly5ql3yc"",
								""uri"": ""at://did:plc:gi6angq6pvstdm6jly5ql3yc/app.bsky.actor.profile/self"",
								""cid"": ""bafyreih44hdpl6thbftcze4gzh5pschcgox73pldf3npejcs5qbkry6b3u"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-11-24T19:39:34.443Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:21:54.483Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreifqr6qdm6scfduazdqakx657ccoun25m2si53atqp4v3mkb4i4fxq"",
								""uri"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.feed.post/3lc4tg3fqxs2o""
							},
							""root"": {
								""cid"": ""bafyreifqr6qdm6scfduazdqakx657ccoun25m2si53atqp4v3mkb4i4fxq"",
								""uri"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.feed.post/3lc4tg3fqxs2o""
							}
						},
						""text"": ""As long as you don't have to go to work the next day it's all good.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:21:55.023Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:ozekxfdo5a4zwv2rm6u6so26"",
					""handle"": ""lance.boston"",
					""displayName"": ""ᒪᗩᑎᑕE"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ozekxfdo5a4zwv2rm6u6so26/bafkreihnqxqjocizjvebfrvyjespaj4gqhql7dcpyxfzymrugeoc2kl5nm@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jytx5sdbey2u"",
						""followedBy"": ""at://did:plc:ozekxfdo5a4zwv2rm6u6so26/app.bsky.graph.follow/3jyrsbywfdx2f""
					},
					""labels"": [],
					""createdAt"": ""2023-04-25T19:11:37.468Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.feed.post/3lcr7nnfbp22u"",
				""cid"": ""bafyreiaaqtxbnbvxo7xw3n4epra23c3vd64e7lccinkvwgjax7wupgezke"",
				""author"": {
					""did"": ""did:plc:chh3plzyhfyffr6wdpwsqli7"",
					""handle"": ""wilcantrell.bsky.social"",
					""displayName"": ""Wil Cantrell"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:chh3plzyhfyffr6wdpwsqli7/bafkreid4h3l7fvpfqsw6umq4g45zp74geqrlgdmlg7q4bmfbgn3n3mkzxy@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbahsn2bdb2y"",
						""followedBy"": ""at://did:plc:chh3plzyhfyffr6wdpwsqli7/app.bsky.graph.follow/3lbah5kbaw32g""
					},
					""labels"": [],
					""createdAt"": ""2024-11-12T19:23:56.144Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:57:05.891Z"",
					""langs"": [
						""en""
					],
					""text"": ""I want to engage more on this platform, but can't seem to find a ditch. I'm taking Blender tutorials again. I'm programming solutions in MAUI, Flutter, and Blazor. And practicing a heck of a lot on my Roland Fantom and Juno synths lately.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:57:06.310Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:mjp2lenosijv5decc646uqcn/app.bsky.feed.post/3lcr7naydkc2h"",
				""cid"": ""bafyreietdoycb7qonm7zs3rgrdgawiumji4nctyegzf3i3e3gedjkkvoeq"",
				""author"": {
					""did"": ""did:plc:mjp2lenosijv5decc646uqcn"",
					""handle"": ""adefwebserver.com"",
					""displayName"": ""Michael Washington"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:mjp2lenosijv5decc646uqcn/bafkreidmwyqev7rlbkfip2fec3nt7tmz2u7lmid73tpasxs6b5j4nsv6qe@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jyrkb7w2lp23"",
						""followedBy"": ""at://did:plc:mjp2lenosijv5decc646uqcn/app.bsky.graph.follow/3jys7y4ruci23""
					},
					""labels"": [],
					""createdAt"": ""2023-06-15T15:41:16.112Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:56:52.886Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreibicnaqji2lwjhajuikkktfo5unexkpp3zqsufxpfilzs2gvf4ypi"",
							""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqswtywxk2t""
						},
						""root"": {
							""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
							""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c""
						}
					},
					""text"": ""I am prepared to do everything I can to oppose fascism.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:56:54.415Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c"",
					""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
					""author"": {
						""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
						""handle"": ""georgetakei.bsky.social"",
						""displayName"": ""George Takei"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-30T19:45:03.507Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:57:10.264Z"",
						""langs"": [
							""en""
						],
						""text"": ""On this date in 1941, Pearl Harbor was attacked by the imperial forces of Japan. The loss of life and destruction was horrific, and it pulled the U.S. into the war. \n\nFor Japanese Americans, it was a terrifying time. Overnight, we became the “enemy within” because we were of Japanese descent. /1""
					},
					""replyCount"": 158,
					""repostCount"": 1248,
					""likeCount"": 6378,
					""quoteCount"": 85,
					""indexedAt"": ""2024-12-07T22:57:10.412Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqswtywxk2t"",
					""cid"": ""bafyreibicnaqji2lwjhajuikkktfo5unexkpp3zqsufxpfilzs2gvf4ypi"",
					""author"": {
						""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
						""handle"": ""georgetakei.bsky.social"",
						""displayName"": ""George Takei"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-30T19:45:03.507Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:09:36.175Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreifsw4blt2iwq44ilojy5m7pitsfmslqrzgtfbnk6afnyri53hxa3i"",
								""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqscm5n452q""
							},
							""root"": {
								""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
								""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c""
							}
						},
						""text"": ""As someone who spent his childhood in such camps, I implore you: We must not let history repeat.\n\nFascism has come before to America. It destroyed so many lives. We can resist it today and not repeat the mistakes of the past.  / end""
					},
					""replyCount"": 97,
					""repostCount"": 529,
					""likeCount"": 4059,
					""quoteCount"": 6,
					""indexedAt"": ""2024-12-07T23:09:36.423Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
					""handle"": ""georgetakei.bsky.social"",
					""displayName"": ""George Takei"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-04-30T19:45:03.507Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqswtywxk2t"",
				""cid"": ""bafyreibicnaqji2lwjhajuikkktfo5unexkpp3zqsufxpfilzs2gvf4ypi"",
				""author"": {
					""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
					""handle"": ""georgetakei.bsky.social"",
					""displayName"": ""George Takei"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-04-30T19:45:03.507Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T23:09:36.175Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreifsw4blt2iwq44ilojy5m7pitsfmslqrzgtfbnk6afnyri53hxa3i"",
							""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqscm5n452q""
						},
						""root"": {
							""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
							""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c""
						}
					},
					""text"": ""As someone who spent his childhood in such camps, I implore you: We must not let history repeat.\n\nFascism has come before to America. It destroyed so many lives. We can resist it today and not repeat the mistakes of the past.  / end""
				},
				""replyCount"": 97,
				""repostCount"": 529,
				""likeCount"": 4059,
				""quoteCount"": 6,
				""indexedAt"": ""2024-12-07T23:09:36.423Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:mjp2lenosijv5decc646uqcn"",
					""handle"": ""adefwebserver.com"",
					""displayName"": ""Michael Washington"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:mjp2lenosijv5decc646uqcn/bafkreidmwyqev7rlbkfip2fec3nt7tmz2u7lmid73tpasxs6b5j4nsv6qe@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jyrkb7w2lp23"",
						""followedBy"": ""at://did:plc:mjp2lenosijv5decc646uqcn/app.bsky.graph.follow/3jys7y4ruci23""
					},
					""labels"": [],
					""createdAt"": ""2023-06-15T15:41:16.112Z""
				},
				""indexedAt"": ""2024-12-08T02:54:24.538Z""
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c"",
					""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
					""author"": {
						""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
						""handle"": ""georgetakei.bsky.social"",
						""displayName"": ""George Takei"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-30T19:45:03.507Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:57:10.264Z"",
						""langs"": [
							""en""
						],
						""text"": ""On this date in 1941, Pearl Harbor was attacked by the imperial forces of Japan. The loss of life and destruction was horrific, and it pulled the U.S. into the war. \n\nFor Japanese Americans, it was a terrifying time. Overnight, we became the “enemy within” because we were of Japanese descent. /1""
					},
					""replyCount"": 158,
					""repostCount"": 1248,
					""likeCount"": 6378,
					""quoteCount"": 85,
					""indexedAt"": ""2024-12-07T22:57:10.412Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqscm5n452q"",
					""cid"": ""bafyreifsw4blt2iwq44ilojy5m7pitsfmslqrzgtfbnk6afnyri53hxa3i"",
					""author"": {
						""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
						""handle"": ""georgetakei.bsky.social"",
						""displayName"": ""George Takei"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-30T19:45:03.507Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:58:16.789Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreihz4z5oevoftfcmhkajoy7cmilfwlsckdwmvul4h346vzb43fvyue"",
								""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsb5fu4d22""
							},
							""root"": {
								""cid"": ""bafyreig4gj6hj7c7bkt76xoksmxpxr3ogdu2mv32xg6633qsrx4a3zowla"",
								""uri"": ""at://did:plc:y4zs4cabaezzwx3bz2e5nnj2/app.bsky.feed.post/3lcqsampd4b2c""
							}
						},
						""text"": ""We lost our homes, our businesses, and our livelihoods. It was a devastating time. \n\nToday I hear echoes of that time in the plans of the Trump administration. They want to round up and deport millions of undocumented migrants and deploy the military and build detention camps to carry this out. /3""
					},
					""replyCount"": 35,
					""repostCount"": 250,
					""likeCount"": 2961,
					""quoteCount"": 8,
					""indexedAt"": ""2024-12-07T22:58:16.924Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:y4zs4cabaezzwx3bz2e5nnj2"",
					""handle"": ""georgetakei.bsky.social"",
					""displayName"": ""George Takei"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y4zs4cabaezzwx3bz2e5nnj2/bafkreihyuljtklac6pgvt4kbndezofm23wswyhdqmh77bgedrxhuigwbh4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-04-30T19:45:03.507Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:ti5r2g22mcze5uawxroj32vm/app.bsky.feed.post/3lcr7bb6a7c2b"",
				""cid"": ""bafyreid755pifdzjbb4gojplrnpej2utldzv2wqml7awnbafywpv5xx4yu"",
				""author"": {
					""did"": ""did:plc:ti5r2g22mcze5uawxroj32vm"",
					""handle"": ""mrdowden.com"",
					""displayName"": ""Michael Dowden"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ti5r2g22mcze5uawxroj32vm/bafkreifc4aybixfta4t5d24nmfir7yjfwk22bqkkcq45hc77zuek23j7ga@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3k4hj7bb6q62d"",
						""followedBy"": ""at://did:plc:ti5r2g22mcze5uawxroj32vm/app.bsky.graph.follow/3k4glqoss4m2j""
					},
					""labels"": [],
					""createdAt"": ""2023-07-06T23:24:33.809Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:50:10.426Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreigdzfqkfslx7krtupevu7o66ryjjhzacy773x4hou7maylecbaaw4"",
							""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.feed.post/3lcr4epw3yc2e""
						},
						""root"": {
							""cid"": ""bafyreigdzfqkfslx7krtupevu7o66ryjjhzacy773x4hou7maylecbaaw4"",
							""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.feed.post/3lcr4epw3yc2e""
						}
					},
					""text"": ""I'm almost definitely on the list.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:50:10.716Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.feed.post/3lcr4epw3yc2e"",
					""cid"": ""bafyreigdzfqkfslx7krtupevu7o66ryjjhzacy773x4hou7maylecbaaw4"",
					""author"": {
						""did"": ""did:plc:omkb4ov5w2bintbbbb47lile"",
						""handle"": ""dlbowman76.com"",
						""displayName"": ""David Bowman"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:omkb4ov5w2bintbbbb47lile/bafkreigywjpnyh75yh25nza2csn67mgfckedcgoyyg6z7ia37vtdkshdye@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:omkb4ov5w2bintbbbb47lile"",
								""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.actor.profile/self"",
								""cid"": ""bafyreihznuhxcrf3bsl6mqo65uasqgzonc3vynofs4pzsmmsezgojgwsne"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-06-22T16:23:20.057Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T01:58:25.326Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.record"",
							""record"": {
								""cid"": ""bafyreiahgkuhosg3dh3ynwwlisd6j2j6d3olyfwf7u3ccxvnxkvmn5ob7y"",
								""uri"": ""at://did:plc:7d7hkphd43rwukszjzxya3yh/app.bsky.feed.post/3lcqvxbxsck26""
							}
						},
						""langs"": [
							""en""
						],
						""text"": ""Ah, so you could completely fill the Indianapolis Motor Speedway with that list, then.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.record#view"",
						""record"": {
							""$type"": ""app.bsky.embed.record#viewRecord"",
							""uri"": ""at://did:plc:7d7hkphd43rwukszjzxya3yh/app.bsky.feed.post/3lcqvxbxsck26"",
							""cid"": ""bafyreiahgkuhosg3dh3ynwwlisd6j2j6d3olyfwf7u3ccxvnxkvmn5ob7y"",
							""author"": {
								""did"": ""did:plc:7d7hkphd43rwukszjzxya3yh"",
								""handle"": ""jtp.bsky.social"",
								""displayName"": ""jenny_tightpants🪑"",
								""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreig7brtgtsvyo5mzr7mq4u7a4ypuj6dhrthlkrfjg5xr5745k5k4uy@jpeg"",
								""associated"": {
									""chat"": {
										""allowIncoming"": ""all""
									}
								},
								""viewer"": {
									""muted"": false,
									""blockedBy"": false
								},
								""labels"": [],
								""createdAt"": ""2023-04-20T05:41:51.589Z""
							},
							""value"": {
								""$type"": ""app.bsky.feed.post"",
								""createdAt"": ""2024-12-08T00:03:32.039Z"",
								""embed"": {
									""$type"": ""app.bsky.embed.recordWithMedia"",
									""media"": {
										""$type"": ""app.bsky.embed.images"",
										""images"": [
											{
												""alt"": """",
												""aspectRatio"": {
													""height"": 534,
													""width"": 1080
												},
												""image"": {
													""$type"": ""blob"",
													""ref"": {
														""$link"": ""bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q""
													},
													""mimeType"": ""image/jpeg"",
													""size"": 140081
												}
											}
										]
									},
									""record"": {
										""$type"": ""app.bsky.embed.record"",
										""record"": {
											""cid"": ""bafyreifrrhi2z62idyhypp3r536mpp75sr2jncq7yxln7jcpr3rvsx4ttu"",
											""uri"": ""at://did:plc:dzezcmpb3fhcpns4n4xm4ur5/app.bsky.feed.post/3lcqfhqcpx22n""
										}
									}
								},
								""langs"": [
									""en""
								],
								""text"": ""this paragraph caught my eye. apparently if you've publicly complained about your insurance they might have you on a person of interest list""
							},
							""labels"": [],
							""likeCount"": 2337,
							""replyCount"": 97,
							""repostCount"": 422,
							""quoteCount"": 54,
							""indexedAt"": ""2024-12-08T00:03:34.113Z"",
							""embeds"": [
								{
									""$type"": ""app.bsky.embed.recordWithMedia#view"",
									""media"": {
										""$type"": ""app.bsky.embed.images#view"",
										""images"": [
											{
												""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q@jpeg"",
												""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q@jpeg"",
												""alt"": """",
												""aspectRatio"": {
													""height"": 534,
													""width"": 1080
												}
											}
										]
									},
									""record"": {
										""record"": {
											""$type"": ""app.bsky.embed.record#viewRecord"",
											""uri"": ""at://did:plc:dzezcmpb3fhcpns4n4xm4ur5/app.bsky.feed.post/3lcqfhqcpx22n"",
											""cid"": ""bafyreifrrhi2z62idyhypp3r536mpp75sr2jncq7yxln7jcpr3rvsx4ttu"",
											""author"": {
												""did"": ""did:plc:dzezcmpb3fhcpns4n4xm4ur5"",
												""handle"": ""cnn.com"",
												""displayName"": ""CNN"",
												""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:dzezcmpb3fhcpns4n4xm4ur5/bafkreieh4mn56re4rw5xtue6k6anahlhclwhib2k7ucwqcuyihszftmz5e@jpeg"",
												""associated"": {
													""chat"": {
														""allowIncoming"": ""following""
													}
												},
												""viewer"": {
													""muted"": false,
													""blockedBy"": false
												},
												""labels"": [],
												""createdAt"": ""2024-02-09T18:44:31.539Z""
											},
											""value"": {
												""$type"": ""app.bsky.feed.post"",
												""createdAt"": ""2024-12-07T19:08:30.340Z"",
												""embed"": {
													""$type"": ""app.bsky.embed.external"",
													""external"": {
														""description"": ""As the investigation into the fatal shooting of a health care executive in Manhattan enters its fourth day, the FBI says it is offering a reward of up to $50,000 for information leading to an arrest a..."",
														""thumb"": {
															""$type"": ""blob"",
															""ref"": {
																""$link"": ""bafkreidfnoml2ogxwcitnfobsg26yvnudah2wi7sxjhomcrkclolyljcfa""
															},
															""mimeType"": ""image/jpeg"",
															""size"": 825467
														},
														""title"": ""FBI offers reward up to $50,000 as health care executive’s suspected killer evades police for fourth day | CNN"",
														""uri"": ""https://cnn.it/4goevIr""
													}
												},
												""facets"": [
													{
														""features"": [
															{
																""$type"": ""app.bsky.richtext.facet#link"",
																""uri"": ""https://cnn.it/4goevIr""
															}
														],
														""index"": {
															""byteEnd"": 122,
															""byteStart"": 108
														}
													}
												],
												""langs"": [
													""en""
												],
												""text"": ""FBI offers reward up to $50,000 as health care executive’s suspected killer evades police for fourth day: cnn.it/4goevIr""
											},
											""labels"": [],
											""likeCount"": 544,
											""replyCount"": 699,
											""repostCount"": 79,
											""quoteCount"": 1145,
											""indexedAt"": ""2024-12-07T19:08:34.314Z""
										}
									}
								}
							]
						}
					},
					""replyCount"": 1,
					""repostCount"": 2,
					""likeCount"": 4,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T01:58:25.524Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.feed.post/3lcr4epw3yc2e"",
					""cid"": ""bafyreigdzfqkfslx7krtupevu7o66ryjjhzacy773x4hou7maylecbaaw4"",
					""author"": {
						""did"": ""did:plc:omkb4ov5w2bintbbbb47lile"",
						""handle"": ""dlbowman76.com"",
						""displayName"": ""David Bowman"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:omkb4ov5w2bintbbbb47lile/bafkreigywjpnyh75yh25nza2csn67mgfckedcgoyyg6z7ia37vtdkshdye@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:omkb4ov5w2bintbbbb47lile"",
								""uri"": ""at://did:plc:omkb4ov5w2bintbbbb47lile/app.bsky.actor.profile/self"",
								""cid"": ""bafyreihznuhxcrf3bsl6mqo65uasqgzonc3vynofs4pzsmmsezgojgwsne"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-06-22T16:23:20.057Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T01:58:25.326Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.record"",
							""record"": {
								""cid"": ""bafyreiahgkuhosg3dh3ynwwlisd6j2j6d3olyfwf7u3ccxvnxkvmn5ob7y"",
								""uri"": ""at://did:plc:7d7hkphd43rwukszjzxya3yh/app.bsky.feed.post/3lcqvxbxsck26""
							}
						},
						""langs"": [
							""en""
						],
						""text"": ""Ah, so you could completely fill the Indianapolis Motor Speedway with that list, then.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.record#view"",
						""record"": {
							""$type"": ""app.bsky.embed.record#viewRecord"",
							""uri"": ""at://did:plc:7d7hkphd43rwukszjzxya3yh/app.bsky.feed.post/3lcqvxbxsck26"",
							""cid"": ""bafyreiahgkuhosg3dh3ynwwlisd6j2j6d3olyfwf7u3ccxvnxkvmn5ob7y"",
							""author"": {
								""did"": ""did:plc:7d7hkphd43rwukszjzxya3yh"",
								""handle"": ""jtp.bsky.social"",
								""displayName"": ""jenny_tightpants🪑"",
								""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreig7brtgtsvyo5mzr7mq4u7a4ypuj6dhrthlkrfjg5xr5745k5k4uy@jpeg"",
								""associated"": {
									""chat"": {
										""allowIncoming"": ""all""
									}
								},
								""viewer"": {
									""muted"": false,
									""blockedBy"": false
								},
								""labels"": [],
								""createdAt"": ""2023-04-20T05:41:51.589Z""
							},
							""value"": {
								""$type"": ""app.bsky.feed.post"",
								""createdAt"": ""2024-12-08T00:03:32.039Z"",
								""embed"": {
									""$type"": ""app.bsky.embed.recordWithMedia"",
									""media"": {
										""$type"": ""app.bsky.embed.images"",
										""images"": [
											{
												""alt"": """",
												""aspectRatio"": {
													""height"": 534,
													""width"": 1080
												},
												""image"": {
													""$type"": ""blob"",
													""ref"": {
														""$link"": ""bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q""
													},
													""mimeType"": ""image/jpeg"",
													""size"": 140081
												}
											}
										]
									},
									""record"": {
										""$type"": ""app.bsky.embed.record"",
										""record"": {
											""cid"": ""bafyreifrrhi2z62idyhypp3r536mpp75sr2jncq7yxln7jcpr3rvsx4ttu"",
											""uri"": ""at://did:plc:dzezcmpb3fhcpns4n4xm4ur5/app.bsky.feed.post/3lcqfhqcpx22n""
										}
									}
								},
								""langs"": [
									""en""
								],
								""text"": ""this paragraph caught my eye. apparently if you've publicly complained about your insurance they might have you on a person of interest list""
							},
							""labels"": [],
							""likeCount"": 2337,
							""replyCount"": 97,
							""repostCount"": 422,
							""quoteCount"": 54,
							""indexedAt"": ""2024-12-08T00:03:34.113Z"",
							""embeds"": [
								{
									""$type"": ""app.bsky.embed.recordWithMedia#view"",
									""media"": {
										""$type"": ""app.bsky.embed.images#view"",
										""images"": [
											{
												""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q@jpeg"",
												""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:7d7hkphd43rwukszjzxya3yh/bafkreiblexc4d5gsusbiu733kgike3jttduaf7c4kespl3jg5mf4r5ey4q@jpeg"",
												""alt"": """",
												""aspectRatio"": {
													""height"": 534,
													""width"": 1080
												}
											}
										]
									},
									""record"": {
										""record"": {
											""$type"": ""app.bsky.embed.record#viewRecord"",
											""uri"": ""at://did:plc:dzezcmpb3fhcpns4n4xm4ur5/app.bsky.feed.post/3lcqfhqcpx22n"",
											""cid"": ""bafyreifrrhi2z62idyhypp3r536mpp75sr2jncq7yxln7jcpr3rvsx4ttu"",
											""author"": {
												""did"": ""did:plc:dzezcmpb3fhcpns4n4xm4ur5"",
												""handle"": ""cnn.com"",
												""displayName"": ""CNN"",
												""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:dzezcmpb3fhcpns4n4xm4ur5/bafkreieh4mn56re4rw5xtue6k6anahlhclwhib2k7ucwqcuyihszftmz5e@jpeg"",
												""associated"": {
													""chat"": {
														""allowIncoming"": ""following""
													}
												},
												""viewer"": {
													""muted"": false,
													""blockedBy"": false
												},
												""labels"": [],
												""createdAt"": ""2024-02-09T18:44:31.539Z""
											},
											""value"": {
												""$type"": ""app.bsky.feed.post"",
												""createdAt"": ""2024-12-07T19:08:30.340Z"",
												""embed"": {
													""$type"": ""app.bsky.embed.external"",
													""external"": {
														""description"": ""As the investigation into the fatal shooting of a health care executive in Manhattan enters its fourth day, the FBI says it is offering a reward of up to $50,000 for information leading to an arrest a..."",
														""thumb"": {
															""$type"": ""blob"",
															""ref"": {
																""$link"": ""bafkreidfnoml2ogxwcitnfobsg26yvnudah2wi7sxjhomcrkclolyljcfa""
															},
															""mimeType"": ""image/jpeg"",
															""size"": 825467
														},
														""title"": ""FBI offers reward up to $50,000 as health care executive’s suspected killer evades police for fourth day | CNN"",
														""uri"": ""https://cnn.it/4goevIr""
													}
												},
												""facets"": [
													{
														""features"": [
															{
																""$type"": ""app.bsky.richtext.facet#link"",
																""uri"": ""https://cnn.it/4goevIr""
															}
														],
														""index"": {
															""byteEnd"": 122,
															""byteStart"": 108
														}
													}
												],
												""langs"": [
													""en""
												],
												""text"": ""FBI offers reward up to $50,000 as health care executive’s suspected killer evades police for fourth day: cnn.it/4goevIr""
											},
											""labels"": [],
											""likeCount"": 544,
											""replyCount"": 699,
											""repostCount"": 79,
											""quoteCount"": 1145,
											""indexedAt"": ""2024-12-07T19:08:34.314Z""
										}
									}
								}
							]
						}
					},
					""replyCount"": 1,
					""repostCount"": 2,
					""likeCount"": 4,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T01:58:25.524Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:ti5r2g22mcze5uawxroj32vm/app.bsky.feed.post/3lcr77fgs6s2b"",
				""cid"": ""bafyreidcvyjdj4erqn6bgothc2jqdnzu3zd33327nlpq5rhimaxyrsbiqq"",
				""author"": {
					""did"": ""did:plc:ti5r2g22mcze5uawxroj32vm"",
					""handle"": ""mrdowden.com"",
					""displayName"": ""Michael Dowden"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ti5r2g22mcze5uawxroj32vm/bafkreifc4aybixfta4t5d24nmfir7yjfwk22bqkkcq45hc77zuek23j7ga@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3k4hj7bb6q62d"",
						""followedBy"": ""at://did:plc:ti5r2g22mcze5uawxroj32vm/app.bsky.graph.follow/3k4glqoss4m2j""
					},
					""labels"": [],
					""createdAt"": ""2023-07-06T23:24:33.809Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:49:07.790Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreifytve3jyofjchkrhgyojd4gdki5qs4gqdlndokuxsbj2kflmagea"",
							""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcqvdnigtc2i""
						},
						""root"": {
							""cid"": ""bafyreifytve3jyofjchkrhgyojd4gdki5qs4gqdlndokuxsbj2kflmagea"",
							""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcqvdnigtc2i""
						}
					},
					""text"": ""This GIF is basically my personality.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:49:08.513Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcqvdnigtc2i"",
					""cid"": ""bafyreifytve3jyofjchkrhgyojd4gdki5qs4gqdlndokuxsbj2kflmagea"",
					""author"": {
						""did"": ""did:plc:b2u7ss22xlzjxocc62is5mxs"",
						""handle"": ""yerawizardcat.com"",
						""displayName"": ""Cat Schneider 🪄🐈‍⬛️ "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreiemq767ukphkv7vwdwhhhouzcwmdai3haf3xwpnreojyzrpguva6i@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-28T14:20:37.588Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:52:33.033Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.recordWithMedia"",
							""media"": {
								""$type"": ""app.bsky.embed.external"",
								""external"": {
									""description"": ""Alt: Captain Mal, making the statement \""I aim to misbehave\"""",
									""thumb"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibqlisbandq6tqutv5jcevabou6mpr2x7b2mhzkfp5mpjqep6gs4e""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 93023
									},
									""title"": ""a man with the words i aim to misbehave above him"",
									""uri"": ""https://media.tenor.com/c9YD8HRDFFYAAAAC/malcolm-reynolds-mal.gif?hh=250&ww=498""
								}
							},
							""record"": {
								""$type"": ""app.bsky.embed.record"",
								""record"": {
									""cid"": ""bafyreidz4qv65dzluqzvhjzkycp45ku4r7cn2eejgsr3e5fa4acb33n2bu"",
									""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcp2ha7owc2t""
								}
							}
						},
						""langs"": [
							""en""
						],
						""text"": """"
					},
					""embed"": {
						""$type"": ""app.bsky.embed.recordWithMedia#view"",
						""media"": {
							""$type"": ""app.bsky.embed.external#view"",
							""external"": {
								""uri"": ""https://media.tenor.com/c9YD8HRDFFYAAAAC/malcolm-reynolds-mal.gif?hh=250&ww=498"",
								""title"": ""a man with the words i aim to misbehave above him"",
								""description"": ""Alt: Captain Mal, making the statement \""I aim to misbehave\"""",
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreibqlisbandq6tqutv5jcevabou6mpr2x7b2mhzkfp5mpjqep6gs4e@jpeg""
							}
						},
						""record"": {
							""record"": {
								""$type"": ""app.bsky.embed.record#viewRecord"",
								""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcp2ha7owc2t"",
								""cid"": ""bafyreidz4qv65dzluqzvhjzkycp45ku4r7cn2eejgsr3e5fa4acb33n2bu"",
								""author"": {
									""did"": ""did:plc:b2u7ss22xlzjxocc62is5mxs"",
									""handle"": ""yerawizardcat.com"",
									""displayName"": ""Cat Schneider 🪄🐈‍⬛️ "",
									""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreiemq767ukphkv7vwdwhhhouzcwmdai3haf3xwpnreojyzrpguva6i@jpeg"",
									""associated"": {
										""chat"": {
											""allowIncoming"": ""all""
										}
									},
									""viewer"": {
										""muted"": false,
										""blockedBy"": false
									},
									""labels"": [],
									""createdAt"": ""2023-06-28T14:20:37.588Z""
								},
								""value"": {
									""$type"": ""app.bsky.feed.post"",
									""createdAt"": ""2024-12-07T06:18:42.565Z"",
									""langs"": [
										""en""
									],
									""text"": ""Be the chaotic good energy you wish to see in this world.""
								},
								""labels"": [],
								""likeCount"": 13,
								""replyCount"": 2,
								""repostCount"": 1,
								""quoteCount"": 1,
								""indexedAt"": ""2024-12-07T06:18:43.313Z"",
								""embeds"": []
							}
						}
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 3,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T23:52:35.120Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcqvdnigtc2i"",
					""cid"": ""bafyreifytve3jyofjchkrhgyojd4gdki5qs4gqdlndokuxsbj2kflmagea"",
					""author"": {
						""did"": ""did:plc:b2u7ss22xlzjxocc62is5mxs"",
						""handle"": ""yerawizardcat.com"",
						""displayName"": ""Cat Schneider 🪄🐈‍⬛️ "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreiemq767ukphkv7vwdwhhhouzcwmdai3haf3xwpnreojyzrpguva6i@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-28T14:20:37.588Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:52:33.033Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.recordWithMedia"",
							""media"": {
								""$type"": ""app.bsky.embed.external"",
								""external"": {
									""description"": ""Alt: Captain Mal, making the statement \""I aim to misbehave\"""",
									""thumb"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreibqlisbandq6tqutv5jcevabou6mpr2x7b2mhzkfp5mpjqep6gs4e""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 93023
									},
									""title"": ""a man with the words i aim to misbehave above him"",
									""uri"": ""https://media.tenor.com/c9YD8HRDFFYAAAAC/malcolm-reynolds-mal.gif?hh=250&ww=498""
								}
							},
							""record"": {
								""$type"": ""app.bsky.embed.record"",
								""record"": {
									""cid"": ""bafyreidz4qv65dzluqzvhjzkycp45ku4r7cn2eejgsr3e5fa4acb33n2bu"",
									""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcp2ha7owc2t""
								}
							}
						},
						""langs"": [
							""en""
						],
						""text"": """"
					},
					""embed"": {
						""$type"": ""app.bsky.embed.recordWithMedia#view"",
						""media"": {
							""$type"": ""app.bsky.embed.external#view"",
							""external"": {
								""uri"": ""https://media.tenor.com/c9YD8HRDFFYAAAAC/malcolm-reynolds-mal.gif?hh=250&ww=498"",
								""title"": ""a man with the words i aim to misbehave above him"",
								""description"": ""Alt: Captain Mal, making the statement \""I aim to misbehave\"""",
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreibqlisbandq6tqutv5jcevabou6mpr2x7b2mhzkfp5mpjqep6gs4e@jpeg""
							}
						},
						""record"": {
							""record"": {
								""$type"": ""app.bsky.embed.record#viewRecord"",
								""uri"": ""at://did:plc:b2u7ss22xlzjxocc62is5mxs/app.bsky.feed.post/3lcp2ha7owc2t"",
								""cid"": ""bafyreidz4qv65dzluqzvhjzkycp45ku4r7cn2eejgsr3e5fa4acb33n2bu"",
								""author"": {
									""did"": ""did:plc:b2u7ss22xlzjxocc62is5mxs"",
									""handle"": ""yerawizardcat.com"",
									""displayName"": ""Cat Schneider 🪄🐈‍⬛️ "",
									""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:b2u7ss22xlzjxocc62is5mxs/bafkreiemq767ukphkv7vwdwhhhouzcwmdai3haf3xwpnreojyzrpguva6i@jpeg"",
									""associated"": {
										""chat"": {
											""allowIncoming"": ""all""
										}
									},
									""viewer"": {
										""muted"": false,
										""blockedBy"": false
									},
									""labels"": [],
									""createdAt"": ""2023-06-28T14:20:37.588Z""
								},
								""value"": {
									""$type"": ""app.bsky.feed.post"",
									""createdAt"": ""2024-12-07T06:18:42.565Z"",
									""langs"": [
										""en""
									],
									""text"": ""Be the chaotic good energy you wish to see in this world.""
								},
								""labels"": [],
								""likeCount"": 13,
								""replyCount"": 2,
								""repostCount"": 1,
								""quoteCount"": 1,
								""indexedAt"": ""2024-12-07T06:18:43.313Z"",
								""embeds"": []
							}
						}
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 3,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T23:52:35.120Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6eqd24"",
				""cid"": ""bafyreicu2mvmtp22pmzf6px2zy33yvhdgrcmnt6343npqgt7bsrgzyyhja"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.627Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreifxyordg42n62h4sfmo7oigt66a2lkkak3hrjoytylvmwdd5pbvou"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6eqc24""
						},
						""root"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						}
					},
					""text"": ""It's made me VERY aware that test cases are gold. AI screwed up a refactor (\""move this into a method called Foo\"", and it gives back a Foo that has different code than I originally gave it). If I did more of this, I'd guide it to making testable code, and using tests to prevent regression.\n\nFIN""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.361Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6eqc24"",
					""cid"": ""bafyreifxyordg42n62h4sfmo7oigt66a2lkkak3hrjoytylvmwdd5pbvou"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.626Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreidvfotbtjpannvw4xvuhuujd3j277cyunwe7gnc2ln7aqx7ddh5rm"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6dr224""
							},
							""root"": {
								""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
							}
						},
						""text"": ""It's surprised me a couple of times -- I see what it's done and my first response was \""that's not right\"" and then I look further and realise it's a more elegant solution than the clunky one I would have used. (eg placing the \""I have seen this\"" at the right place in a complex loop)""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.295Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6eqc24"",
				""cid"": ""bafyreifxyordg42n62h4sfmo7oigt66a2lkkak3hrjoytylvmwdd5pbvou"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.626Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreidvfotbtjpannvw4xvuhuujd3j277cyunwe7gnc2ln7aqx7ddh5rm"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6dr224""
						},
						""root"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						}
					},
					""text"": ""It's surprised me a couple of times -- I see what it's done and my first response was \""that's not right\"" and then I look further and realise it's a more elegant solution than the clunky one I would have used. (eg placing the \""I have seen this\"" at the right place in a complex loop)""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.295Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6dr224"",
					""cid"": ""bafyreidvfotbtjpannvw4xvuhuujd3j277cyunwe7gnc2ln7aqx7ddh5rm"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.625Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreidditfdoy6zqb67fgaujgue5gtbwl7s6pw3o26kqu6fwdji6p53je"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6crs24""
							},
							""root"": {
								""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
							}
						},
						""text"": ""It's so weird. I've spent my time thinking through HOW to solve the problem, but not concerned at all about writing code. Then I run it against the test data and debug it. The oversights are generally in the complex logic of escaping from loops.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.254Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6dr224"",
				""cid"": ""bafyreidvfotbtjpannvw4xvuhuujd3j277cyunwe7gnc2ln7aqx7ddh5rm"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.625Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreidditfdoy6zqb67fgaujgue5gtbwl7s6pw3o26kqu6fwdji6p53je"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6crs24""
						},
						""root"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						}
					},
					""text"": ""It's so weird. I've spent my time thinking through HOW to solve the problem, but not concerned at all about writing code. Then I run it against the test data and debug it. The oversights are generally in the complex logic of escaping from loops.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.254Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6crs24"",
					""cid"": ""bafyreidditfdoy6zqb67fgaujgue5gtbwl7s6pw3o26kqu6fwdji6p53je"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.624Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreigro3jjliikr4hcof7hd2ogovjzqf4ijgtdlc7ptwfdn4qz3vnhg4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6bsk24""
							},
							""root"": {
								""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
							}
						},
						""text"": ""That part of my prompt: \""For each equation, we're going to try and find how many sets of operators cause the equation to be true. Equations are evaluated left to right, not with the usual operator precedence rules.\""\n\nThe rest of my prompt was about how to try all operators.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.204Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6crs24"",
				""cid"": ""bafyreidditfdoy6zqb67fgaujgue5gtbwl7s6pw3o26kqu6fwdji6p53je"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.624Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreigro3jjliikr4hcof7hd2ogovjzqf4ijgtdlc7ptwfdn4qz3vnhg4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6bsk24""
						},
						""root"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						}
					},
					""text"": ""That part of my prompt: \""For each equation, we're going to try and find how many sets of operators cause the equation to be true. Equations are evaluated left to right, not with the usual operator precedence rules.\""\n\nThe rest of my prompt was about how to try all operators.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.204Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6bsk24"",
					""cid"": ""bafyreigro3jjliikr4hcof7hd2ogovjzqf4ijgtdlc7ptwfdn4qz3vnhg4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.623Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
							},
							""root"": {
								""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
								""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
							}
						},
						""text"": ""Today I got the data structures right for the search, but didn't want to think through the How of evaluating an expression with their rules. So I totally punted in that part of my prompt ... and it absolutely wrote the right code first time.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.156Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v6bsk24"",
				""cid"": ""bafyreigro3jjliikr4hcof7hd2ogovjzqf4ijgtdlc7ptwfdn4qz3vnhg4"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.623Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						},
						""root"": {
							""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
							""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24""
						}
					},
					""text"": ""Today I got the data structures right for the search, but didn't want to think through the How of evaluating an expression with their rules. So I totally punted in that part of my prompt ... and it absolutely wrote the right code first time.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.156Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
					""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
					""author"": {
						""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
						""handle"": ""gnat.bsky.social"",
						""displayName"": ""Nat Torkington "",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
							""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
						},
						""labels"": [],
						""createdAt"": ""2023-05-05T22:18:51.120Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:47:43.622Z"",
						""langs"": [
							""en""
						],
						""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:47:44.112Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.feed.post/3lcr74v64wc24"",
				""cid"": ""bafyreid4cjqfgkyszinantyqkuvhuuttlcwsu3ulblvpymcuvgo4j7stc4"",
				""author"": {
					""did"": ""did:plc:powfsotxiiqkssbwzclq4cfd"",
					""handle"": ""gnat.bsky.social"",
					""displayName"": ""Nat Torkington "",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:powfsotxiiqkssbwzclq4cfd/bafkreih33o25txavosl4bmotog7nkewxfzfgob6arg7aemqwvophe5rsvy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3la4ybq525z2s"",
						""followedBy"": ""at://did:plc:powfsotxiiqkssbwzclq4cfd/app.bsky.graph.follow/3la33lfmjkg2h""
					},
					""labels"": [],
					""createdAt"": ""2023-05-05T22:18:51.120Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:47:43.622Z"",
					""langs"": [
						""en""
					],
					""text"": ""Like many people, I've been using ChatGPT for Advent of Code. 🧵\n\nI've been describing the program I want it to write, then fixing logic errors. I have been describing algorithms in detail, leaving it to write syntax. I've written so much code in the last 40 years, I am pretty good at the How.""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:47:44.112Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:kqbyr4gqt6p2l57htlsa4nha/app.bsky.feed.post/3lcr3xotl4k25"",
				""cid"": ""bafyreibovzsj2ynk43a3i7pcfrvwjbacvzevwkdhy7rupon7ymq5o5ud2i"",
				""author"": {
					""did"": ""did:plc:kqbyr4gqt6p2l57htlsa4nha"",
					""handle"": ""hankgreen.bsky.social"",
					""displayName"": ""Hank Green"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:kqbyr4gqt6p2l57htlsa4nha/bafkreibagwdburvff6mkjstq7anuzyynzgnweitmqezxosqjmbkty5gwyi@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-04-29T04:29:07.593Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:51:07.986Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.record"",
						""record"": {
							""cid"": ""bafyreidv6kn72djeaa4jxsietnxxduuc2uhwbhvsqymg4dy3jjcbrugsqm"",
							""uri"": ""at://did:plc:f7raulitdt42qio7vy57d3gh/app.bsky.feed.post/3lcr33s2pyk2b""
						}
					},
					""langs"": [
						""en""
					],
					""text"": ""When I say Bluesky is a bizarre and exciting experiment, this is both /not at all/ and /exactly/ what I mean.""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.record#view"",
					""record"": {
						""$type"": ""app.bsky.embed.record#viewRecord"",
						""uri"": ""at://did:plc:f7raulitdt42qio7vy57d3gh/app.bsky.feed.post/3lcr33s2pyk2b"",
						""cid"": ""bafyreidv6kn72djeaa4jxsietnxxduuc2uhwbhvsqymg4dy3jjcbrugsqm"",
						""author"": {
							""did"": ""did:plc:f7raulitdt42qio7vy57d3gh"",
							""handle"": ""dylantate.com"",
							""displayName"": ""Dylan Tate"",
							""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:f7raulitdt42qio7vy57d3gh/bafkreig5nuxgqyw5ceoq3preudqmj3cmw5qvif5xg6np2v2bbhkhdhalh4@jpeg"",
							""viewer"": {
								""muted"": false,
								""blockedBy"": false
							},
							""labels"": [],
							""createdAt"": ""2024-11-15T21:21:35.663Z""
						},
						""value"": {
							""$type"": ""app.bsky.feed.post"",
							""createdAt"": ""2024-12-08T01:35:31.837Z"",
							""embed"": {
								""$type"": ""app.bsky.embed.images"",
								""images"": [
									{
										""alt"": ""Hank Green Bluesky on microwave"",
										""aspectRatio"": {
											""height"": 1844,
											""width"": 2000
										},
										""image"": {
											""$type"": ""blob"",
											""ref"": {
												""$link"": ""bafkreiezqai3g7cqqfsq3yrbnkwo2zptrihozrx2gmlauroekfsxn7dz2m""
											},
											""mimeType"": ""image/jpeg"",
											""size"": 677357
										}
									}
								]
							},
							""facets"": [
								{
									""$type"": ""app.bsky.richtext.facet"",
									""features"": [
										{
											""$type"": ""app.bsky.richtext.facet#mention"",
											""did"": ""did:plc:kqbyr4gqt6p2l57htlsa4nha""
										}
									],
									""index"": {
										""byteEnd"": 71,
										""byteStart"": 49
									}
								}
							],
							""langs"": [
								""en""
							],
							""text"": ""No way I got Bluesky to work on my microwave ft. @hankgreen.bsky.social""
						},
						""labels"": [],
						""likeCount"": 1057,
						""replyCount"": 37,
						""repostCount"": 103,
						""quoteCount"": 27,
						""indexedAt"": ""2024-12-08T01:35:35.617Z"",
						""embeds"": [
							{
								""$type"": ""app.bsky.embed.images#view"",
								""images"": [
									{
										""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:f7raulitdt42qio7vy57d3gh/bafkreiezqai3g7cqqfsq3yrbnkwo2zptrihozrx2gmlauroekfsxn7dz2m@jpeg"",
										""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:f7raulitdt42qio7vy57d3gh/bafkreiezqai3g7cqqfsq3yrbnkwo2zptrihozrx2gmlauroekfsxn7dz2m@jpeg"",
										""alt"": ""Hank Green Bluesky on microwave"",
										""aspectRatio"": {
											""height"": 1844,
											""width"": 2000
										}
									}
								]
							}
						]
					}
				},
				""replyCount"": 55,
				""repostCount"": 336,
				""likeCount"": 4793,
				""quoteCount"": 9,
				""indexedAt"": ""2024-12-08T01:51:08.411Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""indexedAt"": ""2024-12-08T02:47:41.425Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:fefkggatybfbndz4xzuovqgz/app.bsky.feed.post/3lcr6m3itlk27"",
				""cid"": ""bafyreif2lp3jlawljjll6p7bspespl5nmow4x7m3sr5qmb4aztvpuz5mi4"",
				""author"": {
					""did"": ""did:plc:fefkggatybfbndz4xzuovqgz"",
					""handle"": ""joephilli.bsky.social"",
					""displayName"": ""Joe Philli"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:fefkggatybfbndz4xzuovqgz/bafkreicspjsijoqgh2utic57ftw2swfp6ucb567vuh5q5gkcifa62hwtf4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardo56wpg2w"",
						""followedBy"": ""at://did:plc:fefkggatybfbndz4xzuovqgz/app.bsky.graph.follow/3laqremfqvi24""
					},
					""labels"": [],
					""createdAt"": ""2023-07-07T18:41:01.087Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:38:19.839Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreibhqg27jr4ap6pxelrgdw4m62ijmdaudcefmwp6quqwglt2fpywsm"",
							""uri"": ""at://did:plc:4h4jmqhb6ubdgqgsalmkdqxe/app.bsky.feed.post/3lcr5xjstvs23""
						},
						""root"": {
							""cid"": ""bafyreibhqg27jr4ap6pxelrgdw4m62ijmdaudcefmwp6quqwglt2fpywsm"",
							""uri"": ""at://did:plc:4h4jmqhb6ubdgqgsalmkdqxe/app.bsky.feed.post/3lcr5xjstvs23""
						}
					},
					""text"": ""Fair point, and I really don’t know if it matters, but were those others targeted attacks?""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:38:20.119Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:4h4jmqhb6ubdgqgsalmkdqxe/app.bsky.feed.post/3lcr5xjstvs23"",
					""cid"": ""bafyreibhqg27jr4ap6pxelrgdw4m62ijmdaudcefmwp6quqwglt2fpywsm"",
					""author"": {
						""did"": ""did:plc:4h4jmqhb6ubdgqgsalmkdqxe"",
						""handle"": ""alyssam-infosec.bsky.social"",
						""displayName"": ""Alyssa Miller 🦄👩‍✈️"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:4h4jmqhb6ubdgqgsalmkdqxe/bafkreighfyveb4q5vtwfh6yru6diwln6dhz4tx3jvcsp3qyramrb2j536a@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-27T14:24:24.672Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:26:50.204Z"",
						""langs"": [
							""en""
						],
						""text"": ""One man gets gunned down in Manhattan on a December morning and we've got a nationwide man hunt for the shooter and non-stop news coverage.\n\nFour people were killed in Manhattan from Jan 1 - Dec 1 and no one even knows their names.\n\nMoney and privilege influence everything.""
					},
					""replyCount"": 2,
					""repostCount"": 3,
					""likeCount"": 53,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:26:50.819Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:4h4jmqhb6ubdgqgsalmkdqxe/app.bsky.feed.post/3lcr5xjstvs23"",
					""cid"": ""bafyreibhqg27jr4ap6pxelrgdw4m62ijmdaudcefmwp6quqwglt2fpywsm"",
					""author"": {
						""did"": ""did:plc:4h4jmqhb6ubdgqgsalmkdqxe"",
						""handle"": ""alyssam-infosec.bsky.social"",
						""displayName"": ""Alyssa Miller 🦄👩‍✈️"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:4h4jmqhb6ubdgqgsalmkdqxe/bafkreighfyveb4q5vtwfh6yru6diwln6dhz4tx3jvcsp3qyramrb2j536a@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-27T14:24:24.672Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:26:50.204Z"",
						""langs"": [
							""en""
						],
						""text"": ""One man gets gunned down in Manhattan on a December morning and we've got a nationwide man hunt for the shooter and non-stop news coverage.\n\nFour people were killed in Manhattan from Jan 1 - Dec 1 and no one even knows their names.\n\nMoney and privilege influence everything.""
					},
					""replyCount"": 2,
					""repostCount"": 3,
					""likeCount"": 53,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:26:50.819Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:7iqavrhpeznxztbhopzuhxlz/app.bsky.feed.post/3lcr5thlra22a"",
				""cid"": ""bafyreidzndycgce762zsp7u5uhdjkckxxbohrydqtzgs2yypktwi5h7kgq"",
				""author"": {
					""did"": ""did:plc:7iqavrhpeznxztbhopzuhxlz"",
					""handle"": ""woodruff.dev"",
					""displayName"": ""Chris Woody Woodruff"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:7iqavrhpeznxztbhopzuhxlz/bafkreiapztdlckbs5k5uhbl6l6wy7msmuuwjq5j5dv5mdzdm5ystmcmeme@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3jzfdwufikh2q"",
						""followedBy"": ""at://did:plc:7iqavrhpeznxztbhopzuhxlz/app.bsky.graph.follow/3jz3qyjoyos27""
					},
					""labels"": [],
					""createdAt"": ""2023-04-30T20:18:01.721Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:24:33.656Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""ALT: a man in a suit and tie is smiling with the words i see what you did there above him"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreifmomdb7w3i54nshyadrgdfh4eytceshm645q5c4u3lyminr4bfna""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 137569
							},
							""title"": ""a man in a suit and tie is smiling with the words i see what you did there above him"",
							""uri"": ""https://media.tenor.com/MUsANezp0F0AAAAC/point-hehe.gif?hh=239&ww=498""
						}
					},
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiefijje2wrbpuwoy34d7wbowhtd4fmkm3dt5ccofa2ggjvjwwrv34"",
							""uri"": ""at://did:plc:fghjuxxxssvf2eml7twstd4o/app.bsky.feed.post/3lcr4qjpqlu2v""
						},
						""root"": {
							""cid"": ""bafyreiefijje2wrbpuwoy34d7wbowhtd4fmkm3dt5ccofa2ggjvjwwrv34"",
							""uri"": ""at://did:plc:fghjuxxxssvf2eml7twstd4o/app.bsky.feed.post/3lcr4qjpqlu2v""
						}
					},
					""text"": """"
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://media.tenor.com/MUsANezp0F0AAAAC/point-hehe.gif?hh=239&ww=498"",
						""title"": ""a man in a suit and tie is smiling with the words i see what you did there above him"",
						""description"": ""ALT: a man in a suit and tie is smiling with the words i see what you did there above him"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:7iqavrhpeznxztbhopzuhxlz/bafkreifmomdb7w3i54nshyadrgdfh4eytceshm645q5c4u3lyminr4bfna@jpeg""
					}
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:24:34.623Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:fghjuxxxssvf2eml7twstd4o/app.bsky.feed.post/3lcr4qjpqlu2v"",
					""cid"": ""bafyreiefijje2wrbpuwoy34d7wbowhtd4fmkm3dt5ccofa2ggjvjwwrv34"",
					""author"": {
						""did"": ""did:plc:fghjuxxxssvf2eml7twstd4o"",
						""handle"": ""divyaswor.bsky.social"",
						""displayName"": ""Divyaswor Makai"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:fghjuxxxssvf2eml7twstd4o/bafkreibkufjhyhmh4ndpdhjyxoshtgem6npj5nc76y6zckoqeicoi4gnk4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-26T03:24:05.053Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:05:00.836Z"",
						""text"": ""I can't stand when people mix up \""your\"" and \""you're\""\n\nPeople do it because their stupid""
					},
					""replyCount"": 5,
					""repostCount"": 1,
					""likeCount"": 10,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:05:01.520Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:fghjuxxxssvf2eml7twstd4o/app.bsky.feed.post/3lcr4qjpqlu2v"",
					""cid"": ""bafyreiefijje2wrbpuwoy34d7wbowhtd4fmkm3dt5ccofa2ggjvjwwrv34"",
					""author"": {
						""did"": ""did:plc:fghjuxxxssvf2eml7twstd4o"",
						""handle"": ""divyaswor.bsky.social"",
						""displayName"": ""Divyaswor Makai"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:fghjuxxxssvf2eml7twstd4o/bafkreibkufjhyhmh4ndpdhjyxoshtgem6npj5nc76y6zckoqeicoi4gnk4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-04-26T03:24:05.053Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T02:05:00.836Z"",
						""text"": ""I can't stand when people mix up \""your\"" and \""you're\""\n\nPeople do it because their stupid""
					},
					""replyCount"": 5,
					""repostCount"": 1,
					""likeCount"": 10,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T02:05:01.520Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:ivue5a4i56xxdw4co557jic6/app.bsky.feed.post/3lcr5pw57ik2c"",
				""cid"": ""bafyreihsrudldbasy56ncgcn463sgppb2ut2vbvunn6o27l7hxyzfuxmim"",
				""author"": {
					""did"": ""did:plc:ivue5a4i56xxdw4co557jic6"",
					""handle"": ""tdriver.bsky.social"",
					""displayName"": ""Ted Driver"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ivue5a4i56xxdw4co557jic6/bafkreigqpe5nutwkzjl7wurvmqh3qmg7qnzf6sshg5dz5sanj5ss3nuqdi@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3latuiwmqqb2s"",
						""followedBy"": ""at://did:plc:ivue5a4i56xxdw4co557jic6/app.bsky.graph.follow/3latge3c7fe22""
					},
					""labels"": [
						{
							""src"": ""did:plc:ivue5a4i56xxdw4co557jic6"",
							""uri"": ""at://did:plc:ivue5a4i56xxdw4co557jic6/app.bsky.actor.profile/self"",
							""cid"": ""bafyreihugik5qddvkcgek2342djmu5b5zccbgwauyyxjg6vch55ijf7nyq"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""1970-01-01T00:00:00.000Z""
						}
					],
					""createdAt"": ""2023-09-08T03:32:09.109Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:22:34.691Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""YouTube video by Coldplay"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreifgxpgyborpcwzewb4iy7dcxumevwxl3f6or37hb7cx5qgzdgaqpe""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 888978
							},
							""title"": ""Coldplay - ALL MY LOVE (Official Video) (Directors' Cut)"",
							""uri"": ""https://youtu.be/o4OlL0OpbW8""
						}
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://youtu.be/o4OlL0OpbW8""
								}
							],
							""index"": {
								""byteEnd"": 39,
								""byteStart"": 19
							}
						}
					],
					""langs"": [
						""en""
					],
					""text"": ""This is fantastic! youtu.be/o4OlL0OpbW8""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://youtu.be/o4OlL0OpbW8"",
						""title"": ""Coldplay - ALL MY LOVE (Official Video) (Directors' Cut)"",
						""description"": ""YouTube video by Coldplay"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:ivue5a4i56xxdw4co557jic6/bafkreifgxpgyborpcwzewb4iy7dcxumevwxl3f6or37hb7cx5qgzdgaqpe@jpeg""
					}
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:22:38.820Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:yuewsuaeeuic54njjyxz6r3k/app.bsky.feed.post/3lcqi3r4a322y"",
				""cid"": ""bafyreiayd6duck23frvhmw44tngtyv5d2qlqwp4shhfiuz65fdnqgjasba"",
				""author"": {
					""did"": ""did:plc:yuewsuaeeuic54njjyxz6r3k"",
					""handle"": ""burastar.net"",
					""displayName"": ""bura ★"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:yuewsuaeeuic54njjyxz6r3k/bafkreih4xhhyb2tyzev7f6f5i3nfulntrhhqjuu7gj2rcfdvghx3huh2te@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2024-10-17T16:56:24.739Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T19:55:29.748Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.video"",
						""alt"": ""Pingbert goes ZZZ"",
						""aspectRatio"": {
							""height"": 2130,
							""width"": 3840
						},
						""video"": {
							""$type"": ""blob"",
							""ref"": {
								""$link"": ""bafkreihmtjuvrivad2flwhzbicbyxoerhdy24tpz2g3rd2nxpsvd2i6aim""
							},
							""mimeType"": ""video/mp4"",
							""size"": 3991114
						}
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""GodotEngine""
								}
							],
							""index"": {
								""byteEnd"": 57,
								""byteStart"": 45
							}
						},
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""indiegamedev""
								}
							],
							""index"": {
								""byteEnd"": 71,
								""byteStart"": 58
							}
						},
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""pingbert""
								}
							],
							""index"": {
								""byteEnd"": 81,
								""byteStart"": 72
							}
						}
					],
					""langs"": [
						""en""
					],
					""text"": ""No update, Pingbert needs rest 😴💤\n\n---\n#GodotEngine #indiegamedev #pingbert""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.video#view"",
					""cid"": ""bafkreihmtjuvrivad2flwhzbicbyxoerhdy24tpz2g3rd2nxpsvd2i6aim"",
					""playlist"": ""https://video.bsky.app/watch/did%3Aplc%3Ayuewsuaeeuic54njjyxz6r3k/bafkreihmtjuvrivad2flwhzbicbyxoerhdy24tpz2g3rd2nxpsvd2i6aim/playlist.m3u8"",
					""thumbnail"": ""https://video.bsky.app/watch/did%3Aplc%3Ayuewsuaeeuic54njjyxz6r3k/bafkreihmtjuvrivad2flwhzbicbyxoerhdy24tpz2g3rd2nxpsvd2i6aim/thumbnail.jpg"",
					""alt"": ""Pingbert goes ZZZ"",
					""aspectRatio"": {
						""height"": 2130,
						""width"": 3840
					}
				},
				""replyCount"": 2,
				""repostCount"": 4,
				""likeCount"": 32,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-07T19:55:37.217Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:wd4j56spe4afm7mhudx7yexa"",
					""handle"": ""devjoolz.bsky.social"",
					""displayName"": ""jkelly 🏴󠁧󠁢󠁳󠁣󠁴󠁿"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:wd4j56spe4afm7mhudx7yexa/bafkreicwsa37pmcadcxetn6fa3jaqi2gkyu7vb7ifge4q4aqonnddb7szq@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lb67vfdfyg2y"",
						""followedBy"": ""at://did:plc:wd4j56spe4afm7mhudx7yexa/app.bsky.graph.follow/3lb5kadldjw2r""
					},
					""labels"": [],
					""createdAt"": ""2024-11-15T12:07:33.961Z""
				},
				""indexedAt"": ""2024-12-08T02:18:45.713Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:xud6ge4te4exsw7asqcbjf4g/app.bsky.feed.post/3lcr4x4lsdm22"",
				""cid"": ""bafyreihbrtysuvnkydhtcifwvtg73tytnwid4rucivcyhian6febkhdk7e"",
				""author"": {
					""did"": ""did:plc:xud6ge4te4exsw7asqcbjf4g"",
					""handle"": ""estherschindler.bsky.social"",
					""displayName"": ""Esther Schindler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreiadgon6gwivttid5jsbk6orgcqzxysa77pbmxpzrbwgwpvaav2pbm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3latuftaoib2j"",
						""followedBy"": ""at://did:plc:xud6ge4te4exsw7asqcbjf4g/app.bsky.graph.follow/3latte4ni3q2w""
					},
					""labels"": [],
					""createdAt"": ""2023-04-24T17:28:17.327Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:08:41.000Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.images"",
						""images"": [
							{
								""alt"": """",
								""aspectRatio"": {
									""height"": 1042,
									""width"": 754
								},
								""image"": {
									""$type"": ""blob"",
									""ref"": {
										""$link"": ""bafkreia764eiq3x35bly6yrpyksdmwxfa2kksgrsferzjaxd4y25mg5cxq""
									},
									""mimeType"": ""image/jpeg"",
									""size"": 257526
								}
							}
						]
					},
					""langs"": [],
					""text"": ""In answer to all those people who say: But you don't have seasons! ""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.images#view"",
					""images"": [
						{
							""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreia764eiq3x35bly6yrpyksdmwxfa2kksgrsferzjaxd4y25mg5cxq@jpeg"",
							""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreia764eiq3x35bly6yrpyksdmwxfa2kksgrsferzjaxd4y25mg5cxq@jpeg"",
							""alt"": """",
							""aspectRatio"": {
								""height"": 1042,
								""width"": 754
							}
						}
					]
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 2,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:08:43.715Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:4jc3okcenipmjl5pjxzckty6/app.bsky.feed.post/3lcoikiosqv2x"",
				""cid"": ""bafyreigbo7beibwpmh4b2y2t6edb44e4xc4z7wv3a2544tzjsqlxfawyj4"",
				""author"": {
					""did"": ""did:plc:4jc3okcenipmjl5pjxzckty6"",
					""handle"": ""brideoflinux.bsky.social"",
					""displayName"": ""Christine Hall"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:4jc3okcenipmjl5pjxzckty6/bafkreifoy3z75desnqhlrlcwbfzye4cqnq32fxv5obbaat6iyaxicnuyhq@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-07-03T20:53:16.066Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T00:58:24.732Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.images"",
						""images"": [
							{
								""alt"": """",
								""image"": {
									""$type"": ""blob"",
									""ref"": {
										""$link"": ""bafkreifgxvgkyae3i45qtonyop332yg2ruxmjjv7bm3x36ealmxpe3ckom""
									},
									""mimeType"": ""image/jpeg"",
									""size"": 47360
								}
							}
						]
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://buff.ly/3ZsPDbR""
								}
							],
							""index"": {
								""byteEnd"": 142,
								""byteStart"": 119
							}
						}
					],
					""text"": ""Just published on FOSS Force: How Many People In Your State Are Trying to Get the Same Tech Job That You’re Seeking? https://buff.ly/3ZsPDbR""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.images#view"",
					""images"": [
						{
							""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:4jc3okcenipmjl5pjxzckty6/bafkreifgxvgkyae3i45qtonyop332yg2ruxmjjv7bm3x36ealmxpe3ckom@jpeg"",
							""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:4jc3okcenipmjl5pjxzckty6/bafkreifgxvgkyae3i45qtonyop332yg2ruxmjjv7bm3x36ealmxpe3ckom@jpeg"",
							""alt"": """"
						}
					]
				},
				""replyCount"": 0,
				""repostCount"": 1,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-07T00:58:25.820Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:xud6ge4te4exsw7asqcbjf4g"",
					""handle"": ""estherschindler.bsky.social"",
					""displayName"": ""Esther Schindler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreiadgon6gwivttid5jsbk6orgcqzxysa77pbmxpzrbwgwpvaav2pbm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3latuftaoib2j"",
						""followedBy"": ""at://did:plc:xud6ge4te4exsw7asqcbjf4g/app.bsky.graph.follow/3latte4ni3q2w""
					},
					""labels"": [],
					""createdAt"": ""2023-04-24T17:28:17.327Z""
				},
				""indexedAt"": ""2024-12-08T02:03:00.118Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:qczklopsofm7f3urh2shtwfs/app.bsky.feed.post/3lcr4mk6z5c2a"",
				""cid"": ""bafyreieukfojfgfws5ud2yfozgmodxmjd6dcim4pkmdxsyggtn45qzrzza"",
				""author"": {
					""did"": ""did:plc:qczklopsofm7f3urh2shtwfs"",
					""handle"": ""importedreality.com"",
					""displayName"": ""ImportedReality"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:qczklopsofm7f3urh2shtwfs/bafkreibe7wao55fdsnzeakklr6f2x3jrpxniusrx5gpo6epnlji42h7noi@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3latufft74b2c"",
						""followedBy"": ""at://did:plc:qczklopsofm7f3urh2shtwfs/app.bsky.graph.follow/3lassh4vj6h2e""
					},
					""labels"": [],
					""createdAt"": ""2024-11-02T17:12:03.015Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T02:02:47.749Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.images"",
						""images"": [
							{
								""alt"": ""A pressure cooker full of some delicious Chili"",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 1992
								},
								""image"": {
									""$type"": ""blob"",
									""ref"": {
										""$link"": ""bafkreibmlyldacoqihhepxas5vpxvbxjcfc4lwyhoiklyuazmgctehxx3y""
									},
									""mimeType"": ""image/jpeg"",
									""size"": 745724
								}
							}
						]
					},
					""langs"": [
						""en""
					],
					""text"": ""Nothing warms you up on a cold winter day like a piping hot bowl of Chilli 😋""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.images#view"",
					""images"": [
						{
							""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:qczklopsofm7f3urh2shtwfs/bafkreibmlyldacoqihhepxas5vpxvbxjcfc4lwyhoiklyuazmgctehxx3y@jpeg"",
							""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:qczklopsofm7f3urh2shtwfs/bafkreibmlyldacoqihhepxas5vpxvbxjcfc4lwyhoiklyuazmgctehxx3y@jpeg"",
							""alt"": ""A pressure cooker full of some delicious Chili"",
							""aspectRatio"": {
								""height"": 2000,
								""width"": 1992
							}
						}
					]
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 2,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T02:02:48.818Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcr3vkiskk2g"",
				""cid"": ""bafyreic73vu525etyfha5hrsyjlykxhmmbocqxcd2fmtsxqmwbj44zxbuq"",
				""author"": {
					""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
					""handle"": ""ardalis.com"",
					""displayName"": ""ardalis (Steve Smith)"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
						""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
					},
					""labels"": [],
					""createdAt"": ""2023-07-01T19:01:03.320Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:49:56.331Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
							""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227""
						},
						""root"": {
							""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
							""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227""
						}
					},
					""text"": ""Yes obviously""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:49:56.612Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227"",
					""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
					""author"": {
						""did"": ""did:plc:64ryvurqwzr6ljn5v7lwninh"",
						""handle"": ""filmgirl.bsky.social"",
						""displayName"": ""Christina Warren"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:64ryvurqwzr6ljn5v7lwninh/bafkreia7h5afj7iwvgjbrnyhmfqp7wrtthlk7u6yqjogh2rzr5wqb3hc2q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-03-09T01:45:08.371Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T08:52:20.608Z"",
						""langs"": [
							""en""
						],
						""text"": ""With the knowledge that I’m flying back from Korea tonight and thus will be insanely jet lagged and I’m going to Rome on Thursday, should I try to do the final Eras Tour show in Vancouver on Sunday? Plz respond yes or no b/c no poll function""
					},
					""replyCount"": 26,
					""repostCount"": 0,
					""likeCount"": 20,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T08:52:21.417Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227"",
					""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
					""author"": {
						""did"": ""did:plc:64ryvurqwzr6ljn5v7lwninh"",
						""handle"": ""filmgirl.bsky.social"",
						""displayName"": ""Christina Warren"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:64ryvurqwzr6ljn5v7lwninh/bafkreia7h5afj7iwvgjbrnyhmfqp7wrtthlk7u6yqjogh2rzr5wqb3hc2q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-03-09T01:45:08.371Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T08:52:20.608Z"",
						""langs"": [
							""en""
						],
						""text"": ""With the knowledge that I’m flying back from Korea tonight and thus will be insanely jet lagged and I’m going to Rome on Thursday, should I try to do the final Eras Tour show in Vancouver on Sunday? Plz respond yes or no b/c no poll function""
					},
					""replyCount"": 26,
					""repostCount"": 0,
					""likeCount"": 20,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T08:52:21.417Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:h7umuggqfvmgh6ydrulpiu44/app.bsky.feed.post/3lcr3aauhgs22"",
				""cid"": ""bafyreibcyyhdnwulfeb2hqdj2bu7xq734x2qwnaaptfwwrspxzec57mwb4"",
				""author"": {
					""did"": ""did:plc:h7umuggqfvmgh6ydrulpiu44"",
					""handle"": ""soniacuff.com"",
					""displayName"": ""Sonia Cuff"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:h7umuggqfvmgh6ydrulpiu44/bafkreiekvvowvzeyfjvwmthjhhebx6ixtdgqctphdqcpjucapf2t7qaug4@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lccl3cfe5n2d""
					},
					""labels"": [],
					""createdAt"": ""2023-05-02T22:23:41.892Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:38:01.583Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
							""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227""
						},
						""root"": {
							""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
							""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227""
						}
					},
					""text"": ""I am the queen of rest and balance but this is only a yes. Also how the heck would you get a ticket?""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:38:02.618Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227"",
					""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
					""author"": {
						""did"": ""did:plc:64ryvurqwzr6ljn5v7lwninh"",
						""handle"": ""filmgirl.bsky.social"",
						""displayName"": ""Christina Warren"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:64ryvurqwzr6ljn5v7lwninh/bafkreia7h5afj7iwvgjbrnyhmfqp7wrtthlk7u6yqjogh2rzr5wqb3hc2q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-03-09T01:45:08.371Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T08:52:20.608Z"",
						""langs"": [
							""en""
						],
						""text"": ""With the knowledge that I’m flying back from Korea tonight and thus will be insanely jet lagged and I’m going to Rome on Thursday, should I try to do the final Eras Tour show in Vancouver on Sunday? Plz respond yes or no b/c no poll function""
					},
					""replyCount"": 26,
					""repostCount"": 0,
					""likeCount"": 20,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T08:52:21.417Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:64ryvurqwzr6ljn5v7lwninh/app.bsky.feed.post/3lcpczxa22227"",
					""cid"": ""bafyreigpk3zmzrm3nzrytu35xjok5g2nupabtkmi7fax7p2c77m3efuigm"",
					""author"": {
						""did"": ""did:plc:64ryvurqwzr6ljn5v7lwninh"",
						""handle"": ""filmgirl.bsky.social"",
						""displayName"": ""Christina Warren"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:64ryvurqwzr6ljn5v7lwninh/bafkreia7h5afj7iwvgjbrnyhmfqp7wrtthlk7u6yqjogh2rzr5wqb3hc2q@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-03-09T01:45:08.371Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T08:52:20.608Z"",
						""langs"": [
							""en""
						],
						""text"": ""With the knowledge that I’m flying back from Korea tonight and thus will be insanely jet lagged and I’m going to Rome on Thursday, should I try to do the final Eras Tour show in Vancouver on Sunday? Plz respond yes or no b/c no poll function""
					},
					""replyCount"": 26,
					""repostCount"": 0,
					""likeCount"": 20,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T08:52:21.417Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.feed.post/3lcr34pyoc22h"",
				""cid"": ""bafyreihsznpkfyxfeugjdy5r4q6xg5xu3mfzv3lflziddizld52z4tbhly"",
				""author"": {
					""did"": ""did:plc:m3p5j3o66yghzlkbwnbgmcsi"",
					""handle"": ""merill.net"",
					""displayName"": ""Merill Fernando 💚"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:m3p5j3o66yghzlkbwnbgmcsi/bafkreid4iexettc7kwclgh52jj4p5tbxmu6v4zywicdp2berr4jfvdiibm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3l7s4txisow2m"",
						""followedBy"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.graph.follow/3l7rtl3s4h42u""
					},
					""labels"": [],
					""createdAt"": ""2023-04-23T22:25:56.230Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:36:03.232Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreifyqk3qifufm7me5sndt3wonrwdpbkac23nzjp5bi2f4g2x636uku"",
							""uri"": ""at://did:plc:a5ce2zyyfsivusu6s65rsixr/app.bsky.feed.post/3lcqu7dcqjc2o""
						},
						""root"": {
							""cid"": ""bafyreibidsge4s7ygllhmoxd325i4jlnqwpi6hrz7tm33frbw4aolwg2tq"",
							""uri"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.feed.post/3lah4vuwaxr2w""
						}
					},
					""text"": ""Yes""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:36:04.216Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.feed.post/3lah4vuwaxr2w"",
					""cid"": ""bafyreibidsge4s7ygllhmoxd325i4jlnqwpi6hrz7tm33frbw4aolwg2tq"",
					""author"": {
						""did"": ""did:plc:m3p5j3o66yghzlkbwnbgmcsi"",
						""handle"": ""merill.net"",
						""displayName"": ""Merill Fernando 💚"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:m3p5j3o66yghzlkbwnbgmcsi/bafkreid4iexettc7kwclgh52jj4p5tbxmu6v4zywicdp2berr4jfvdiibm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3l7s4txisow2m"",
							""followedBy"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.graph.follow/3l7rtl3s4h42u""
						},
						""labels"": [],
						""createdAt"": ""2023-04-23T22:25:56.230Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-11-08T15:51:00.222Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.external"",
							""external"": {
								""description"": ""Use this page to search for the Microsoft community on bluesky.ms."",
								""thumb"": {
									""$type"": ""blob"",
									""ref"": {
										""$link"": ""bafkreihseulsxoc5zlidvtpa3cftxh2vennmgagbyoozdxwyqtejwgjp7y""
									},
									""mimeType"": ""image/jpeg"",
									""size"": 268082
								},
								""title"": ""Search bluesky.ms"",
								""uri"": ""https://bluesky.ms/""
							}
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#link"",
										""uri"": ""https://bluesky.ms""
									}
								],
								""index"": {
									""byteEnd"": 27,
									""byteStart"": 17
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""🦋 Introducing bluesky.ms 👏 = A crowdsourced database of anyone and everyone in the Microsoft community on Bluesky.\n\n👉 Add yourself and anyone you know today 👈\n\n🫂 All are welcome.\n\nThis is my v1, I'll add options to directly follow from the site itself but first 👇\n\nLET'S FILL IT UP! 🙏""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.external#view"",
						""external"": {
							""uri"": ""https://bluesky.ms/"",
							""title"": ""Search bluesky.ms"",
							""description"": ""Use this page to search for the Microsoft community on bluesky.ms."",
							""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:m3p5j3o66yghzlkbwnbgmcsi/bafkreihseulsxoc5zlidvtpa3cftxh2vennmgagbyoozdxwyqtejwgjp7y@jpeg""
						}
					},
					""replyCount"": 60,
					""repostCount"": 263,
					""likeCount"": 588,
					""quoteCount"": 33,
					""indexedAt"": ""2024-11-08T15:51:01.222Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:a5ce2zyyfsivusu6s65rsixr/app.bsky.feed.post/3lcqu7dcqjc2o"",
					""cid"": ""bafyreifyqk3qifufm7me5sndt3wonrwdpbkac23nzjp5bi2f4g2x636uku"",
					""author"": {
						""did"": ""did:plc:a5ce2zyyfsivusu6s65rsixr"",
						""handle"": ""sqlworldwide.bsky.social"",
						""displayName"": ""Taiob Ali"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:a5ce2zyyfsivusu6s65rsixr/bafkreiamozda3f7jzedmwr3kui6lyhkm2gonmihvlbkdmyrtkce6rzzwly@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-08-18T12:50:36.757Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:32:14.401Z"",
						""facets"": [
							{
								""$type"": ""app.bsky.richtext.facet"",
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#mention"",
										""did"": ""did:plc:m3p5j3o66yghzlkbwnbgmcsi""
									}
								],
								""index"": {
									""byteEnd"": 11,
									""byteStart"": 0
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#link"",
										""uri"": ""https://bluesky.ms""
									}
								],
								""index"": {
									""byteEnd"": 59,
									""byteStart"": 49
								}
							}
						],
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreibidsge4s7ygllhmoxd325i4jlnqwpi6hrz7tm33frbw4aolwg2tq"",
								""uri"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.feed.post/3lah4vuwaxr2w""
							},
							""root"": {
								""cid"": ""bafyreibidsge4s7ygllhmoxd325i4jlnqwpi6hrz7tm33frbw4aolwg2tq"",
								""uri"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.feed.post/3lah4vuwaxr2w""
							}
						},
						""text"": ""@merill.net  do I need to sign up for account in bluesky.ms for the MVP badge to show up in my profile?""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T23:32:14.524Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:m3p5j3o66yghzlkbwnbgmcsi"",
					""handle"": ""merill.net"",
					""displayName"": ""Merill Fernando 💚"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:m3p5j3o66yghzlkbwnbgmcsi/bafkreid4iexettc7kwclgh52jj4p5tbxmu6v4zywicdp2berr4jfvdiibm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3l7s4txisow2m"",
						""followedBy"": ""at://did:plc:m3p5j3o66yghzlkbwnbgmcsi/app.bsky.graph.follow/3l7rtl3s4h42u""
					},
					""labels"": [],
					""createdAt"": ""2023-04-23T22:25:56.230Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:t3yrezpfsotf5odizahregq3/app.bsky.feed.post/3lcr2zcadr22x"",
				""cid"": ""bafyreigzndyfxpw3ufizxvfiqb5olce7ruka3cugcn5oh2lzoygazdkgbm"",
				""author"": {
					""did"": ""did:plc:t3yrezpfsotf5odizahregq3"",
					""handle"": ""merriemcgaw.bsky.social"",
					""displayName"": ""Merrie McGaw"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:t3yrezpfsotf5odizahregq3/bafkreigi6pdfaepm4is6hw6npri2cfxhpxsblk4qdjc52p53ajlgikfz3e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lb67ypbfip2y"",
						""followedBy"": ""at://did:plc:t3yrezpfsotf5odizahregq3/app.bsky.graph.follow/3lb5z5tkc4a2y""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T01:35:41.311Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:34:08.141Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiawnzfeb4omxfei6ypreu5l44uma5zskevbqeyraowf3qmcxpjgsu"",
							""uri"": ""at://did:plc:6uimut56ihor2p6ubnepkudz/app.bsky.feed.post/3lcr2ingjz227""
						},
						""root"": {
							""cid"": ""bafyreiawnzfeb4omxfei6ypreu5l44uma5zskevbqeyraowf3qmcxpjgsu"",
							""uri"": ""at://did:plc:6uimut56ihor2p6ubnepkudz/app.bsky.feed.post/3lcr2ingjz227""
						}
					},
					""text"": ""That was GREAT! The perfect response.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:34:08.414Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:6uimut56ihor2p6ubnepkudz/app.bsky.feed.post/3lcr2ingjz227"",
					""cid"": ""bafyreiawnzfeb4omxfei6ypreu5l44uma5zskevbqeyraowf3qmcxpjgsu"",
					""author"": {
						""did"": ""did:plc:6uimut56ihor2p6ubnepkudz"",
						""handle"": ""jenrubin.bsky.social"",
						""displayName"": ""Jen Rubin"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:6uimut56ihor2p6ubnepkudz/bafkreicap5kbaxk4vkncytxcjbthajcwnxvpzf7rqkbyedt65dwa76gfda@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-10T02:32:03.322Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T01:24:49.452Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.record"",
							""record"": {
								""cid"": ""bafyreiaokaf7wgfso3ou2yu24cgmo5ozeryrwt5d3xoa3yxsznzpwfencm"",
								""uri"": ""at://did:plc:dd5oeph7qd6x42t6j2kumgvr/app.bsky.feed.post/3lcqyvqlflc2h""
							}
						},
						""langs"": [
							""en""
						],
						""text"": ""Best letter. Ever.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.record#view"",
						""record"": {
							""$type"": ""app.bsky.embed.record#viewRecord"",
							""uri"": ""at://did:plc:dd5oeph7qd6x42t6j2kumgvr/app.bsky.feed.post/3lcqyvqlflc2h"",
							""cid"": ""bafyreiaokaf7wgfso3ou2yu24cgmo5ozeryrwt5d3xoa3yxsznzpwfencm"",
							""author"": {
								""did"": ""did:plc:dd5oeph7qd6x42t6j2kumgvr"",
								""handle"": ""calltoactivism.bsky.social"",
								""displayName"": ""CALL TO ACTIVISM"",
								""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreihvmi4wwst5dpo5wcppowlbth2el6qy4k2apquw7ny3n5iwvdlnku@jpeg"",
								""associated"": {
									""chat"": {
										""allowIncoming"": ""all""
									}
								},
								""viewer"": {
									""muted"": false,
									""blockedBy"": false
								},
								""labels"": [],
								""createdAt"": ""2023-05-23T00:56:31.178Z""
							},
							""value"": {
								""$type"": ""app.bsky.feed.post"",
								""createdAt"": ""2024-12-08T00:56:21.481Z"",
								""embed"": {
									""$type"": ""app.bsky.embed.images"",
									""images"": [
										{
											""alt"": """",
											""aspectRatio"": {
												""height"": 1527,
												""width"": 1080
											},
											""image"": {
												""$type"": ""blob"",
												""ref"": {
													""$link"": ""bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a""
												},
												""mimeType"": ""image/jpeg"",
												""size"": 490038
											}
										},
										{
											""alt"": """",
											""aspectRatio"": {
												""height"": 1494,
												""width"": 1080
											},
											""image"": {
												""$type"": ""blob"",
												""ref"": {
													""$link"": ""bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu""
												},
												""mimeType"": ""image/jpeg"",
												""size"": 265676
											}
										}
									]
								},
								""langs"": [
									""en""
								],
								""text"": ""Holy shit. Did you see the letter Olivia Troye’s attorney sent to Kash Patel after he threatened to sue her for defamation? Troye, a former Homeland Security advisor, had the BEST response.""
							},
							""labels"": [],
							""likeCount"": 6339,
							""replyCount"": 375,
							""repostCount"": 1491,
							""quoteCount"": 207,
							""indexedAt"": ""2024-12-08T00:56:25.621Z"",
							""embeds"": [
								{
									""$type"": ""app.bsky.embed.images#view"",
									""images"": [
										{
											""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a@jpeg"",
											""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a@jpeg"",
											""alt"": """",
											""aspectRatio"": {
												""height"": 1527,
												""width"": 1080
											}
										},
										{
											""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu@jpeg"",
											""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu@jpeg"",
											""alt"": """",
											""aspectRatio"": {
												""height"": 1494,
												""width"": 1080
											}
										}
									]
								}
							]
						}
					},
					""replyCount"": 34,
					""repostCount"": 128,
					""likeCount"": 922,
					""quoteCount"": 9,
					""indexedAt"": ""2024-12-08T01:24:49.715Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:6uimut56ihor2p6ubnepkudz/app.bsky.feed.post/3lcr2ingjz227"",
					""cid"": ""bafyreiawnzfeb4omxfei6ypreu5l44uma5zskevbqeyraowf3qmcxpjgsu"",
					""author"": {
						""did"": ""did:plc:6uimut56ihor2p6ubnepkudz"",
						""handle"": ""jenrubin.bsky.social"",
						""displayName"": ""Jen Rubin"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:6uimut56ihor2p6ubnepkudz/bafkreicap5kbaxk4vkncytxcjbthajcwnxvpzf7rqkbyedt65dwa76gfda@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-10T02:32:03.322Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T01:24:49.452Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.record"",
							""record"": {
								""cid"": ""bafyreiaokaf7wgfso3ou2yu24cgmo5ozeryrwt5d3xoa3yxsznzpwfencm"",
								""uri"": ""at://did:plc:dd5oeph7qd6x42t6j2kumgvr/app.bsky.feed.post/3lcqyvqlflc2h""
							}
						},
						""langs"": [
							""en""
						],
						""text"": ""Best letter. Ever.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.record#view"",
						""record"": {
							""$type"": ""app.bsky.embed.record#viewRecord"",
							""uri"": ""at://did:plc:dd5oeph7qd6x42t6j2kumgvr/app.bsky.feed.post/3lcqyvqlflc2h"",
							""cid"": ""bafyreiaokaf7wgfso3ou2yu24cgmo5ozeryrwt5d3xoa3yxsznzpwfencm"",
							""author"": {
								""did"": ""did:plc:dd5oeph7qd6x42t6j2kumgvr"",
								""handle"": ""calltoactivism.bsky.social"",
								""displayName"": ""CALL TO ACTIVISM"",
								""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreihvmi4wwst5dpo5wcppowlbth2el6qy4k2apquw7ny3n5iwvdlnku@jpeg"",
								""associated"": {
									""chat"": {
										""allowIncoming"": ""all""
									}
								},
								""viewer"": {
									""muted"": false,
									""blockedBy"": false
								},
								""labels"": [],
								""createdAt"": ""2023-05-23T00:56:31.178Z""
							},
							""value"": {
								""$type"": ""app.bsky.feed.post"",
								""createdAt"": ""2024-12-08T00:56:21.481Z"",
								""embed"": {
									""$type"": ""app.bsky.embed.images"",
									""images"": [
										{
											""alt"": """",
											""aspectRatio"": {
												""height"": 1527,
												""width"": 1080
											},
											""image"": {
												""$type"": ""blob"",
												""ref"": {
													""$link"": ""bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a""
												},
												""mimeType"": ""image/jpeg"",
												""size"": 490038
											}
										},
										{
											""alt"": """",
											""aspectRatio"": {
												""height"": 1494,
												""width"": 1080
											},
											""image"": {
												""$type"": ""blob"",
												""ref"": {
													""$link"": ""bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu""
												},
												""mimeType"": ""image/jpeg"",
												""size"": 265676
											}
										}
									]
								},
								""langs"": [
									""en""
								],
								""text"": ""Holy shit. Did you see the letter Olivia Troye’s attorney sent to Kash Patel after he threatened to sue her for defamation? Troye, a former Homeland Security advisor, had the BEST response.""
							},
							""labels"": [],
							""likeCount"": 6339,
							""replyCount"": 375,
							""repostCount"": 1491,
							""quoteCount"": 207,
							""indexedAt"": ""2024-12-08T00:56:25.621Z"",
							""embeds"": [
								{
									""$type"": ""app.bsky.embed.images#view"",
									""images"": [
										{
											""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a@jpeg"",
											""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreifgwkknkznkfp2qhcbu5phjl6ugxtfiwk6um2kmclmo7ror7iqt4a@jpeg"",
											""alt"": """",
											""aspectRatio"": {
												""height"": 1527,
												""width"": 1080
											}
										},
										{
											""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu@jpeg"",
											""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:dd5oeph7qd6x42t6j2kumgvr/bafkreibmvrjdfcijxod3pdqnj4k3qg2ggjtqxqmzmamll4bcttwiqxfanu@jpeg"",
											""alt"": """",
											""aspectRatio"": {
												""height"": 1494,
												""width"": 1080
											}
										}
									]
								}
							]
						}
					},
					""replyCount"": 34,
					""repostCount"": 128,
					""likeCount"": 922,
					""quoteCount"": 9,
					""indexedAt"": ""2024-12-08T01:24:49.715Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcqzzlx4vk2p"",
				""cid"": ""bafyreiaz4pt76jqgvjofut3b4tz2h7kz7bx4qdnc2whnxt2rhxdnelmrk4"",
				""author"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:16:24.583Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreihlk3ixwdtog4q36lvandpzw66dnghlsnatyspsrdl32czau5icj4"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcqzy45hok2p""
						},
						""root"": {
							""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
							""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z""
						}
					},
					""text"": ""They probably are. Let me rephrase the question: how do they taste when white?""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:16:24.018Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z"",
					""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
					""author"": {
						""did"": ""did:plc:2anyrijvpsfnkvg5gzjjvugg"",
						""handle"": ""gardengirl13.bsky.social"",
						""displayName"": ""Dr. Dandelion"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreig2lwjnx7eiprni3ybqhxmcfuoescm6vjc4qblcosgm23i6mx7q2q@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-14T00:42:57.319Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T11:31:43.907Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 1500
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 764372
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 1238,
										""width"": 1584
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 507960
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Daydreaming about spring. I plant these bell peppers most years. I love how they start nearly white. 🌱""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 1500
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 1238,
									""width"": 1584
								}
							}
						]
					},
					""replyCount"": 6,
					""repostCount"": 2,
					""likeCount"": 67,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T11:31:46.113Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcqzy45hok2p"",
					""cid"": ""bafyreihlk3ixwdtog4q36lvandpzw66dnghlsnatyspsrdl32czau5icj4"",
					""author"": {
						""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
						""handle"": ""chayotejarocho.space"",
						""displayName"": ""Carlos Sánchez López"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
						},
						""labels"": [
							{
								""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
								""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
								""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""2024-11-12T05:41:20.432Z""
							}
						],
						""createdAt"": ""2024-11-12T05:41:20.038Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T01:15:34.458Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
								""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z""
							},
							""root"": {
								""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
								""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z""
							}
						},
						""text"": ""Are they edible when white?""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T01:15:33.711Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:2anyrijvpsfnkvg5gzjjvugg"",
					""handle"": ""gardengirl13.bsky.social"",
					""displayName"": ""Dr. Dandelion"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreig2lwjnx7eiprni3ybqhxmcfuoescm6vjc4qblcosgm23i6mx7q2q@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:42:57.319Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.feed.post/3lcqzy45hok2p"",
				""cid"": ""bafyreihlk3ixwdtog4q36lvandpzw66dnghlsnatyspsrdl32czau5icj4"",
				""author"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:15:34.458Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
							""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z""
						},
						""root"": {
							""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
							""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z""
						}
					},
					""text"": ""Are they edible when white?""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:15:33.711Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z"",
					""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
					""author"": {
						""did"": ""did:plc:2anyrijvpsfnkvg5gzjjvugg"",
						""handle"": ""gardengirl13.bsky.social"",
						""displayName"": ""Dr. Dandelion"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreig2lwjnx7eiprni3ybqhxmcfuoescm6vjc4qblcosgm23i6mx7q2q@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-14T00:42:57.319Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T11:31:43.907Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 1500
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 764372
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 1238,
										""width"": 1584
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 507960
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Daydreaming about spring. I plant these bell peppers most years. I love how they start nearly white. 🌱""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 1500
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 1238,
									""width"": 1584
								}
							}
						]
					},
					""replyCount"": 6,
					""repostCount"": 2,
					""likeCount"": 67,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T11:31:46.113Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2anyrijvpsfnkvg5gzjjvugg/app.bsky.feed.post/3lcplwxit6c2z"",
					""cid"": ""bafyreieiqirgzoggvjhtv72lst4ciwzvtji4v4k4txk7e5xm5mdigdtkya"",
					""author"": {
						""did"": ""did:plc:2anyrijvpsfnkvg5gzjjvugg"",
						""handle"": ""gardengirl13.bsky.social"",
						""displayName"": ""Dr. Dandelion"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreig2lwjnx7eiprni3ybqhxmcfuoescm6vjc4qblcosgm23i6mx7q2q@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-14T00:42:57.319Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T11:31:43.907Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 1500
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 764372
									}
								},
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 1238,
										""width"": 1584
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 507960
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Daydreaming about spring. I plant these bell peppers most years. I love how they start nearly white. 🌱""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreiey4nznak7glcddt3yjl5efkn66mfrp4uawf2bio2oh2ldkyfwhi4@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 1500
								}
							},
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2anyrijvpsfnkvg5gzjjvugg/bafkreieciilyc6kzrfztvtpagrhnggytawqdsoslbmarztr6wo4dkkv4be@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 1238,
									""width"": 1584
								}
							}
						]
					},
					""replyCount"": 6,
					""repostCount"": 2,
					""likeCount"": 67,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T11:31:46.113Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:y2jx6e2zkxmygpcpld2xxa47/app.bsky.feed.post/3lcq2hdjvy226"",
				""cid"": ""bafyreiapujkshnrbzxpkqfupvrkswnfiz5demwcscqdn43fyrzcs4qc7ui"",
				""author"": {
					""did"": ""did:plc:y2jx6e2zkxmygpcpld2xxa47"",
					""handle"": ""ladyjenpool.bsky.social"",
					""displayName"": ""Jen"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:y2jx6e2zkxmygpcpld2xxa47/bafkreighbun3r2bldll2efl2eovp2uf2vsut72l3dbpxzw4x3slq3s42ji@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-08-20T03:22:55.592Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-07T15:51:25.783Z"",
					""langs"": [
						""en""
					],
					""text"": ""I don't know who needs to hear this, but you don't need to justify your reading preferences and choices to anyone. Audiobooks count as reading. Also, you don't have to finish books you don't enjoy. Don't read to impress, read for yourself. Life's too short, love yourself, ok?\n\nSigned, a librarian.""
				},
				""replyCount"": 727,
				""repostCount"": 2859,
				""likeCount"": 23355,
				""quoteCount"": 179,
				""indexedAt"": ""2024-12-07T15:51:23.621Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reason"": {
				""$type"": ""app.bsky.feed.defs#reasonRepost"",
				""by"": {
					""did"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
					""handle"": ""chayotejarocho.space"",
					""displayName"": ""Carlos Sánchez López"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:u6oezqzkhb5epe65ggeuhfqo/bafkreia77ntwdmsi6e5pzyxqu2eyoc2ds6ohqwq563eutdiap3gdxim7du@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lardff3lcp2h""
					},
					""labels"": [
						{
							""src"": ""did:plc:u6oezqzkhb5epe65ggeuhfqo"",
							""uri"": ""at://did:plc:u6oezqzkhb5epe65ggeuhfqo/app.bsky.actor.profile/self"",
							""cid"": ""bafyreid4ge5wid4vlfjckqk4yb4pyaclrothrswvebwrmleuhjk5jsea4i"",
							""val"": ""!no-unauthenticated"",
							""cts"": ""2024-11-12T05:41:20.432Z""
						}
					],
					""createdAt"": ""2024-11-12T05:41:20.038Z""
				},
				""indexedAt"": ""2024-12-08T01:15:13.014Z""
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.feed.post/3lcqzqwjpqs2j"",
				""cid"": ""bafyreicfiipjhtkkneymaxnpvfn7webhnvr2qgsn2blc32kqfh5yzgghga"",
				""author"": {
					""did"": ""did:plc:tamw32kks6jfqqtkmzc3fpdm"",
					""handle"": ""box-of-stuff.bsky.social"",
					""displayName"": ""No Cats Here"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:tamw32kks6jfqqtkmzc3fpdm/bafkreihkn76oc3a2esz42lkskxtupk6qviaojnc35heecoos2awiwe2vau@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lciomi5uwa22"",
						""followedBy"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.graph.follow/3lcgualp6ek2y""
					},
					""labels"": [],
					""createdAt"": ""2024-11-17T21:58:14.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:11:33.687Z"",
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""theodinproject""
								}
							],
							""index"": {
								""byteEnd"": 30,
								""byteStart"": 15
							}
						},
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#tag"",
									""tag"": ""100dev""
								}
							],
							""index"": {
								""byteEnd"": 127,
								""byteStart"": 120
							}
						}
					],
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreihhnfw7o5oqnya6wss7o5ufrhehinuw3vbi5wnu3vunsr6tmy6jki"",
							""uri"": ""at://did:plc:bmez6aoimo2z6v4hwgwoxu7p/app.bsky.feed.post/3lcqz3j7rws2d""
						},
						""root"": {
							""cid"": ""bafyreig2dj36457tlog2fx5p557sqdjcecjtmzsip2ynmslzbbpxpzdxfy"",
							""uri"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.feed.post/3lcqwltt4v22j""
						}
					},
					""text"": ""Yeah, same for #theodinproject. People are real there and it's amazing. \n\nBtw if you have insights by any chance .. did #100dev start any new cohort this year? Or any plans for 2025?""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:11:33.413Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.feed.post/3lcqwltt4v22j"",
					""cid"": ""bafyreig2dj36457tlog2fx5p557sqdjcecjtmzsip2ynmslzbbpxpzdxfy"",
					""author"": {
						""did"": ""did:plc:tamw32kks6jfqqtkmzc3fpdm"",
						""handle"": ""box-of-stuff.bsky.social"",
						""displayName"": ""No Cats Here"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:tamw32kks6jfqqtkmzc3fpdm/bafkreihkn76oc3a2esz42lkskxtupk6qviaojnc35heecoos2awiwe2vau@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lciomi5uwa22"",
							""followedBy"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.graph.follow/3lcgualp6ek2y""
						},
						""labels"": [],
						""createdAt"": ""2024-11-17T21:58:14.815Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:15:01.852Z"",
						""langs"": [
							""en""
						],
						""text"": ""Random thought about self-teaching 1/n\n\nI was trying to learn how to code myself 2.5 times. First by signing into full-stack (python+ react) bootcamp. Finished python basics and dropped out because life happened.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:15:01.111Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:bmez6aoimo2z6v4hwgwoxu7p/app.bsky.feed.post/3lcqz3j7rws2d"",
					""cid"": ""bafyreihhnfw7o5oqnya6wss7o5ufrhehinuw3vbi5wnu3vunsr6tmy6jki"",
					""author"": {
						""did"": ""did:plc:bmez6aoimo2z6v4hwgwoxu7p"",
						""handle"": ""kaceyrenee.bsky.social"",
						""displayName"": ""Kacey"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:bmez6aoimo2z6v4hwgwoxu7p/bafkreihsjevbvwbe2b5sgcpruwqapi52rcpwia3pmk3bc2he43w4vymkau@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:bmez6aoimo2z6v4hwgwoxu7p"",
								""uri"": ""at://did:plc:bmez6aoimo2z6v4hwgwoxu7p/app.bsky.actor.profile/self"",
								""cid"": ""bafyreihekpfvrtoiwq63rucsr37v7ce7ct76ftj6pgnmu3mfqqxwhziidm"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""2024-11-18T16:18:50.505Z""
							}
						],
						""createdAt"": ""2024-11-18T16:18:51.380Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:59:35.088Z"",
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""100Devs""
									}
								],
								""index"": {
									""byteEnd"": 61,
									""byteStart"": 53
								}
							},
							{
								""$type"": ""app.bsky.richtext.facet"",
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#mention"",
										""did"": ""did:plc:geq7xsn3o7gdl3jujp4ifoar""
									}
								],
								""index"": {
									""byteEnd"": 128,
									""byteStart"": 107
								}
							},
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""100Devs""
									}
								],
								""index"": {
									""byteEnd"": 280,
									""byteStart"": 272
								}
							}
						],
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreiad6kl5tke2pqsj2knxew4xwg63tehujnptkxf2bvydv3yhg5syn4"",
								""uri"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.feed.post/3lcqxflqm722j""
							},
							""root"": {
								""cid"": ""bafyreig2dj36457tlog2fx5p557sqdjcecjtmzsip2ynmslzbbpxpzdxfy"",
								""uri"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.feed.post/3lcqwltt4v22j""
							}
						},
						""text"": ""I feel your pain... but at least for me, personally, #100Devs has been a game changer.\n\nSpecifically since @leonnoel.bsky.social is a real person and so fun to learn from. Tutorial hell is so boring and I'd lose interest quickly. Different strokes for different folks but #100Devs truly has been FUN.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:59:34.811Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:tamw32kks6jfqqtkmzc3fpdm"",
					""handle"": ""box-of-stuff.bsky.social"",
					""displayName"": ""No Cats Here"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:tamw32kks6jfqqtkmzc3fpdm/bafkreihkn76oc3a2esz42lkskxtupk6qviaojnc35heecoos2awiwe2vau@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lciomi5uwa22"",
						""followedBy"": ""at://did:plc:tamw32kks6jfqqtkmzc3fpdm/app.bsky.graph.follow/3lcgualp6ek2y""
					},
					""labels"": [],
					""createdAt"": ""2024-11-17T21:58:14.815Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqzjlwfes2b"",
				""cid"": ""bafyreielgxb7ds2tgj37eflnomj62agj45lbc7iwwmp4byqrj2at2unwd4"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:07:27.686Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreicywqcolmrnqrola3womn77cjeowpc7vlquobsegvwpzrqfx52xmy"",
							""uri"": ""at://did:plc:awx5xeduginviy7hrrhjqfx5/app.bsky.feed.post/3lcqvj26mps2p""
						},
						""root"": {
							""cid"": ""bafyreicywqcolmrnqrola3womn77cjeowpc7vlquobsegvwpzrqfx52xmy"",
							""uri"": ""at://did:plc:awx5xeduginviy7hrrhjqfx5/app.bsky.feed.post/3lcqvj26mps2p""
						}
					},
					""text"": ""These are adorable! Do it!""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:07:28.118Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:awx5xeduginviy7hrrhjqfx5/app.bsky.feed.post/3lcqvj26mps2p"",
					""cid"": ""bafyreicywqcolmrnqrola3womn77cjeowpc7vlquobsegvwpzrqfx52xmy"",
					""author"": {
						""did"": ""did:plc:awx5xeduginviy7hrrhjqfx5"",
						""handle"": ""limerickslife.com"",
						""displayName"": ""Sharon Slater"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreiafkjcytgx66frzdvqa4rqexerfvmpea232kdgrhms6esrbdmzhoa@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-10-03T10:02:47.403Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:55:34.099Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""Four knitted dolls. All wearing different styled clothes and hair"",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 725432
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Thinking of becoming one of those ladies that enters their crafts in the Limerick Show!""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q@jpeg"",
								""alt"": ""Four knitted dolls. All wearing different styled clothes and hair"",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 2,
					""repostCount"": 2,
					""likeCount"": 11,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T23:55:37.817Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:awx5xeduginviy7hrrhjqfx5/app.bsky.feed.post/3lcqvj26mps2p"",
					""cid"": ""bafyreicywqcolmrnqrola3womn77cjeowpc7vlquobsegvwpzrqfx52xmy"",
					""author"": {
						""did"": ""did:plc:awx5xeduginviy7hrrhjqfx5"",
						""handle"": ""limerickslife.com"",
						""displayName"": ""Sharon Slater"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreiafkjcytgx66frzdvqa4rqexerfvmpea232kdgrhms6esrbdmzhoa@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-10-03T10:02:47.403Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T23:55:34.099Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""Four knitted dolls. All wearing different styled clothes and hair"",
									""aspectRatio"": {
										""height"": 2000,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 725432
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""Thinking of becoming one of those ladies that enters their crafts in the Limerick Show!""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:awx5xeduginviy7hrrhjqfx5/bafkreihbifdj4qib4qxnpseljebiu6nvib2wvqraio4bvyjjfhjal6m45q@jpeg"",
								""alt"": ""Four knitted dolls. All wearing different styled clothes and hair"",
								""aspectRatio"": {
									""height"": 2000,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 2,
					""repostCount"": 2,
					""likeCount"": 11,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T23:55:37.817Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqzfglu3c2b"",
				""cid"": ""bafyreigch6q3jkeibwgrqdloxi7mwdmm7fe6gpede3reldvwqjw2kxughy"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:05:07.882Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiewua4o6jcjftxiqeiuc6ixxx3ybftwhh4stgjsieh3xfvutwm3pm"",
							""uri"": ""at://did:plc:tpgnsvgqsu5b752uud3wmtja/app.bsky.feed.post/3lcqwndny6s2n""
						},
						""root"": {
							""cid"": ""bafyreiewua4o6jcjftxiqeiuc6ixxx3ybftwhh4stgjsieh3xfvutwm3pm"",
							""uri"": ""at://did:plc:tpgnsvgqsu5b752uud3wmtja/app.bsky.feed.post/3lcqwndny6s2n""
						}
					},
					""text"": ""Yes please""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:05:08.426Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:tpgnsvgqsu5b752uud3wmtja/app.bsky.feed.post/3lcqwndny6s2n"",
					""cid"": ""bafyreiewua4o6jcjftxiqeiuc6ixxx3ybftwhh4stgjsieh3xfvutwm3pm"",
					""author"": {
						""did"": ""did:plc:tpgnsvgqsu5b752uud3wmtja"",
						""handle"": ""tjmahr.com"",
						""displayName"": ""tj mahr 🎅"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:tpgnsvgqsu5b752uud3wmtja/bafkreifzncdevk567nc2baibwic3wwmdgiam3nm4zoxjlqpc5ocbmmompq@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-05-13T02:58:31.704Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:15:52.015Z"",
						""langs"": [
							""en""
						],
						""text"": ""where’s the AI for muting/blocking text in screenshots""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 2,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:15:52.324Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:tpgnsvgqsu5b752uud3wmtja/app.bsky.feed.post/3lcqwndny6s2n"",
					""cid"": ""bafyreiewua4o6jcjftxiqeiuc6ixxx3ybftwhh4stgjsieh3xfvutwm3pm"",
					""author"": {
						""did"": ""did:plc:tpgnsvgqsu5b752uud3wmtja"",
						""handle"": ""tjmahr.com"",
						""displayName"": ""tj mahr 🎅"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:tpgnsvgqsu5b752uud3wmtja/bafkreifzncdevk567nc2baibwic3wwmdgiam3nm4zoxjlqpc5ocbmmompq@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-05-13T02:58:31.704Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:15:52.015Z"",
						""langs"": [
							""en""
						],
						""text"": ""where’s the AI for muting/blocking text in screenshots""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 2,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:15:52.324Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:al22q57r347wb5nvrpkqkl7x/app.bsky.feed.post/3lcqzf22ajc2n"",
				""cid"": ""bafyreidcwp55xt26rtcve7x56ppk7du3ra5axhgpptqrxjt7p527twkvfm"",
				""author"": {
					""did"": ""did:plc:al22q57r347wb5nvrpkqkl7x"",
					""handle"": ""growhighlow.bsky.social"",
					""displayName"": ""Brian Engler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:al22q57r347wb5nvrpkqkl7x/bafkreian4x37s3ohbzpo7vc3rumaqvvkb4k2lxsnouyfjkwherchayahxy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3k4cwibsqfs2p"",
						""followedBy"": ""at://did:plc:al22q57r347wb5nvrpkqkl7x/app.bsky.graph.follow/3k3ot7p4hot2k""
					},
					""labels"": [],
					""createdAt"": ""2023-07-26T00:43:13.703Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:04:54.722Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreie4cborv6nkaotqfcrkbgqdsjf2u4wfy55p2fx46vydzhhle77h6y"",
							""uri"": ""at://did:plc:vxitab3itp27me5ynvygr33k/app.bsky.feed.post/3lcqxwvskls2v""
						},
						""root"": {
							""cid"": ""bafyreie4cborv6nkaotqfcrkbgqdsjf2u4wfy55p2fx46vydzhhle77h6y"",
							""uri"": ""at://did:plc:vxitab3itp27me5ynvygr33k/app.bsky.feed.post/3lcqxwvskls2v""
						}
					},
					""text"": ""Could be worse, you could be a K-state fan and watch Ohio State be ranked 6th with the QB you drove off. 😂""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:04:56.015Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:vxitab3itp27me5ynvygr33k/app.bsky.feed.post/3lcqxwvskls2v"",
					""cid"": ""bafyreie4cborv6nkaotqfcrkbgqdsjf2u4wfy55p2fx46vydzhhle77h6y"",
					""author"": {
						""did"": ""did:plc:vxitab3itp27me5ynvygr33k"",
						""handle"": ""thrillcat.bsky.social"",
						""displayName"": ""thrillcat"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:vxitab3itp27me5ynvygr33k/bafkreiff62dp4ihdt2hmed24amoguukqjznnm7x37rnu5pftpkhovnxkpi@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-30T02:03:53.236Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:39:06.770Z"",
						""facets"": [
							{
								""$type"": ""app.bsky.richtext.facet"",
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#mention"",
										""did"": ""did:plc:ggxe4v3y6za43lpbpywddtdd""
									}
								],
								""index"": {
									""byteEnd"": 87,
									""byteStart"": 63
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Good to post my frustration at ISU FB and see my old colleague @vannishanks.bsky.social in my notifications. You can’t have me sitting 10 rows behind you anymore but you can still hear me complain online. 😂""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:39:07.218Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:vxitab3itp27me5ynvygr33k/app.bsky.feed.post/3lcqxwvskls2v"",
					""cid"": ""bafyreie4cborv6nkaotqfcrkbgqdsjf2u4wfy55p2fx46vydzhhle77h6y"",
					""author"": {
						""did"": ""did:plc:vxitab3itp27me5ynvygr33k"",
						""handle"": ""thrillcat.bsky.social"",
						""displayName"": ""thrillcat"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:vxitab3itp27me5ynvygr33k/bafkreiff62dp4ihdt2hmed24amoguukqjznnm7x37rnu5pftpkhovnxkpi@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-30T02:03:53.236Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:39:06.770Z"",
						""facets"": [
							{
								""$type"": ""app.bsky.richtext.facet"",
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#mention"",
										""did"": ""did:plc:ggxe4v3y6za43lpbpywddtdd""
									}
								],
								""index"": {
									""byteEnd"": 87,
									""byteStart"": 63
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Good to post my frustration at ISU FB and see my old colleague @vannishanks.bsky.social in my notifications. You can’t have me sitting 10 rows behind you anymore but you can still hear me complain online. 😂""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:39:07.218Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:al22q57r347wb5nvrpkqkl7x/app.bsky.feed.post/3lcqzbcs4ck2n"",
				""cid"": ""bafyreigpyjcakr6a3dexounw432zzh4vajjowejl3g52d5uahu3cicpbsi"",
				""author"": {
					""did"": ""did:plc:al22q57r347wb5nvrpkqkl7x"",
					""handle"": ""growhighlow.bsky.social"",
					""displayName"": ""Brian Engler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:al22q57r347wb5nvrpkqkl7x/bafkreian4x37s3ohbzpo7vc3rumaqvvkb4k2lxsnouyfjkwherchayahxy@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3k4cwibsqfs2p"",
						""followedBy"": ""at://did:plc:al22q57r347wb5nvrpkqkl7x/app.bsky.graph.follow/3k3ot7p4hot2k""
					},
					""labels"": [],
					""createdAt"": ""2023-07-26T00:43:13.703Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:02:49.674Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreibnfvq2obi6d64b7whlmklhymoaeddsf6wssfbj6htpywom3eikuu"",
							""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.feed.post/3lcqyadvs5s2j""
						},
						""root"": {
							""cid"": ""bafyreibnfvq2obi6d64b7whlmklhymoaeddsf6wssfbj6htpywom3eikuu"",
							""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.feed.post/3lcqyadvs5s2j""
						}
					},
					""text"": ""Reminds me, I don't remember the last time I've watched Oklahoma State.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:02:51.323Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.feed.post/3lcqyadvs5s2j"",
					""cid"": ""bafyreibnfvq2obi6d64b7whlmklhymoaeddsf6wssfbj6htpywom3eikuu"",
					""author"": {
						""did"": ""did:plc:ypk2gwoe5qpe54udozopkini"",
						""handle"": ""stevierea.bsky.social"",
						""displayName"": ""Stephen C. Rea"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ypk2gwoe5qpe54udozopkini/bafkreieos3vknxyr7zi3ffrmjm7z5clmkri7ssdrs6qoq3i67i5rz2boje@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:ypk2gwoe5qpe54udozopkini"",
								""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.actor.profile/self"",
								""cid"": ""bafyreiatrev3zos3yfihx3wfnbu4zrek5ijlxa6n7qzlyr5innekhe3kte"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-05-25T00:15:23.990Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:44:23.548Z"",
						""langs"": [
							""en""
						],
						""text"": ""I’m going to say it if no one else will:\n\nQuinn Ewers should’ve never gotten rid of the mullet""
					},
					""replyCount"": 2,
					""repostCount"": 0,
					""likeCount"": 3,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:44:23.820Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.feed.post/3lcqyadvs5s2j"",
					""cid"": ""bafyreibnfvq2obi6d64b7whlmklhymoaeddsf6wssfbj6htpywom3eikuu"",
					""author"": {
						""did"": ""did:plc:ypk2gwoe5qpe54udozopkini"",
						""handle"": ""stevierea.bsky.social"",
						""displayName"": ""Stephen C. Rea"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ypk2gwoe5qpe54udozopkini/bafkreieos3vknxyr7zi3ffrmjm7z5clmkri7ssdrs6qoq3i67i5rz2boje@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [
							{
								""src"": ""did:plc:ypk2gwoe5qpe54udozopkini"",
								""uri"": ""at://did:plc:ypk2gwoe5qpe54udozopkini/app.bsky.actor.profile/self"",
								""cid"": ""bafyreiatrev3zos3yfihx3wfnbu4zrek5ijlxa6n7qzlyr5innekhe3kte"",
								""val"": ""!no-unauthenticated"",
								""cts"": ""1970-01-01T00:00:00.000Z""
							}
						],
						""createdAt"": ""2023-05-25T00:15:23.990Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:44:23.548Z"",
						""langs"": [
							""en""
						],
						""text"": ""I’m going to say it if no one else will:\n\nQuinn Ewers should’ve never gotten rid of the mullet""
					},
					""replyCount"": 2,
					""repostCount"": 0,
					""likeCount"": 3,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:44:23.820Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqzbaj7cc2b"",
				""cid"": ""bafyreigcv2xhw2xvzc6jwadn74kafcwi7lnpf37xn6htp6oto4e33rn5va"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T01:02:47.286Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreibuv5xyb4pzqhgajli3wbgrir2kpmeulbt2wbkl4oq6wvmenxehfe"",
							""uri"": ""at://did:plc:ofzkhjyyh4kl4a35wxgmobmm/app.bsky.feed.post/3lcqxi6xacc2q""
						},
						""root"": {
							""cid"": ""bafyreibuv5xyb4pzqhgajli3wbgrir2kpmeulbt2wbkl4oq6wvmenxehfe"",
							""uri"": ""at://did:plc:ofzkhjyyh4kl4a35wxgmobmm/app.bsky.feed.post/3lcqxi6xacc2q""
						}
					},
					""text"": ""Mmmmmm…. I can imagine :)""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T01:02:48.111Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:ofzkhjyyh4kl4a35wxgmobmm/app.bsky.feed.post/3lcqxi6xacc2q"",
					""cid"": ""bafyreibuv5xyb4pzqhgajli3wbgrir2kpmeulbt2wbkl4oq6wvmenxehfe"",
					""author"": {
						""did"": ""did:plc:ofzkhjyyh4kl4a35wxgmobmm"",
						""handle"": ""sonjadrimmer.bsky.social"",
						""displayName"": ""Sonja Drimmer"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ofzkhjyyh4kl4a35wxgmobmm/bafkreiezdrkwnju63iuh74asnfj5divzr7ioobessijto6t7v6srfimodm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-08-19T14:21:25.410Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:30:53.046Z"",
						""langs"": [
							""en""
						],
						""text"": ""I don’t like to brag but.\n\nWe picked up some beets from a local farm yesterday and I just roasted them and they are amazing.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 16,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:30:53.221Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:ofzkhjyyh4kl4a35wxgmobmm/app.bsky.feed.post/3lcqxi6xacc2q"",
					""cid"": ""bafyreibuv5xyb4pzqhgajli3wbgrir2kpmeulbt2wbkl4oq6wvmenxehfe"",
					""author"": {
						""did"": ""did:plc:ofzkhjyyh4kl4a35wxgmobmm"",
						""handle"": ""sonjadrimmer.bsky.social"",
						""displayName"": ""Sonja Drimmer"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:ofzkhjyyh4kl4a35wxgmobmm/bafkreiezdrkwnju63iuh74asnfj5divzr7ioobessijto6t7v6srfimodm@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-08-19T14:21:25.410Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:30:53.046Z"",
						""langs"": [
							""en""
						],
						""text"": ""I don’t like to brag but.\n\nWe picked up some beets from a local farm yesterday and I just roasted them and they are amazing.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 16,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:30:53.221Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqyxgqtck2g"",
				""cid"": ""bafyreihrixgmm3znm27qxlbdeujf3si5q2hkiprbug2jezvuwq2h6fzbai"",
				""author"": {
					""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
					""handle"": ""ardalis.com"",
					""displayName"": ""ardalis (Steve Smith)"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
						""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
					},
					""labels"": [],
					""createdAt"": ""2023-07-01T19:01:03.320Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:57:18.282Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreidjg5bsy72k4ns7g7axpkjvn4kzps5rfok4gteesunw4n5djfelem"",
							""uri"": ""at://did:plc:5uwpxw3pddke4dp2pnmwoems/app.bsky.feed.post/3lcqyq2fgyc2u""
						},
						""root"": {
							""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
							""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g""
						}
					},
					""text"": ""This guy gets it""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:57:19.024Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g"",
					""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
					""author"": {
						""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
						""handle"": ""ardalis.com"",
						""displayName"": ""ardalis (Steve Smith)"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
							""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
						},
						""labels"": [],
						""createdAt"": ""2023-07-01T19:01:03.320Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:11:25.344Z"",
						""langs"": [
							""en""
						],
						""text"": ""The way so much snow and ice just disappeared into the air today was simply sublime.""
					},
					""replyCount"": 1,
					""repostCount"": 1,
					""likeCount"": 5,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T22:11:25.617Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:5uwpxw3pddke4dp2pnmwoems/app.bsky.feed.post/3lcqyq2fgyc2u"",
					""cid"": ""bafyreidjg5bsy72k4ns7g7axpkjvn4kzps5rfok4gteesunw4n5djfelem"",
					""author"": {
						""did"": ""did:plc:5uwpxw3pddke4dp2pnmwoems"",
						""handle"": ""arthurdoler.com"",
						""displayName"": ""Arthur Doler"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:5uwpxw3pddke4dp2pnmwoems/bafkreihq34sjxe65aolje5z2q2yhp3mcbu3tsxvujkqenjktspbscmxha4@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""all""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-07T18:08:09.955Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:53:10.446Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreidakp72sfsmibrbwvdn62yddlg7xf56w3cgi57zhi2gdukfazqyye"",
								""uri"": ""at://did:plc:5uwpxw3pddke4dp2pnmwoems/app.bsky.feed.post/3lcqyphyfes2u""
							},
							""root"": {
								""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
								""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g""
							}
						},
						""text"": ""THIS post is sublime.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 2,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:53:11.018Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:5uwpxw3pddke4dp2pnmwoems"",
					""handle"": ""arthurdoler.com"",
					""displayName"": ""Arthur Doler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:5uwpxw3pddke4dp2pnmwoems/bafkreihq34sjxe65aolje5z2q2yhp3mcbu3tsxvujkqenjktspbscmxha4@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""all""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false
					},
					""labels"": [],
					""createdAt"": ""2023-06-07T18:08:09.955Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:qkulxlxgznoyw4vdy7nu2mof/app.bsky.feed.post/3lcqyxc7d2k22"",
				""cid"": ""bafyreieu3ilyubbczj5cgxucvwtkfqck3xoxiwdtfxo2vy7cwszgr3rsom"",
				""author"": {
					""did"": ""did:plc:qkulxlxgznoyw4vdy7nu2mof"",
					""handle"": ""sinclairinat0r.com"",
					""displayName"": ""Jeremy Sinclair #ฺNET"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:qkulxlxgznoyw4vdy7nu2mof/bafkreih4i5r5uckmhrqajwqvlc7254fmngikinklmjvzkksxsq6h4ddt5i@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3koik3a4aov2a"",
						""followedBy"": ""at://did:plc:qkulxlxgznoyw4vdy7nu2mof/app.bsky.graph.follow/3kojbr646gj2x""
					},
					""labels"": [],
					""createdAt"": ""2023-04-22T18:28:48.482Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:57:13.513Z"",
					""facets"": [
						{
							""$type"": ""app.bsky.richtext.facet"",
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#mention"",
									""did"": ""did:plc:omn7aezn55fdq5fjm7hxu7wq""
								}
							],
							""index"": {
								""byteEnd"": 144,
								""byteStart"": 117
							}
						}
					],
					""langs"": [
						""en""
					],
					""text"": ""😂😂 I just now discovered when my grandson starts going, \""Ayeeeeee!\"", he wants to listen to music. We can thank @glotheofficial.bsky.social for that one lmao""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 4,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:57:13.824Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqyszvgy22n"",
				""cid"": ""bafyreicry5d43hiifpp3l3pel5oxfcly4uvez4fgs47wwnajfrdmx6ysgu"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:54:50.585Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreihbnbahzo5iejwb7jkijaakiggbsufhouvn6tg62fe7b4bz4e2pja"",
							""uri"": ""at://did:plc:vb6u4estbczjpm465dnwpxtv/app.bsky.feed.post/3lcq3eqkapc2p""
						},
						""root"": {
							""cid"": ""bafyreihbnbahzo5iejwb7jkijaakiggbsufhouvn6tg62fe7b4bz4e2pja"",
							""uri"": ""at://did:plc:vb6u4estbczjpm465dnwpxtv/app.bsky.feed.post/3lcq3eqkapc2p""
						}
					},
					""text"": ""This white lady do and it feels like everyone around me do too. :D""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:54:51.723Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:vb6u4estbczjpm465dnwpxtv/app.bsky.feed.post/3lcq3eqkapc2p"",
					""cid"": ""bafyreihbnbahzo5iejwb7jkijaakiggbsufhouvn6tg62fe7b4bz4e2pja"",
					""author"": {
						""did"": ""did:plc:vb6u4estbczjpm465dnwpxtv"",
						""handle"": ""purplecar.bsky.social"",
						""displayName"": ""Christine Cavalier✅"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:vb6u4estbczjpm465dnwpxtv/bafkreid4fyncw7zfsm5nlsv56zmvr4bwf3vrrwj4mjkgmmmpmuksgzrvru@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-22T00:35:04.428Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T16:07:52.504Z"",
						""langs"": [
							""en""
						],
						""text"": ""Can I ask some Black, Vietnamese or Puerto Rican people - do youse all put paprika or other seasonings besides salt and pepper in your egg salad? \n\nBecause the white ladies around here don’t and I’m wondering where I got the idea. Youse all are who I grew up with.""
					},
					""replyCount"": 2,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T16:07:52.714Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:vb6u4estbczjpm465dnwpxtv/app.bsky.feed.post/3lcq3eqkapc2p"",
					""cid"": ""bafyreihbnbahzo5iejwb7jkijaakiggbsufhouvn6tg62fe7b4bz4e2pja"",
					""author"": {
						""did"": ""did:plc:vb6u4estbczjpm465dnwpxtv"",
						""handle"": ""purplecar.bsky.social"",
						""displayName"": ""Christine Cavalier✅"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:vb6u4estbczjpm465dnwpxtv/bafkreid4fyncw7zfsm5nlsv56zmvr4bwf3vrrwj4mjkgmmmpmuksgzrvru@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-06-22T00:35:04.428Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T16:07:52.504Z"",
						""langs"": [
							""en""
						],
						""text"": ""Can I ask some Black, Vietnamese or Puerto Rican people - do youse all put paprika or other seasonings besides salt and pepper in your egg salad? \n\nBecause the white ladies around here don’t and I’m wondering where I got the idea. Youse all are who I grew up with.""
					},
					""replyCount"": 2,
					""repostCount"": 0,
					""likeCount"": 1,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T16:07:52.714Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqyql2nic2n"",
				""cid"": ""bafyreifjuocppqn74ss2e6jt3ah3vssih2trmtctxncw272qbih2og3kay"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:53:27.917Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiaifmi7dcgzcrztm5vesltxq5fv5dqmntnz6wbma6kix63echeliy"",
							""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqymdue3c2p""
						},
						""root"": {
							""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
							""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23""
						}
					},
					""text"": ""SHHHHHH! I was joking. I’m not building anything. I was just rambling.""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:53:28.614Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23"",
					""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
					""author"": {
						""did"": ""did:plc:2uagjr4c4rcustcqnhlclabv"",
						""handle"": ""mrfumblethumbs.bsky.social"",
						""displayName"": ""Brian Greiner 🇨🇦🍁"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreidks2mo7qstsk6evrqbrfxlzniu2eirjw6ooxetnknypaszoyr27u@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-14T19:51:24.424Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T21:13:08.974Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""Spice cookies cooling on a wire rack \n"",
									""aspectRatio"": {
										""height"": 1921,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 825570
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""baking""
									}
								],
								""index"": {
									""byteEnd"": 81,
									""byteStart"": 74
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Today's baking was molasses spice cookies. \nVery seasonal and delicious.\n\n#baking""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""alt"": ""Spice cookies cooling on a wire rack \n"",
								""aspectRatio"": {
									""height"": 1921,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 4,
					""repostCount"": 0,
					""likeCount"": 4,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T21:13:10.028Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqymdue3c2p"",
					""cid"": ""bafyreiaifmi7dcgzcrztm5vesltxq5fv5dqmntnz6wbma6kix63echeliy"",
					""author"": {
						""did"": ""did:plc:2uagjr4c4rcustcqnhlclabv"",
						""handle"": ""mrfumblethumbs.bsky.social"",
						""displayName"": ""Brian Greiner 🇨🇦🍁"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreidks2mo7qstsk6evrqbrfxlzniu2eirjw6ooxetnknypaszoyr27u@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-14T19:51:24.424Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-08T00:51:06.154Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreiav5uqrnnczbku2nrwacarb5ib7dotlpjci7eoc7rnmuvl4z6g3gm"",
								""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqyjli3722m""
							},
							""root"": {
								""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
								""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23""
							}
						},
						""text"": ""Thank you.\nDid you get an MZO from Dougie for that house?""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-08T00:51:05.015Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqyn6afts2g"",
				""cid"": ""bafyreiecox5yilf5dbl36brzv5qfcjfmb2lzu55f6s3dsxh6swtnyiapxu"",
				""author"": {
					""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
					""handle"": ""ardalis.com"",
					""displayName"": ""ardalis (Steve Smith)"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
						""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
					},
					""labels"": [],
					""createdAt"": ""2023-07-01T19:01:03.320Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:51:33.812Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreidotrmkszyeayeynw3qria6p6yowchywilurrnsq6zzytda6dzhc4"",
							""uri"": ""at://did:plc:55fz3mxcsksuz422vhpvdrba/app.bsky.feed.post/3lcqrnvjzxs2s""
						},
						""root"": {
							""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
							""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g""
						}
					},
					""text"": ""I don’t think very many people appreciated my wit here…""
				},
				""replyCount"": 2,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:51:34.118Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g"",
					""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
					""author"": {
						""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
						""handle"": ""ardalis.com"",
						""displayName"": ""ardalis (Steve Smith)"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false,
							""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
							""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
						},
						""labels"": [],
						""createdAt"": ""2023-07-01T19:01:03.320Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:11:25.344Z"",
						""langs"": [
							""en""
						],
						""text"": ""The way so much snow and ice just disappeared into the air today was simply sublime.""
					},
					""replyCount"": 1,
					""repostCount"": 1,
					""likeCount"": 5,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T22:11:25.617Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:55fz3mxcsksuz422vhpvdrba/app.bsky.feed.post/3lcqrnvjzxs2s"",
					""cid"": ""bafyreidotrmkszyeayeynw3qria6p6yowchywilurrnsq6zzytda6dzhc4"",
					""author"": {
						""did"": ""did:plc:55fz3mxcsksuz422vhpvdrba"",
						""handle"": ""tedneward.bsky.social"",
						""displayName"": ""Ted Neward"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:55fz3mxcsksuz422vhpvdrba/bafkreihpl5jq5sipt3s7r22bykhhihf2vqtzvvxuaou4hs4damq4t2sioe@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-05-31T02:42:54.372Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T22:46:42.052Z"",
						""langs"": [
							""en""
						],
						""reply"": {
							""parent"": {
								""cid"": ""bafyreicgqw7h3hmgmduxto3pjvcx26rpp6krmcpfzqq7umtxc33fufzxbu"",
								""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqpu6fuzs2g""
							},
							""root"": {
								""cid"": ""bafyreiaeemg5w64mfajr6vcwts2wejupux5jtw252xanwtcwvkwpiwpmii"",
								""uri"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.feed.post/3lcqposvbc22g""
							}
						},
						""text"": ""This post is sublime.""
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T22:46:42.511Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""grandparentAuthor"": {
					""did"": ""did:plc:3vo24c2exro34bgvkqcupbot"",
					""handle"": ""ardalis.com"",
					""displayName"": ""ardalis (Steve Smith)"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:3vo24c2exro34bgvkqcupbot/bafkreiegxothjkfv3pvag6ecfzdhqsn7xhd2o4pm4go2ep6wczqw6vqwnq@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3kbupv35tkx2w"",
						""followedBy"": ""at://did:plc:3vo24c2exro34bgvkqcupbot/app.bsky.graph.follow/3kbunlknd2z26""
					},
					""labels"": [],
					""createdAt"": ""2023-07-01T19:01:03.320Z""
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqymdbdok2m"",
				""cid"": ""bafyreiausvzhnpnhsi7yrmyegphqp3zt3vdhqvukqapprvmxpsp5irbtxq"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:51:05.529Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreiemkhtt7cjogiqq7ueodmyhzcy573tgsxh6veklmntu2lf5ns4d2i"",
							""uri"": ""at://did:plc:r65vbpxaevahryame7jvbcey/app.bsky.feed.post/3lcqa32pxqs23""
						},
						""root"": {
							""cid"": ""bafyreiemkhtt7cjogiqq7ueodmyhzcy573tgsxh6veklmntu2lf5ns4d2i"",
							""uri"": ""at://did:plc:r65vbpxaevahryame7jvbcey/app.bsky.feed.post/3lcqa32pxqs23""
						}
					},
					""text"": ""👏🏾👏🏾👏🏾""
				},
				""replyCount"": 0,
				""repostCount"": 0,
				""likeCount"": 0,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:51:06.223Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:r65vbpxaevahryame7jvbcey/app.bsky.feed.post/3lcqa32pxqs23"",
					""cid"": ""bafyreiemkhtt7cjogiqq7ueodmyhzcy573tgsxh6veklmntu2lf5ns4d2i"",
					""author"": {
						""did"": ""did:plc:r65vbpxaevahryame7jvbcey"",
						""handle"": ""ellewilson.bsky.social"",
						""displayName"": ""Laura Wilson"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreiewem37me2wcr6afrpck5rrcabbj4d4w2ypdqimt4v6ob7f527x34@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-14T00:18:53.616Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T17:31:56.335Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 1637,
										""width"": 1080
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 358524
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""In 2011, I was sworn in to practice law, at the Vermont Supreme Court.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 1637,
									""width"": 1080
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T17:31:57.227Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:r65vbpxaevahryame7jvbcey/app.bsky.feed.post/3lcqa32pxqs23"",
					""cid"": ""bafyreiemkhtt7cjogiqq7ueodmyhzcy573tgsxh6veklmntu2lf5ns4d2i"",
					""author"": {
						""did"": ""did:plc:r65vbpxaevahryame7jvbcey"",
						""handle"": ""ellewilson.bsky.social"",
						""displayName"": ""Laura Wilson"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreiewem37me2wcr6afrpck5rrcabbj4d4w2ypdqimt4v6ob7f527x34@jpeg"",
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2024-11-14T00:18:53.616Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T17:31:56.335Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": """",
									""aspectRatio"": {
										""height"": 1637,
										""width"": 1080
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 358524
									}
								}
							]
						},
						""langs"": [
							""en""
						],
						""text"": ""In 2011, I was sworn in to practice law, at the Vermont Supreme Court.""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:r65vbpxaevahryame7jvbcey/bafkreia5qcy4syihmvjsab7ntqqoxfiiogjblrdrf3u3wgsqfsqv6kici4@jpeg"",
								""alt"": """",
								""aspectRatio"": {
									""height"": 1637,
									""width"": 1080
								}
							}
						]
					},
					""replyCount"": 1,
					""repostCount"": 0,
					""likeCount"": 0,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T17:31:57.227Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.feed.post/3lcqyjli3722m"",
				""cid"": ""bafyreiav5uqrnnczbku2nrwacarb5ib7dotlpjci7eoc7rnmuvl4z6g3gm"",
				""author"": {
					""did"": ""did:plc:efiq5u65jhg5lg4d6jwr5jsf"",
					""handle"": ""anniepettit.bsky.social"",
					""displayName"": ""Annie Pettit (LoveStats, She/They) 🎸🍞 🧀 🦋 🗿 ✍️ 🇨🇦"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:efiq5u65jhg5lg4d6jwr5jsf/bafkreia33mdrlfyxtntz5d324bbzvuai2stnajnvdhwr7cat6mahyx2j2e@jpeg"",
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3lbryusxhrr2b"",
						""followedBy"": ""at://did:plc:efiq5u65jhg5lg4d6jwr5jsf/app.bsky.graph.follow/3lbrwtks6tp2b""
					},
					""labels"": [],
					""createdAt"": ""2024-11-14T00:49:55.815Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:49:33.475Z"",
					""langs"": [
						""en""
					],
					""reply"": {
						""parent"": {
							""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
							""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23""
						},
						""root"": {
							""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
							""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23""
						}
					},
					""text"": ""They look lovely! I baked the pieces of a gingerbread house today. Building tomorrow :)""
				},
				""replyCount"": 1,
				""repostCount"": 0,
				""likeCount"": 1,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:49:34.317Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			},
			""reply"": {
				""root"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23"",
					""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
					""author"": {
						""did"": ""did:plc:2uagjr4c4rcustcqnhlclabv"",
						""handle"": ""mrfumblethumbs.bsky.social"",
						""displayName"": ""Brian Greiner 🇨🇦🍁"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreidks2mo7qstsk6evrqbrfxlzniu2eirjw6ooxetnknypaszoyr27u@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-14T19:51:24.424Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T21:13:08.974Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""Spice cookies cooling on a wire rack \n"",
									""aspectRatio"": {
										""height"": 1921,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 825570
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""baking""
									}
								],
								""index"": {
									""byteEnd"": 81,
									""byteStart"": 74
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Today's baking was molasses spice cookies. \nVery seasonal and delicious.\n\n#baking""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""alt"": ""Spice cookies cooling on a wire rack \n"",
								""aspectRatio"": {
									""height"": 1921,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 4,
					""repostCount"": 0,
					""likeCount"": 4,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T21:13:10.028Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				},
				""parent"": {
					""$type"": ""app.bsky.feed.defs#postView"",
					""uri"": ""at://did:plc:2uagjr4c4rcustcqnhlclabv/app.bsky.feed.post/3lcqmgmisas23"",
					""cid"": ""bafyreidw5shqwswb44kpygxgeckexur55uhdroquo5ss4nzz75p6dhc6w4"",
					""author"": {
						""did"": ""did:plc:2uagjr4c4rcustcqnhlclabv"",
						""handle"": ""mrfumblethumbs.bsky.social"",
						""displayName"": ""Brian Greiner 🇨🇦🍁"",
						""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreidks2mo7qstsk6evrqbrfxlzniu2eirjw6ooxetnknypaszoyr27u@jpeg"",
						""associated"": {
							""chat"": {
								""allowIncoming"": ""following""
							}
						},
						""viewer"": {
							""muted"": false,
							""blockedBy"": false
						},
						""labels"": [],
						""createdAt"": ""2023-07-14T19:51:24.424Z""
					},
					""record"": {
						""$type"": ""app.bsky.feed.post"",
						""createdAt"": ""2024-12-07T21:13:08.974Z"",
						""embed"": {
							""$type"": ""app.bsky.embed.images"",
							""images"": [
								{
									""alt"": ""Spice cookies cooling on a wire rack \n"",
									""aspectRatio"": {
										""height"": 1921,
										""width"": 2000
									},
									""image"": {
										""$type"": ""blob"",
										""ref"": {
											""$link"": ""bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi""
										},
										""mimeType"": ""image/jpeg"",
										""size"": 825570
									}
								}
							]
						},
						""facets"": [
							{
								""features"": [
									{
										""$type"": ""app.bsky.richtext.facet#tag"",
										""tag"": ""baking""
									}
								],
								""index"": {
									""byteEnd"": 81,
									""byteStart"": 74
								}
							}
						],
						""langs"": [
							""en""
						],
						""text"": ""Today's baking was molasses spice cookies. \nVery seasonal and delicious.\n\n#baking""
					},
					""embed"": {
						""$type"": ""app.bsky.embed.images#view"",
						""images"": [
							{
								""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""fullsize"": ""https://cdn.bsky.app/img/feed_fullsize/plain/did:plc:2uagjr4c4rcustcqnhlclabv/bafkreiccmugmtlc3nheu6nbtlqfztwvii6oszzn3pb7wy3lmxa4apduchi@jpeg"",
								""alt"": ""Spice cookies cooling on a wire rack \n"",
								""aspectRatio"": {
									""height"": 1921,
									""width"": 2000
								}
							}
						]
					},
					""replyCount"": 4,
					""repostCount"": 0,
					""likeCount"": 4,
					""quoteCount"": 0,
					""indexedAt"": ""2024-12-07T21:13:10.028Z"",
					""viewer"": {
						""threadMuted"": false,
						""embeddingDisabled"": false
					},
					""labels"": []
				}
			}
		},
		{
			""post"": {
				""uri"": ""at://did:plc:xud6ge4te4exsw7asqcbjf4g/app.bsky.feed.post/3lcqyfs5nnw22"",
				""cid"": ""bafyreig5tcifksnw2562bo2m265bqyzpjxoegfyyhikwt3m7cp3tum3xha"",
				""author"": {
					""did"": ""did:plc:xud6ge4te4exsw7asqcbjf4g"",
					""handle"": ""estherschindler.bsky.social"",
					""displayName"": ""Esther Schindler"",
					""avatar"": ""https://cdn.bsky.app/img/avatar/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreiadgon6gwivttid5jsbk6orgcqzxysa77pbmxpzrbwgwpvaav2pbm@jpeg"",
					""associated"": {
						""chat"": {
							""allowIncoming"": ""following""
						}
					},
					""viewer"": {
						""muted"": false,
						""blockedBy"": false,
						""following"": ""at://did:plc:25t5atrycib2wbdj5cpcax4k/app.bsky.graph.follow/3latuftaoib2j"",
						""followedBy"": ""at://did:plc:xud6ge4te4exsw7asqcbjf4g/app.bsky.graph.follow/3latte4ni3q2w""
					},
					""labels"": [],
					""createdAt"": ""2023-04-24T17:28:17.327Z""
				},
				""record"": {
					""$type"": ""app.bsky.feed.post"",
					""createdAt"": ""2024-12-08T00:47:25.000Z"",
					""embed"": {
						""$type"": ""app.bsky.embed.external"",
						""external"": {
							""description"": ""The air fryer is just a little oven that blows, and the idea that air frying is a new cooking technique is a myth"",
							""thumb"": {
								""$type"": ""blob"",
								""ref"": {
									""$link"": ""bafkreias53ueoh6g3s6kdmtm467uv2agkf6ljcyj26ojbruyayopnhhefa""
								},
								""mimeType"": ""image/jpeg"",
								""size"": 76984
							},
							""title"": ""The air fryer is a hoax"",
							""uri"": ""https://qz.com/air-fryer-hoax-myth-origin-convection-oven-1851378365""
						}
					},
					""facets"": [
						{
							""features"": [
								{
									""$type"": ""app.bsky.richtext.facet#link"",
									""uri"": ""https://qz.com/air-fryer-hoax-myth-origin-convection-oven-1851378365""
								}
							],
							""index"": {
								""byteEnd"": 147,
								""byteStart"": 124
							}
						}
					],
					""text"": ""Besides the wind and the crisping tray, an air fryer is really more of an optimized convection oven than a new technology. \nqz.com/air-fryer-hoa... ""
				},
				""embed"": {
					""$type"": ""app.bsky.embed.external#view"",
					""external"": {
						""uri"": ""https://qz.com/air-fryer-hoax-myth-origin-convection-oven-1851378365"",
						""title"": ""The air fryer is a hoax"",
						""description"": ""The air fryer is just a little oven that blows, and the idea that air frying is a new cooking technique is a myth"",
						""thumb"": ""https://cdn.bsky.app/img/feed_thumbnail/plain/did:plc:xud6ge4te4exsw7asqcbjf4g/bafkreias53ueoh6g3s6kdmtm467uv2agkf6ljcyj26ojbruyayopnhhefa@jpeg""
					}
				},
				""replyCount"": 2,
				""repostCount"": 0,
				""likeCount"": 3,
				""quoteCount"": 0,
				""indexedAt"": ""2024-12-08T00:47:26.919Z"",
				""viewer"": {
					""threadMuted"": false,
					""embeddingDisabled"": false
				},
				""labels"": []
			}
		}
	],
	""cursor"": ""2024-12-08T00:47:25Z""
}";

    const string ErrorTweet = @"{
	""errors"": [
		{
			""detail"": ""Could not find tweet with ids: [1]."",
			""title"": ""Not Found Error"",
			""resource_type"": ""tweet"",
			""parameter"": ""ids"",
			""value"": ""1"",
			""type"": ""https://api.twitter.com/2/problems/resource-not-found""
		}
	]
}";

}

