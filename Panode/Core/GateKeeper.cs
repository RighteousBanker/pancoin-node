using System;
using System.Collections.Generic;
using System.Text;

namespace Panode.Core
{
    public static class GateKeeper
    {
        public static object ChainManagerLock = new object();
        public static object BalanceLedgerLock = new object();
        public static object ContactLock = new object();
        public static object TransactionPoolLock = new object();
    }
}
