using LinqToBlueSky.Feed;
using LinqToBlueSky.Provider;
using LinqToBlueSky.Tests.Common;

using System.Globalization;
using System.Linq.Expressions;

namespace LinqToBlueSky.Tests.FeedTests;

[TestClass]
internal class FeedRequestProcessorTests
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
		public void BuildUrl_WithMissingAlgorithm_Throws()
		{
			var tweetReqProc = new FeedRequestProcessor<FeedQuery> { BaseUrl = BaseUrl };
			var parameters =
				new Dictionary<string, string>
				{
					//{ nameof(FeedQuery.Type), FeedType.Timeline.ToString() },
			    };

			ArgumentException ex =
				L2BSkyAssert.Throws<ArgumentException>(() =>
					tweetReqProc.BuildUrl(parameters));

			Assert.AreEqual(nameof(FeedQuery.Algorithm), ex.ParamName);
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
        public void ProcessResults_Populates_FeedItems()
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
			Assert.AreEqual("Yup I agree with this one", record.Text);
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

		const string TimelineTweets = @"{
	""data"": [
		{
			""id"": ""1529568259011252224"",
			""text"": ""RT @beeradmoore: HAHAHAHA I accidentally deployed a #dotnetmaui app to my Android watch and it just worked. https://t.co/Ral7om02o1""
		},
		{
			""id"": ""1529490697618763777"",
			""text"": ""@buhakmeh @alvinashcraft Whatever is the F5 default. Haven't had problems and will probably continue until I learn about a compelling reason to change.""
		},
		{
			""id"": ""1529204113623330816"",
			""text"": ""That last presentation I did was using C# and .NET 6 on a MacBook Pro M1. With MAUI in GA, the x-plat story for .NET improves.""
		},
		{
			""id"": ""1528511616882421760"",
			""text"": ""RT @jimwooley: Looks like I won't be able to attend the inaugural @ThatConference Austin due to flight issues. I'll still present the Stati…""
		},
		{
			""id"": ""1528511186790010880"",
			""text"": ""RT @techgirl1908: Decentralized Twitter has released early code\n\nhttps://t.co/OIgGFKUkov""
		},
		{
			""id"": ""1528181517393899521"",
			""text"": ""RT @J_aa_p: My new @TwitterDev @Linq2Twitr #Blazor WASM Twitter Client (Alpha ❗) now parses\n\n✅ Retweets\n✅ Quoted Retweets\n✅ Urls\n✅ Hashtags…""
		},
		{
			""id"": ""1528100853294301184"",
			""text"": ""RT @LauraViglioni: Finally... The GitHub bathroom https://t.co/A43IM1HUaF""
		},
		{
			""id"": ""1527401749329301504"",
			""text"": ""@RafaelH_us @tacobell https://t.co/VSO4CMu6Xa""
		},
		{
			""id"": ""1527371742343114752"",
			""text"": ""RT @jguadagno: @buhakmeh @terrajobst @mohdali If you are using C#, use the Linq2Twitter NuGet package by @JoeMayo . Its super easy to use.…""
		},
		{
			""id"": ""1527016962995343360"",
			""text"": ""Presentation slides and source code for my Intro to LINQ presentation for @DataScienceDojo today:\n\nhttps://t.co/hTx6u3RHmN""
		}
	],
	""meta"": {
		""previous_token"": ""7140dibdnow9c7btw421e9l0f3cacd5qxve3023jqz48g"",
		""next_token"": ""7140dibdnow9c7btw421e9l0f3cacd5qxve3023jqz48g"",
		""result_count"": 10,
		""newest_id"": ""1529568259011252224"",
		""oldest_id"": ""1527016962995343360""
	}
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

