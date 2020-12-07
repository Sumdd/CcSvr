using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Core_v1
{
    public class RedisClient
    {
        private DateTime m_dtStart = DateTime.Now;
        private IConnectionMultiplexer ConnMultiplexer;
        private IDatabase RedisDatabase;

        public RedisClient(string host, int port, string password, int db)
        {
            string connectionString = $"{host}:{port},password={password}";
            if (ConnMultiplexer == null)
            {
                ConnMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            }
            if (RedisDatabase == null)
            {
                RedisDatabase = ConnMultiplexer.GetDatabase(db);
            }
        }

        /// <summary>
        /// 是否正常链接
        /// </summary>
        /// <returns></returns>
        public bool IsSocketConnected()
        {
            if (ConnMultiplexer != null && ConnMultiplexer.IsConnected)
                return true;
            return false;
        }

        public bool Set(string key, string value, DateTime expiresAt)
        {
            ///修正过期时间不准确问题
            bool m_bSuccess = RedisDatabase.StringSet(key, value, expiresAt.Subtract(m_dtStart));
            return m_bSuccess;
        }

        public bool Expire(string key, int seconds)
        {
            bool m_bSuccess = RedisDatabase.KeyExpire(key, TimeSpan.FromSeconds(seconds));
            return m_bSuccess;
        }

        public List<string> GetAllKeys()
        {
            var pattern = "*";
            var redisResult = RedisDatabase.ScriptEvaluate(LuaScript.Prepare(
                            " local res = redis.call('KEYS', @keypattern) " +
                            " return res "), new { @keypattern = pattern });
            return ((string[])redisResult).ToList();
        }

        public long Del(string key)
        {
            return RedisDatabase.KeyDelete(key) ? 1 : 0;
        }

        public long Del(params string[] keys)
        {
            long d = 0;
            foreach (string item in keys)
            {
                d = RedisDatabase.KeyDelete(item) ? 1 : 0;
            }
            return d;
        }

        public long SetNX(string key, string value, int seconds)
        {
            return RedisDatabase.StringSet(key, value, TimeSpan.FromSeconds(seconds), When.NotExists, CommandFlags.None) ? 1 : 0;
        }

        public IDictionary<string, string> GetAll<T>(IEnumerable<string> keys, bool m_bRemoveNull = false)
        {
            RedisValue[] m_lRedisValue = RedisDatabase.StringGet(keys.Select(x => (RedisKey)x).ToArray());
            IDictionary<string, string> m_lDic = new Dictionary<string, string>();
            string[] m_lKeys = keys.ToArray();
            for (int i = 0; i < m_lRedisValue.Length; i++)
            {
                RedisValue m_pRedisValue = m_lRedisValue[i];
                if (m_pRedisValue.HasValue) m_lDic.Add(new KeyValuePair<string, string>(m_lKeys[i], m_pRedisValue.ToString()));
                else if (!m_bRemoveNull) m_lDic.Add(new KeyValuePair<string, string>(m_lKeys[i], null));
            }
            return m_lDic;
        }

        public void Dispose()
        {
            if (ConnMultiplexer != null)
                ConnMultiplexer.Dispose();
        }

        public string Get<T>(string key)
        {
            RedisValue m_pRedisValue = RedisDatabase.StringGet(key);
            if (m_pRedisValue.HasValue)
                return m_pRedisValue.ToString();
            return null;
        }

        public bool ContainsKey(string key)
        {
            return RedisDatabase.KeyExists(key);
        }
    }
}
