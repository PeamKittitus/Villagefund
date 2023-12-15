
/*
 * init data
 */

$(function () {

    // leftSide
    $("#leftSide").html('<img  src="/img/loading.gif" width="200">');
    $.get("/Home/leftSide", function (JsonLeft) {
        setTimeout(function () {
            $("#leftSide").html(JsonLeft);
        }, 100);
    });
});