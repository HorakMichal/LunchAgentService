﻿using System;

namespace LunchAgentService.Services
{
    public class RestaurantSetting : ICloneable
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Emoji { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}