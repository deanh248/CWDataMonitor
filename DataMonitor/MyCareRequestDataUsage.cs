using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataMonitor
{
    class MyCareRequestDataUsage : MyCareRequest
    {
        public async Task<object> Run()
        {
            try
            {
                Console.WriteLine("MyCareRequestDataUsage - Fetching data usage...");

                await MyCare.Token.WaitLock();

                string token = MyCare.Token.Value;

                string req = String.Format("pg={0}&type={1}&cookie={2}&app={3}", 1, 1, token, 0);
                string resp = await MyCare.HTTPRequestString(req);
                if (resp == null)
                {
                    MyCare.Token.Value = null;
                    return null;
                }


                // Console.WriteLine(resp);
                // -99 when not logged in?

                string[] s = MyCare.SplitDelimiter(resp);

                if (s.Length < 1)
                {
                    Console.WriteLine("MyCareRequestDataUsage - Unexpected length");
                    return null;
                }

                if (!MyCare.ValidateLoginToken(s[0]))
                {
                    Console.WriteLine("MyCareRequestDataUsage - Token does not appear valid.: " + s[0]);
                    return null;
                }

                // Clear nasty whitespaces.
                string context = s[1];
                context = Regex.Replace(context, @"\s+", " ");
                context = Regex.Replace(context, @"\t|\r|\n", "");
                MyCareData data = new MyCareData();

                {
                    // PACKAGE NAME
                    // <b>&nbsp;([A-z-\s,.0-9]+)&nbsp;<i
                    Match match = Regex.Match(context, @"<b>&nbsp;([A-z-\s,.0-9]+)&nbsp;<i", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        data.packageName = match.Groups[1].Value.Trim();
                        data.package = MyCareData.ParsePackage(data.packageName);
                    }

                    if (data.packageName == "" || data.packageName.Length > 64)
                    {
                        Console.WriteLine("ERROR: Could not parse package name");
                        data.packageName = "Cable Package";
                    }

                    Console.WriteLine("PACKAGE: >" + data.packageName + "<");
                    Console.WriteLine("PACKAGE: >" + data.package + "<");
                    Console.WriteLine("PACKAGE: >" + (int) data.package + "<");
                }

                {
                    // PACKAGE MONTHLY SIZE
                    // \s([0-9]+)\w+\sMonthly
                    Match match = Regex.Match(context, @"\s([0-9]+)\w+\sMonthly", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (!double.TryParse(match.Groups[1].Value.Trim(), out data.packageSize))
                        {
                            Console.WriteLine("ERROR: Could not parse Monthly Package Size.");
                        }
                    }

                    if (data.packageSize == -1)
                    {
                        // Estimate from package name.

                    }

                    Console.WriteLine("PACKAGE SIZE (Monthly): >" + data.packageSize + "<");
                }
                /*
                {
                    // PACKAGE DATA REMAINING
                    // <b>([0-9.]+)<\/b>
                    Match match = Regex.Match(context, @"<b>([0-9.]+)<\/b>", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (!float.TryParse(match.Groups[1].Value.Trim(), out data.packageRemainingData))
                        {
                            Console.WriteLine("ERROR: Could not parse Data Remaining. (1)");
                        }
                    }

                    Console.WriteLine("Data Reamining: >" + data.packageRemainingData + "<");
                }
                */
                {
                    // PACKAGE AVAILABLE BALANCE (nb: same as PACKAGE DATA REMAINING ^^ probably safer)
                    // Available Balance.+<b>([0-9.]+)<\/b>	
                    Match match = Regex.Match(context, @"Available Balance.+<b>([0-9.]+)<\/b>", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        if (!double.TryParse(match.Groups[1].Value.Trim(), out data.packageRemainingData))
                        {
                            Console.WriteLine("ERROR: Could not parse Available Balance. (2)");
                            data.packageRemainingData = -1;
                        }
                    }

                    Console.WriteLine("Available Balance: >" + data.packageRemainingData + "<");
                }
                {
                    // LAST USED ON (09-04-2022 15:20:04)
                    // last used on.+([0-9]{2}-[0-9]{2}-[0-9]{4}\s[0-9]{2}:[0-9]{2}:[0-9]{2})
                    Match match = Regex.Match(context, @"last used on.+([0-9]{2}-[0-9]{2}-[0-9]{4}\s[0-9]{2}:[0-9]{2}:[0-9]{2})", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        data.packageLastUsageTimeRaw = match.Groups[1].Value.Trim();
                        try
                        {
                            data.packageLastUsageTime = DateTime.ParseExact(data.packageLastUsageTimeRaw, "dd-MM-yyyy HH:mm:ss", null);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not parse Date: {0}", data.packageLastUsageTimeRaw);
                        }

                    }

                    Console.WriteLine("Last Used on: >" + data.packageLastUsageTime + "<");
                }

                MyCare.Token.Value = s[0];
                return data;
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                MyCare.Token.ReleaseLock();
            }

            return null;

        }
    }
}
