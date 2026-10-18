using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace CSV.Writers
{
    internal class AddHeaderWriter : AWriter
    {
        public override void AddData<T>(string path, List<T> model)
        {
            var properties = typeof(T).GetProperties();
            StreamReader reader = new StreamReader(path, Encoding.UTF8);
            List<string> data = new List<string>();
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                data.Add(line);
            }
            reader.Close();

            foreach (var row in model)
            {
                string modelData = String.Join(",", properties.Select(p => p.GetValue(row)));
                data.Add(modelData);
            }

            StreamWriter streamWriter = new StreamWriter(path, false, Encoding.UTF8);
            streamWriter.WriteLine(String.Join(",", properties.Select(p => p.Name)));
            foreach (string line in data)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Flush();
            streamWriter.Close();
        }
    }
}
