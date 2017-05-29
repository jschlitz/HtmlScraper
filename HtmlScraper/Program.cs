using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using System.Threading;

namespace HtmlScraper
{
  class Program
  {
    static void Main(string[] args)
    {

      using (var client = new System.Net.WebClient())
      {
        var baseUrl = args[0];
        var query = args[1];



        var targetDir = args.Length >=3 ? args[2] : System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        if (!Directory.Exists(targetDir)) throw new Exception($"Target directory does not exist: \r{targetDir}");
        targetDir = Path.Combine(targetDir, query);
        Directory.CreateDirectory(targetDir);


        var pages = new Dictionary<string, bool>();
        var posts = new Dictionary<string, bool>();
        pages[CombineUri(baseUrl, @"/posts?tags=" + query)] = false;

        Console.WriteLine("Getting pages...");
        while (pages.Any(kvp=>!kvp.Value))
        {
          Console.Write("-");
          var queryUri = pages.First(kvp => !kvp.Value).Key;

          using (var stream = client.OpenRead(queryUri))
          {
            var doc = new HtmlDocument();
            doc.Load(stream);

            foreach (var item in GetPagination(doc, baseUrl))
            {
              if (!pages.ContainsKey(item)) pages[item] = false; 
            }

            foreach (var item in GetPostPages(doc, baseUrl, query))
            {
              if (!posts.ContainsKey(item)) posts[item] = false;
            };
          }

          pages[queryUri] = true;
        }

        VisitPosts(posts, client, baseUrl, targetDir);
        while (posts.Any(kvp=>!kvp.Value))
        {
          Console.WriteLine();
          Console.WriteLine("Some files failed to download. Try again? [y/N]");
          if (Console.ReadKey().ToString().ToUpper() != "Y") break;
          VisitPosts(posts, client, baseUrl, targetDir);
        }

        Console.ReadKey(true);

      }
    }

    private static void VisitPosts(Dictionary<string, bool> posts, System.Net.WebClient client, string baseUrl, string targetDir)
    {
      string[] postUrls = posts.Where(kvp => !kvp.Value).Select(kvp => kvp.Key).ToArray();
      Console.WriteLine($"{postUrls.Length} items.");
      foreach (var item in postUrls)
      {
        using (var stream = client.OpenRead(item))
        {
          var doc = new HtmlDocument();
          doc.Load(stream);

          var n = doc.DocumentNode.SelectSingleNode("//img[@id='image']");
          var target = n.GetAttributeValue("src", "");
          if (string.IsNullOrEmpty(target)) continue;
          target = CombineUri(baseUrl, target);
          var fileName = Path.Combine(targetDir, Path.GetFileName(target).Trim('_'));
          try
          {
            if (!File.Exists(fileName)) //don't re-download
            {
              client.DownloadFile(target, fileName);
              //Console.WriteLine(Path.Combine(targetDir, fileName);
              Thread.Sleep(222); //don't overload server.
            }
            posts[item] = true;//visited
            Console.Write(".");

          }
          catch (Exception ex)
          {
            Console.WriteLine();
            Console.WriteLine($"Failed with {target}:\r{ex.Message}");
          }
        }
      }
    }

    private static string[] GetPagination(HtmlDocument doc, string baseUrl)
    {
      var links = doc.DocumentNode.SelectNodes("//div[@class='paginator']/menu/li/a");
      return links
        .Select(hn => CombineUri(baseUrl, HttpUtility.HtmlDecode(hn.Attributes["href"].Value)))
        .ToArray();
    }

    public static string CombineUri(string uri1, string uri2)
    {
      uri1 = uri1.TrimEnd('/');
      uri2 = uri2.TrimStart('/');
      return string.Format("{0}/{1}", uri1, uri2);
    }

    private static string[] GetPostPages(HtmlDocument doc,  string baseUrl, string query)
    {
      var descendents = doc.DocumentNode.Descendants().Where(h => h.Attributes.Contains("Class") && h.Attributes["Class"].Value == "post-preview").ToList();
      var articles = doc.DocumentNode.SelectNodes(@"//article[@class]")
        .Where(hn => hn.Attributes["class"].Value.Split(' ').Contains("post-preview"))
        .ToList();
      var hrefs = articles.Select(hn => hn.Element("a").Attributes["href"])
        .Select(s => CombineUri(baseUrl, HttpUtility.HtmlDecode(s.Value)))
        .ToArray();
      return hrefs;
    }
  }
}
