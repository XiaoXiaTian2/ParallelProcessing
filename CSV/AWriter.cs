using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV
{
    public abstract class AWriter
    {
        protected AWriter() { }

        public abstract void AddData<T>(string path, List<T> model);
    }
}
