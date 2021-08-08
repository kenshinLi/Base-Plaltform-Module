using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Platform.ServiceLib.Define
{
    public enum MessageCode
    {
        UNEXPECTED_ERROR = 900,

        FAILDED = 901,
        ILLEGAL_INPUT = 902,
        UNKNOWN_FUNCTION = 999,

        SUCCESS = 1000,
    }
}
