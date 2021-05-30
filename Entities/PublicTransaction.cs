using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qrame.Web.TransactServer.Entities
{
    public class PublicTransaction
    {
        public string ApplicationID { get; set; }
        public string ProjectID { get; set; }
        public string TransactionID { get; set; }
    }
}
