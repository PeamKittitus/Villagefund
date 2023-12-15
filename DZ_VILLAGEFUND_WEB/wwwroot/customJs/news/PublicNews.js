$(function () {
    $("#JsonData").html('<img src="/img/loading.gif" width="200">');
    $.get("/NewsLatter/GetPublicNews", function (JsonResult) {
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

            // approve
            $('#JsonTable').on("click", ".approve", function () {
                const NewsId = $(this).attr("data-val");
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
                            $.get("/NewsLatter/ApproveNews", { "NewsId": NewsId }, function (Result) {
                                if (Result.valid == true) {
                                    toastr.success(Result.message);
                                    setTimeout(function () {
                                        window.location.href = "/NewsLatter/PublicNews";
                                    }, 700);
                                }
                                else {
                                    toastr.error('Error!' + Result.message);
                                }
                            });
                        }
                    });
               
            });

            // not approve
            $('#JsonTable').on("click", ".notapprove", function () {
                const NewsId = $(this).attr("data-val");
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
                            $.get("/NewsLatter/ApproveNews", { "NewsId": NewsId }, function (Result) {
                                if (Result.valid == true) {
                                    toastr.success(Result.message);
                                    setTimeout(function () {
                                        window.location.href = "/NewsLatter/PublicNews";
                                    }, 700);
                                }
                                else {
                                    toastr.error('Error!' + Result.message);
                                }
                            });
                        }
                    });
            });

        }, 200);
    });

});