/*load xml document
this function only aply to mozilla firefox*/
loadXML = function (xmlFile)
{
    var xmlDoc = null;
    xmlDoc = document.implementation.createDocument("", "root", null);
    if (xmlDoc != null && xmlDoc != undefined) {
        try {
            xmlDoc.async = false;
            xmlDoc.load(xmlFile);
        }
        catch (e) {
            alert(e.Message);
        }
    }
    return xmlDoc;
}
//load user and group information
InitUserAgent = function ()
{
    // initialize group xml document
    var GroupDoc = loadXML('../SipAgent/Group/default.xml');

    //get group info
    var GroupDocEle = GroupDoc.getElementsByTagName('group');
    for (var i = 0; i < GroupDocEle.length; i++) {
        var TableName = GroupDocEle[i].getAttribute('name');
        if (TableName == 'default') {
            continue;
        }
        var TableHtml = '<table id="list" class="table table-striped table-bordered table-condensed list">' +
                '<thead><tr style="cursor: pointer;"><td colspan="9" class="tabtr" style="text-align: center;width: 1800px;">' + TableName + '<i class="tip-up"></i></td><td><button id="DelGroup" class="btn btn-mini btn-danger">删除该组</button></td></tr></thead>' +
                '<tbody><tr><td>帐号</td><td>密码</td><td>级别</td><td>拨号计划</td><td>实际名字</td><td>实际号码</td><td>外拨名字</td><td>外拨号码</td><td>组名</td><td>&nbsp;操作</td></tr>';

        var UserAgent = GroupDocEle[i].getElementsByTagName('user');
        for (var j = 0; j < UserAgent.length; j++) {
            var UserAgentDocName = UserAgent[j].getAttribute('id');
            var UserAgentDoc = loadXML('../SipAgent/UserAgent/' + UserAgent[j].getAttribute('id') + '.xml');
            var UserAgentPassword = UserAgentDoc.getElementsByTagName('param')[0].getAttribute('value');
            TableHtml += '<tr>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="' + UserAgentDocName + '" /></td>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="'+UserAgentPassword+'" /></td>' +
                '<td style="text-align: center;"><select disabled="disabled" style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><select disabled="disabled" style="width: 130px"><option value="1">default</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" readonly="readonly" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><select disabled="disabled" style="width: 130px"><option value="1">外呼1组</option><option value="2">外呼2组</option><option value="3">外呼3组</option></select></td>' +
                '<td style="text-align: center;"><button class="btn btn-mini btn-danger edit">编辑</button> ' +
                '<button class="btn btn-mini btn-danger del">删除</button></td></tr>';
        }
        TableHtml += '</tbody>' +
                '<tfoot><tr><td colspan="5"><a class="btn btn-mini btn-primary add" style="color: #FFF;">添加</a></td>' +
                '<td colspan="5"><input id="AddCount" type="text" style="width: 80px; color: #CCC" value="输入添加个数" /><a id="AddMore" class="btn btn-mini btn-primary" style="color: #FFF;">批量添加</a></td></tr>' +
                '</tfoot></table>';
        $('body').append(TableHtml);
        $('body').find('td').attr('style', 'text-align: center;');

    }
}