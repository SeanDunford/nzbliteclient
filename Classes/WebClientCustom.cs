using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Classes
{
    class WebClientCustom : WebClient
    {
        public int TimeoutMs = 60000;
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest wr = base.GetWebRequest(address);
            wr.Timeout = TimeoutMs; // timeout in milliseconds (ms)
            return wr;
        }
    }
}
