$(function () {

    $("#TransactionType").select2();
    $("#TransactionType").change(function () {
        $("#JsonData").html('<img src="/img/loading.gif" width="200">');

        if ($("#TransactionType").val() === "2") {
            $.get("/Members/GetViewsMember", { "VillageId": $("#VillageId").val() }, function (JsonResult) {
                setTimeout(function () {
                    $("#JsonData").html(JsonResult);
                    $('#JsonTable').dataTable(
                        {
                            responsive: true,
                            lengthChange: false,
                            dom: "<'row mb-3'<'col-sm-12 col-md-6 d-flex align-items-center justify-content-start'f><'col-sm-12 col-md-6 d-flex align-items-center justify-content-end'lB>>" +
                                "<'row'<'col-sm-12'tr>>" +
                                "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                            buttons: [
                                {
                                    extend: 'print',
                                    text: 'สั่งพิมพ์',
                                    titleAttr: 'Print Table',
                                    className: 'btn-outline-primary btn-sm'
                                }
                            ]
                        });

                    $(".renew").on("click", function () {
                        const MemberId = $(this).attr('data-val');
                        $.ajax({
                            url: '/Members/MemberRenewal',
                            type: 'PUT',
                            data: { 'MemberId': MemberId },
                            success: function (response) {
                                if (response.valid == true) {
                                    toastr.success(response.message);
                                    setTimeout(function () {
                                        window.location.href = "";
                                    }, 700);
                                }
                                else {
                                    toastr.error('แจ้งเตือน! ' + response.message);
                                }
                            }
                        });
                    });

                }, 200);
                return null;
            });
        }

        $.get("/Villages/GetRegisterVillages", { "TransactionType": $(this).val() }, function (JsonResult) {
            setTimeout(function () {

                $("#JsonData").html(JsonResult);
                $('#JsonTable').dataTable(
                    {
                        responsive: true,
                        lengthChange: false,
                        dom: "<'row mb-3'<'col-sm-12 col-md-6 d-flex align-items-center justify-content-start'f><'col-sm-12 col-md-6 d-flex align-items-center justify-content-end'lB>>" +
                            "<'row'<'col-sm-12'tr>>" +
                            "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
                        buttons: [
                            {
                                extend: 'print',
                                text: 'สั่งพิมพ์',
                                titleAttr: 'Print Table',
                                className: 'btn-outline-primary btn-sm'
                            }
                        ]
                    });

            }, 200);
        });
    });
    $("#TransactionType").val($("#TransactionType").val()).change();

    //Import Excel
    $("#ImportExcel").on("click", function () {
        $.get("/Villages/FormImportEregister", function (FormResult) {
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
                            url: "/Villages/ImportEregister",
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
        window.location.href = "/Villages/ExportEregister";
    });

});
