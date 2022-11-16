using lesson1.Controller;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace lesson1
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            DatabaseLoader dbLoader = DatabaseLoader.GetInstance();
            var comps = new List<string>() { "12", "13", "14", "15", "16", "17", "18", "19", "742", "739", "741", "736", "738", "740", "737", "20", "2032", "687" };
            //dbLoader.LoadPlayersAndClubsInfo(comps);
            dbLoader.LoadStatistics(comps);
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine($"Время работы: {elapsedTime}");
        }
    }
}
