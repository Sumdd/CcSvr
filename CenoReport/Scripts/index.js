$(function ()
{
    //日历
    $("#datepicker").datepicker();
    //左边菜单
    $('.showStart').show(600);
    $('.one').click(function ()
    {
        $('.one').removeClass('one-hover');
        $(this).addClass('one-hover');
        $(this).parent().parent().find('.kid').hide(600);
        $(this).parent().parent().find('b').attr('class', 'tip');
        $(this).parent().find('.kid').show(600);
        $(this).parent().find('b').attr('class', 'tip_out');
    });
    //影藏菜单
    var l = $('.left_c');
    var r = $('.right_c');
    var c = $('.Conframe');
    $('.nav-tip').click(function ()
    {
        if (l.css('left') == '8px') {
            l.animate({
                left: -300
            }, 500);
            r.animate({
                left: 21
            }, 500);
            c.animate({
                left: 29
            }, 500);
            $(this).animate({
                "background-position-x": "-12"
            }, 300);
        } else {
            l.animate({
                left: 8
            }, 500);
            r.animate({
                left: 190
            }, 500);
            c.animate({
                left: 198
            }, 500);
            $(this).animate({
                "background-position-x": "0"
            }, 300);
        };
    })
    //横向菜单
    $('.top-menu-nav li').click(function ()
    {
        $('.kidc').hide();
        $(this).find('.kidc').show();

    })
    $('.kidc').bind('mouseleave', function ()
    {
        $('.kidc').hide();
    })

})