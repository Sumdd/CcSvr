<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PersonalInfo.aspx.cs" Inherits="WebApplication2.PersonalInfo.P_Overview.PersonalInfo" %>

<%@ Register Assembly="DevExpress.XtraReports.v15.2.Web, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.XtraReports.Web" TagPrefix="dx" %>

<%@ Register Assembly="DevExpress.XtraCharts.v15.2.Web, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.XtraCharts.Web" TagPrefix="dxchartsui" %>
<%@ Register Assembly="DevExpress.XtraCharts.v15.2, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.XtraCharts" TagPrefix="cc1" %>

<%@ Register Assembly="DevExpress.Web.v15.2, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web" TagPrefix="dx" %>

<%@ Register Assembly="DevExpress.Web.ASPxScheduler.v15.2, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxScheduler.Controls" TagPrefix="dxwschsc" %>
<%@ Register Assembly="DevExpress.Web.ASPxScheduler.v15.2, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" Namespace="DevExpress.Web.ASPxScheduler.Reporting" TagPrefix="dxwschsc" %>

<%@ Register assembly="DevExpress.XtraReports.v15.2.Web, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" namespace="DevExpress.XtraReports.Web.ClientControls" tagprefix="cc2" %>
<%@ Register assembly="DevExpress.Dashboard.v15.2.Web, Version=15.2.4.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a" namespace="DevExpress.DashboardWeb" tagprefix="dx" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
	<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
	<title></title>
	<link rel="stylesheet" type="text/css" href="../../Styles/bootstrap.min.css" />
	<link rel="stylesheet" type="text/css" href="../../Styles/admin-all.css" />
	<script type="text/javascript" src="../../Scripts/jquery-1.7.2.js"></script>
	<script type="text/javascript" src="../../Scripts/jquery-ui-1.8.22.custom.min.js"></script>
	<link rel="stylesheet" type="text/css" href="../../Styles/ui-lightness/jquery-ui-1.8.22.custom.css" />
	<script type="text/javascript">
		$(function () {
			$(".datepicker").datepicker();

			$('#list').hide();
			$('#find').click(function () {
				$('#list').show();
			})
		})

	</script>
</head>
<body>
	<form id="form1" runat="server">
		<table class="table table-striped table-bordered table-condensed">
			<thead>
				<tr>
					<td colspan="6" class="auto-style2">&nbsp;个人信息 - 今日统计 </td>
				</tr>
			</thead>
			<tbody style="text-align: center">
				<tr style="height: 35px">
					<td style="width: 13%">帐号信息</td>
					<td class="detail" colspan="5">姓名：张三&nbsp;&nbsp;&nbsp;&nbsp;工号：QD001&nbsp;&nbsp;&nbsp;&nbsp;部门：法务员&nbsp;&nbsp;&nbsp;&nbsp;电话：58876001
					</td>
				</tr>
				<tr style="height: 30px">
					<td style="width: 13%">来电数量</td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%">去电数量</td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%">未接数量</td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
				</tr>
				<tr style="height: 30px">
					<td style="width: 13%">本币金额</td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%">申请人 </td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%"></td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
				</tr>
				<tr style="height: 30px">
					<td style="width: 13%">提交财务审核日期 </td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%">单据状态 </td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
					<td style="width: 13%"></td>
					<td><span style="color: red">&nbsp;&nbsp;50&nbsp;&nbsp;</span></td>
				</tr>
				<tr>
					<td>通话量分布线形图
					</td>
					<td colspan="5">
						<div style="margin-left:10%">
							<dxchartsui:WebChartControl ID="WebChartControl2" runat="server" CrosshairEnabled="True" Height="210px" Width="1039px" AppearanceNameSerializable="Light" BackColor="White" PaletteName="Aspect">
								<borderoptions color="255, 255, 255" />
								<diagramserializable>
				<cc1:XYDiagram>
					<axisx minorcount="1" title-text="时  间" title-visibility="True" visibleinpanesserializable="-1">
						<range scrollingrange-maxvalueserializable="23" scrollingrange-minvalueserializable="1" />
						<visualrange auto="False" maxvalueserializable="23" minvalueserializable="1" />
						<wholerange auto="False" maxvalueserializable="23" minvalueserializable="1" />
						<gridlines minorvisible="True">
						</gridlines>
					</axisx>
					<axisy title-text="数  量" title-visibility="True" visibleinpanesserializable="-1">
					</axisy>
					<defaultpane backcolor="Snow">
					</defaultpane>
				</cc1:XYDiagram>
			</diagramserializable>
								<legend visibility="True"></legend>
								<seriesserializable>
				<cc1:Series Name="来  电">
					<points>
						<cc1:SeriesPoint ArgumentSerializable="1" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="2" Values="0.1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="3" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="4" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="5" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="6" Values="0.2">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="7" Values="0.6">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="8" Values="1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="9" Values="2">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="10" Values="3">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="11" Values="2">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="12" Values="1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="13" Values="1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="14" Values="3.2">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="15" Values="2.4">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="16" Values="4.5">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="17" Values="4">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="18" Values="3">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="19" Values="1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="20" Values="1.1">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="21" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="22" Values="0.2">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="23" Values="0">
						</cc1:SeriesPoint>
						<cc1:SeriesPoint ArgumentSerializable="24" Values="0">
						</cc1:SeriesPoint>
					</points>
					<viewserializable>
						<cc1:SplineSeriesView Color="0, 176, 240" MarkerVisibility="False">
							<linestyle thickness="1" />
							<linemarkeroptions kind="Diamond" size="5">
							</linemarkeroptions>
						</cc1:SplineSeriesView>
					</viewserializable>
				</cc1:Series>
									<cc1:Series Name="去  电">
										<points>
											<cc1:SeriesPoint ArgumentSerializable="1" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="2" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="3" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="4" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="5" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="6" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="7" Values="1.2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="8" Values="1.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="9" Values="2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="10" Values="3.4">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="11" Values="3">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="12" Values="1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="13" Values="0.2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="14" Values="2.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="15" Values="3.4">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="16" Values="4.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="17" Values="3.3">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="18" Values="2.5">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="19" Values="2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="20" Values="1.3">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="21" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="22" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="23" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="24" Values="0">
											</cc1:SeriesPoint>
										</points>
										<viewserializable>
											<cc1:SplineSeriesView MarkerVisibility="False">
											</cc1:SplineSeriesView>
										</viewserializable>
									</cc1:Series>
									<cc1:Series Name="未  接">
										<points>
											<cc1:SeriesPoint ArgumentSerializable="1" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="2" Values="0.3">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="3" Values="0.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="4" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="5" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="6" Values="0.2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="7" Values="0.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="8" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="9" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="10" Values="0.2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="11" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="12" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="13" Values="0.5">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="14" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="15" Values="0.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="16" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="17" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="18" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="19" Values="0.6">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="20" Values="0.1">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="21" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="22" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="23" Values="0">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="24" Values="0.1">
											</cc1:SeriesPoint>
										</points>
										<viewserializable>
											<cc1:SplineSeriesView Color="255, 255, 0" MarkerVisibility="False">
											</cc1:SplineSeriesView>
										</viewserializable>
									</cc1:Series>
			</seriesserializable>
								<titles>
				<cc1:ChartTitle Font="宋体, 18pt, style=Bold" Indent="10" MaxLineCount="2" Text="来电数量统计  2016-05-10  " Visibility="False" />
			</titles>
							</dxchartsui:WebChartControl>
						</div>
					</td>
				</tr>
				<tr>
					<td>通话时长柱状图图
					</td>
					<td colspan="5">
						<div style="margin-left:10%">
							<dxchartsui:WebChartControl ID="WebChartControl3" runat="server" CrosshairEnabled="True" Height="200px" Width="964px">
								<borderoptions visibility="False" />
								<diagramserializable>
									<cc1:XYDiagram>
										<axisx minorcount="1" title-text="通话类型" title-visibility="True" visibleinpanesserializable="-1">
											<tickmarks minorvisible="False" />
											<label visible="False">
											</label>
										</axisx>
										<axisy title-text="通话量" title-visibility="True" visibleinpanesserializable="-1">
										</axisy>
									</cc1:XYDiagram>
								</diagramserializable>
								<legend visibility="False"></legend>
								<seriesserializable>
									<cc1:Series Name="Series 1">
										<points>
											<cc1:SeriesPoint ArgumentSerializable="未接" ColorSerializable="#4F81BD" Values="2">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="去电" ColorSerializable="#C0504D" Values="55">
											</cc1:SeriesPoint>
											<cc1:SeriesPoint ArgumentSerializable="来电" ColorSerializable="#9BBB59" Values="36">
											</cc1:SeriesPoint>
										</points>
										<viewserializable>
											<cc1:SideBySideBarSeriesView>
												<indicators>
													<cc1:AverageTrueRange Name="Average True Range 1">
													</cc1:AverageTrueRange>
												</indicators>
											</cc1:SideBySideBarSeriesView>
										</viewserializable>
									</cc1:Series>
								</seriesserializable>
							</dxchartsui:WebChartControl>
						</div>
					</td>
				</tr>
			</tbody>
		</table>

	</form>
</body>
</html>
