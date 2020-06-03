using CenoSipFactory;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB.Basic;

namespace CenoSipBusiness
{

	public class init_phonenum_list
	{
		private static ILog _Ilog = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public void init_channel_phonenum_list()
		{
			if (call_factory.phonenum_list == null)
				call_factory.phonenum_list = new List<phonenumlist>();

			var _list = call_phonenum_list.GetList(_list_type.black.ToString(), Int32.MaxValue);
			if (_list == null || _list.Count == 0)
				return;

			foreach (var _model in _list)
				call_factory.phonenum_list.Add(new phonenumlist()
				{
					uid = new Guid(_model.uniqueid),
					list_type = (_list_type)Enum.Parse(typeof(_list_type), _model.list_type),
					bound = (_bound)Enum.Parse(typeof(_bound), _model.bound),
					localflag = _model.localflag == 1,
					remoteflag = _model.remoteflag == 1,
					start_num = _model.start_num,
					end_num = _model.end_num,
					valid_time = _model.validtime,
					add_time = _model.addtime,
					limit_user = new Guid(_model.limituser),
					addreason = _model.addreason,
					add_user = _model.adduser
				});
		}
	}
}
