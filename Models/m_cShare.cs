using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model_v1
{
    public class dial_area
    {
        /// <summary>
        /// 域ID
        /// </summary>
        public int id
        {
            get; set;
        }
        /// <summary>
        /// 域名称
        /// </summary>
        public string aname
        {
            get; set;
        }
        /// <summary>
        /// 域IP
        /// </summary>
        public string aip
        {
            get; set;
        }
        /// <summary>
        /// 域端口
        /// </summary>
        public int aport
        {
            get; set;
        }
        /// <summary>
        /// 域数据库名称
        /// </summary>
        public string adb
        {
            get; set;
        }
        /// <summary>
        /// 域用户名
        /// </summary>
        public string auid
        {
            get; set;
        }
        /// <summary>
        /// 域密码
        /// </summary>
        public string apwd
        {
            get; set;
        }
        /// <summary>
        /// 是否为主域
        /// </summary>
        public int amain
        {
            get; set;
        }
        /// <summary>
        /// 状态
        /// </summary>
        public int astate
        {
            get; set;
        }
    }
    public class share_number
    {
        /// <summary>
        /// UUID方便获取数据
        /// </summary>
        public string uuid
        {
            get; set;
        }
        /// <summary>
        /// 域ID,需要将其持久化至数据中
        /// </summary>
        public int areaid
        {
            get; set;
        }
        /// <summary>
        /// id
        /// </summary>
        public int id
        {
            get; set;
        }
        /// <summary>
        /// 号码
        /// </summary>
        public string number
        {
            get; set;
        }
        /// <summary>
        /// 同号限制呼叫
        /// </summary>
        public int limitthedial
        {
            get; set;
        }
        /// <summary>
        /// 使用次数
        /// </summary>
        public int usecount
        {
            get; set;
        }
        /// <summary>
        /// 使用时长
        /// </summary>
        public int useduration
        {
            get; set;
        }
        /// <summary>
        /// 限制次数
        /// </summary>
        public int limitcount
        {
            get; set;
        }
        /// <summary>
        /// 限制时长
        /// </summary>
        public int limitduration
        {
            get; set;
        }
        /// <summary>
        /// 使用天
        /// </summary>
        public DateTime usethetime
        {
            get; set;
        }
        /// <summary>
        /// 当日使用次数
        /// </summary>
        public int usethecount
        {
            get; set;
        }
        /// <summary>
        /// 当日使用时长
        /// </summary>
        public int usetheduration
        {
            get; set;
        }
        /// <summary>
        /// 当日限制使用次数
        /// </summary>
        public int limitthecount
        {
            get; set;
        }
        /// <summary>
        /// 当日限制使用时长
        /// </summary>
        public int limittheduration
        {
            get; set;
        }
        public int isuse
        {
            get; set;
        }
        /// <summary>
        /// 前缀加拨
        /// </summary>
        public string dialprefix
        {
            get; set;
        }
        /// <summary>
        /// 本地前缀加拨
        /// </summary>
        public string diallocalprefix
        {
            get; set;
        }
        /// <summary>
        /// 区号
        /// </summary>
        public string areacode
        {
            get; set;
        }
        /// <summary>
        /// 地区
        /// </summary>
        public string areaname
        {
            get; set;
        }
        /// <summary>
        /// 禁止呼出
        /// </summary>
        public int isusedial
        {
            get; set;
        }
        /// <summary>
        /// 禁止呼入
        /// </summary>
        public int isusecall
        {
            get; set;
        }
        /// <summary>
        /// dtmf方式
        /// </summary>
        public string dtmf
        {
            get; set;
        }
        /// <summary>
        /// 电话状态
        /// </summary>
        public SHARE_NUM_STATUS state
        {
            get; set;
        }
        /// <summary>
        /// 网关
        /// </summary>
        public string gw
        {
            get; set;
        }
        /// <summary>
        /// 网关类型
        /// </summary>
        public string gwtype
        {
            get; set;
        }
        /// <summary>
        /// 真实号码
        /// </summary>
        public string tnumber
        {
            get; set;
        }
        /// <summary>
        /// 排序
        /// </summary>
        public decimal ordernum
        {
            get; set;
        }
        /// <summary>
        /// 共享号码的延申,不是直接使用,而是转接
        /// </summary>
        public int isshare
        {
            get; set;
        }
        /// <summary>
        /// 软交换的IP
        /// </summary>
        public string fs_ip
        {
            get; set;
        }
        /// <summary>
        /// 软交换的Ua
        /// </summary>
        public string fs_num
        {
            get; set;
        }
        /// <summary>
        /// 坐席ID
        /// </summary>
        public int agentID
        {
            get; set;
        }
        /// <summary>
        /// 通道ID
        /// </summary>
        public int channelID
        {
            get; set;
        }
        /// <summary>
        /// 续联ID,直接写入登录,可以一直不注销或者每打电话就登录一次
        /// </summary>
        public int xxID
        {
            get; set;
        }
        /// <summary>
        /// 续联Ua
        /// </summary>
        public string xxUa
        {
            get; set;
        }
        /// <summary>
        /// 续联密码
        /// </summary>
        public string xxPwd
        {
            get; set;
        }
        /// <summary>
        /// 续联登录状态
        /// </summary>
        public int xxLogin
        {
            get; set;
        }
        /// <summary>
        /// 调用范围:0默认可用,1查对照表
        /// </summary>
        public int xxUse
        {
            get; set;
        }
    }
    public enum SHARE_NUM_STATUS
    {
        /// <summary>
        /// 空闲
        /// </summary>
        IDLE,
        /// <summary>
        /// 拨号
        /// </summary>
        DIAL,
        /// <summary>
        /// 通话
        /// </summary>
        TALKING,
        /// <summary>
        /// 来电
        /// </summary>
        CALL,
        /// <summary>
        /// 不可用
        /// </summary>
        UNIDLE
    }
    public class AddRecByRec
    {
        public string m_sEndPointStr
        {
            get; set;
        }
        public int m_uAgentID
        {
            get; set;
        }
        public int m_uFromAgentID
        {
            get; set;
        }
        public int m_uChannelID
        {
            get; set;
        }
        public string m_sFreeSWITCHIPv4
        {
            get; set;
        }
        public string UAID
        {
            get; set;
        }
    }
}
