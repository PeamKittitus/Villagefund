

/*
 * get data
 * init table 
 */


$(function () {
    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/ElectronicProjects/GetAssets", function (JsonResult) {
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



    // create new 
    $("#Create").click(function () {
        $.get("/ElectronicProjects/FormAddAsset", function (JsonForm) {
            $("#JsonCreateForm").html(JsonForm);
            $("#FormCreateModal").modal("show");

            $('#Amount').autoNumeric('init', { aSign: '' });

            $('#JsonForm').bootstrapValidator();
            $("#Submit").click(function () {
                $('#JsonForm').data("bootstrapValidator").validate();
                if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                    $('#Amount').autoNumeric('init', { aSep: '.', aDec: ',', mDec: '0' });
                    var Data = new FormData($("#JsonForm")[0]);
                    $.ajax(
                        {
                            type: "POST",
                            url: "/ElectronicProjects/FormAddAsset",
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
})