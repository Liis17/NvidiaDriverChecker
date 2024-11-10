using SQLite;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NvidiaDriverChecker.CLI.DB
{
    public class ProgramConf
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string BotToten { get; set; } = string.Empty;
        public long AdminID { get; set; } = 0;
        public long ChannelID { get; set; } = 0;
        public string LastVersion { get; set; } = string.Empty;
    }
}
