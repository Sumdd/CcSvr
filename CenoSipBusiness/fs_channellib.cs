using CenoCommon;
using CenoSipFactory;

using DB.Basic;
using DB.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenoSipBusiness
{
	public class fs_channellib
	{
		private static ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static void intilizate_fs_account_info()
		{
			if (call_factory.fs_account_list == null)
				call_factory.fs_account_list = new List<fs_account_info>();

            /*
                加载freeswith通道信息
                根本不知道这是要干啥
             */

			List<fs_channel_model> fs_account_list = new List<fs_channel_model>(fs_channel.GetList());

			if (fs_account_list.Count <= 0)
			{
				_Ilog.Warn("failed to initilizate fs channel info, not found any data");
				return;
			}

			foreach (fs_channel_model fs_mod in fs_account_list)
			{
				try
				{
					call_factory.fs_account_list.Add(
						new fs_account_info()
						{
							uniqueid = fs_mod.UniqueID.ToString(),
							password = fs_mod.password.ToString(),
							user_context = fs_mod.user_context.ToString(),
							vm_password = fs_mod.vm_password.ToString(),
							toll_allow = fs_mod.toll_allow.ToString(),
							accountcode = fs_mod.accountcode.ToString(),
							effective_caller_id_name = fs_mod.effective_caller_id_name.ToString(),
							effective_caller_id_number = fs_mod.effective_caller_id_number.ToString(),
							outbound_caller_id_name = fs_mod.outbound_caller_id_name.ToString(),
							outbound_caller_id_number = fs_mod.outbound_caller_id_number.ToString(),
							call_group = fs_mod.call_group.ToString()
						});
					_Ilog.Info("initilizate fs channel(" + fs_mod.UniqueID + ") info");

				}
				catch (Exception ex)
				{
					_Ilog.Error("failed to initilizate fs channel(" + fs_mod.UniqueID + ") info:" + ex.Message, ex);
				}
			}
		}
	}
}
