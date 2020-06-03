using DB.Model;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB.Basic {
    public class PhoneAutoCall {
        public static IList<PhoneAutoCall_model> GetList() {
            IList<PhoneAutoCall_model> list = new List<PhoneAutoCall_model>();
            string sql = @"select ID,PhoneNum,pici,progressFlag,contentTxt,status,addTime,callTime,endTime,result,IsUpdate,luyinId,CallNum,CallStatus,CallCount,source_id,ajid,inpici,shfzh18,czy from PhoneAutoCall where status<>0 and status<>-1 and status<>-2 and CallStatus<>1 order by  random,addTime  asc";

            using(var dr = MySQL_Method.ExecuteDataReader(sql)) {
                while(dr != null && dr.HasRows && dr.Read()) {
                    list.Add(new PhoneAutoCall_model() {
                        ID = int.Parse(dr["ID"].ToString()),
                        PhoneNum = dr["PhoneNum"].ToString(),
                        pici = dr["pici"].ToString(),
                        progressFlag = Convert.ToInt32(dr["progressFlag"]),
                        contentTxt = dr["contentTxt"].ToString(),
                        status = dr["status"].ToString(),
                        addTime = Convert.ToDateTime(dr["addTime"]),
                        callTime = Convert.ToDateTime(dr["callTime"]),
                        endTime = Convert.ToDateTime(dr["endTime"]),
                        result = dr["result"].ToString(),
                        IsUpdate = int.Parse(dr["IsUpdate"].ToString()),
                        luyinId = dr["luyinId"].ToString(),
                        CallNum = dr["CallNum"].ToString(),
                        CallStatus = int.Parse(dr["CallStatus"].ToString()),
                        CallCount = int.Parse(dr["CallCount"].ToString()),
                        source_id = int.Parse(dr["source_id"].ToString()),
                        ajid = dr["ajid"].ToString(),
                        inpici = dr["inpici"].ToString(),
                        shfzh18 = dr["shfzh18"].ToString(),
                        czy = dr["czy"].ToString()
                    });
                }
            }
            return list;
        }


        /*
         * 修正自动外呼逻辑
         * 这里目前想直接全部提取出来
         * 比如说队列,一次最多提取多少条,每过多长时间提取一次
         * 这样可以降低查询频率,减小数据库的查询压力
         */

        public static object lock_obj = new object();
        public static DataTable GetData() {
            DataTable AutoDialDT = new DataTable();
            lock(lock_obj) {
                string asSQL = $@"select *,'{Guid.NewGuid()}' as random from PhoneAutoCall where status<>0 and status<>-1 and status<>-2 and CallStatus<>1 order by  random,addTime  asc limit 10";
                AutoDialDT = MySQL_Method.BindTable(asSQL);
                if(AutoDialDT != null && AutoDialDT.Rows.Count > 0) {
                    List<string> list = new List<string>();
                    foreach(DataRow item in AutoDialDT.Rows) {
                        list.Add(item["id"].ToString());
                    }
                    string UpdateStatus = "update PhoneAutoCall set CallStatus='1',CallCount=CallCount+1 where id in(" + string.Join(",", list) + ")";
                    MySQL_Method.ExecuteNonQuery(UpdateStatus);
                }
            }
            return AutoDialDT;
        }

        /// <summary>
        /// 程序初始化时,加载已加入队列,但未拨打的数据
        /// </summary>
        /// <returns></returns>
        public static DataTable m_fGetUnDequeueTaskDataTable()
        {
            string asSQL = $@"
select * from phoneautocall
where 1=1
and status <> '0'     -- 不详
and status <> '-1'    -- 不详
and status <> '-2'    -- 不详
and callstatus = 2  -- 新增一个参数值:已加入队列
order by addtime asc
";
            return MySQL_Method.BindTable(asSQL);
        }

        /// <summary>
        /// 查询需要自动拨号的数据
        /// </summary>
        /// <param name="m_uLimit"></param>
        /// <returns></returns>
        public static DataTable m_fGetEnQueueTaskDataTable(int m_uLimit)
        {
            try
            {
                string asSQL = $@"
create temporary table if not exists ta_task
(
  `id` int PRIMARY KEY,
  `PhoneNum` varchar(20),
  `pici` varchar(20),
  `progressFlag` int(11),
  `contentTxt` varchar(2000),
  `status` varchar(50),
  `addTime` datetime(0),
  `callTime` datetime(0),
  `endTime` datetime(0),
  `result` varchar(50),
  `IsUpdate` int(11),
  `luyinId` varchar(200),
  `CallNum` varchar(50),
  `CallStatus` int(11),
  `CallCount` int(11),
  `source_id` bigint(20),
  `ajid` varchar(75),
  `inpici` varchar(75),
  `shfzh18` varchar(20),
  `czy` varchar(75),
  `asr_status` int(11)
) engine = memory;
truncate table ta_task;
insert into ta_task
select * from phoneautocall
where 1=1
and status <> '0'     -- 不详
and status <> '-1'    -- 不详
and status <> '-2'    -- 不详
and callstatus <> 1 -- 已拨打
and callstatus <> 2 -- 新增一个参数值:已加入队列
order by addtime asc
limit {m_uLimit};
update phoneautocall
set callstatus=2    -- 只修改状态即可,因为读取之后未必会打 
where id in (select id from ta_task);
select * from ta_task;
drop temporary table if exists ta_task;
";
                return MySQL_Method.BindTable(asSQL);
            }
            catch (Exception ex)
            {
                Core_v1.Log.Instance.Error($"[DB.Basic][PhoneAutoCall][m_fGetEnQueueTaskDataTable][Exception][{ex.Message}]");
            }
            return null;
        }

        public static void CallCount1(int id) {
            string UpdateStatus = "update PhoneAutoCall set CallCount=CallCount+1 where id = " + id + " ";
            MySQL_Method.ExecuteNonQuery(UpdateStatus);
        }

        public static int Update(int? ID, params object[] KeyValues) {
            StringBuilder sb = new StringBuilder();
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            if(KeyValues.Length % 2 != 0 && KeyValues.Length > 0)
                throw new Exception("参数需为非零偶数个");
            sb.Append("update PhoneAutoCall set ");
            for(int i = 0; i < KeyValues.Length; i += 2) {
                sb.Append(KeyValues[i] + "=@" + KeyValues[i] + ",");
                parameters.Add(new MySqlParameter("@" + KeyValues[i], KeyValues[i + 1]));
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(" where id=@id");
            parameters.Add(new MySqlParameter("@id", ID));
            return MySQL_Method.ExecuteNonQuery(sb.ToString(), parameters.ToArray());
        }

        public static int Update(PhoneAutoCall_model m_mPhoneAutoCall)
        {
            return 0;
        }

        public static bool Insert(PhoneAutoCall_model model)
        {
            #region SQL语句
            string m_sInsertSQL = $@"
INSERT INTO `phoneautocall` (
	`PhoneNum`,
	`pici`,
	`progressFlag`,
	`contentTxt`,
	`status`,
	`addTime`,
	`callTime`,
	`endTime`,
	`result`,
	`IsUpdate`,
	`luyinId`,
	`CallNum`,
	`CallStatus`,
	`CallCount`,
	`source_id`,
	`ajid`,
	`inpici`,
	`shfzh18`,
	`czy`,
    `asr_status`
)
VALUES
	(
		@PhoneNum,
		@pici,
		@progressFlag,
		@contentTxt,
		@status,
		@addTime,
		@callTime,
		@endTime,
		@result,
		@IsUpdate,
		@luyinId,
		@CallNum,
		@CallStatus,
		@CallCount,
		@source_id,
		@ajid,
		@inpici,
		@shfzh18,
	    @czy,
        @asr_status
	);
";
            #endregion

            #region 设置动态参数
            MySqlParameter[] parameters = {
                new MySqlParameter("@PhoneNum",MySqlDbType.VarChar,20),
                new MySqlParameter("@pici",MySqlDbType.VarChar,20),
                new MySqlParameter("@progressFlag",MySqlDbType.Int32),
                new MySqlParameter("@contentTxt",MySqlDbType.VarChar,2000),
                new MySqlParameter("@status",MySqlDbType.VarChar,50),
                new MySqlParameter("@addTime",MySqlDbType.DateTime),
                new MySqlParameter("@callTime",MySqlDbType.DateTime),
                new MySqlParameter("@endTime",MySqlDbType.DateTime),
                new MySqlParameter("@result",MySqlDbType.VarChar,50),
                new MySqlParameter("@IsUpdate",MySqlDbType.Int32),
                new MySqlParameter("@luyinId",MySqlDbType.VarChar,200),
                new MySqlParameter("@CallNum",MySqlDbType.VarChar,50),
                new MySqlParameter("@CallStatus",MySqlDbType.Int32),
                new MySqlParameter("@CallCount",MySqlDbType.Int32),
                new MySqlParameter("@source_id",MySqlDbType.Int64),
                new MySqlParameter("@ajid",MySqlDbType.VarChar,75),
                new MySqlParameter("@inpici",MySqlDbType.VarChar,75),
                new MySqlParameter("@shfzh18",MySqlDbType.VarChar,20),
                new MySqlParameter("@czy",MySqlDbType.VarChar,75),
                new MySqlParameter("@asr_status",MySqlDbType.Int32)
            };
            parameters[0].Value = model.PhoneNum;
            parameters[1].Value = model.pici;
            parameters[2].Value = model.progressFlag;
            parameters[3].Value = model.contentTxt;
            parameters[4].Value = model.status;
            parameters[5].Value = model.addTime;
            parameters[6].Value = model.callTime;
            parameters[7].Value = model.endTime;
            parameters[8].Value = model.result;
            parameters[9].Value = model.IsUpdate;
            parameters[10].Value = model.luyinId;
            parameters[11].Value = model.CallNum;
            parameters[12].Value = model.CallStatus;
            parameters[13].Value = model.CallCount;
            parameters[14].Value = model.source_id;
            parameters[15].Value = model.ajid;
            parameters[16].Value = model.inpici;
            parameters[17].Value = model.shfzh18;
            parameters[18].Value = model.czy;
            parameters[19].Value = model.asr_status;
            #endregion

            return MySQL_Method.ExecuteNonQuery(m_sInsertSQL, parameters) > 0;
        }
    }
}
