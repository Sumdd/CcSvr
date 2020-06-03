// ClassName  : call_switchtacticsmodelModel
// Creator    : Sun Zhongguan
// CreateTime : 2016-02-19 14:26:45
// Copyright  : Cenozoic Software Co.,Ltd  www.ceno-soft.net

using System;
using System.Data;

namespace DB.Model
{
	public class call_switchtacticsmodel_model
	{
		public int ID { get; set; }

		public string TacticsModel { get; set; }

		public string TacticsDescription { get; set; }

		public int SwitchTypeID { get; set; }

	}
}