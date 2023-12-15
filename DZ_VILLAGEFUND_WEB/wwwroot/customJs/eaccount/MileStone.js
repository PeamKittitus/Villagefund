
/*
 * create by bunchuai
 */


$(function () {

    $("#CurrentBudgetYear").select2();
    $("#AccountBudgetId").select2();
    $("#AccChartId").select2();
    $(".ActionReport").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/EAccount/GetMileStoneIndex", { "BudgetYear": $("#CurrentBudgetYear").val(), "AccountBudgetId": $("#AccountBudgetId").val(), "AccChartId": $("#AccChartId").val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                });

            // check all
            $("#JsonTable").on("click", "#check-all", function () {
                $(".check-item").prop('checked', $(this).prop("checked"));
            });
        });
    });
    $("#CurrentBudgetYear").val($("#CurrentBudgetYear").val()).change();
    $("#AccountBudgetId").val($("#AccountBudgetId").val()).change();
    $("#AccChartId").val($("#AccChartId").val()).change();

    $("#SetMileStone").click(function () {
        var ItemChecked = CheckValue();
        if (ItemChecked == false) {
            toastr.error('แจ้งเตือน! กรุณาเลือกรายการ');
        }
        else {
            window.location.href = "/EAccount/ViewMileStone?Id=" + ItemChecked + "&BudgetYear=" + $("#CurrentBudgetYear").val();
        }
    });
});


function CheckValue() {
    var ItemCheckboxes = new Array();
    $(".check-item:input:checked").each(function () {
        ItemCheckboxes.push($(this).val());
    });

    if (ItemCheckboxes == "") {
        return false;
    }
    else {
        return ItemCheckboxes;
    }
}