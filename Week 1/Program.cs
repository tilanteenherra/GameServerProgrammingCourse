using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Week_1
{
    public class Program
    {
        
        public static async Task Main(string[] args)
        {
            var fetcher = new RealTimeCityBikeDataFetcher();
            var offlineFetch = new OfflineCityBikeDataFetcher();

            if(args.Length > 1 && !args[1].Equals("realtime")) {
                if(args[1].Equals("offline")) {
                    offlineFetch = new OfflineCityBikeDataFetcher();
                    int bikeCount = await offlineFetch.GetBikeCountInStation(args[0]);
                    if(bikeCount < 0) {
                        Console.WriteLine("Error occured, could not fetch data...");
                    } else {
                        Console.WriteLine("Last known, Bikes at station " + args[0] + ": " + bikeCount);
                    }
                } else {
                    Console.WriteLine("Invalid argument");
                }
            } else {
                fetcher = new RealTimeCityBikeDataFetcher();
                Console.WriteLine("Bikes at station " + args[0] + ": " + await fetcher.GetBikeCountInStation(args[0]));
            }
        }
    }
    public interface ICityBikeDataFetcher {
        Task<int> GetBikeCountInStation(string stationName);
    }
    public class NotFoundException : Exception 
    {
        public NotFoundException() {

        }
    }
    public class RealTimeCityBikeDataFetcher : ICityBikeDataFetcher
    {
       
        HttpClient client = new HttpClient() {
            BaseAddress = new Uri(@"http://api.digitransit.fi/routing/v1/routers/hsl/bike_rental")
        };

        
        public async Task<int> GetBikeCountInStation(string stationName)
        {
            BikeRentalStationList lista;
            try {
                if(stationName.Any(c => char.IsDigit(c))) {
                    throw new ArgumentException();
                }
            } catch (ArgumentException ae) {
                Console.WriteLine("Invalid argument: " + ae);
                return -1;
            } finally {
                lista = JsonConvert.DeserializeObject<BikeRentalStationList>(await client.GetStringAsync(client.BaseAddress));
            }
            try {
                if(!lista.stations.Exists(item => item.name == stationName)) {
                    throw new NotFoundException();
                }
            }  catch (NotFoundException nfe) {
                Console.WriteLine("Not found: " + nfe);
                return -1;  
            }
            return lista.stations.Find(item => item.name == stationName).bikesAvailable;
        }
    }

    public class OfflineCityBikeDataFetcher : ICityBikeDataFetcher
    {
        string fileName = "bikedata.txt";
        string filePath = Environment.CurrentDirectory;
        
        public async Task<int> GetBikeCountInStation(string stationName)
        {
            
            try {
                if(stationName.Any(c => char.IsDigit(c))) {
                    throw new ArgumentException();
                }
            } catch(ArgumentException ae) {
                Console.WriteLine("Invalid argument: " + ae);
                return -1;
            }
            try {
                string[] arr = await File.ReadAllLinesAsync(Path.Combine(filePath, fileName));
                Console.WriteLine(Path.Combine(filePath, fileName));
                for(int i = 0; i < arr.Count(); i++) {
                    if(arr[i].Contains(stationName)) {
                        return Int32.Parse(arr[i].Remove(0, arr[i].IndexOf(" : ") + 3));;
                    }
                }
                throw new NotFoundException();
            } catch (FileNotFoundException e) {
                Console.WriteLine(e);
            } catch (NullReferenceException e) {
                Console.WriteLine(e);
            } catch (NotFoundException e) {
                Console.WriteLine("Not found: " + e);
                return -1;
            }
            return -1;
        }
    }
    public class BikeRentalStationList {
        public List<Stations> stations;
        public class Stations {
            public int id {get; set;}
            public string name {get; set;}
            public float x {get; set;}
            public float y {get; set;}
            public int bikesAvailable {get; set;}
            public int spaceAvailable {get; set;}
            public bool allowDropOff {get; set;}
            public bool isFloatingBike {get; set;}
            public bool isCarStation {get; set;}
            public bool realTimeData {get; set;}

        }


    }
}
