
/*
 *  ChartAccountIndex
 */

$(function () {
    $("#BudgetYear").select2();
    $("#IsChartCenter,#BudgetYear").on("change", function () {
        let IsChartCenter = false;
        $.get("/EAccount/GetChartAccount", { "IsChartCenter": IsChartCenter, "BudgetYear": $("#BudgetYear").val() }, function (JsonreSult) {
            $("#JsonData").html(JsonreSult);
            $('#JsonTable').dataTable(
                {
                    "pageLength": 100,
                    responsive: true,
                    lengthChange: false,
                    dom:
                        "<'row mb-3'<'col-sm-12 col-md-6 d-flex align-items-center justify-content-start'f><'col-sm-12 col-md-6 d-flex align-items-center justify-content-end'B>>" +
                        "<'row'<'col-sm-12'tr>>" +
                        "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                    lengthChange: false,
                    buttons: [
                        {
                            extend: 'excelHtml5',
                            text: '<i class="fal fa-file-excel"></i>&nbsp;&nbsp;EXCEL',
                            titleAttr: 'Excel',
                            className: 'btn-success btn-sm mr-1 hidden-btn exportExcel',
                            exportOptions: {
                                columns: [0, 1, 2] // Export only the first three columns
                            },
                            visible: false
                        }
                    ],
                    "ordering": false
                });

            // add child
            $('#JsonTable').on("click", ".addParent", function () {
                $.get("/EAccount/FromAddChartAccount", { "FormType": false, "AccountChartId": $(this).attr("data-val"), "IsChartCenter": IsChartCenter }, function (JsonForm) {
                    $("#JsonCreateData").html(JsonForm);
                    $("#ViewCreateModal").modal("show");

                    // submit form
                    $('#JsonForm').bootstrapValidator();
                    $("#Submit").click(function () {
                        $('#JsonForm').data("bootstrapValidator").validate();
                        if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                            var Data = new FormData($("#JsonForm")[0]);
                            $.ajax(
                                {
                                    type: "POST",
                                    url: "/EAccount/FromAddChartAccount",
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
                                        }
                                    }
                                });
                        }
                    });
                });
            });

            // update
            $('#JsonTable').on("click", ".edit", function () {
                $.get("/EAccount/FromEditChartAccount", { "FormType": $(this).attr("data-type"), "AccountChartId": $(this).attr("data-val"), "AccountParentId": $(this).attr("data-val-parent") }, function (JsonForm) {
                    $("#JsonCreateData").html(JsonForm);
                    $("#ViewCreateModal").modal("show");

                    // submit form
                    $('#JsonForm').bootstrapValidator();
                    $("#Submit").click(function () {
                        $('#JsonForm').data("bootstrapValidator").validate();
                        if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                            var Data = new FormData($("#JsonForm")[0]);
                            $.ajax(
                                {
                                    type: "POST",
                                    url: "/EAccount/FromEditChartAccount",
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
                                        }
                                    }
                                });
                        }
                    });
                });
            });

            // delete
            $('#JsonTable').on("click", ".delete", function () {
                const AccountChartId = $(this).attr("data-val");
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
                            $.get("/EAccount/DeleteChartAccount", { "AccountChartId": AccountChartId }, function (rs) {
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
                        } else {
                            window.location.href = "";
                        }
                    });
            });

            //Import Excel
            $("#ImportExcel").on("click", function () {
                $.get("/EAccount/FormImportChartAccount", function (FormResult) {
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
                                    url: "/EAccount/ImportChartAccount",
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

            //Export Excel
            $("#ExportExcel").on("click", function () {
                $(".exportExcel").click();
            });

        });
    });
    $("#BudgetYear").val($("#BudgetYear").val()).change();

    // create
    $("#Create").click(function () {
        $.get("/EAccount/FromAddChartAccount", { "FormType": true }, function (JsonForm) {
            $("#JsonCreateData").html(JsonForm);
            $("#ViewCreateModal").modal("show");

            // submit form
            $('#JsonForm').bootstrapValidator();
            $("#Submit").click(function () {
                $('#JsonForm').data("bootstrapValidator").validate();
                if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                    var Data = new FormData($("#JsonForm")[0]);
                    $.ajax(
                        {
                            type: "POST",
                            url: "/EAccount/FromAddChartAccount",
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
                                }
                            }
                        });
                }
            });
        });
    });
});