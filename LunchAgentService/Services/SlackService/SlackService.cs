﻿using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LunchAgentService.Services
{
    public class SlackService
    {
        private static readonly string PostMessageUri = "https://slack.com/api/chat.postMessage";
        private static readonly string UpdateMessageUri = "https://slack.com/api/chat.update";
        private static readonly string ChatHistoryUri = "https://slack.com/api/channels.history";

        private SlackServiceSetting _serviceSetting;
        public SlackServiceSetting ServiceSetting
        {
            get
            {
                lock (_serviceSetting)
                {
                    return _serviceSetting.Clone();
                }
            }
            set
            {
                lock (_serviceSetting)
                {
                    _serviceSetting = value;
                }
            }
        }
        public bool AlreadyPosted { get; set; }

        private ILog Log { get; }

        public SlackService(SlackServiceSetting serviceSetting, ILog log)
        {
            _serviceSetting = serviceSetting;
            Log = log;
        }

        public void ProcessMenus(List<RestaurantMenu> menus)
        {
            Log.Debug("Getting slack history");

            var history = GetSlackChannelHistory();

            Log.Debug("Filtering messages for messages from today");

            var todayMessage = history.Messages.FindAll(message => message.Date.Date == DateTime.Today && message.BotId == ServiceSetting.BotId)
                .OrderByDescending(message => message.Date)
                .FirstOrDefault();

            if (AlreadyPosted == false)
            {
                Log.Debug("Posting new menus to slack");

                PostToSlack(menus);

                AlreadyPosted = true;
            }
            else
            {
                Log.Debug("Updating already existing menu on slack");

                UpdateToSlack(menus, todayMessage.Timestamp);
            }

            //if (todayMessage == null)
            //{
            //    Log.Debug("Posting new menus to slack");

            //    PostToSlack(menus);
            //}
            //else
            //{
            //    Log.Debug("Updating already existing menu on slack");

            //    UpdateToSlack(menus, todayMessage.Timestamp);
            //}
        }

        public void PostToSlack(List<RestaurantMenu> menus)
        {
            dynamic postRequestObject = GetRequestObjectFromSlackConfiguration();

            postRequestObject.text = FormatMenuForSlack(menus);

            PostToSlack(postRequestObject, PostMessageUri);
        }

        public void UpdateToSlack(List<RestaurantMenu> menus, string timestamp)
        {
            dynamic postRequestObject = GetRequestObjectFromSlackConfiguration();

            postRequestObject.text = FormatMenuForSlack(menus);
            postRequestObject.ts = timestamp;

            PostToSlack(postRequestObject, UpdateMessageUri);
        }

        private string PostToSlack(dynamic requestObject, string requestUri)
        {
            Log.Debug($"Posting request to slack uri: {requestUri}");

            var data = new StringContent(JObject.FromObject(requestObject).ToString(), Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requestObject.token);
                    var response = client.PostAsync(requestUri, data);

                    response.Wait();
                    var result = response.Result.Content.ReadAsStringAsync().Result;

                    Log.Debug($"Request posted successfuly. Response: {response.Result.StatusCode}");

                    return result;
                }
                catch (Exception e)
                {
                    Log.Error("Failed posting request", e);

                    return null;
                }
            }
        }

        private SlackChannelHistory GetSlackChannelHistory()
        {
            var stringResponse = "";

            var formData = new Dictionary<string, string>();

            using (var client = new HttpClient())
            {
                Log.Debug($"Posting request to slack uri: {ChatHistoryUri}");

                var slackSetting = ServiceSetting;
                formData["token"] = slackSetting.BotToken;
                formData["channel"] = slackSetting.ChannelName;
                formData["bot_id"] = slackSetting.BotId;

                var data = new FormUrlEncodedContent(formData);

                try
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", formData["token"]);
                    var response = client.PostAsync(ChatHistoryUri, data);

                    response.Wait();
                    stringResponse = response.Result.Content.ReadAsStringAsync().Result;

                    Log.Debug($"Request posted successfuly. Response: {response.Result.StatusCode}");

                }
                catch (Exception e)
                {
                    Log.Error("Failed posting request", e);
                }
            }

            var result = JsonConvert.DeserializeObject<SlackChannelHistory>(stringResponse);

            return result;
        }

        private string FormatMenuForSlack(List<RestaurantMenu> menus)
        {
            Log.Debug("Formating menu for slack");

            var result = new List<string>();

            foreach (var parsedMenu in menus)
            {
                parsedMenu.Items.FindAll(x => x.FoodType == FoodType.Soup).ForEach(x => x.Description = "_" + x.Description + "_");

                var formatedFood = string.Join(Environment.NewLine, parsedMenu.Items);

                result.Add($"{parsedMenu.Restaurant.Emoji}*     {parsedMenu.Restaurant.Name}*{Environment.NewLine}{formatedFood}");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, result);
        }

        private ExpandoObject GetRequestObjectFromSlackConfiguration()
        {
            dynamic result = new ExpandoObject();

            lock (_serviceSetting)
            {
                result.token = _serviceSetting.BotToken;
                result.channel = _serviceSetting.ChannelName;
                result.bot_id = _serviceSetting.BotId;
            }

            return result;
        }
    }
}
