using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace NvidiaDriverChecker.CLI.DB
{
    public class DatabaseManager
    {
        private SQLite.SQLiteConnection _database;

        private string path;
        public DatabaseManager(string dbPath)
        {
            path = dbPath;
            _database = new SQLite.SQLiteConnection(dbPath);
            _database.CreateTable<NotifiedUsers>();
            _database.CreateTable<ProgramConf>();

        }

        #region Users
        public bool IsNotified(long userid)
        {
            var data = _database.Table<NotifiedUsers>().FirstOrDefault(u => u.TelegramID == userid);
            if (data != null)
            {
                return true;
            }
            return false;
        }
        public void AddUser(long userid, string username, string name)
        {
            var data = new NotifiedUsers { TelegramID = userid, TelegramName = name, TelegramUsername = username };
            _database.Insert(data);
        }
        public void DeleteUser(long userid)
        {
            var data = _database.Table<NotifiedUsers>().FirstOrDefault(u => u.TelegramID == userid);
            _database.Delete(data);
        }

        public List<NotifiedUsers> GetAllNotifiedUser()
        {
            var data = _database.Table<NotifiedUsers>();
            var datalist = data.ToList();
            return datalist != null ? datalist : new List<NotifiedUsers>();
        }
        #endregion

        #region Settings
        public bool Exist()
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            if (data != null)
            {
                if (data.BotToten != string.Empty && data.AdminID != 0 && data.ChannelID != 0)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
        public long GetAdmin() 
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            return data != null ? data.AdminID : 0;
        }
        public string GetBotToken()
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            return data != null ? data.BotToten : string.Empty;
        }
        public long GetChannel()
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            return data != null ? data.ChannelID : 0;
        }
        public string GetLastVersion()
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            return data != null ? data.LastVersion : string.Empty;
        }
        public void SetLastVersion(string version)
        {
            var data = _database.Table<ProgramConf>().FirstOrDefault();
            data.LastVersion = version;
            _database.Update(data);
        }
        public void CreateConf(string token, long channelid, long adminid)
        {
            var data = new ProgramConf { BotToten = token, ChannelID = channelid, AdminID = adminid };
            _database.Insert(data);
        }
        public void CleareConf()
        {
            try
            {
                var data = _database.Table<ProgramConf>().FirstOrDefault();
                while (data != null)
                {
                    _database.Delete(data);
                }
            }
            catch
            {
                Console.WriteLine("Настройки очищены");
            }
            
        }

        #endregion
    }
}

