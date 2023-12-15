$(function () {

    $("#Roles").select2();
    $("#Roles").change(function () {
        $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
        $.get("/Settings/GetUsers", { "RoleId": $(this).val() }, function (JsonResult) {
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
                $('#JsonTable').on("click", ".edit",function () {
                    window.location.href = "/Settings/FormEditUser?UserId=" + $(this).attr("data-val");
                });

                // delete
                $('#JsonTable').on("click", ".delete",function () {
                    const UserId = $(this).attr("data-val");
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
                                $.get("/Settings/DeleteUser", { "UserId": UserId }, function (rs) {
                                    if (rs.valid == true) {
                                        toastr.success(rs.message);
                                        setTimeout(function () {
                                            window.location.href = "/Settings/UserIndex";
                                        }, 700);
                                    }
                                    else {
                                        toastr.error('Error!' + rs.message);
                                    }
                                });
                            }
                        });
                });

            }, 200);
        });
    });
    $("#Roles").val($("#Roles").val()).change();

});