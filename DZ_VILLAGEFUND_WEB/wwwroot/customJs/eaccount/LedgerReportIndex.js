/*
 * load data 
 * init data table
 */

$(function () {

    // init select 2
    $("#CurrentBudgetYear").select2();
    $("#AccountBudgetId").select2();
    $("#AccChartId").select2();
    $("#Group,#Department").select2();
    $(".ActionReport").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/EAccount/GetLedgerReport", { "BudgetYear": $("#CurrentBudgetYear").val(), "AccountBudgetId": $("#AccountBudgetId").val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                }
            );

        });
    });
    $("#CurrentBudgetYear").val($("#CurrentBudgetYear").val()).change();
    $("#AccountBudgetId").val($("#AccountBudgetId").val()).change();
    $("#AccChartId").val($("#AccChartId").val()).change();

    //Filter กลุ่ม ฝ่าย
    $("#Group,#Department").change(function () {
        let JsonTable = $('#JsonTable').DataTable();
        JsonTable.search(this.value).draw();  
    });

})