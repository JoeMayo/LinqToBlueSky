using System;
using System.Linq;
using System.Text.Json;
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

            FeedQuery? feedResponse =
                await
                (from feed in ctx.Feed
                 where feed.Type == FeedType.Timeline
                 select feed)
                .SingleOrDefaultAsync();

            Console.WriteLine("Timeline Posts");

            if (feedResponse?.Feed != null)
                feedResponse.Feed.ForEach(feed =>
                    Console.WriteLine(
                        $"\nID: {feed.Post?.Cid ?? "missing"}" +
                        $"\nPost: {feed.Post?.Record?.Text ?? "missing"}"));
            else
                Console.WriteLine("No entries found.");
        }

        static async Task PostAsync(BlueSkyContext ctx)
        {
            Console.Write("Enter text to post: ");
            string? text = Console.ReadLine() ?? string.Empty;

            Console.WriteLine("\nHere's what you typed: \n\n\"{0}\"", text);
            Console.Write("\nDo you want to send this post? (y or n): ");
            string? confirm = Console.ReadLine();

            if (confirm?.ToUpper() == "N")
            {
                Console.WriteLine("\nThis post is *not* being sent.");
            }
            else if (confirm?.ToUpper() == "Y")
            {
                Console.WriteLine("\nPress any key to send post...\n");
                Console.ReadKey(true);

                PostResponse? response = await ctx.PostAsync(text);

                string responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));

                if (response != null)
                    Console.WriteLine($"Response:\n {responseJson}");
            }
            else
            {
                Console.WriteLine(
                    $"Sorry, you typed '{confirm}', " +
                    $"but I only recognize 'Y' or 'N'.");
            }
        }
    }
}
