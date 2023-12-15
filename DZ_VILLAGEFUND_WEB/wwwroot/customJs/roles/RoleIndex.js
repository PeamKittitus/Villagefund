$(function () {

    $("#JsonData").html('<img  src="/img/loading.gif" width="200">');
    $.get("/Settings/GetRoles", function (JsonResult) {
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
                window.location.href = "/Settings/FormEditRole?Id=" + $(this).attr("data-val");
            });

            // delete
            $('#JsonTable .delete').unbind().click(function () {
                const RoleId = $(this).attr("data-val");
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
                            $.get("/Settings/DeleteRole", { "Id": RoleId }, function (rs) {
                                if (rs.valid == true) {
                                    toastr.success(rs.message);
                                    setTimeout(function () {
                                        window.location.href = "/Settings/RoleIndex";
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

            $("#JsonTable .setPermission").unbind().click(function (e, state) {
                $.get('/Settings/SetPermission', { "Active": e.target.checked, "RoleId": $(this).attr("data-role"), "IsAction": $(this).attr("data-is") }, function (result) {
                    if (result.valid == true) {
                        toastr.success(result.message);
                        setTimeout(function () {
                           
                        }, 700);
                    } else {
                        toastr.error('Error!' + result.message);
                        setTimeout(function () {
                            window.location.href = "";
                        }, 700);
                    }
                });
            });
        }, 200);
    });

});