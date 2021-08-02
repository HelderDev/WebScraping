using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebScraping.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace WebScraping.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        //Index for static pages below

#if __STATIC__
        public IActionResult Index()
        {
            string url = "https://en.wikipedia.org/wiki/List_of_programmers";
            var response = CallUrl(url).Result;
            var linkList = ParseHtml(response);
            WriteToCsv(linkList);

            return View();
        }
#endif

#if __Puppeteer__

        public async Task<IActionResult> Index()
        {
            string fullUrl = "https://en.wikipedia.org/wiki/List_of_programmers";

            List<string> programmerLinks = new List<string>();

            var options = new LaunchOptions()
            {
                Headless = true,
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
            var browser = await Puppeteer.LaunchAsync(options);
            var page = await browser.NewPageAsync();
            await page.GoToAsync(fullUrl);
            var links = @"Array.from(document.querySelectorAll('a')).map(a => a.href);";
            var urls = await page.EvaluateExpressionAsync<string[]>(links);


            foreach (string url in urls)
            {
                programmerLinks.Add(url);
            }

            return View();
        }
#endif

        public IActionResult Index()
        {
            string fullUrl = "https://en.wikipedia.org/wiki/List_of_programmers";
            List<string> programmerLinks = new List<string>();

            var options = new ChromeOptions()
            {
                BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };

            options.AddArguments(new List<string>() { "headless", "disable-gpu" });
            var browser = new ChromeDriver(options);
            browser.Navigate().GoToUrl(fullUrl);
            var links = browser.FindElementsByTagName("a");

            foreach (var url in links)
            {
                programmerLinks.Add(url.GetAttribute("href"));
            }

            return View();
        }
        private static async Task<string> CallUrl(string fullUrl)
        {
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13;
            client.DefaultRequestHeaders.Accept.Clear();
            var response = client.GetStringAsync(fullUrl);
            return await response;
        }

        private List<string> ParseHtml(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var programmerLinks = htmlDoc.DocumentNode.Descendants("li")
                    .Where(node => !node.GetAttributeValue("class", "").Contains("tocsection")).ToList();

            List<string> wikiLink = new List<string>();

            foreach (var link in programmerLinks)
            {
                if (link.FirstChild.Attributes.Count > 0)
                    wikiLink.Add("https://en.wikipedia.org/" + link.FirstChild.Attributes[0].Value);
            }

            return wikiLink;

        }
        private void WriteToCsv(List<string> links)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var link in links)
            {
                sb.AppendLine(link);
            }

            System.IO.File.WriteAllText("links.csv", sb.ToString());
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
