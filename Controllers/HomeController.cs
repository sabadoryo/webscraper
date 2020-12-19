using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Endterm.Models;
using System.Text.RegularExpressions;

using AngleSharp;
using AngleSharp.Html.Parser;
using System.Runtime.CompilerServices;
using Endterm.Data;

namespace Endterm.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly String websiteUrl = "https://www.olx.kz/";
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var items = _context.Item.ToList();
            ViewBag.Items = items;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Fetch()
        {
            await CheckForUpdates("https://www.olx.kz/", "Check for update of first 16 VIP items");

            return Redirect("~/Home/Index");
        }

        private async Task CheckForUpdates(string url, string mailTitle)
        {
            List<dynamic> adverts = new List<dynamic>();

            await GetPageData(url, adverts);

            var dbcontents = _context.Item.ToList();

            foreach(Item it in dbcontents)
            {
                var advertExists = adverts.Find(item => item.ItemUrl == it.ItemUrl);
                if (advertExists == null)
                {
                    _context.Item.Remove(it);
                }

                await _context.SaveChangesAsync();
            }


            foreach (Item it in adverts)
            {
                var existingItem = _context.Item.SingleOrDefault(row => row.ItemUrl == it.ItemUrl);
                if (existingItem != null)
                {
                    if (existingItem.Price != it.Price)
                    {
                        existingItem.Price = it.Price;
                        await _context.SaveChangesAsync();
                    }
                    continue;
                }

                _context.Item.Add(it);
                await _context.SaveChangesAsync();
            }
        }

            public IActionResult Privacy()
        {
            return View();
        }

        private async Task<List<dynamic>> GetPageData(string url, List<dynamic> results)
        {
            var config = Configuration.Default.WithDefaultLoader();
            var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(url);

            var advertrows = document.QuerySelectorAll("li.wrap.tleft.rel.fleft");

            _logger.LogInformation(advertrows.Length.ToString());

            _logger.LogInformation("HELLO");

            foreach (var row in advertrows)
            {
                Item advert = new Item();

                // Use regex to get all the numbers from this string
                MatchCollection regxMatches = Regex.Matches(row.QuerySelector(".price").TextContent, @"\d+\.*\d+");
                uint.TryParse(string.Join("", regxMatches), out uint price);
                advert.Price = price;

                advert.Name = row.QuerySelector(".link.linkWithHash.detailsLinkPromoted").TextContent;


                // Get the fuel type from the ad
                advert.Descriptipn = row.QuerySelector(".date-location").TextContent;

                // Make and model

                // Link to the advert
                advert.ItemUrl = websiteUrl + row.QuerySelector(".link.linkWithHash.detailsLinkPromoted").GetAttribute("Href");

                results.Add(advert);
            }

            return results;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
