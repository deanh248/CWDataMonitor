using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace DataMonitor
{

    class NetworkAdapterWrapper
    {
        public long LastByteReceived = 0;
        public long LastByteSent = 0;

        public NetworkInterface nic;

        public NetworkAdapterWrapper(NetworkInterface nic)
        {
            this.nic = nic;
        }
    }

    class NetworkDataRateWrapper
    {
        public NetworkDataRateWrapper(long received, long sent)
        {
            BytesReceivedRate = received;
            BytesSentRate = sent;
        }

        public long BytesReceivedRate = 0;
        public long BytesSentRate = 0;
    }

    static class NetworkAdapterHelper
    {
        private static List<NetworkAdapterWrapper> networkAdapters = new List<NetworkAdapterWrapper>();

        public static void RefreshAdapterList()
        {
            networkAdapters.Clear();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType != NetworkInterfaceType.Ethernet && nic.NetworkInterfaceType != NetworkInterfaceType.GigabitEthernet && nic.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                    continue;

                networkAdapters.Add(new NetworkAdapterWrapper(nic));
            }
        }

        public static NetworkDataRateWrapper GetDataRate()
        {
            if (networkAdapters.Count == 0) {
                RefreshAdapterList();
            }

            long totalSent = 0;
            long totalReceived = 0;

            foreach (NetworkAdapterWrapper wrapper in networkAdapters)
            {
                if (wrapper.nic.OperationalStatus != OperationalStatus.Up)
                    continue;

                var stat = wrapper.nic.GetIPStatistics();

                // If adapter disconnects or resets.
                if (stat.BytesSent < wrapper.LastByteSent)
                    wrapper.LastByteSent = 0;

                if (stat.BytesReceived < wrapper.LastByteReceived)
                    wrapper.LastByteReceived = 0;
                
                if (wrapper.LastByteSent == 0)
                    wrapper.LastByteSent = stat.BytesSent;

                if (wrapper.LastByteReceived == 0)
                    wrapper.LastByteReceived = stat.BytesReceived;

                totalSent += stat.BytesSent - wrapper.LastByteSent;
                totalReceived += stat.BytesReceived - wrapper.LastByteReceived;

                wrapper.LastByteSent = stat.BytesSent;
                wrapper.LastByteReceived = stat.BytesReceived;
            }

            return new NetworkDataRateWrapper(totalReceived, totalSent);
        }

        public static string ParseBytesToReadableFormat(long size)
        {
            if (size < 0)
                size = 0;

            long kb = (long)(size / Math.Pow(2, 10));

            if (kb > 1000000)
            {
                return (kb / Math.Pow(2, 20)).ToString("0.00") + " GB";
            }
            else if (kb > 1000)
            {
                return (kb / Math.Pow(2, 10)).ToString("0.00") + " MB";

            }
            else if (kb > 0)
            {
                return kb + " KB";
            }

            return "0 KB";
        }
    }
}
