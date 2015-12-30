using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace AMF.Data
{
    internal class CustomConnection<CT> : IDisposable
        where CT : DbConnection
    {
        private CT _con { get; set; }
        public bool IsDisposed { get; set; }
        public ConnectionState CurrentState { get { return _con.State; } }

        public CustomConnection(string ConnectionString)
        {
            string _FinalConnectionString = null;

            if (ConnectionString.ToLower().Contains("name="))
            {
                string _ConnectionName = ConnectionString.ToLower().Replace("name=", "");
                _FinalConnectionString = ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>().Where(p => _ConnectionName == p.Name.ToLower()).FirstOrDefault().ConnectionString;
            }
            else
                _FinalConnectionString = ConnectionString;

            _con = Activator.CreateInstance<CT>();
            _con.ConnectionString = _FinalConnectionString;
            _con.Close();
        }

        public void OpenConnection()
        {
            _con.Open();
        }

        public void CloseConnection()
        {
            _con.Close();
        }

        public DbCommand CreateCommand()
        {
            return _con.CreateCommand();
        }

        public void Dispose()
        {
            _con.Dispose();
            _con = null;
            IsDisposed = true;
        }
    }
}