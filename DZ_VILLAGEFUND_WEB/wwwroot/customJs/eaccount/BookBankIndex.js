/*
 * load data
 * init data table
 */

$(function () {

    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/EAccount/GetBookBanks", function (JsonResult) {
        $("#JsonData").html(JsonResult);
        $('#JsonTable').dataTable(
            {
                "pageLength": 50,
                responsive: true,
                lengthChange: false
            });

        // view document
        $("#JsonData").on("click", '.viewDocument', function () {
            $.get("/EAccount/ViewBookbankDocuments", { "BookbankId": $(this).attr("data-id") }, function (JsonResult) {
                $("#JsonViewData").html(JsonResult);
                $("#ViewDataModal").modal('show');
            });
        });

        /* edit data */
        $("#JsonData").on("click", ".edit", function () {
            const Id = $(this).attr("data-val");
            $.get("/EAccount/FormEditBookBank", { "Id": Id }, function (FormResult) {
                $("#JsonEditForm").html(FormResult);
                $("#FormEditModal").modal("show");

                $("#FormEditModal #BankCode").select2({
                    dropdownParent: $("#FormEditModal")
                });
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
                                url: "/EAccount/FormEditBookBank",
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
                        $.get("/EAccount/DeleteBookBank", { "Id": Id }, function (rs) {
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
    });

    /* form add */
    $("#AddForm").click(function () {
        $.get("/EAccount/FormAddBookBank", function (FormResult) {
            $("#JsonCreateForm").html(FormResult);
            $("#FormCreateModal").modal("show");

            $("#BankCode").select2({
                dropdownParent: $("#FormCreateModal")
            });
            $('#Amount').autoNumeric('init', { aSign: '' });

            $('#JsonForm').bootstrapValidator();
            $("#Submit").click(function () {
                $('#JsonForm').data("bootstrapValidator").validate();
                if ($('#JsonForm').data("bootstrapValidator").isValid() == true) {
                    $('#Amount').autoNumeric('init', { aSep: '.', aDec: ',', mDec: '0' });
                    var Data = new FormData($("#JsonForm")[0]);

                    let combinedText = '';
                    let count = 0;
                    $('.withdraw-office-name').each(function () {
                        if (count == 5) {
                            return false;
                        }
                        combinedText += $(this).val() + ', ';
                        count++;
                    });
                    // Remove the trailing comma and space
                    combinedText = combinedText.slice(0, -2);

                    Data.set("WithdrawOfficeName", combinedText);

                    $.ajax(
                        {
                            type: "POST",
                            url: "/EAccount/FormAddBookBank",
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


