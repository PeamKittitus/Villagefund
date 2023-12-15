
/*
 * load data 
 */

$(function () {

    $("#CurrentBudgetYear").select2();
    $("#Month").select2();
    var Selected = $("#CurrentBudgetYear, #Month");
    Selected.change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/ElectronicProjects/GetApproves", { "BudgetYear": $("#CurrentBudgetYear").val(), "Month": $("#Month").val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult).promise().done(function () {

                // chaeck all
                $("#JsonData").on("change", "#all", function () {
                    $(".ProjectId").prop('checked', $(this).prop("checked"));
                });

                //Approve
                $("#JsonData").on("click", "#Approves", function () {
                    let checkboxes = new Array();
                    $('input[name="ProjectId"]:checked').each(function () {
                        checkboxes.push($(this).val());
                    });

                    $.ajax({
                        type: "PUT",
                        url: "/ElectronicProjects/UpdateProjectStatusRangeApproves",
                        processData: false,
                        contentType: 'application/json',
                        data: JSON.stringify(checkboxes),
                        dataType: 'json',
                        success: function (result) {
                            if (result.valid == true) {
                                toastr.success(result.message);
                                setTimeout(function () {
                                    window.location.href = "";
                                }, 700);
                            }
                            else if (result.warning == true) {
                                toastr.warning('Warning ' + result.message);
                            }
                            else {
                                toastr.error('Error! ' + result.message);
                            }
                        }
                    }).delay(800);

                });

                // Initialize DataTables for the updated content
                $('#JsonTable').DataTable({
                    order: [[0, 'desc']],
                    "ordering": false,
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                });

            });


        });
    });
    $("#CurrentBudgetYear").val($("#CurrentBudgetYear").val()).change();

});