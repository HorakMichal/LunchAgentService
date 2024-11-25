using LunchAgent.Core.Menus.Entities;
using System.Text;

namespace LunchAgent.Core.Menus;

public static class MenuFormatting
{
    private static string MessageHeader(DateTime date, string messagePart) =>
        $"*Meníčka na den {date.Day}/{date.Month}/{date.Year}* {messagePart}\n\n";

    public static List<string> CreateMenuMessages(this List<RestaurantMenu> menus, int maximumMessageSize)
    {
        var now = DateTime.Now;

        List<string> messages = [];

        StringBuilder message = new();
    
        foreach (var menu in menus)
        {
            message.CreateMenuForRestaurant(menu);

            if (message.Length < maximumMessageSize) 
                continue;

            message.Insert(0, MessageHeader(now, "Část " + (messages.Count + 1)));

            message.Replace('"', '\'');

            messages.Add(message.ToString());
            message.Clear();
        }

        if (message.Length <= 0) 
            return messages;

        message.Insert(0, messages.Count > 0
            ? MessageHeader(now, "Část " + (messages.Count + 1)) 
            : MessageHeader(now, string.Empty));

        message.Replace('"', '\'');

        messages.Add(message.ToString());

        return messages;
    }

    public static StringBuilder CreateMenuForRestaurant(this StringBuilder message, RestaurantMenu menu)
    {
        var soups = string.Join("\n", menu.Items
            .Where(item => item.FoodType == FoodType.Soup)
            .Select(item => $"_{item.Description.Trim()}_ {item.Price}"));

        var mains = string.Join("\n", menu.Items
            .Where(item => item.FoodType == FoodType.Main)
            .Select((item, index) => $"  {index + 1}. {item.Description} {item.Price}"));

        var items = string.Join("\n", soups, mains).Trim('\n');

        message.Append($"{menu.Restaurant.Emoji} *{menu.Restaurant.Name}:* \n {items}\n\n");

        return message;
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
