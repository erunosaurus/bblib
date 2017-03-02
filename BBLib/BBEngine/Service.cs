using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using APIService = Bloomberglp.Blpapi.Service;

namespace BBLib.BBEngine
{
    /// <summary>
    /// Designs a service by its <paramref name="Reference"/> and <paramref name="Status"/>.
    /// </summary>
    internal class Service
    {
        public APIService Reference;
        public ManualResetEventSlim Status;

        public Service(ManualResetEventSlim Status)
        {
            this.Status = Status;
        }
    }
}
