
/*
 * create by bunchuai
 */


$(function () {

    $("#BudgetYear").select2();
    $("#ProjectId").select2();
    $("#VillageId").select2();
    var Select = $("#BudgetYear, #ProjectId, #VillageId");
    Select.change(function () {
        $("#JsonData").html('<img src="/img/loading.gif" width="200">');
        $.get("/ElectronicProjects/GetMileStoneIndex",
            {
                "ProjectId": $("#ProjectId").val(),
                "BudgetYear": $("#BudgetYear").val(),
                "VillageId": $("#VillageId").val()
            }, function (JsonData) {

                $("#JsonData").html(JsonData);
                $('#JsonTable').dataTable(
                    {
                        "pageLength": 10,
                        responsive: true,
                        lengthChange: false
                    });

                // check all
                $("#JsonTable").on("click", "#check-all", function () {
                    $(".check-item").prop('checked', $(this).prop("checked"));
                });
            });
    });
    $("#BudgetYear").val($("#BudgetYear").val()).change();

    // filter project
    $("#VillageId").change(function () {
        $.get("/ElectronicProjects/GetProjectByBudget", { "BudgetYear": $("#BudgetYear").val(), "VillageId": $("#VillageId").val() }, function (Result) {
            $("#ProjectId").empty();
            $.each(Result, function (i, val) {
                if ($("#ProjectId").val() == val.value) {
                    $("#ProjectId").append($("<option selected></option>").attr("value", val.value).text(val.text));
                } else {
                    $("#ProjectId").append($("<option></option>").attr("value", val.value).text(val.text));
                }
            });
            $("#ProjectId").select2();
        });
    });
    $("#VillageId").val($("#VillageId").val()).change();

    $("#SetMileStone").click(function () {
        var ItemChecked = CheckValue();
        if (ItemChecked == false) {
            toastr.error('แจ้งเตือน! กรุณาเลือกกิจกรรม');
        }
        else {
            window.location.href = "/ElectronicProjects/ViewMileStone?ActivityId=" + ItemChecked;
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