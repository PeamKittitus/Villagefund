
/*
 * load data
 * init data table
 */

$(function () {
    $("#CurrentBudgetYear").select2();
    $("#CurrentBudgetYear").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/EAccount/GetStructures", { "BudgetYear": $(this).val() }, function (JsonResult) {
            $("#JsonData").html(JsonResult);
            $('#JsonTable').dataTable(
                {
                    order: [[0, 'asc']],
                    "pageLength": 100,
                    responsive: true,
                    lengthChange: false
                });

            /* edit data */
            $("#JsonData").on("click", ".edit", function () {
                const Id = $(this).attr("data-val");
                const IsParent = $(this).attr("data-parent");

                window.location.href = "/EAccount/FormEditStructure?AccountBudgetd=" + Id + "&IsParent=" + IsParent;
            });

            /* delete data */
            $("#JsonData").on("click", ".delete", function () {
                const AccountBudgetd = $(this).attr("data-val");
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
                            $.get("/EAccount/DeleteStructure", { "AccountBudgetd": AccountBudgetd }, function (rs) {
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

            // add sub 
            $("#JsonData").on("click", ".addParent", function () {

                window.location.href = "/EAccount/FormAddSubStructure?AccountBudgetd=" + $(this).attr("data-val");

            });

            // Update status
            $("#JsonData").on("change", ".UpdateStatus", function () {
                var AccountBudgetd = $(this).attr("data-val");
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
                            $.get("/EAccount/UpdateStatusProject", { "AccountBudgetd": AccountBudgetd }, function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                                else {
                                    toastr.error('แจ้งเตือน! ' + result.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 1000);
                                }
                            });
                        }
                    });


            });

        });
    });
    $("#CurrentBudgetYear").val($("#CurrentBudgetYear").val()).change();

    /* form add */
    $("#AddForm").click(function () {
        window.location.href = "/EAccount/FormAddStructure";
    });

    //Export Excel
    $("#ExportExcel").on("click", function () {
        window.location.href = "/EAccount/ExportExcelStructureProject?BudgetYear=" + $("#CurrentBudgetYear").val();
    });

    //Import Excel
    $("#ImportExcel").on("click", function () {
        $.get("/EAccount/FormImportExcelStructureProject", function (FormResult) {
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
                            url: "/EAccount/ImportExcelStructureProject",
                            contentType: false,
                            processData: false,
                            data: Data,
                            success: function (result) {
                                if (result.valid == true) {
                                    toastr.success(result.message);
                                    setTimeout(function () {
                                        window.location.href = "/EAccount/StructureIndex";
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
});


