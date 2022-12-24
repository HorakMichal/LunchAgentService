﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using LunchAgentService.Helpers;
using LunchAgentService.Services.RestaurantService;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace LunchAgentService.Services.TeamsService
{
    public class TeamsService : ITeamsService
    {
        private readonly AppSettings _appSettings;

        public TeamsService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }      

        public string Post(List<RestaurantMenu> menus)
        {
            var requestUri = _appSettings.TeamsBotLink;

            Trace.TraceInformation($"Posting request to teams uri: {requestUri}");

            var requestObject = new TeamsMessage();
            requestObject.Title = "Meníčka na den " + DateTime.Now.Date.ToString("dd.MM.yyyy");
            requestObject.Summary = "Meníčka na den " + DateTime.Now.Date.ToString("dd.MM.yyyy");
            requestObject.Text = FormatMenuForTeams(menus);

            var data = new StringContent(JObject.FromObject(requestObject).ToString(), Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                try
                {                   
                    var response = client.PostAsync(requestUri, data);

                    response.Wait();
                    var result = response.Result.Content.ReadAsStringAsync().Result;

                    Trace.TraceInformation($"Request posted successfuly. Response: {response.Result.StatusCode}");                    

                    return result;
                }
                catch (Exception e)
                {
                    Trace.TraceError("Failed posting request", e);

                    return null;
                }
            }
        }
       
        private string FormatMenuForTeams(List<RestaurantMenu> menus)
        {
            Trace.TraceInformation("Formating menu for teams");

            var result = new List<string>();

            foreach (var parsedMenu in menus)
            {
                parsedMenu.Items.FindAll(x => x.FoodType == FoodType.Soup).ForEach(x => x.Description = "_" + x.Description.Trim() + "_");

                var formatedFood = string.Join(Environment.NewLine, parsedMenu.Items);

                result.Add($"## {parsedMenu.Restaurant.Name} ##{Environment.NewLine} {formatedFood}");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, result);
        }
    }
}
