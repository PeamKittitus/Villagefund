$(function () {

    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/Villages/GetVillagesApproved", function (JsonResult) {
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

            // edit
            $('#JsonTable .edit').unbind().click(function () {
                window.location.href = "/Villages/FormAddVillage";
            });

        }, 200);
    });

    // Terminate
    $('#terminate').unbind().click(function () {
        Swal.fire(
            {
                title: "ยืนยันการทำรายการ",
                text: "คุณต้องการยกเลิกบัญชีนี้ หรือไม่?",
                type: "warning",
                showCancelButton: true,
                confirmButtonText: "ยืนยัน",
                cancelButtonText: "ยกเลิก",
                closeOnConfirm: false
            }).then(function (isConfirm) {
                if (isConfirm.value == true) {
                    $.post("/Villages/TerminateVillage", function (rs) {
                        if (rs.valid == true) {
                            toastr.success(rs.message);
                            setTimeout(function () {
                                window.location.href = "";
                            }, 1000);
                        }
                        else {
                            toastr.error('Error!' + rs.message);
                            setTimeout(function () {
                                window.location.href = "";
                            }, 1000);
                        }
                    });
                }
            });
    });

});