using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model_v1;
using DB.Basic;
using Core_v1;
using System.Text.RegularExpressions;

namespace DB.Basic
{
    public class m_cPhone
    {
        public static List<string> m_fGetPhoneNumberMemo(string m_sPhoneNumber)
        {
            /*
             * 精简一下
             * 解决一:由于多号码的引入,是否需要加0出局要通过真正的线路来决定
             * 问题一:手机号码不需要加0,线路非本地需加0
             * 难点一:有的客户使用的线路不同,需要做兼容
             */

            List<string> m_lStrings = new List<string>();
            string m_sRealPhoneNumberStr = string.Empty;
            string m_sPhoneNumberTemp = m_sPhoneNumber;
            string m_sFirstChar = string.Empty;
            string m_sPrefixStr = string.Empty;
            string m_sPhoneAddressStr = string.Empty;
            string m_sCityCodeStr = string.Empty;
            string m_sDealWithStr = string.Empty;
            ///修改规则,如果尾缀有*1000等格式则判断为内呼,并兼容内呼规则
            Regex m_rRegex = new Regex("(.*)[*][0-9][0-9][0-9][0-9]$");

            try
            {

                /*
                 * 到底前面的零需不需要去掉需要根据规则来判断
                 * 解决一:这里直接开始处理
                 * 优化一:这里处理了之后,最好后台部分就不需要再做处理了,直接拿来使用即可
                 * 冗余一:前后端对一个号码来回处理多次
                 * 解决二:电话都当作不需要加0的来处理,或者增加一个开关变量,允许自动处理电话,否则电话是什么样子就都必须到后方
                 * 问题一:如特殊命令*57*怎么办呢?加了0是
                 */

                switch (m_sPhoneNumber[0])
                {

                    /*
                     * 由于内呼是*
                     * 解决一:也就是含有业务的必须加0
                     * OK,目前以这个逻辑继续
                     * 问题一:*开头业务,无法操作
                     */

                    case '*':
                        if (m_rRegex.IsMatch(m_sPhoneNumber))
                        {
                            m_sFirstChar = Special.Star;
                            m_sRealPhoneNumberStr = m_sPhoneNumber.Substring(1);
                            m_sPhoneAddressStr = "内呼";
                            m_sDealWithStr = Special.Complete;
                        }
                        else
                        {
                            m_sFirstChar = Special.Zero;
                            m_sRealPhoneNumberStr = m_sPhoneNumber;
                            m_sPhoneAddressStr = "业务";
                            m_sDealWithStr = Special.Complete;
                        }
                        break;
                    case '0':
                    default:
                        m_sFirstChar = Special.Zero;

                        if (m_rRegex.IsMatch(m_sPhoneNumber))
                        {
                            ///判断为内呼,并兼容内呼规则
                            m_sFirstChar = Special.Star;
                            ///真实号码不做处理即可
                            m_sRealPhoneNumberStr = m_sPhoneNumber;
                            m_sPhoneAddressStr = "内呼";
                            m_sDealWithStr = Special.Complete;
                        }
                        else if (m_sPhoneNumber.Contains(Special.Star) ||
                            m_sPhoneNumber.Contains(Special.Hash))
                        {
                            /*
                             * 业务前面的零需要去掉
                             * 假设没有以0开头的其他业务
                             */

                            m_sRealPhoneNumberStr = m_sPhoneNumber.TrimStart('0');
                            m_sPhoneAddressStr = "业务";
                            m_sDealWithStr = Special.Complete;
                        }
                        else
                        {
                            if ((m_sPhoneNumber.Length == 7 || m_sPhoneNumber.Length == 8) && !m_sPhoneNumber.StartsWith("0"))
                            {
                                /*
                                 * 将此电话直接算作本地电话
                                 */
                                m_sRealPhoneNumberStr = m_sPhoneNumberTemp;
                                m_sPrefixStr = "";
                                m_sPhoneAddressStr = "本地";
                                m_sDealWithStr = Special.Complete;
                            }
                            else
                            {
                                /*
                                 * 移除首个0进行判断
                                 * 后面才可能是区号
                                 */
                                m_sPhoneNumber = m_sPhoneNumber.TrimStart('0');
                                if (m_sPhoneNumber.Length < 7)
                                {
                                    m_sRealPhoneNumberStr = m_sPhoneNumberTemp;
                                    m_sPhoneAddressStr = "特殊号码";
                                    m_sDealWithStr = Special.Complete;
                                }
                                else if (m_sPhoneNumber.Length >= 7 && m_sPhoneNumber[0] == '1' && m_sPhoneNumber[1] >= '3')
                                {
                                    m_sRealPhoneNumberStr = m_sPhoneNumber;
                                    m_sPrefixStr = m_sRealPhoneNumberStr.Substring(0, 7);
                                    m_sPhoneAddressStr = call_phoneaddress.m_fGetCityNameByPhoneNumber(m_sPrefixStr, out m_sCityCodeStr);
                                    m_sDealWithStr = Special.Mobile;
                                }
                                else
                                {
                                    switch (m_sPhoneNumber[0])
                                    {
                                        case '1':
                                        case '2':
                                            m_sRealPhoneNumberStr = $"0{m_sPhoneNumber}";
                                            m_sPrefixStr = m_sRealPhoneNumberStr.Substring(0, 3);
                                            break;
                                        default:
                                            if (m_sPhoneNumber.StartsWith("852") ||
                                                m_sPhoneNumber.StartsWith("853") ||
                                                m_sPhoneNumber.StartsWith("856"))
                                            {
                                                m_sRealPhoneNumberStr = $"00{m_sPhoneNumber}";
                                                m_sPrefixStr = m_sRealPhoneNumberStr.Substring(0, 5);
                                            }
                                            else
                                            {
                                                m_sRealPhoneNumberStr = $"0{m_sPhoneNumber}";
                                                m_sPrefixStr = m_sRealPhoneNumberStr.Substring(0, 4);
                                            }
                                            break;
                                    }

                                    ///特殊处理400、800
                                    if (m_sPhoneNumber.StartsWith("400") || m_sPhoneNumber.StartsWith("800"))
                                    {
                                        m_sRealPhoneNumberStr = $"{m_sPhoneNumber}";
                                        m_sPhoneAddressStr = "特殊";
                                        m_sDealWithStr = Special.Complete;
                                    }
                                    else
                                    {
                                        m_sPhoneAddressStr = call_phoneaddress.m_fGetCityNameByCityCode(m_sPrefixStr, out m_sCityCodeStr);
                                        m_sDealWithStr = Special.Telephone;
                                    }
                                }

                                if (string.IsNullOrWhiteSpace(m_sPhoneAddressStr))
                                    m_sPhoneAddressStr = call_phoneaddress.m_fGetPhoneAddressByNet(m_sPrefixStr);

                                if (string.IsNullOrWhiteSpace(m_sPhoneAddressStr))
                                    m_sPhoneAddressStr = "未知";
                            }

                        }
                        break;
                }

                //0 处理
                m_lStrings.Add(m_sRealPhoneNumberStr);
                //1 原号
                m_lStrings.Add(m_sPhoneNumberTemp);
                //2 内呼外呼
                m_lStrings.Add(m_sFirstChar.ToString());
                //3 归属地
                m_lStrings.Add(m_sPhoneAddressStr);
                //4 区号
                m_lStrings.Add(m_sCityCodeStr);
                //5 是否需要继续处理的代码
                m_lStrings.Add(m_sDealWithStr);
            }
            catch (Exception ex)
            {
                Log.Instance.Error($"[DB.Basic][m_cPhone][m_fGetPhoneNumberMemo][{ex.Message}]");
            }

            return m_lStrings;
        }

        /// <summary>
        /// 更新号码任务
        /// </summary>
        /// <param name="m_sPhoneNumberStr">7位手机号码前缀</param>
        public static void m_fTaskUpdPhone(string m_sPhoneNumberStr, DateTime? m_dtUpdTime)
        {
            ///判断更新类型
            int m_uTaskUpdPhoneInterval = Call_ParamUtil.m_uTaskUpdPhoneInterval;
            if (m_uTaskUpdPhoneInterval == -1) return;

            new System.Threading.Thread(new System.Threading.ThreadStart(() =>
            {
                try
                {
                    ///更新URL
                    string m_sTaskUpdPhoneURL = Call_ParamUtil.m_sTaskUpdPhoneURL;
                    ///判断逻辑
                    if (m_sPhoneNumberStr?.Length == 7 && m_dtUpdTime != null && !string.IsNullOrWhiteSpace(m_sTaskUpdPhoneURL))
                    {

                        switch (m_uTaskUpdPhoneInterval)
                        {
                            case 0://实时更新
                                break;
                            case -1://不更新
                                return;
                            case -2://待定
                            default://其它
                                if (m_uTaskUpdPhoneInterval > 0 && DateTime.Compare(m_dtUpdTime.Value.AddDays(m_uTaskUpdPhoneInterval), DateTime.Now) < 0)
                                    break;
                                else return;
                        }

                        ///插入查询队列
                        lock (m_cPhone.m_pTaskUpdPhoneLock)
                        {
                            if (!m_lTaskUpdPhone.Contains(m_sPhoneNumberStr) && m_lTaskUpdPhone.Count <= 1000)
                            {
                                m_lTaskUpdPhone.Add(m_sPhoneNumberStr);
                                Log.Instance.Warn($"[DB.Basic][m_cPhone][m_fTaskUpdPhone][lock][Need Add:{m_sPhoneNumberStr}]");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Instance.Error($"[DB.Basic][m_cPhone][m_fTaskUpdPhone][Exception][{ex.Message}]");
                }
            })).Start();
        }

        /// <summary>
        /// 电话更新锁
        /// </summary>
        public static object m_pTaskUpdPhoneLock = new object();
        /// <summary>
        /// 电话更新列表
        /// </summary>
        public static List<string> m_lTaskUpdPhone = new List<string>();
    }
}
