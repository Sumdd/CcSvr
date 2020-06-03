$(function ()
{
    InitUserAgent();
    $(this).find('td').attr('style', 'text-align: center;');

    //折叠
    $('.list').find('thead').find('.tabtr').live('click', function ()
    {
        var i = $(this).find('i');
        if (i.attr('class') == 'tip-down') { i.attr('class', 'tip-up') } else { i.attr('class', 'tip-down') }
        $(this).parent().parent().parent().find('tbody').toggle();
        $(this).parent().parent().parent().find('tfoot').toggle();
    })


    //添加
    $('.add').live('click', function ()
    {
        var _html = '<tr>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1234" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="waihuyizu" /></td>' +
                '<td style="text-align: center;"><button class="btn btn-mini btn-danger edit">保存</button> ' +
                '<button class="btn btn-mini btn-danger del">删除</button></td>' +
                 '</tr>';
        $(this).parents('.list').find('tbody').append(_html);
    })
    //删除
    $('.del').live('click', function ()
    {
        var _tr = $(this).parents().parent('tr');
        // alert(_tr.attr('class'))
        if (_tr.attr('class') != "demo") {
            $('body').alert({
                type: 'warning',
                content: '你确定删除此用户吗？操作无法恢复',
                buttons: [{
                    id: 'yes',
                    name: '确定',
                    callback: function ()
                    {
                        _tr.remove();
                    }
                }, {
                    id: 'no',
                    name: '取消'
                }]
            })
        }
    })
    //编辑
    $('.edit').live('click', function ()
    {
        if ($(this).text().trim() == '编辑') {
            var _tr = $(this).parents().parent('tr');
            _tr.find('input').removeAttr('readonly');
            _tr.find('select').removeAttr('disabled');
            $(this).text('保存');
        }
        else {
            var _tr = $(this).parents().parent('tr');
            _tr.find('input').attr('readonly', 'readonly');
            _tr.find('select').attr('disabled', 'disabled');
            $(this).text('编辑');
        }
    })

    //删除组
    $('#DelGroup').live('click', function ()
    {
        var delcon = $(this);
        $('body').alert({
            type: 'warning',
            content: '你确定删除此组及组内所有用户吗？',
            buttons: [{
                id: 'yes',
                name: '确定',
                callback: function ()
                {
                    delcon.parent().parent().parent().parent().remove();
                }
            }, {
                id: 'no',
                name: '取消'
            }]
        })
    })

    //批量添加用户
    $("#AddCount").live('focus', function ()
    {
        $(this).attr('value', '');
        $(this).attr('style', 'width: 80px; color:#000;');
    });
    $('#AddCount').live('blur', function ()
    {
        if ($(this).attr('value') == '') {
            $(this).attr('value', '输入添加个数');
            $(this).attr('style', 'width: 80px; color:#CCC;');
        }
    })
    $("#AddMore").live('click', function ()
    {
        var AddCount = parseInt($(this).parent().find('#AddCount').attr('value'));
        if (AddCount >= 1 && AddCount <= 20) {
            for (var i = 0; i < AddCount; i++) {
                var _html = '<tr>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1234" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="waihuyizu" /></td>' +
                '<td style="text-align: center;"><button class="btn btn-mini btn-danger edit">保存</button> ' +
                '<button class="btn btn-mini btn-danger del">删除</button></td>' +
                 '</tr>';
                $(this).parents('.list').find('tbody').append(_html);
            }
        }
        else {
            alert('请输入1-20数字');
            $('#AddCount').attr('value', '');
        }
    })
    //组名
    $("#GroupName").focus(function ()
    {
        $(this).attr('value', '');
        $(this).attr('style', 'width: 80px; color: #000');
    });
    $('#GroupName').blur(function ()
    {
        if ($(this).attr('value') == '') {
            $(this).attr('value', '请输入组名');
            $(this).attr('style', 'width: 80px; color: #CCC');
        }
    })
    $('#AddGroup').click(function ()
    {
        var grname = $('#GroupName').attr('value');
        if (grname == '请输入组名') {
            alert('请输入组名');
        }
        else {
            var _html = '<table id="list" class="table table-striped table-bordered table-condensed list">' +
                '<thead><tr style="cursor: pointer;"><td colspan="9" class="tabtr" style="text-align: center;">' + grname + '<i class="tip-up"></i></td><td><button id="DelGroup" class="btn btn-mini btn-danger">删除该组</button></td></tr></thead>' +
                '<tbody><tr><td>帐号</td><td>密码</td><td>级别</td><td>拨号计划</td><td>实际名字</td><td>实际号码</td><td>外拨名字</td><td>外拨号码</td><td>组名</td><td>&nbsp;操作</td></tr>' +
                '<tr>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1234" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">domestic</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">default</option><option value="2">international</option><option value="3">local</option></select></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><input type="text" style="width: 130px" value="1000" /></td>' +
                '<td style="text-align: center;"><select style="width: 130px"><option value="1">外呼1组</option><option value="2">外呼2组</option><option value="3">外呼3组</option></select></td>' +
                '<td style="text-align: center;"><button class="btn btn-mini btn-danger edit">保存</button> ' +
                '<button class="btn btn-mini btn-danger del">删除</button></td>' +
                '</tr></tbody>' +
                '<tfoot><tr><td colspan="5"><a class="btn btn-mini btn-primary add" style="color: #FFF;">添加</a></td>' +
                '<td colspan="5"><input id="AddCount" type="text" style="width: 80px; color: #CCC" value="输入添加个数" /><a id="AddMore" class="btn btn-mini btn-primary" style="color: #FFF;">批量添加</a></td></tr>' +
                '</tfoot></table>';
            $('body').append(_html);
            $('body').find('td').attr('style', 'text-align: center;');
        }
    })
})
