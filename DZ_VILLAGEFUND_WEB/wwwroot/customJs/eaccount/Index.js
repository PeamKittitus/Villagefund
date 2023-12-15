/*
 * load data 
 * init data table
 */

$(function () {

    // init select 2
    $("#CurrentBudgetYear").select2();
    $("#TransactionType").select2();
    $("#AccountBudgetId").select2();

    // Create a URLSearchParams object
    var searchParams = new URLSearchParams(new URL(window.location.href).search);

    // Get the value of the "BudgetYear" parameter
    var budgetYear = searchParams.get("BudgetYear");

    // Select the option in the dropdown based on the value from the query parameter
    $("#CurrentBudgetYear").val(budgetYear);

    $("#CurrentBudgetYear").change(function () {
        // Get the selected value
        var selectedYear = $(this).val();

        // Check if the selected value is null
        if (selectedYear !== null) {
            // Construct the new URL with query parameters
            var newUrl = updateQueryStringParameter(window.location.href, "BudgetYear", selectedYear);

            // Check if the new URL is different from the current URL
            if (newUrl !== window.location.href) {
                // Replace the current URL with the new URL
                window.location.replace(newUrl);
            }
        }
    });

    fectCurrentYear(budgetYear);

    $("#AccountBudgetId, #TransactionType").change(function () {

        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/EAccount/GetTransactions", { "BudgetYear": budgetYear, "AccountBudgetId": $("#AccountBudgetId").val(), "TransactionType": $("#TransactionType").val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 50,
                    responsive: true,
                    lengthChange: false
                });

            /* edit data */
            $("#JsonData").on("click", ".edit", function () {
                window.location.href = "/EAccount/FormEdittransaction?TransactionAccBudgetId=" + $(this).attr("data-val");
            });

            // view document
            $("#JsonData").on("click", '.viewDocument', function () {
                $.get("/EAccount/ViewDocuments", { "TransactionAccBudgetId": $(this).attr("data-id") }, function (JsonResult) {
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
                            $.get("/EAccount/DeleteTransaction", { "TransactionAccBudgetId": Id }, function (rs) {
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

    $("#AccountBudgetId").val($("#AccountBudgetId").val()).change();


    //Import Excel
    $("#ImportExcel").on("click", function () {
        $.get("/EAccount/FormImportExcelActTransaction", function (FormResult) {
            $("#JsonImportForm").html(FormResult);
            $("#FormImport").modal("show");

            $("#Submit").click(function () {
                let validate = false;
                if ($('#FileUpload').get(0).files.length != 0) {
                    validate = true;
                }
                else {
                    $("#FormImport").modal("hide");
                    Swal.fire(
                        {
                            title: "ไม่มีไฟล์",
                            text: "กรุณาแนบไฟล์",
                            type: "error",
                            confirmButtonText: "เข้าใจแล้ว",
                            closeOnConfirm: false
                        });
                }
                if (validate) {
                    let Data = new FormData($("#JsonForm")[0]); JsonForm
                    $.ajax(
                        {
                            type: "POST",
                            url: "/EAccount/ImportExcelActTransaction",
                            contentType: false,
                            processData: false,
                            data: Data,
                            success: function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                                else {
                                    toastr.error('Error! ' + result.message);
                                    setTimeout(function () {

                                    }, 700);
                                }
                            }
                        });
                }
            });

        });
    });

})

// Function to update query parameters in the URL
function updateQueryStringParameter(uri, key, value) {
    var re = new RegExp("([?&])" + key + "=.*?(&|$)", "i");
    var separator = uri.indexOf('?') !== -1 ? "&" : "?";

    if (uri.match(re)) {
        return uri.replace(re, '$1' + key + "=" + value + '$2');
    } else {
        return uri + separator + key + "=" + value;
    }
}