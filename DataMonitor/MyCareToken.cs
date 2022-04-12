using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataMonitor
{
    class MyCareToken
    {
        // Tokens handed from mycare can only be used once per request.
        // a new token must be parsed from last reply each time.
        // Always "await WaitLock()" prior to using this value
        public string Value;
        private static SemaphoreSlim TokenSemaphore = new SemaphoreSlim(1, 1);

        public async Task<string> WaitLock()
        {
            await TokenSemaphore.WaitAsync();
            return Value;
        }

        public bool IsValid()
        {
            return MyCare.ValidateLoginToken(Value);
        }

        public void ReleaseLock()
        {
            try
            {
                TokenSemaphore.Release();
            }
            catch { }
        }
    }
}
