using LunchAgent.Core.Menus.Entities;
using System.Text;

namespace LunchAgent.Core.Menus;

public static class MenuFormatting
{
    public static string CreateMenuMessage(this List<RestaurantMenu> menus)
    {
        var now = DateTime.Now;

        StringBuilder message = new();
        message.Append($"*Meníčka na den {now.Day}/{now.Month}/{now.Year}*\n\n");

        foreach (var menu in menus)
        {
            var soups = string.Join("\n", menu.Items
                .Where(item => item.FoodType == FoodType.Soup)
                .Select(item => $"_{item.Description.Trim()}_ {item.Price}"));

            var mains = string.Join("\n", menu.Items
                .Where(item => item.FoodType == FoodType.Main)
                .Select((item, index) => $"  {index + 1}. {item.Description} {item.Price}"));

            var items = string.Join("\n", soups, mains).Trim('\n');

            message.Append($"{menu.Restaurant.Emoji} *{menu.Restaurant.Name}:* \n {items}\n\n");
        }

        return message.ToString();
    }

    public static StringContent CreateHttpRequest(this string menu)
    {
        var request =
            $$"""
              {
                  "text": "{{menu}}"
              }
              """;

        return new StringContent(request, Encoding.UTF8, "application/json");
    }
}
