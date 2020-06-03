using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic
{
    public class tellModelRecord
    {
        public static string m_fGetRecPath(string m_sID)
        {
            string m_sSQL = $@"
SELECT
	`recPath` 
FROM
	tellmodelrecord 
WHERE
	id = '{m_sID}'
";
            object m_oObject = MySQL_Method.ExecuteScalar(m_sSQL);
            return m_oObject.ToString();
        }
    }
}
