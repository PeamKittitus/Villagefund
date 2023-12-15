/*
 * load data 
 * init data table
 */

$(function () {

    // init select 2
    $("#CurrentBudgetYear").select2();
    $("#ProjectId").select2();
    $("#CurrentBudgetYear, #ProjectId").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/EAccount/GetActTransactions", { "BudgetYear": $("#CurrentBudgetYear").val(), "ProjectId": $("#ProjectId").val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                });

            /* edit data */
            $("#JsonData").on("click", ".edit", function () {
                if ($(this).attr("data-type") == "True") {
                    window.location.href = "/EAccount/FormEditActTransaction?AccActivityId=" + $(this).attr("data-val");
                } else {
                    window.location.href = "/EAccount/FormEditActTransactionPay?AccActivityId=" + $(this).attr("data-val");
                }

            });

            // view document
            $("#JsonData").on("click", '.viewDocument', function () {
                $.get("/EAccount/ViewActDocuments", { "AccActivityId": $(this).attr("data-id") }, function (JsonResult) {
                    $("#JsonViewData").html(JsonResult);
                    $("#ViewDataModal").modal('show');


                    $("#ViewDataModal").on("change", ".IsApprove", function () {
                        $("#ViewDataModal").modal('hide');
                        var TransactionFileBudgetId = $(this).attr("data-val");
                        Swal.fire(
                            {
                                title: "ยืนยันการทำรายการ",
                                text: "คุณต้องการทำรายการนี้ หรือไม่?",
                                type: "warning",
                                showCancelButton: true,
                                confirmButtonText: "ยืนยัน",
                                cancelButtonText: "ยกเลิก",
                                closeOnConfirm: false
                            }).then(function (isConfirm) {
                                if (isConfirm.value == true) {
                                    $.get("/EAccount/UpdateApproveFile", { "TransactionFileBudgetId": TransactionFileBudgetId }, function (result) {
                                        if (result.valid == true) {
                                            toastr.success(result.message);
                                            setTimeout(function () {
                                                window.location.href = "/EAccount/Index";
                                            }, 700);
                                        }
                                        else {
                                            toastr.error('Error!' + result.message);
                                            setTimeout(function () {

                                            }, 700);
                                        }
                                    });
                                }
                            });

                    });

                });
            });

            /* delete data */
            $("#JsonData").on("click", ".delete", function () {
                const Id = $(this).attr("data-val");
                Swal.fire(
                    {
                        title: "ยืนยันการทำรายการ",
                        text: "คุณต้องการลบรายการนี้ หรือไม่?",
                        type: "warning",
                        showCancelButton: true,
                        confirmButtonText: "ยืนยัน",
                        cancelButtonText: "ยกเลิก",
                        closeOnConfirm: false
                    }).then(function (isConfirm) {
                        if (isConfirm.value == true) {
                            $.get("/EAccount/DeleteActTransaction", { "TransactionAccActivityId": Id }, function (rs) {
                                if (rs.valid == true) {
                                    toastr.success(rs.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                                else {
                                    toastr.error('แจ้งเตือน! ' + rs.message);
                                }
                            });
                        }
                    });
            });

        });
    });
    $("#ProjectId").val($("#ProjectId").val()).change();
})