using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataMonitor
{
    class MyCareRequestLogin : MyCareRequest
    {
        string username = "";
        string password = "";

        public MyCareRequestLogin(string name, string pass)
        {
            this.username = name;
            this.password = pass;
        }

        public async Task<object> Run()
        {
            Console.WriteLine("Fetching login token...");

            try
            {
                string req = String.Format("pg=loginValidate&uname={0}&pword={1}&app=0", username, password);
                string response = await MyCare.HTTPRequestString(req);

                if (response == null)
                {
                    MyCare.Token.Value = null;
                    return null;
                }

                // Console.WriteLine(response);
                string[] s = MyCare.SplitDelimiter(response);
                int code = 0;
                if (Int32.TryParse(s[0], out code))
                {
                    if (code == 1)
                    {
                        Console.WriteLine("Login success.");
                        MyCare.Token.Value = s[2];
                        return true;

                    }
                    else
                    {
                        Console.WriteLine("Wrong username or password?");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}
