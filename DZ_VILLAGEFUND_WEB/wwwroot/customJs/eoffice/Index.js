

/*
 *  get data 
 */

$(function () {
    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/EOffice/GetData", { "": 11 }, function () {

    });
});