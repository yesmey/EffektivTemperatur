using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EffektivTemperatur
{
    public partial class MainWindow
    {
        private const string SMHI_URL =
            "http://opendata-download-metfcst.smhi.se/api/category/pmp1.5g/version/1/geopoint/lat/65.60/lon/22.18/data.json";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonClick(object sender, RoutedEventArgs e)
        {
            await LoadTemperatures().ConfigureAwait(false);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTemperatures().ConfigureAwait(false);
        }

        private async Task LoadTemperatures()
        {
            try
            {
                UpdateButton.IsEnabled = false;
                var temp = await GetNextTimesery();
                var luftTemp = temp.Lufttemperatur;
                var vind = temp.Vindhastighet;
                TempLabel.Content = $"{luftTemp} °C";
                VindLabel.Content = $"{vind} m/s";
                EffTempLabel.Content = $"{Math.Round(CalcEffTemp(luftTemp, vind), 2)} °C";
            }
            finally
            {
                UpdateButton.IsEnabled = true;
            }
        }

        private double CalcEffTemp(double temp, double vind)
        {
            var tempVind = Math.Pow(vind, 0.16);
            return 13.126667 + 0.6215 * temp - 13.924748 * tempVind + 0.4875195 * temp * tempVind;
        }

        private static async Task<Timesery> GetNextTimesery()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false,
                Proxy = null,
                UseProxy = false
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var str = await client.GetStringAsync(SMHI_URL);
            return await Task.Run(() =>
            {
                var timeSeries = JsonConvert.DeserializeObject<Temperatur>(str).Timeseries;
                var targetDate = DateTime.Now;
                return timeSeries.MinBy(t => Math.Abs((DateTime.Parse(t.ValidTime) - targetDate).Ticks));
            }).ConfigureAwait(false);
        }
    }

    public class TemperaturConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException("WriteJson");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var target = new Temperatur();
            serializer.Populate(jObject.CreateReader(), target);
            return target;
        }

        public override bool CanRead => true;
        public override bool CanConvert(Type objectType) => typeof(Temperatur) == objectType;
    }

    [JsonConverter(typeof(TemperaturConverter))]
    public class Temperatur
    {
        [JsonProperty(PropertyName = "lat")]
        public double Latitude { get; set; }
        [JsonProperty(PropertyName = "lon")]
        public double Longitude { get; set; }
        [JsonProperty(PropertyName = "referenceTime")]
        public string ReferenceTime { get; set; }
        [JsonProperty(PropertyName = "timeseries")]
        public List<Timesery> Timeseries { get; set; }
    }

    public class Timesery
    {
        [JsonProperty(PropertyName = "validTime")]
        public string ValidTime { get; set; }
        [JsonProperty(PropertyName = "msl")]
        public double Lufttryck { get; set; }
        [JsonProperty(PropertyName = "t")]
        public double Lufttemperatur { get; set; }
        [JsonProperty(PropertyName = "vis")]
        public double Sikt { get; set; }
        [JsonProperty(PropertyName = "wd")]
        public int Vindriktning { get; set; }
        [JsonProperty(PropertyName = "ws")]
        public double Vindhastighet { get; set; }
        [JsonProperty(PropertyName = "r")]
        public int RelativLuftfuktighet { get; set; }
        [JsonProperty(PropertyName = "tstm")]
        public int SannorlikhetAska { get; set; }
        [JsonProperty(PropertyName = "tcc")]
        public int TotalMolnMangd { get; set; }
        [JsonProperty(PropertyName = "lcc")]
        public int MolnLag { get; set; }
        [JsonProperty(PropertyName = "mcc")]
        public int MolnMed { get; set; }
        [JsonProperty(PropertyName = "hcc")]
        public int MolnHog { get; set; }
        [JsonProperty(PropertyName = "gust")]
        public double Byvind { get; set; }
        [JsonProperty(PropertyName = "pit")]
        public double NederbordsintensitetTotal { get; set; }
        [JsonProperty(PropertyName = "pis")]
        public double NederbordsintensitetSno { get; set; }
        [JsonProperty(PropertyName = "pcat")]
        public int NederbordsForm { get; set; }
    }
}
