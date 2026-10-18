using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSV.Enums
{
    public enum HeaderType
    {
        NoHeader = 0, // 空白
        AddHeader = 1, // 重寫header
        AppendData = 2, // 不須新增header
    }
}
