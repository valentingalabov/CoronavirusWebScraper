﻿namespace CoronavirusWebScraper.Data.Models
{
    using MongoDB.Bson.Serialization.Attributes;

    /// <summary>
    /// Hold information about total and last 24 hours counts.
    /// </summary>
    public class TotalAndLast
    {
        /// <summary>
        /// Gets or sets total count.
        /// </summary>
        [BsonElement("total")]
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets count for last 24 hours.
        /// </summary>
        [BsonElement("last")]
        public int Last { get; set; }
    }
}
