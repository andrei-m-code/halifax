using System;

namespace Halifax.Api
{
    internal static class Utils
    {
        public static string GetAppName()
        {
            return AppDomain.CurrentDomain.FriendlyName;
        }
    }
}