﻿using HtmlAgilityPack;
using LunchAgent.Core.Menus.Entities;
using LunchAgent.Core.Restaurants.Entitties;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace LunchAgent.Core.Menus;

public sealed class MenuReadingService(ILogger logger) : IMenuReadingService
{
    public List<RestaurantMenu> GetMenus(IReadOnlyCollection<Restaurant> restaurants)
    {
        var result = new List<RestaurantMenu>();
        var document = new HtmlDocument();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        foreach (var restaurant in restaurants)
        {
            try
            {
                using var client = new HttpClient();

                var data = restaurant.Url.Contains("makalu")
                    ? Encoding.UTF8.GetString(client.GetByteArrayAsync(restaurant.Url).Result)
                    : Encoding.GetEncoding(1250).GetString(client.GetByteArrayAsync(restaurant.Url).Result);

                document.LoadHtml(data);
            }
            catch (Exception e)
            {
                logger.LogDebug("Failed to get menu from {RestaurantName}. Exception: {Exception}", restaurant.Name, e);
            }

            try
            {
                var parsedMenu = restaurant.Url.Contains("makalu")
                    ? ParseMenuFromMakalu(document.DocumentNode)
                    : ParseMenuFromMenicka(document.DocumentNode);

                result.Add(new RestaurantMenu
                {
                    Items = parsedMenu,
                    Restaurant = restaurant
                });
            }
            catch (Exception e)
            {
                logger.LogDebug("Failed to parse menu from {RestaurantName}. Exception: {Exception}", restaurant.Name, e);
            }

            logger.LogDebug("Successfully got menu from {RestaurantName}", restaurant.Name);
        }

        return result;
    }

    private static List<RestaurantMenuItem> ParseMenuFromMenicka(HtmlNode todayMenu)
    {
        var result = new List<RestaurantMenuItem>();

        var foodMenus = todayMenu.SelectNodes(".//tr")
            .Where(node => node.GetClasses().Contains("soup") || node.GetClasses().Contains("main"));

        foreach (var food in foodMenus)
        {
            var item = new RestaurantMenuItem();

            if (food.GetClasses().Contains("soup"))
            {
                item = item with
                {
                    FoodType = FoodType.Soup,
                    Description = Regex.Replace(
                        food.SelectNodes(".//td").Single(x => x.GetClasses().Contains("food")).InnerText,
                        "\\d+.?", string.Empty),
                    Price = food.SelectNodes(".//td").Single(x => x.GetClasses().Contains("prize")).InnerText
                };
            }
            else
            {
                item = item with
                {
                    FoodType = FoodType.Main,
                    Description = Regex.Replace(
                        food.SelectNodes(".//td").Single(x => x.GetClasses().Contains("food")).InnerText,
                        "\\d+.?", string.Empty),
                    Price = food.SelectNodes(".//td").Single(x => x.GetClasses().Contains("prize")).InnerText,
                    Index = food.SelectNodes(".//td").Single(x => x.GetClasses().Contains("no")).InnerText,
                };
            }

            result.Add(item);
        }

        return result;
    }

    private static List<RestaurantMenuItem> ParseMenuFromMakalu(HtmlNode todayMenu)
    {
        var todayString = GetTodayInCzech();

        var menuNodes = todayMenu.Descendants("div")
            .Where(node => node.HasClass("weeklyDayCont"))
            .Where(node => node.InnerText.Contains(todayString, StringComparison.InvariantCultureIgnoreCase))
            .SelectMany(node => node.Descendants())
            .Where(node => node.HasClass("menuPageMealName"));

        var menuItems = menuNodes.Select(node =>
        {
            var childNodes = node.Descendants("td").ToList();
            var isSoup = node.InnerText.Contains("polévka", StringComparison.InvariantCultureIgnoreCase);

            return new RestaurantMenuItem
            {
                FoodType = isSoup ? FoodType.Soup : FoodType.Main,
                Index = isSoup ? string.Empty : childNodes[0].InnerText.Trim(),
                Description = childNodes[isSoup ? 0 : 1].InnerText.Trim() + (isSoup ? " / " + childNodes[1].InnerText.Trim() : " " + childNodes[2].InnerText.Trim()),
                Price = isSoup ? string.Empty : childNodes[3].InnerText.Trim()
            };
        }).ToList();

        return menuItems;
    }

    private static string GetTodayInCzech()
    {
        return DateTime.Today.DayOfWeek switch
        {
            DayOfWeek.Monday => "Pondělí",
            DayOfWeek.Sunday => "Úterý",
            DayOfWeek.Tuesday => "Středa",
            DayOfWeek.Wednesday => "Čtvrtek",
            DayOfWeek.Thursday => "Pátek",
#if DEBUG
            DayOfWeek.Friday => "Pátek",
            DayOfWeek.Saturday => "Pátek",
#endif
            _ => string.Empty
        };
    }
}