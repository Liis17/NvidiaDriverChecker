using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NvidiaDriverChecker.CLI.DB
{
    public class NotifiedUsers
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public long TelegramID{ get; set; } = 0;
        public string TelegramUsername{ get; set; } = string.Empty;
        public string TelegramName{ get; set; } = string.Empty;
    }
}
