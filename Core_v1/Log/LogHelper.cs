using System;
using System.Reflection;

namespace Core_v1 {
    /// <summary>
    /// 日志
    /// </summary>
    public class Log {
        /// <summary>
        /// 日志级别
        /// </summary>
        private int m_Loglevel = 4;
        /// <summary>
        /// 日志构造函数
        /// </summary>
        private Log() {
            log4net.Config.XmlConfigurator.Configure();
            log4 = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        /// <summary>
        /// 私人静态实例
        /// </summary>
        private log4net.ILog log4 = null;
        /// <summary>
        /// 私人静态实例
        /// </summary>
        private static Log _instance = null;
        /// <summary>
        /// 公共静态实例
        /// </summary>
        public static Log Instance {
            get {
                return _instance == null ? _instance = new Log() : _instance;
            }
        }
        /// <summary>
        /// 调试
        /// </summary>
        /// <param name="msg">错误信息对象</param>
        public void Debug(object msg) {
            if(m_Loglevel >= 5)
                log4.Debug(msg);
        }
        /// <summary>
        /// 信息
        /// </summary>
        /// <param name="msg">信息字符串</param>
        public void Success(string msg) {
            if(m_Loglevel >= 4)
                log4.Info(msg);
        }
        /// <summary>
        /// 警告
        /// </summary>
        /// <param name="msg">警告信息字符串</param>
        public void Warn(string msg) {
            if(m_Loglevel >= 3)
                log4.Warn(msg);
        }
        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="msg">错误信息字符串</param>
        public void Error(string msg) {
            if(m_Loglevel >= 2)
                log4.Error(msg);
        }
        /// <summary>
        /// 失败
        /// </summary>
        /// <param name="msg">失败信息字符串</param>
        public void Fail(string msg) {
            if(m_Loglevel >= 1)
                log4.Fatal(msg);
        }
        /// <summary>
        /// 设置日志级别
        /// </summary>
        /// <param name="Loglevel">日志级别数字</param>
        public void SetLogLevel(int Loglevel) {
            m_Loglevel = Loglevel;
            log4.Info($"[Log][SetLogLevel][设置日志级别为{Loglevel}[ALL(>=5)|DEBUG(5)|INFO(4)|WARN(3)|ERROR(2)|FATAL(1)|OFF(<=0)]]");
        }
        /// <summary>
        /// 查看当前日志级别
        /// </summary>
        /// <returns></returns>
        public int GetLogLevel()
        {
            return m_Loglevel;
        }
    }
}
