using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MySql.Data.MySqlClient
{
    public enum MySqlBulkLoaderPriority
    {
        None,
        Low,
        Concurrent
    }
}
