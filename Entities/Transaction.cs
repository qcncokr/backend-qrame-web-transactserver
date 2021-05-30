using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Qrame.Web.TransactServer.Entities
{
    public partial class Transaction
    {
        public string TX_ID { get; set; }
        public string ProjectID { get; set; }
        public string BusinessID { get; set; }
        public string TransactionID { get; set; }
        public string ServiceID { get; set; }
        public string ReturnType { get; set; }
        public bool AutoCommit { get; set; }
        public DateTimeOffset ModifiedDate { get; set; }
        public List<Input> Inputs { get; set; }
    }
}