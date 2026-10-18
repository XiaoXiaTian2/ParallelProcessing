using CSV.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CSV
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AccountsModel obj = new AccountsModel
            {
                Date = "2026/01/01",
                Money = "12312",
                Category = "ABc",
                Detail = "DF",
                Member = "DD",
                Payway = "DDD"
            };
            List<AccountsModel> list = new List<AccountsModel>()
            {
                obj,
                obj,
                obj,
                obj,
            };
            //CSVHelper.Write<AccountsModel>("data/data.csv", obj);
            //CSVHelper.WriteList<AccountsModel>("data/data.csv", list);
            List<AccountsModel> data = CSVHelper.Read<AccountsModel>("data/data.csv");
            foreach (var item in data)
            {
                Console.WriteLine(JsonConvert.SerializeObject(item));
            }
        }
    }
}
