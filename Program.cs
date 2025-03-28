using AncientMountain.Managed.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json.Serialization;

namespace AncientMountain
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Logging.SetMinimumLevel(LogLevel.None);
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<SignalRService>();
            builder.Services.AddSingleton<RadarService>();
            builder.Services.AddSingleton<LootFilterService>();
            builder.Services.AddScoped<SettingsService>();

            await builder.Build().RunAsync();
        }
    }


    public sealed class TarkovMarketData
    {
        [JsonPropertyName("items")]
        public List<TarkovMarketItem> Items { get; set; }
    }

    public class TarkovMarketItem
    {
        /// <summary>
        /// Item ID.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("bsgID")]
        public string BsgId { get; init; } = "NULL";
        /// <summary>
        /// Item Full Name.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("name")]
        public string Name { get; init; } = "NULL";
        /// <summary>
        /// Item Short Name.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("shortName")]
        public string ShortName { get; init; } = "NULL";
        /// <summary>
        /// Highest Vendor Price.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("price")]
        public long TraderPrice { get; init; } = 0;
        /// <summary>
        /// Optimal Flea Market Price.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("fleaPrice")]
        public long FleaPrice { get; init; } = 0;
        /// <summary>
        /// Number of slots taken up in the inventory.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("slots")]
        public int Slots { get; init; } = 1;
        [JsonInclude]
        [JsonPropertyName("categories")]
        public IReadOnlyList<string> Tags { get; init; } = new List<string>();
        /// <summary>
        /// Is a Medical Item.
        /// </summary>
        [JsonIgnore]
        public bool IsMed => Tags.Contains("Meds");
        /// <summary>
        /// Is a Food Item.
        /// </summary>
        [JsonIgnore]
        public bool IsFood => Tags.Contains("Food and drink");
        /// <summary>
        /// Is a backpack.
        /// </summary>
        [JsonIgnore]
        public bool IsBackpack => Tags.Contains("Backpack");
        /// <summary>
        /// Is a Weapon Item.
        /// </summary>
        [JsonIgnore]
        public bool IsWeapon => Tags.Contains("Weapon");
        /// <summary>
        /// Is Currency (Roubles,etc.)
        /// </summary>
        [JsonIgnore]
        public bool IsCurrency => Tags.Contains("Money");
    }
}
