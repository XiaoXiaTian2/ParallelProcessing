using CSV.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace CSV
{
    internal class HeaderManager
    {
        public static HeaderType CheckHeader<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return HeaderType.NoHeader;

            StreamReader reader = new StreamReader(filePath);
            string line = reader.ReadLine();
            reader.Close();
            if (line == null)
                return HeaderType.NoHeader;

            // 判斷是不是有header
            var modelType = typeof(T).GetProperties();
            int total = 0;
            Dictionary<string, int> dataPosMap = new Dictionary<string, int>();
            for (int i = 0; i < modelType.Length; i++)
            {
                Console.WriteLine(modelType[i].Name);
                dataPosMap.Add(modelType[i].Name, i);
                total += i;
            }
            int fileIdx = line.Split(',').Sum(title =>
            {
                if (dataPosMap.TryGetValue(title, out int idx)) { return idx; }
                return 0;
            });
            Console.WriteLine("fileIdx" + fileIdx);
            if (fileIdx < total)
                return HeaderType.AddHeader;

            return HeaderType.AppendData;
        }
    }
}
