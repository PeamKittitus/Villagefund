$(document).ready(function () {

    // get data 
    $.get("/Settings/GetMenus", function (respond) {
        $("#ShowMenuItem").html(respond);

        // edit
        $("#ShowMenuItem").on("click", ".edit", function () {
            window.location.href = "/Settings/FormEditMenu?Id=" + $(this).val();
        });

        // delete
        $("#ShowMenuItem").on("click", ".delete", function () {
            const Id = $(this).val();
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
                        $.get("/Settings/DeleteMenu", { "Id": Id }, function (rs) {
                            if (rs.valid == true) {
                                toastr.success(rs.message);
                                setTimeout(function () {
                                    window.location.href = "/Settings/MenusIndex";
                                }, 1000);
                            }
                            else {
                                toastr.error('Error!' + rs.message);
                            }
                        });
                    }
                });
        });
       
        /* update position */
        var updateOutput = function (e) {
            var list = e.length ? e : $(e.target), output = list.data('output');
            if (window.JSON) {
                $(".dd").change(function () {
                    $.ajax({
                        dataType: 'JSON',
                        type: 'POST',
                        url: "/Settings/UpdatePosition",
                        data: "Position=" + JSON.stringify(list.nestable('serialize')),
                        success: function (result) {
                            if (result.valid == true) {
                                toastr.success(result.message);
                                setTimeout(function () {
                                    window.location.href = "";
                                }, 700);
                            }
                            else {
                                toastr.error('Error!' + result.message);
                            }
                        }
                    });
                });

            } else {
                output.val('JSON browser support required for this demo.');
            }
        };

        $('#nestable3').nestable({
            group: 1
        }).on('change', updateOutput);

        updateOutput($('#nestable3').data('output', $('#nestable3-output')));

        $('#nestable3').nestable();

    });
});