using FootballTracker.Controllers;
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
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using static lesson1.Controller.DataFetcher;

namespace lesson1
{
    class Program
    {
        static DatabaseLoader dbLoader = DatabaseLoader.GetInstance();
        static int count = 0;
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            dbLoader.UpdateCurrentMatches();
            Timer aTimer = new Timer();
            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 60000;
            aTimer.Enabled = true;
            Console.ReadLine();
            //string htmlCode = dbLoader.dataFetcher.GetHTMLInfo("1743393", SearchScope.games);
            //var squad = dbLoader.dataFetcher.GetMatchSquadByHtml(htmlCode);
            //dbLoader.dataFetcher.GetMatchEventsByHtml(htmlCode, squad);
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine($"Время работы: {elapsedTime}");
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine($"[{DateTime.Now:T}] Событие таймера[{count}]");
            dbLoader.UpdateCurrentMatches();
            if (count % 10 == 0)
            {
                count = 0;
                dbLoader.UpdateStatistics();
            }
            count++;
        }
    }
}
