using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DataMonitor
{
    interface MyCareRequest
    {
        Task<object> Run();
    }
}
