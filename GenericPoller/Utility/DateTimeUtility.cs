using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenericPoller.Utility
{
    public class DateTimeUtilities
    {
        public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1);

        private DateTimeUtilities()
        {
        }

        public static long EpochTime
        {
            get
            {
                return (long)(DateTime.UtcNow - UnixEpoch).TotalSeconds;
            }
        }
    }
}
