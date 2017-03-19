using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

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
        foreach (var item in GetPostPages(client, baseUrl, query))
        {
          Console.WriteLine(item);
        }
        ;

        Console.ReadKey(true);

      }
    }

    private static string[] GetPostPages(System.Net.WebClient client, string baseUrl, string query)
    {
      using(var stream = client.OpenRead(baseUrl + @"/posts?tags=" + query))
      {
        var doc = new HtmlDocument();
        doc.Load(stream);
        var descendents = doc.DocumentNode.Descendants().Where(h => h.Attributes.Contains("Class") && h.Attributes["Class"].Value == "post-preview").ToList();
        var articles = doc.DocumentNode.SelectNodes(@"//article[@class]")
          .Where(hn => hn.Attributes["class"].Value.Split(' ').Contains("post-preview"))
          .ToList();
        var hrefs = articles.Select(hn => hn.Element("a").Attributes["href"])
          .Select(s=>baseUrl + s.Value)
          .ToArray();
        return hrefs;
      }
    }
  }
}
