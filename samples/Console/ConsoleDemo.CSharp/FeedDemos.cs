using System;
using System.Linq;
using System.Threading.Tasks;
using LinqToBlueSky;
using LinqToBlueSky.Common;
using LinqToBlueSky.Feed;

using static System.Net.Mime.MediaTypeNames;

namespace ConsoleDemo.CSharp
{
    public class FeedDemos
    {
        internal static async Task RunAsync(BlueSkyContext ctx)
        {
            char key;

            do
            {
                ShowMenu();

                key = Console.ReadKey(true).KeyChar;

                switch (key)
                {
                    case '0':
                        Console.WriteLine("\n\tGetting the Timeline...\n");
                        await GetDefaultTimelineAsync(ctx);
                        break;
                    case '1':
                        Console.WriteLine("\n\tPosting...");
                        await PostAsync(ctx);
                        break;
                    case 'q':
                    case 'Q':
                        Console.WriteLine("\nReturning...\n");
                        break;
                    default:
                        Console.WriteLine(key + " is unknown");
                        break;
                }

            } while (char.ToUpper(key) != 'Q');
        }

        static void ShowMenu()
        {
            Console.WriteLine("\nFeed Demos - Please select:\n");

            Console.WriteLine("\t 0. Default Timeline");
            Console.WriteLine("\t 1. New Post");
            Console.WriteLine();
            Console.Write("\t Q. Return to Main menu");
        }

        static async Task GetDefaultTimelineAsync(BlueSkyContext ctx)
        {
            // TODO: Show how to do paging with a cursor
            // TODO: Show how to override the default limit
            // TODO: Show how to use an algorithm

            FeedQuery? tweetResponse =
                await
                (from tweet in ctx.Feed
                 where tweet.Type == FeedType.Timeline
                 select tweet)
                .SingleOrDefaultAsync();

            Console.WriteLine("Timeline Posts");

            if (tweetResponse?.Feed != null)
                tweetResponse.Feed.ForEach(tweet =>
                    Console.WriteLine(
                        $"\nID: {tweet.Post?.Cid ?? "missing"}" +
                        $"\nTweet: {tweet.Post?.Record?.Text ?? "missing"}"));
            else
                Console.WriteLine("No entries found.");
        }

        static async Task PostAsync(BlueSkyContext ctx)
        {
            //Console.Write("Enter text to tweet: ");
            //string? status = Console.ReadLine() ?? "";

            //Console.WriteLine("\nHere's what you typed: \n\n\"{0}\"", status);
            //Console.Write("\nDo you want to send this tweet? (y or n): ");
            //string? confirm = Console.ReadLine();

            //if (confirm?.ToUpper() == "N")
            //{
            //    Console.WriteLine("\nThis tweet is *not* being sent.");
            //}
            //else if (confirm?.ToUpper() == "Y")
            //{
            //    Console.WriteLine("\nPress any key to post tweet...\n");
            //    Console.ReadKey(true);

            //    Tweet? tweet = await ctx.TweetAsync(status);

            //    if (tweet != null)
            //        Console.WriteLine(
            //            "Tweet returned: " +
            //            "(" + tweet.ID + ") " +
            //            tweet.Text + "\n");
            //}
            //else
            //{
            //    Console.WriteLine(
            //        $"Sorry, you typed '{confirm}', " +
            //        $"but I only recognize 'Y' or 'N'.");
            //}
        }
    }
}
