using AMF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;

namespace AMF.Data
{
    public class DataClass<TConnectionType, TParameterType>
        where TConnectionType : DbConnection
        where TParameterType : DbParameter
    {
        private string _connectionString = "";
        private CustomConnection<TConnectionType> _connection = null;

        private CustomConnection<TConnectionType> Connection
        {
            get { return (_connection == null || _connection.IsDisposed ? new CustomConnection<TConnectionType>(_connectionString) : _connection); }
        }

        public DataClass(string ConnectionString)
        {
            _connectionString = ConnectionString;
            _connection = new CustomConnection<TConnectionType>(ConnectionString);
        }

        public void CloseConnection()
        {
            Connection.CloseConnection();
        }

        #region Private Methods
        private List<PropertyList> GetTypeProperties<T>(params ExclusionTypes[] Excluded)
        {
            List<PropertyList> modelProperties = typeof(T).GetProperties().Where(p => p.GetCustomAttributes(typeof(MappingAttribute), false).Count() <= 0 ||
            p.GetCustomAttributes(typeof(MappingAttribute), false).Cast<MappingAttribute>().Any(a => !Excluded.Contains(a.Exclude))).OrderBy(p => p.MetadataToken)
            .Select(s => new PropertyList
            {
                PropertyName = s.Name,
                ColumnName = (s.GetCustomAttributes(typeof(MappingAttribute), false).Count() <= 0 ? s.Name :
                              string.IsNullOrEmpty(s.GetCustomAttributes(typeof(MappingAttribute), false).Cast<MappingAttribute>().FirstOrDefault().ColumnName) ?
                              s.Name : s.GetCustomAttributes(typeof(MappingAttribute), false).Cast<MappingAttribute>().FirstOrDefault().ColumnName),
                PropertyType = s.PropertyType,
                ParameterName = (s.GetCustomAttributes(typeof(MappingAttribute), false).Count() <= 0 ? s.Name :
                               string.IsNullOrEmpty(s.GetCustomAttributes(typeof(MappingAttribute), false).Cast<MappingAttribute>().FirstOrDefault().ParameterName) ?
                               s.Name : s.GetCustomAttributes(typeof(MappingAttribute), false).Cast<MappingAttribute>().FirstOrDefault().ParameterName.Replace("@", ""))
            }).ToList();

            return modelProperties;
        }

        private void AttachParameters(DbCommand Cmd, params DbParameter[] P)
        {
            Cmd.Parameters.Clear();
            if (P.Any())
                foreach (DbParameter _param in P)
                {
                    if (_param.Value == null) _param.Value = DBNull.Value;
                    Cmd.Parameters.Add(_param);
                }
        }

        private void AttachParameters<T>(DbCommand Cmd, T Model)
        {
            Cmd.Parameters.Clear();

            List<PropertyList> modelProperties = GetTypeProperties<T>(ExclusionTypes.CUD, ExclusionTypes.Both);

            for (int i = 0; i < modelProperties.Count; i++)
            {
                var _value = Model.GetType().GetProperty(modelProperties[i].PropertyName).GetValue(Model, null);
                TParameterType _param = Activator.CreateInstance<TParameterType>();
                var _paramType = _param.GetType();
                _paramType.GetProperty("ParameterName").SetValue(_param, "@" + modelProperties[i].ParameterName, null);
                _paramType.GetProperty("Value").SetValue(_param,
                    (_value == null ? DBNull.Value : Convert.ChangeType(_value, modelProperties[i].PropertyType)), null);

                Cmd.Parameters.Add(_param);
            }
        }

        private List<T> MapToClass<T>(DataTable dt)
        {
            if (typeof(T).BaseType == null)
                return MapToDynamic<T>(dt);

            Collection<T> _return = new Collection<T>();

            List<PropertyList> modelProperties = GetTypeProperties<T>(ExclusionTypes.Select, ExclusionTypes.Both);

            var _columnNames = dt.Columns.Cast<DataColumn>().Select(s => s.ColumnName).ToList();

            for (int i = 0; i < dt.Rows.Count; i++)
            {
                T _object = Activator.CreateInstance<T>();
                for (int j = 0; j < modelProperties.Count; j++)
                {
                    _object.GetType().GetProperty(modelProperties[j].PropertyName)
                    .SetValue(_object, Convert.ChangeType(dt.Rows[i][modelProperties[j].ColumnName], modelProperties[j].PropertyType), null);
                }
                _return.Add(_object);
            }
            return _return.ToList();
        }

        private List<T> MapToDynamic<T>(DataTable dt)
        {
            List<T> _return = (List<T>)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T)));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dynamic _object = new ExpandoObject();
                IDictionary<string, object> _Properties = _object;
                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    object _value = dt.Rows[i][j];
                    string _name = dt.Columns[j].ColumnName;

                    _Properties.Add(_name, _value);
                }
                _return.Add(_object);
            }

            return _return;
        }
        #endregion

        //public int ExecuteQuery(string Query, CommandType Type, params DbParameter[] Parameters)
        //{
        //    int _rowNumber = 0;

        //    using (Connection)
        //    {
        //        if (Connection.CurrentState != ConnectionState.Open)
        //            Connection.OpenConnection();

        //        using (DbCommand _cmd = Connection.CreateCommand())
        //        {
        //            _cmd.CommandText = Query;
        //            _cmd.CommandType = Type;

        //            AttachParameters(_cmd, Parameters);

        //            _rowNumber = _cmd.ExecuteNonQuery();
        //        }
        //    }
        //    return _rowNumber;
        //}

        public List<T> ExecteQuery_BatchReturn<T>(string Query, CommandType Type, params DbParameter[] Parameters)
        {
            List<T> _return = null;
            using (Connection)
            {
                if (Connection.CurrentState != ConnectionState.Open)
                    Connection.OpenConnection();
                using (DbCommand _cmd = Connection.CreateCommand())
                {
                    _cmd.CommandText = Query;
                    _cmd.CommandType = Type;

                    AttachParameters(_cmd, Parameters);

                    DbDataReader _reader = _cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    DataTable _dt = new DataTable();
                    _dt.Load(_reader);

                    _return = MapToClass<T>(_dt);
                }
            }
            return _return;
        }

        public T ExecteQuery_SingleReturn<T>(string Query, CommandType Type, params DbParameter[] Parameters)
        {
            return ExecteQuery_BatchReturn<T>(Query, Type, Parameters).FirstOrDefault();
        }

        public int ExecuteQuery_DML<T>(string Query, CommandType Type, T Model)
        {
            int _rowNumber = 0;

            using (Connection)
            {
                if (Connection.CurrentState != ConnectionState.Open)
                    Connection.OpenConnection();

                using (DbCommand _cmd = Connection.CreateCommand())
                {
                    _cmd.CommandText = Query;
                    _cmd.CommandType = Type;

                    AttachParameters<T>(_cmd, Model);

                    _rowNumber = _cmd.ExecuteNonQuery();
                }
            }
            return _rowNumber;
        }
    }
}